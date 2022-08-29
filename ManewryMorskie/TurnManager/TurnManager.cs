using CellLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Selectable = System.Collections.Generic.Dictionary<CellLib.CellLocation, (ManewryMorskie.TurnManagerComponents.MoveChecker? moveChecker, System.Collections.Generic.IList<ManewryMorskie.TurnManagerComponents.ICellAction> actions)>;

namespace ManewryMorskie.TurnManagerComponents
{

    public partial class TurnManager : IDisposable
    {
        private readonly StandardMap map;
        private readonly PlayerManager playerManager;
        private readonly InternationalWaterManager internationalWaterManager;
        private readonly ILogger? logger;
        private readonly SemaphoreSlim semaphore = new(0, 1);
        private readonly Selectable selectable = new();
        private readonly CellMarker marker;
        private readonly Move result = new();

        private CellLocation? selectedUnitLocation;
        private CancellationToken? cancellationToken;

        public bool ActionSelectionActive { get; set; } = true;

        private IUserInterface PlayerUi => playerManager.CurrentPlayer.UserInterface;

        public TurnManager(StandardMap map, PlayerManager playerManager, InternationalWaterManager internationalWaterManager, ILogger? logger = null)
        {
            this.map = map;
            this.playerManager = playerManager;
            this.internationalWaterManager = internationalWaterManager;
            this.logger = logger;
            this.marker = new CellMarker(this);
        }

#if DEBUG
        Stopwatch makeMoveWatch = new();
#endif
        public async Task<Move> MakeMove(CancellationToken token)
        {
#if DEBUG
            makeMoveWatch = new();
            makeMoveWatch.Start();
#endif
            selectable.Clear();
            result.Clear();
            result.CurrentPlayerColor = playerManager.CurrentPlayer.Color;
            cancellationToken = token;
            
            foreach (CellLocation unitLocation in map.LocationsWithPlayersUnits(playerManager.CurrentPlayer))
                selectable.Add(unitLocation, 
                    (new MoveChecker(map, playerManager, unitLocation, internationalWaterManager),
                    new List<ICellAction>()));
#if DEBUG
            logger?.LogInformation("MakeMove MoveCheckers added {ms}ms", makeMoveWatch.ElapsedMilliseconds);
#endif
            foreach (var item in selectable.Where(kpv => kpv.Value.moveChecker?.UnitIsSelectable() ?? false))
                item.Value.actions.Add(new SelectUnitAction(item.Key, this));
#if DEBUG
            logger?.LogInformation("MakeMove SelectCellAction added {ms}ms", makeMoveWatch.ElapsedMilliseconds);
#endif
            await PlayerUi.DisplayMessage("Wybierz jednostkę", MessageType.SideMessage);
            await marker.UpdateMarks();

            ActionSelectionActive = true;
            PlayerUi.ClickedLocation += SelectedLocation;
#if DEBUG
            makeMoveWatch.Stop();
            logger?.LogInformation("MakeMove waiting for release {ms}ms", makeMoveWatch.ElapsedMilliseconds);
#endif
            await semaphore.WaitAsync(token);
            token.ThrowIfCancellationRequested();

            marker.LastMove = new(result);
            await marker.ClearAndMarkLastMove(playerManager.UniqueInferfaces);

            await PlayerUi.DisplayMessage("Poczekaj aż przeciwnik wykona ruch", MessageType.SideMessage);
            return result;
        }

        private async void SelectedLocation(object sender, CellLocation e)
        {
            if (!ActionSelectionActive)
                return;

            if (selectable.TryGetValue(e, out var value))
            {
                //ActionSelectionActive = false;
                selectedUnitLocation = e;

                if (value.actions.Count == 1)
                {
                    await RealiseAction(value.actions.First());
                }
                else if (value.actions.Count > 1)
                {
                    //wyświetl listę i pozwól wybrać
                    await PlayerUi.DisplayContextOptionsMenu(e, value.actions.Select(o => o.Name).ToArray());

                    PlayerUi.ChoosenOptionId -= ChooseOption;
                    PlayerUi.ChoosenOptionId += ChooseOption;
                }
            }
        }

        private async void ChooseOption(object sender, int e)
        {
            if (selectable[selectedUnitLocation!.Value].actions.Count <= e)
                return;

            PlayerUi.ChoosenOptionId -= ChooseOption;
            
            await RealiseAction(selectable[selectedUnitLocation!.Value].actions[e]);
        }

        private async ValueTask RealiseAction(ICellAction action)
        {
#if DEBUG
            Stopwatch watch = new();
            watch.Start();
            logger?.LogInformation("Realising Action Started.");
#endif
            ActionSelectionActive = true;
            bool finishTurn = await action.Execute(result!, cancellationToken!.Value);
#if DEBUG
            watch.Stop();
            logger?.LogInformation("Realised Action \"{actionName}\" in {time}ms", action.Name, watch.ElapsedMilliseconds);
#endif

            if (finishTurn)
            {
                PlayerUi.ClickedLocation -= SelectedLocation;
                PlayerUi.ChoosenOptionId -= ChooseOption;
                selectedUnitLocation = null;
                result!.SourceUnitDescription = map[result.From].Unit!.ToString();
                semaphore.Release();
            }
            else
            {
                await marker.UpdateMarks();
            }
        }

        public void Dispose()
        {
            PlayerUi.ChoosenOptionId -= ChooseOption;
            PlayerUi.ClickedLocation -= SelectedLocation;
        }
    }
}

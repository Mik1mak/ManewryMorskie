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
        private readonly Selectable selectable = new();
        private readonly CellMarker marker;
        private readonly Move result = new();

        private CellLocation? selectedUnitLocation;
        private CancellationToken? cancellationToken;
        private bool turnFinished = false;

        private IUserInterface PlayerUi => playerManager.CurrentPlayer.UserInterface;

        public TurnManager(StandardMap map, PlayerManager playerManager,
            InternationalWaterManager internationalWaterManager, ILogger? logger = null)
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
            turnFinished = false;
            
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

            using CancellableLocationSelectionHandler selectionHandler = new(PlayerUi, selectable.Keys);
            await selectionHandler.Handle(
                afterSelection: async (e, localToken) =>
                {
                    var (moveChecker, actions) = selectable[e];

                    if (actions.Count == 0)
                        return;

                    selectedUnitLocation = e;

                    if (actions.Count == 1)
                    {
                        await RealiseAction(actions.First());
                    }
                    else if (actions.Count > 1)
                    {
                        try
                        {
                            OptionsHandler optionsHandler = new(PlayerUi);
                            ICellAction result = await optionsHandler.ChooseOption(
                                options: actions.Select(a => new KeyValuePair<string, ICellAction>(a.Name, a)),
                                context: selectedUnitLocation,
                                token: localToken);

                            await RealiseAction(result);
                        }
                        catch(OperationCanceledException)
                        {
                            await PlayerUi.DisplayContextOptionsMenu(selectedUnitLocation.Value, Array.Empty<string>());
                        }
                    }
                },
                until: () => !turnFinished,
                token: token);

#if DEBUG
            makeMoveWatch.Stop();
            logger?.LogInformation("MakeMove waiting for release {ms}ms", makeMoveWatch.ElapsedMilliseconds);
#endif

            marker.LastMove = new(result);
            await marker.ClearAndMarkLastMove(playerManager.UniqueInferfaces);

            await PlayerUi.DisplayMessage("Poczekaj aż przeciwnik wykona ruch", MessageType.SideMessage);
            return result;
        }

        
        private async ValueTask RealiseAction(ICellAction action)
        {
#if DEBUG
            Stopwatch watch = new();
            watch.Start();
            logger?.LogInformation("Realising Action Started.");
#endif
            turnFinished = await action.Execute(result!, cancellationToken!.Value);
#if DEBUG
            watch.Stop();
            logger?.LogInformation("Realised Action \"{actionName}\" in {time}ms", action.Name, watch.ElapsedMilliseconds);
#endif

            if (turnFinished)
            {
                selectedUnitLocation = null;
                result!.SourceUnitDescription = map[result.From].Unit!.ToString();
            }
            else
            {
                await marker.UpdateMarks();
            }
        }

        public void Dispose(){}
    }
}

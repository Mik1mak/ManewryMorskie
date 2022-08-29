using CellLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ManewryMorskie.PlacingManagerComponents
{

    public class ManualPlacingManager : PlacingMangerBase, IPlacingManager, IDisposable
    {
        private readonly List<CellLocation> topBatteriesPlaces = new() { (1, 16), (1, 15), (3, 16), (8, 14), (10, 14) };
        private readonly List<CellLocation> bottomBatteriesPlaces = new() { (1, 3), (3, 3), (8, 1), (10, 1), (10, 2) };

        private readonly Dictionary<Type, string> unitsLabels = new();
        private readonly Player currentPlayer;

        private IUserInterface Ui => currentPlayer.UserInterface;

        public ManualPlacingManager(Dictionary<Type, int> unitsToPlace, RectangleCellMap<MapField> map,
            PlayerManager players, Player currentPlayer)
            : base(unitsToPlace, map, players)
        {
            this.currentPlayer = currentPlayer;

            foreach (Type unitType in unitsToPlace.Keys)
                unitsLabels.Add(unitType, unitType.Name);
            unitsLabels[typeof(Krazownik)] = "Krążownik";
            unitsLabels[typeof(OkretDesantowy)] = "Okręt Desantowy";
            unitsLabels[typeof(OkretPodwodny)] = "Okręt Podwodny";
            unitsLabels[typeof(OkretRakietowy)] = "Okręt Rakietowy";
            unitsLabels[typeof(Tralowiec)] = "Trałowiec";
        }

        public async Task PlacePawns(CancellationToken token)
        {
            List<CellLocation> selectable = players.TopPlayer == currentPlayer ? topBatteriesPlaces : bottomBatteriesPlaces;
            await Place(selectable, kpv => kpv.Key == typeof(Bateria) && kpv.Value != 0,
                "Rozmieść swoje baterie na planszy", token);

            selectable = map.Keys.Where(l => map[l].Owner == currentPlayer && map[l].Unit == null).ToList();
            await Place(selectable, kpv => kpv.Value != 0, "Rozmieść swoje pionki na planszy", token);

            await currentPlayer.UserInterface.MarkCells(map.Keys, MarkOptions.None);
            await currentPlayer.UserInterface.DisplayMessage("Zaczekaj aż przeciwnik ustawi pionki", MessageType.SideMessage);
        }

        private async Task Place(List<CellLocation> selectable, Func<KeyValuePair<Type, int>, bool> selector,
            string msg, CancellationToken token)
        {
            LocationSelectionHandler selectionHandler = new(Ui);
            OptionsHandler optionsHandler = new(Ui);

            await Ui.MarkCells(selectable, MarkOptions.Selectable);

            do
            {
                await Ui.DisplayMessage(msg, MessageType.SideMessage);

                CellLocation selected = await selectionHandler.WaitForCorrectSelection(selectable, token);
                await Ui.MarkCells(selected, MarkOptions.Selected);
                await Ui.DisplayMessage("Wybierz jednostkę jaką chcesz umieścić", MessageType.SideMessage);

                Type? chosenUnitType = await optionsHandler.ChooseOption(
                    options: unitsToPlace.Where(selector)
                        .Select(vp => new KeyValuePair<string, Type>($"{unitsLabels[vp.Key]} ({vp.Value})", vp.Key))
                        .ToList(),
                     context: selected,
                     token: token);

                await PlaceUnit(selected, chosenUnitType!, currentPlayer);
                selectable.Remove(selected);
                await currentPlayer.UserInterface.MarkCells(selected, MarkOptions.None);
            } while (unitsToPlace.Any(selector));
        }

        public void Dispose() { }
    }

}

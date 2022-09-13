using CellLib;
using ManewryMorskie.PlacingManagerComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie.PlacingManager
{
    public class ManualUnselectablePlacingManager : ManualPlacingManager
    {
        public ManualUnselectablePlacingManager(Dictionary<Type, int> unitsToPlace,
            RectangleCellMap<MapField> map, PlayerManager players, Player currentPlayer)
            : base(unitsToPlace, map, players, currentPlayer)
        {
        }

        protected override async Task Place(List<CellLocation> selectable,
            Func<KeyValuePair<Type, int>, bool> selector, string msg, CancellationToken token)
        {
            using CancellableLocationSelectionHandler selectionHandler = new(Ui, selectable);

            await Ui.DisplayMessage(msg, MessageType.SideMessage);
            await Ui.MarkCells(selectable, MarkOptions.Selectable);

            await selectionHandler.Handle(
                afterSelection: async (selected, localToken) =>
                {
                    if (localToken.IsCancellationRequested)
                    {
                        await Ui.MarkCells(selected, MarkOptions.Selectable);
                        return;
                    }

                    OptionsHandler optionsHandler = new(Ui);

                    await Ui.MarkCells(selected, MarkOptions.Selected);
                    await Ui.DisplayMessage("Wybierz jednostkę jaką chcesz umieścić", MessageType.SideMessage);

                    try
                    {
                        Type chosenUnitType = await optionsHandler.ChooseOption(
                        options: unitsToPlace.Where(selector)
                            .Select(vp => new KeyValuePair<string, Type>($"{unitsLabels[vp.Key]} ({vp.Value})", vp.Key))
                            .ToList(),
                         context: selected,
                         token: localToken);

                        await PlaceUnit(selected, chosenUnitType!, currentPlayer);
                        selectable.Remove(selected);
                        await currentPlayer.UserInterface.MarkCells(selected, MarkOptions.None);
                    }
                    catch (OperationCanceledException e)
                    {
                        await Ui.MarkCells(selected, MarkOptions.None);
                        await Ui.MarkCells(selected, MarkOptions.Selectable);
                        await Ui.DisplayContextOptionsMenu(selected, Array.Empty<string>());
                    }
                    await Ui.DisplayMessage(msg, MessageType.SideMessage);
                },
                until: () => unitsToPlace.Any(selector),
                token: token);
        }
    }
}

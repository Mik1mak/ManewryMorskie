using CellLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie.TurnManagerComponents
{
    public partial class TurnManager
    {
        private class SelectUnitAction : ICellAction
        {
            public string Name => "Wybierz jednostkę";
            public MarkOptions MarkMode => MarkOptions.Selectable;

            private readonly CellLocation locationToSelect;
            private readonly TurnManager parent;

            public SelectUnitAction(CellLocation location, TurnManager parent)
            {
                locationToSelect = location;
                this.parent = parent;
            }

            public async Task<bool> Execute(Move move, CancellationToken token)
            {
                ClearNonSelectableUnitActions();
                parent.selectedUnitLocation = locationToSelect;

                (MoveChecker? moveChecker, _)= parent.selectable[locationToSelect];

                foreach (CellLocation moveableLocation in moveChecker!.Moveable())
                {
                    if (parent.map[locationToSelect].Unit!.IsAbleToSetMines
                        && parent.playerManager.CurrentPlayer.Fleet.MinesAreAvaible)
                    {
                        AddAction(moveableLocation, new MoveAndSetMines(moveableLocation, moveChecker, parent));
                        continue;
                    }
                                
                    AddAction(moveableLocation, new MoveAction(moveableLocation, moveChecker));
                }

                foreach (CellLocation minableLocation in moveChecker!.Minable().Except(parent.internationalWaterManager.InternationalWaters))
                    AddAction(minableLocation, new SetMineAction(minableLocation, parent));

                foreach (CellLocation attackableOrDisarmableLcation in moveChecker!.AttackableOrDisarmable())
                {
                    if (parent.internationalWaterManager.InternationalWaters.Contains(attackableOrDisarmableLcation))
                        continue;

                    Unit attacker = parent.map[parent.selectedUnitLocation!.Value].Unit!;
                    Unit target = parent.map[attackableOrDisarmableLcation].Unit!;
                    MoveChecker checker = parent.selectable[parent.selectedUnitLocation!.Value].moveChecker!;

                    AttackAction atkAction = new(
                        attacker,
                        target,
                        parent,
                        checker,
                        attackableOrDisarmableLcation);

                    AddAction(attackableOrDisarmableLcation, atkAction);

                    if (parent.map[locationToSelect].Unit!.IsAbleToDisarmMines)
                        AddAction(attackableOrDisarmableLcation, new DiasrmAction(atkAction));
                }

                await parent.PlayerUi.DisplayMessage("Wybierz działanie", MessageType.SideMessage);

                return await Task.FromResult(false);
            }

            private void AddAction(CellLocation location, ICellAction action)
            {
                if (!parent.selectable.ContainsKey(location))
                    parent.selectable.Add(location, (null, new List<ICellAction>()));

                parent.selectable[location].actions.Add(action);
            }

            private void ClearNonSelectableUnitActions()
            {
                foreach (var item in parent.selectable)
                {
                    ICellAction selectAction = item.Value.actions.FirstOrDefault(o => o is SelectUnitAction);
                    item.Value.actions.Clear();

                    if(selectAction != default)
                        item.Value.actions.Add(selectAction);
                }
            }
        }
    }
}

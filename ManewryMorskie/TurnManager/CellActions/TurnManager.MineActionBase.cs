using CellLib;
using System.Collections.Generic;
using System.Linq;

namespace ManewryMorskie.TurnManagerComponents
{
    public partial class TurnManager
    {
        private abstract class MineActionBase
        {
            private readonly TurnManager parent;

            protected MineActionBase(TurnManager parent)
            {
                this.parent = parent;
            }

            protected void RefreshActions(CellLocation center)
            {
                foreach ((MoveChecker? moveChecker, IList<ICellAction> actions)
                    in parent.selectable.Keys
                        .Intersect(center.SquereRegion(4))
                        .Select(k => parent.selectable[k]))
                {
                    actions.Clear();

                    if (moveChecker?.UnitIsSelectable() ?? false)
                    {
                        moveChecker.UpdatePaths();

                        if (moveChecker.UnitIsSelectable())
                            actions.Add(new SelectUnitAction(moveChecker.From, parent));
                    }
                }

                parent.selectedUnitLocation = null;

                foreach (CellLocation minedLocation in parent.result.SetMines)
                    parent.selectable[minedLocation].actions.Add(new UndoSetMineAction(minedLocation, parent));
            }
        }
    }
}

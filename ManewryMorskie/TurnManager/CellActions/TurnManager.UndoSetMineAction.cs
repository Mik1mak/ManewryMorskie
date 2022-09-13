using CellLib;
using ManewryMorskie.TurnManagerComponents;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie.TurnManagerComponents
{
    public partial class TurnManager
    {
        private class UndoSetMineAction : MineActionBase, ICellAction
        {
            private readonly CellLocation undoSetMineLocation;
            private readonly TurnManager parent;

            public string Name => "Anuluj ustawienie miny";

            public MarkOptions MarkMode => MarkOptions.Mined;

            public UndoSetMineAction(CellLocation undoSetMineLocation, TurnManager parent) : base(parent)
            {
                this.undoSetMineLocation = undoSetMineLocation;
                this.parent = parent;
            }

            public async Task<bool> Execute(Move move, CancellationToken token)
            {
                Player currentPlayer = parent.playerManager.CurrentPlayer;

                Unit mine = parent.map[undoSetMineLocation].Unit!;
                currentPlayer.Fleet.UndoSet(mine);
                parent.map[undoSetMineLocation].Unit = null;
                
                move.SetMines.Remove(undoSetMineLocation);

                RefreshActions(undoSetMineLocation);

                return await Task.FromResult(false);
            }
        }
    }
}

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
        private class SetMineAction : MineActionBase, ICellAction
        {
            public string Name => "Ustaw minę";
            public MarkOptions MarkMode => MarkOptions.Minable;

            private readonly CellLocation setMineLocation;
            private readonly TurnManager parent;


            public SetMineAction(CellLocation setMineLocation, TurnManager parent) : base(parent)
            {
                this.setMineLocation = setMineLocation;
                this.parent = parent;
            }

            public async Task<bool> Execute(Move move, CancellationToken token)
            {
                Player currentPlayer = parent.playerManager.CurrentPlayer;

                move.SetMines.Add(setMineLocation);
                Unit mine = new Mina();

                currentPlayer.Fleet.Set(mine);
                parent.map[setMineLocation].Unit = mine;

                RefreshActions(setMineLocation);

                return await Task.FromResult(false);
            }
        }
    }
    
}

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
        private class AttackAction : MoveAction, ICellAction
        {
            private readonly TurnManager parent;
            private readonly Unit attacker;
            private readonly Unit target;

            private CellLocation attackLocation;

            public override string Name => "Atakuj";
            public override MarkOptions MarkMode => MarkOptions.Attackable;

            public AttackAction(Unit attacker, Unit target, TurnManager parent, MoveChecker checker, CellLocation attackLocation) : base((-1, -1), checker)
            {
                this.attacker = attacker;
                this.target = target;

                this.attackLocation = attackLocation;
                this.parent = parent;
            }

            protected AttackAction(AttackAction a)
            : this(a.attacker, a.target, a.parent, a.MoveChecker, a.attackLocation)
                    { }

            public override async Task<bool> Execute(Move move, CancellationToken token)
            {
                move.Attack = attackLocation;
                move.TargetUnitDescription = target.ToString();

                IEnumerable<CellLocation> selectableEndLocations = MoveChecker.Moveable()
                    .Append(MoveChecker.From)
                    .Intersect(attackLocation.SquereRegion((int)attacker.AttackRange))
                    .Except(parent.internationalWaterManager.InternationalWaters);

                IUserInterface ui = parent.playerManager.CurrentPlayer.UserInterface;
                LocationSelectionHandler selectionHandler = new(ui);
                await ui.MarkCells(parent.map.Keys, MarkOptions.None);
                await ui.MarkCells(attackLocation, MarkOptions.Attacked);
                await ui.DisplayMessage("Wybierz pozycję końcową", MessageType.SideMessage);
                await ui.MarkCells(selectableEndLocations, MarkOptions.Moveable);

                Destination = await selectionHandler.WaitForCorrectSelection(selectableEndLocations, token);

                if(attacker.IsAbleToSetMines && parent.playerManager.CurrentPlayer.Fleet.MinesAreAvaible)
                    return await new MoveAndSetMines(Destination, MoveChecker, parent).Execute(move, token);
                else
                    return await base.Execute(move, token);
            }
        }
    }
}

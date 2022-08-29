using CellLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie.TurnManagerComponents
{
    public partial class TurnManager
    {
        private class MoveAndSetMines : MoveAction, ICellAction
        {
            private readonly TurnManager parent;

            public override string Name => "Przemieść się i ustaw miny";

            public override MarkOptions MarkMode => MarkOptions.Minable;


            public MoveAndSetMines(CellLocation destination, MoveChecker moveChecker, TurnManager parent)
                : base(destination, moveChecker)
            {
                this.parent = parent;
            }

            public override async Task<bool> Execute(Move move, CancellationToken token)
            {
                IUserInterface current = parent.playerManager.CurrentPlayer.UserInterface;
                LocationSelectionHandler locationSelection = new(current);
                await parent.marker.ClearAndMarkLastMove(new[] { current });

                List<CellLocation> selectable = parent.map
                    .AvaibleWaysFrom(Destination)
                    .EverySingleWay()
                    .Select(w => Destination + w)
                    .Concat(Destination.SquereRegion(1).Intersect(MoveChecker.From))
                    .Except(parent.internationalWaterManager.InternationalWaters).ToList();

                await current.DisplayMessage("Wskaż gdzie ustawić miny lub kliknij jeszcze raz pozycję końcową aby zakończyć",
                    MessageType.SideMessage);
                await current.MarkCells(selectable, MarkOptions.Minable);
                await current.MarkCells(Destination, MarkOptions.Selected);
                selectable.Add(Destination);

                parent.ActionSelectionActive = false;

                int placedMines = 0;
                while(true)
                {
                    CellLocation selected = await locationSelection.WaitForCorrectSelection(selectable, token);

                    if(selected != Destination)
                    {
                        move.SetMines.Add(selected);

                        if (parent.playerManager.CurrentPlayer.Fleet.UsedMines + ++placedMines == Fleet.UnitLimits[typeof(Mina)])
                            break;

                        await current.MarkCells(selected, MarkOptions.Mined);
                        selectable.Remove(selected);
                    }
                    else
                    {
                        break;
                    }
                }

                parent.ActionSelectionActive = true;

                return await base.Execute(move, token);
            }
        }
    }
}

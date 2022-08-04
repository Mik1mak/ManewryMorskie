﻿using CellLib;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ManewryMorskie
{

    public partial class TurnManager
    {
        private class CellMarker
        {
            private TurnManager parent;

            public Move? LastMove { get; set; }

            public CellMarker(TurnManager parent)
            {
                this.parent = parent;
            }

            public async ValueTask UpdateMarks()
            {
                await parent.PlayerUi.MarkCells(parent.map.Keys, MarkOptions.None);

                if (LastMove is not null)
                    foreach (IUserInterface ui in parent.playerManager.UniqueInferfaces)
                    {
                        await ui.MarkCells(LastMove.SetMines, MarkOptions.Mined);
                        await ui.MarkCells(new[] { LastMove.From, LastMove.To }, MarkOptions.Moved);

                        if (LastMove.Attack.HasValue || LastMove.Disarm.HasValue)
                            await ui.MarkCells((LastMove.Attack ?? LastMove.Disarm!).Value, MarkOptions.Attacked);
                    }

                Dictionary<MarkOptions, HashSet<CellLocation>> buffer = new()
                {
                    { MarkOptions.Selectable, new() },
                    { MarkOptions.Moveable, new() },
                    { MarkOptions.Attackable, new() },
                    { MarkOptions.Minable, new() },
                    { MarkOptions.Disarmable, new() },
                };

                foreach (var (location, actions) in parent.selectable.Select(kpv => (kpv.Key, kpv.Value.actions)))
                    foreach (ICellAction option in actions)
                        buffer[option.MarkMode].Add(location);

                foreach (var item in buffer)
                    await parent.PlayerUi.MarkCells(item.Value, item.Key);


                if (parent.selectedUnitLocation.HasValue)
                    await parent.PlayerUi.MarkCells(parent.selectedUnitLocation.Value, MarkOptions.Selected);

            }
        }
    }
}

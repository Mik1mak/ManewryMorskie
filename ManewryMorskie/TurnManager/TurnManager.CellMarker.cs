using CellLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ManewryMorskie.TurnManagerComponents
{

    public partial class TurnManager
    {
        private class CellMarker
        {
            private TurnManager parent;
            private CellLocation? lastSelected;

            public Move? LastMove { get; set; }

            public CellMarker(TurnManager parent)
            {
                this.parent = parent;
            }

            public async ValueTask UpdateMarks()
            {
                Dictionary<MarkOptions, HashSet<CellLocation>> buffer = new()
                {
                    { MarkOptions.Selectable, new() },
                    { MarkOptions.Moveable, new() },
                    { MarkOptions.Attackable, new() },
                    { MarkOptions.Minable, new() },
                    { MarkOptions.Mined, new() },
                    { MarkOptions.Disarmable, new() },
                };

                foreach (var (location, actions) in parent.selectable.Select(kpv => (kpv.Key, kpv.Value.actions)))
                    foreach (ICellAction option in actions)
                        buffer[option.MarkMode].Add(location);
#if DEBUG
                parent.logger?.LogInformation("Buffer prepared for update cell marks ({ms}ms since MakeMoveStarted)",
                    parent.makeMoveWatch.ElapsedMilliseconds);
#endif
                var toClear = parent.map.Keys.Except(buffer.SelectMany(x => x.Value));
                toClear = lastSelected.HasValue ? toClear.Union(lastSelected.Value) : toClear;
                toClear = toClear.ToArray();
                await ClearAndMarkLastMove(new[] { parent.PlayerUi }, toClear);
#if DEBUG
                parent.logger?.LogInformation("Clear and mark last move ({ms}ms since MakeMoveStarted)",
                    parent.makeMoveWatch.ElapsedMilliseconds);
#endif
                foreach (var item in buffer)
                    await parent.PlayerUi.MarkCells(item.Value, item.Key);

                if (parent.selectedUnitLocation.HasValue)
                {
                    lastSelected = parent.selectedUnitLocation.Value;
                    await parent.PlayerUi.MarkCells(parent.selectedUnitLocation.Value, MarkOptions.Selected);
                }
            }

            public async ValueTask ClearAndMarkLastMove(IEnumerable<IUserInterface> uis, IEnumerable<CellLocation>? cellsToClear = null)
            {
                cellsToClear ??= parent.map.Keys;

                foreach (IUserInterface ui in uis)
                {
                    await ui.MarkCells(cellsToClear, MarkOptions.None);

                    if (LastMove is not null)
                    {
                        if(LastMove.SetMines.Any())
                            await ui.MarkCells(LastMove.SetMines, MarkOptions.Mined);

                        await ui.MarkCells(new[] { LastMove.From, LastMove.To }, MarkOptions.Moved);

                        if (LastMove.Attack.HasValue || LastMove.Disarm.HasValue)
                            await ui.MarkCells((LastMove.Attack ?? LastMove.Disarm!).Value, MarkOptions.Attacked);
                    }
                }
            }
        }
    }
}

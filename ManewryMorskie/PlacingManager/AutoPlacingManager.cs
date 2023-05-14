using CellLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ManewryMorskie.PlacingManagerComponents
{
    public class AutoPlacingManager : PlacingMangerBase, IPlacingManager, IDisposable
    {
        private readonly Player currentPlayer;

        public AutoPlacingManager(Dictionary<Type, int> unitsToPlace, RectangleCellMap<MapField> map, PlayerManager players,
            Player currentPlayer)
            : base(unitsToPlace, map, players)
        {
            this.currentPlayer = currentPlayer;
        }

        public async Task PlacePawns(CancellationToken token)
        {
            if(unitsToPlace.ContainsKey(typeof(Bateria)))
                await PlaceDefaultBatteries(currentPlayer, unitsToPlace[typeof(Bateria)]);

            while (unitsToPlace.Any(x => x.Value != 0))
            {
                await PlaceUnit(map.Keys.First(l => map[l].Owner == currentPlayer && map[l].Unit == null),
                    unitsToPlace.First(x => x.Value != 0).Key, currentPlayer);
            }
        }

        private async ValueTask PlaceDefaultBatteries(Player currentPlayer, int batteriesToPlace)
        {
            if (batteriesToPlace <= 0)
                return;

            IEnumerable<CellLocation> entries = players.TopPlayer == currentPlayer ?
                StandardMap.DefaultTopEnterences : StandardMap.DefaultBottomEnterences;

            foreach (CellLocation location in entries)
                foreach (Ways way in CellLib.Extensions.HorizontalDirections)
                {
                    await PlaceUnit(location + way, typeof(Bateria), currentPlayer);

                    if (--batteriesToPlace == 0)
                        return;
                }
                    
        }

        public void Dispose() { }
    }

}

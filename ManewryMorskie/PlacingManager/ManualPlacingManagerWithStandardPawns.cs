using CellLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using ManewryMorskie.PlacingManager;

namespace ManewryMorskie.PlacingManagerComponents
{
    public class ManualPlacingManagerWithStandardPawns : IPlacingManager
    {
        private readonly ManualPlacingManager placingManager;

        public ManualPlacingManagerWithStandardPawns
            (RectangleCellMap<MapField> map, PlayerManager players, Player currentPlayer, ILogger? logger = null)
        {
            Dictionary<Type, int> unitsToPlace = new(Fleet.UnitLimits)
            {
                [typeof(Mina)] = 0
            };
            placingManager = new ManualUnselectablePlacingManager(unitsToPlace, map, players, currentPlayer);
        }

        public async Task PlacePawns(CancellationToken token)
        {
            await placingManager.PlacePawns(token);
        }

        public void Dispose() { }
    }

}

using CellLib;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using ManewryMorskie.PlacingManager;

namespace ManewryMorskie.PlacingManagerComponents
{
    public class ComplexPlacingManager : IPlacingManager
    {
        private readonly Player currentPlayer;
        private readonly Dictionary<string, IPlacingManager> placingMangers;

        public ComplexPlacingManager(StandardMap map, PlayerManager players, Player currentPlayer, ILogger? logger = null)
        {
            this.currentPlayer = currentPlayer;
            Dictionary<Type, int> standatdUnitsToPlace = new(Fleet.UnitLimits)
            {
                [typeof(Mina)] = 0
            };
            Dictionary<Type, int> testUnitsToPlace = new()
            {
                [typeof(OkretDesantowy)] = 1,
                [typeof(Niszczyciel)] = 1,
                [typeof(OkretRakietowy)] = 1,
                [typeof(Bateria)] = 1,
                [typeof(Tralowiec)] = 1,
            };

            placingMangers = new Dictionary<string, IPlacingManager>()
            {
                ["Ręczne ustawianie pionków"] = new ManualUnselectablePlacingManager(standatdUnitsToPlace, map, players, currentPlayer),
                ["Automatyczne ustawienie pionków"] = new AutoPlacingManager(standatdUnitsToPlace, map, players, currentPlayer),
                ["Test"] = new AutoPlacingManager(testUnitsToPlace, map, players, currentPlayer),
            };
        }

        public async Task PlacePawns(CancellationToken token)
        {
            OptionsHandler optionsHandler = new(currentPlayer.UserInterface);
            IPlacingManager? choosenManager =
                await optionsHandler.ChooseOption(placingMangers, "Wybierz tryb ustawiania pionków", null, token);

            if (choosenManager != null)
                await choosenManager.PlacePawns(token);
        }

        public void Dispose() 
        {
            foreach (IPlacingManager placingManager in placingMangers.Values)
                placingManager.Dispose();
        }
    }

}

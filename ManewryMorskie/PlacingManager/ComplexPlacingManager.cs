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

            Dictionary<Type, int> unitsToPlace = new(Fleet.UnitLimits)
            {
                [typeof(Mina)] = 0
            };
            placingMangers = new Dictionary<string, IPlacingManager>()
            {
                ["Ręczne ustawienie pionków"] = new ManualPlacingManager(unitsToPlace, map, players, currentPlayer),
                ["Ręczne ustawianie pionków z cofaniem"] = new ManualUnselectablePlacingManager(unitsToPlace, map, players, currentPlayer),
                ["Automatyczne ustawienie pionków"] = new AutoPlacingManager(unitsToPlace, map, players, currentPlayer),
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

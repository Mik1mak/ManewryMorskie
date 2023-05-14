using CellLib;
using System;
using System.Collections.Generic;

namespace ManewryMorskie.GameEndManagerComponents
{

    public class GameEndManager : IDisposable
    {
        private readonly TurnCounter turnCommander;
        private readonly List<IGameEnd> gameEnds = new();

        public bool GameIsEnded { get; private set; } = false;

        public GameEndManager(StandardMap map, TurnCounter turnCommander, PlayerManager playersManager, MoveExecutor executor)
        {
            this.turnCommander = turnCommander;

            gameEnds.Add(new DestroyedOkretyRakietoweGameEnd(playersManager, map));
            gameEnds.Add(new OkretDesantowyDestroyedGameEnd(playersManager));
            gameEnds.Add(new OkretDesantowyReachedEnemyFieldGameEnd(map, playersManager, executor));

            turnCommander.TurnChanged += CheckGameEnds;
            turnCommander.TurnChanging += CheckGameEnds;
        }

        private async void CheckGameEnds(object sender, int currentTurn)
        {
            if (GameIsEnded)
                return;

            foreach (IGameEnd gameEnd in gameEnds)
            {
                (GameIsEnded, Player? winner) = gameEnd.IsGameEnded(currentTurn);

                if (GameIsEnded)
                {
                    await gameEnd.Handle(winner);
                    return;
                }
            }
        }

        public void Dispose()
        {
            turnCommander.TurnChanged -= CheckGameEnds;
            turnCommander.TurnChanging -= CheckGameEnds;
        }
    }
}

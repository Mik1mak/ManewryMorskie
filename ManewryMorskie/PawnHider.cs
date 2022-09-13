using CellLib;
using System;

namespace ManewryMorskie
{
    public class PawnHider : IDisposable
    {
        private readonly MoveExecutor executor;
        private readonly PlayerManager players;
        private readonly StandardMap map;
        private readonly TurnCounter turnCounter;

        public PawnHider(StandardMap map, MoveExecutor executor, PlayerManager players, TurnCounter turnCounter)
        {
            this.executor = executor;
            this.players = players;
            this.map = map;
            this.turnCounter = turnCounter;
        }

        public void RegisterEvents(bool asyncGame, TurnCounter turnCounter)
        {
            if (asyncGame)
                turnCounter.TurnChanging += HideEnemyPawns;
            else
                turnCounter.TurnChanged += FlipAllPawns;
        }

        private async void FlipAllPawns(object sender, int e)
        {
            IUserInterface ui = players.CurrentPlayer.UserInterface;

            foreach (CellLocation l in map.LocationsWithPlayersUnits(players.CurrentPlayer))
            {
                if (LocationIsExcluded(l))
                    continue;

                Unit unit = map[l].Unit!;
                await ui.PlacePawn(l, players.CurrentPlayer.Color, unit is Bateria, unit!.ToString());
            }
            foreach (CellLocation l in map.LocationsWithPlayersUnits(players.GetOpositePlayer()))
            {
                if (LocationIsExcluded(l))
                    continue;
                await ui.PlacePawn(l, players.GetOpositePlayer().Color, map[l].Unit is Bateria);
            }
        }

        private async void HideEnemyPawns(object sender, int e)
        {
            if (executor.LastExecuted == null)
                return;

            Move last = executor.LastExecuted;
            Player current = players.CurrentPlayer;
            Player enemy = players.GetOpositePlayer();

            if (last.Result != BattleResult.None)
            {
                IUserInterface ui = current.UserInterface;
                if(!last.Result.HasFlag(BattleResult.TargetDestroyed))
                {
                    CellLocation l = (last.Attack ?? last.Disarm!).Value;
                    await ui.PlacePawn(l, enemy.Color, map[l].Unit is Bateria);
                }
                if(!last.Result.HasFlag(BattleResult.SourceDestroyed))
                    await enemy
                        .UserInterface.PlacePawn(last.To, current.Color, map[last.To].Unit is Bateria);
            }

            if (executor.PreviousLastExecuted == null)
                return;

            foreach (CellLocation mineLocation in executor.PreviousLastExecuted.SetMines)
                await current.UserInterface.PlacePawn(mineLocation, enemy.Color, false);
        }

        private bool LocationIsExcluded(CellLocation l)
        {
            if (executor.LastExecuted == null)
                return false;

            Move last = executor.LastExecuted;

            if (last.Result != BattleResult.None)
                return l == (last.Attack ?? last.Disarm!).Value || l == last.To;

            return last.SetMines.Contains(l);
        }

        public void Dispose()
        {
            turnCounter.TurnChanging -= HideEnemyPawns;
            turnCounter.TurnChanged -= FlipAllPawns;
        }
    }
}

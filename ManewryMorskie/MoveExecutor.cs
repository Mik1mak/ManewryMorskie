using CellLib;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ManewryMorskie
{
    public class MoveExecutor
    {
        private readonly StandardMap map;
        private readonly PlayerManager players;

        public Move? LastExecuted { get; private set; }
        public Move? PreviousLastExecuted { get; private set; }

        public MoveExecutor(StandardMap map, PlayerManager players)
        {
            this.map = map;
            this.players = players;
        }

        public async Task Execute(Move move)
        {
            move.Result = GetResult(move);

            if (move.Result.HasFlag(BattleResult.TargetDestroyed))
                DestroyUnit((move.Attack ?? move.Disarm)!.Value, players.GetOpositePlayer());

            if (move.Result.HasFlag(BattleResult.SourceDestroyed))
            {
                DestroyUnit(move.From, players.CurrentPlayer);
            }
            else
            {
                Unit unit = map[move.From].Unit!;
                map[move.From].Unit = null;
                map[move.To].Unit = unit;
            }

            foreach (CellLocation mineLocation in move.SetMines)
            {
                if (map[mineLocation].Unit is not null)
                    continue;

                Mina mine = new();
                players.CurrentPlayer.Fleet.Set(mine);
                map[mineLocation].Unit = mine;
            }

            foreach (IUserInterface ui in players.UniqueInferfaces)
                await ui.ExecuteMove(move);

            PreviousLastExecuted = LastExecuted;
            LastExecuted = new(move);
        }

        private BattleResult GetResult(Move move)
        {
            if (move.Attack.HasValue)
            {
                if (map[move.Attack.Value].Unit is Mina)
                    return BattleResult.Draw;

                return map[move.Attack.Value].Unit!.AttackedBy(map[move.From].Unit!);
            }
            else if(move.Disarm.HasValue)
            {
                if (map[move.Disarm.Value].Unit is Mina)
                    return BattleResult.TargetDestroyed;
                else
                    return BattleResult.SourceDestroyed;
            }
            else
            {
                return BattleResult.None;
            }
        }

        private void DestroyUnit(CellLocation unitLocation, Player unitOwner)
        {
            Unit destroyedUnit = map[unitLocation].Unit!;
            map[unitLocation].Unit = null;
            unitOwner.Fleet.Destroy(destroyedUnit);
        }
    }
}

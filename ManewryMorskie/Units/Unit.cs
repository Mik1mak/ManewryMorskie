using System;
using System.Collections.Generic;
using System.Linq;

namespace ManewryMorskie
{
    public abstract class Unit
    {
        protected abstract IEnumerable<Type> StrongerUnits { get; }

        public abstract uint Step { get; }
        public virtual uint AttackRange => Step;

        public virtual bool IsSelectable => true;
        public virtual bool IsAbleToSetMines => false;
        public virtual bool IsAbleToDisarmMines => false;

        public BattleResult AttackedBy(Unit u)
        {
            Type attackingType = u.GetType();

            if (GetType() == attackingType)
                return BattleResult.Draw;

            if (StrongerUnits.Contains(attackingType))
                return BattleResult.TargetDestroyed;

            return BattleResult.SourceDestroyed;
        }
            
    }
}

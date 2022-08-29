using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ManewryMorskie
{
    public class Fleet
    {
        private readonly List<Unit> units = new();
        private readonly List<Unit> destroyedUnits = new();

        public IReadOnlyList<Unit> Units => units;
        public IReadOnlyList<Unit> DestroyedUnits => destroyedUnits;

        public int UsedMines { get; private set; } = 0;
        public bool MinesAreAvaible => UsedMines < minesLimit;

        public static Dictionary<Type, int> UnitLimits { get; } = new()
        {
            { typeof(Bateria), 4 },
            { typeof(Eskortowiec), 4 },
            { typeof(Krazownik), 3 },
            { typeof(Mina), 6 },
            { typeof(Niszczyciel), 4 },
            { typeof(OkretDesantowy), 1 },
            { typeof(OkretPodwodny), 4 },
            { typeof(OkretRakietowy), 3 },
            { typeof(Pancernik), 3 },
            { typeof(Tralowiec), 4 },
        };
        private static readonly int minesLimit = UnitLimits[typeof(Mina)];

        public int ActiveUnitsCount<T>() where T : Unit
        {
            return units.Count(u => u is T);
        }

        public int DestroyedUnitsCount<T>() where T : Unit
        {
            return destroyedUnits.Count(u => u is T);
        }

        public int UnitsCount<T>() where T : Unit
        {
            return ActiveUnitsCount<T>() + DestroyedUnitsCount<T>();
        }

        public bool ContainUnit(Unit unit)
        {
            if (units.Contains(unit))
                return true;

            return destroyedUnits.Contains(unit);
        }

        public void Destroy(Unit unit)
        {
            units.Remove(unit);
            destroyedUnits.Add(unit);
        }

        public void Set(Unit unit)
        {
            Type unitType = unit.GetType();

            if (UnitLimits[unitType] <= UnitsCount(unitType))
                throw new ArgumentException("The limit of this unit type has been reached.");

            units.Add(unit);

            if (unitType == typeof(Mina))
                UsedMines++;
        }

        private int UnitsCount(Type type)
        {
            return units.Count(u => u.GetType() == type) + destroyedUnits.Count(u => u.GetType() == type);
        }

        public void Clear()
        {
            units.Clear();
            destroyedUnits.Clear();
            UsedMines = 0;
        }
    }
}

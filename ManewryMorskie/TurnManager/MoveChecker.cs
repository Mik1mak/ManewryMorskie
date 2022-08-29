using CellLib;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ManewryMorskie.TurnManagerComponents
{
    public class MoveChecker
    {
        private readonly StandardMap map;
        private readonly PlayerManager playerMgr;
        private readonly InternationalWaterManager internationalWaterManager;
        private Lazy<IPathFinder>? pathFinder;

        public CellLocation From { get; private set; }


        public MoveChecker(StandardMap map, PlayerManager playerMgr,
            CellLocation source, InternationalWaterManager internationalWaterManager)
        {
            this.map = map;
            this.playerMgr = playerMgr;
            From = source;
            this.internationalWaterManager = internationalWaterManager;

            UpdatePaths();
        }

        public void UpdatePaths()
        {
            if (map[From].Unit != null)
            {
                IEnumerable<CellLocation> forbidden =
                    map[From].Unit is OkretRakietowy ?
                        Array.Empty<CellLocation>() :
                        map.CellsProtectedByBatteries(playerMgr.GetOpositePlayer());

                pathFinder = new(() => new DijkstraPathFinder(map, From, forbidden));
            }
        }

        public IEnumerable<CellLocation> Moveable()
        {
            if (map[From].Unit == null)
                return Array.Empty<CellLocation>();

            if (map.AvaibleWaysFrom(From) == Ways.None)
                return Array.Empty<CellLocation>();

            uint distace = map[From].Unit!.Step + 1;
            return pathFinder!.Value.CellsWhereDistanceFromSourceIsLowerThan(distace);
        }

        public IEnumerable<CellLocation> AttackableOrDisarmable()
        {
            Unit? unit = map[From].Unit;

            if (unit == null)
                return Array.Empty<CellLocation>();

            IEnumerable<CellLocation> moveable = Moveable();

            //lokacje pól zajętych przez przeciwnika w zasięgu ruchu i ataku jednostki
            return moveable
                .Concat(From)
                .Except(internationalWaterManager.InternationalWaters)
                .SelectMany(l => l.SquereRegion((int)unit.AttackRange))
                .Intersect(map.LocationsWithPlayersUnits(playerMgr.GetOpositePlayer()));
        }

        public IEnumerable<CellLocation> Minable()
        {
            if (map[From].Unit == null)
                return Array.Empty<CellLocation>();

            if (!map[From].Unit!.IsAbleToSetMines || !playerMgr.CurrentPlayer.Fleet.MinesAreAvaible)
                return Array.Empty<CellLocation>();

            if (map.AvaibleWaysFrom(From) == Ways.None)
                return Array.Empty<CellLocation>();

            return pathFinder!.Value.CellsWhereDistanceFromSourceIsLowerThan(2);
        }

        public IEnumerable<CellLocation> PathTo(CellLocation target)
        {
            if (pathFinder == null)
                return Array.Empty<CellLocation>();

            if (target.Equals(From))
                return Array.Empty<CellLocation>();

            return pathFinder!.Value.ShortestPathTo(target);
        }

        public bool UnitIsSelectable()
        {
            if (map[From].Unit == null)
                return false;

            if (!map[From].Unit!.IsSelectable)
                return false;

            if (map.AvaibleWaysFrom(From) == Ways.None)
                return AttackableOrDisarmable().Any();

            return Moveable().Any() || Minable().Any() || AttackableOrDisarmable().Any();
        }
    }
}

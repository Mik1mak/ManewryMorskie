using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellLib
{
    public static class Extensions
    {
        private class DirectionsGroup : IEnumerable<Ways>
        {
            private readonly int startDirection;
            private readonly int step;
            public DirectionsGroup(int step, Ways startWay = Ways.Top)
            {
                this.step = step;
                startDirection = (int)startWay;
            }

            public IEnumerator<Ways> GetEnumerator()
            {
                for (int i = startDirection; i <= (int)Ways.All; i <<= step)
                    yield return (Ways)i;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static IEnumerable<Ways> AllDirections { get; } = new DirectionsGroup(1);
        public static IEnumerable<Ways> MainDirections { get; } = new DirectionsGroup(2);
        public static IEnumerable<Ways> VerticalDirections { get; } = new DirectionsGroup(4);
        public static IEnumerable<Ways> HorizontalDirections { get; } = new DirectionsGroup(4, Ways.Right);
        public static IEnumerable<Ways> EverySingleWay(this Ways ways) => AllDirections.Where(dir => ways.HasFlag(dir));

        public static Ways RotateWays(this Ways ways, uint stepsToRight)
        {
            int fixedStep = (int)stepsToRight % 8;
            int val = (int)ways << fixedStep;

            if (val >= 256)
                val = (val % 256) + (val >> 8);

            return (Ways)val;
        }

        public static IList<CellLocation> NextLocations(this CellLocation start, Ways ways, int length = 1)
        {
            List<CellLocation> list = new()
            {
                start
            };

            foreach (Ways way in AllDirections)
            {
                CellLocation last = start;

                if (ways.HasFlag(way))
                {
                    for (int j = 0; j < length; j++)
                    {
                        last += way;
                        list.Add(last);
                    }
                }
            }

            return list;
        }
        public static IList<CellLocation> NextLocations(this (int, int) start, Ways ways, int length = 1) 
            => NextLocations((CellLocation)start, ways, length);

        public static Region SquereRegion(this CellLocation from, int radius)
        {
            CellLocation fromRegion = from + (Ways.TopLeft, radius);
            CellLocation toRegion = from + (Ways.BottomRight, radius);

            return new(fromRegion, toRegion);
        }
        public static Region Region(this (int, int) from, CellLocation to) => new(from, to);

        public static Region Region(this CellLocation from, CellLocation to) => new(from, to);

        public static IList<CellLocation> LineBetween(this CellLocation from, CellLocation to)
        {
            var output = new List<CellLocation>();
            int min, max;

            if (from.Column == to.Column)
            {
                if (from.Row > to.Row)
                    (min, max) = (to.Row, from.Row);
                else
                    (min, max) = (from.Row, to.Row);

                for (int i = (min + 1); i < max; i++)
                    output.Add(new CellLocation(from.Column, i));
            }
            else
            {
                if (from.Column > to.Column)
                    (min, max) = (to.Column, from.Column);
                else
                    (min, max) = (from.Column, to.Column);

                double a = (from.Row - to.Row) / (double)(from.Column - to.Column);
                double b = from.Row - (a * from.Column);

                for (int x = (min + 1); x < max; x++)
                    output.Add(new CellLocation(x, (int)Math.Round(a * x + b)));
            }

            return output;
        }
    }
}

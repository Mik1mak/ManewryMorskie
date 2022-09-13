using CellLib;
using ManewryMorskie;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;

namespace ManewryTests
{
    public class Tests
    {

        [Test]
        public void FieldIteratorTest()
        {
            RectangleCellMap<MapField> map = new(12, 18);

            map[(11, 0)].Unit = new Pancernik();
            map[(0, 17)].Unit = new OkretPodwodny();

            int i = 0;

            foreach (MapField field in map)
            {
                if(i++ == 0)
                {
                    if(field.Unit is not OkretPodwodny)
                        Assert.Fail();
                }
                else if(i == map.Width*map.Height)
                {
                    if (field.Unit is not Pancernik)
                        Assert.Fail();
                }
            }
        }

        [Test]
        public void NextCells()
        {
            CellLocation pivot = (3, 3);

            IList<CellLocation> locations = pivot.NextLocations(Ways.Right | Ways.BottomLeft, 2);

            CellLocation[] expectedLocations = new[]
            {
                (4, 3),
                (5, 3),
                pivot,
                (2, 2),
                (1, 1),
            };

            foreach (CellLocation item in locations)
                if (!expectedLocations.Contains(item))
                    Assert.Fail();

            Assert.Pass();
        }

        [Test]
        public void SelectRegion()
        {
            IEnumerable<CellLocation>[] cellLocations = new[]
            {
                (1, 1).Region((3, 3)),
                (3, 1).Region((1, 3)),
                (3, 3).Region((1, 1)),
            };

            IEnumerable<CellLocation> expectedLocations = new CellLocation[]
            {
                (1,1),
                (1,2),
                (1,3),
                (2,1),
                (2,2),
                (2,3),
                (3,1),
                (3,2),
                (3,3),
            };

            foreach (IEnumerable<CellLocation> cellLocationSet in cellLocations)
                if (!expectedLocations.SequenceEqual(cellLocationSet, EqualityComparer<CellLocation>.Default))
                    Assert.Fail();
                

            Assert.Pass();
        }

        [Test]
        public void InternationalWaterManagerTest()
        {
            Unit pancernik = new Pancernik();
            Unit podwodny = new OkretPodwodny();
            Unit rakietowy = new OkretRakietowy();

            int ticks = 0;

            RectangleCellMap<MapField> map = new(12, 18);
            map[(6, 7)].Unit = podwodny;
            map[(6, 9)].Unit = pancernik;

            map.MarkInternationalWaters((5, 8).NextLocations(Ways.All));

            var manager = new InternationalWaterManager(map, 3);

            manager.InternedUnit += (sender, unit) => 
            {
                if (unit == pancernik)
                    Assert.Fail();
                if (unit == podwodny && ticks != 3)
                    Assert.Fail();
                if(unit == rakietowy && ticks != 4)
                    Assert.Fail();
            };

            ticks++;
            manager.Iterate();
            
            map[(5, 8)].Unit = rakietowy;

            map[(6, 9)].Unit = null;
            map[(0, 0)].Unit = pancernik;

            for(int i = 0; i < 3; i++, ++ticks)
                manager.Iterate();

            map[(6, 9)].Unit = pancernik;

            for (int i = 0; i < 2; i++, ++ticks)
                manager.Iterate();
        }

        [Test]
        public void BarrierBuilder()
        {
            RectangleCellMap<MapField> map = new(12, 18);

            BarrierBuilder barierBuilder = new(map);
            barierBuilder
                .AddRange((0, 3).NextLocations(Ways.Right).Select(l => (l, l + Ways.Top)))
                .AddRange((3, 3).NextLocations(Ways.Right).Select(l => (l, l + Ways.Top)))
                .AddRange((5, 1).NextLocations(Ways.Right, 3).Select(l => (l, l + Ways.Top)))
                .AddRange((10, 2).NextLocations(Ways.Right).Select(l => (l, l + Ways.Top)))
                .AddRange((1, 3).NextLocations(Ways.Right).Select(l => (l, l + Ways.Right)))
                .AddRange((4, 3).NextLocations(Ways.Bottom).Select(l => (l, l + Ways.Right)))
                .AddRange((10, 2).NextLocations(Ways.Bottom).Select(l => (l, l + Ways.Left)))
                .Add(((8, 1), (9, 1)))
                .AddSymmetricBarriers()
                .BuildBarriers();

            List<(CellLocation, Ways)> expected = new()
            {
                ((0, 3), Ways.Top | Ways.Left | Ways.TopLeft | Ways.TopRight | Ways.BottomLeft),
                ((11, 17), Ways.Top | Ways.Right | Ways.TopRight | Ways.TopLeft | Ways.BottomRight),

                ((4, 3), Ways.Top | Ways.Right | Ways.TopRight | Ways.TopLeft | Ways.BottomRight),
                ((1, 3), Ways.Top | Ways.Right | Ways.TopRight | Ways.TopLeft),
                ((2, 3), Ways.Right | Ways.Left),
                ((3, 3), Ways.Top | Ways.Left | Ways.TopLeft | Ways.TopRight),

                ((9, 1), Ways.Right | Ways.Left | Ways.TopRight),
                ((6, 15), Ways.Top | Ways.Right | Ways.TopRight | Ways.TopLeft | Ways.BottomRight),
                ((9, 14), Ways.Left | Ways.Right),
                ((4, 16), Ways.Bottom | Ways.BottomLeft | Ways.BottomRight),

                ((6, 1), Ways.TopLeft | Ways.Top | Ways.TopRight),

                ((4,1), Ways.TopRight),
            };

            foreach (var item in expected)
                if (map[item.Item1].Barriers != item.Item2)
                    Assert.Fail();

            Assert.Pass();
        }

        [Test]
        public void DirectionsGroups()
        {
            IEnumerable<(IEnumerable<Ways>, IEnumerable<Ways>)> tests = new (IEnumerable<Ways>, IEnumerable<Ways>)[]
            {
                (CellLib.Extensions.AllDirections, new Ways[]
                {
                    Ways.Top,
                    Ways.TopRight,
                    Ways.Right,
                    Ways.BottomRight,
                    Ways.Bottom,
                    Ways.BottomLeft,
                    Ways.Left,
                    Ways.TopLeft,
                }),
                (CellLib.Extensions.MainDirections, new Ways[]
                {
                    Ways.Top,
                    Ways.Right,
                    Ways.Bottom,
                    Ways.Left,
                }),
                (CellLib.Extensions.HorizontalDirections, new Ways[]
                {
                    Ways.Right,
                    Ways.Left,
                }),
                (CellLib.Extensions.VerticalDirections, new Ways[]
                {
                    Ways.Top,
                    Ways.Bottom,
                }),
            };

            foreach (var test in tests)
                if (!test.Item1.SequenceEqual(test.Item2))
                    Assert.Fail();

            Assert.Pass();
        }


        [Test]
        public void RotateWays()
        {
            List <(Ways way, uint step, Ways expected)> tests = new()
            {
                (Ways.Top, 1, Ways.TopRight),
                (Ways.Top, 8, Ways.Top),
                (Ways.Top, 7, Ways.TopLeft),
                (Ways.Top, 4, Ways.Bottom),
                (Ways.Top, 10, Ways.Right),

                (Ways.TopLeft, 1, Ways.Top),
                (Ways.TopLeft, 3, Ways.Right),

                (Ways.Right | Ways.BottomRight, 2, Ways.Bottom | Ways.BottomLeft),
                (Ways.Left | Ways.TopLeft, 1, Ways.TopLeft | Ways.Top),
                (Ways.Right | Ways.Bottom, 2, Ways.Left | Ways.Bottom),
                (Ways.Left | Ways.Bottom, 4, Ways.Top | Ways.Right),

                (Ways.Top | Ways.Right | Ways.Bottom | Ways.Left, 1, Ways.TopRight | Ways.BottomRight | Ways.BottomLeft | Ways.TopLeft),
            };

            foreach (var (way, step, expected) in tests)
                if (way.RotateWays(step) != expected)
                    Assert.Fail();

            Assert.Pass();
        }

        [Test]
        public void CellLocationSerialization()
        {
            CellLocation location = (0, 5);
            var options = new JsonSerializerOptions()
            {
                Converters =
                {
                    new CellLocationConverter(),
                }
            };
            var serializedLocation = JsonSerializer.Serialize(location, options);
            Console.WriteLine(serializedLocation);
            CellLocation deserializedLocation = JsonSerializer.Deserialize<CellLocation>(serializedLocation, options);

            if(location != deserializedLocation)
                Assert.Fail();
            Assert.Pass();
        }

        [Test]
        public void SquereRegionTest()
        {
            CellLocation z = (0,0);

            List<(CellLocation pivot, int length, HashSet<CellLocation> expected)> tests = new()
            {
                (z, 0, new() {z}),
                (z, 1, new() {(-1,1),(0,1),(1,1),(-1,0),z,(1,0),(-1,-1),(0,-1),(1,-1)}),
            };

            foreach (var (pivot, length, expected) in tests)
            {
                var result = pivot.SquereRegion(length);
                if (!expected.SetEquals(result))
                    Assert.Fail();
            }
                
            Assert.Pass();
        }

        [Test]
        public void AlgebraicNotationTest()
        {
            INotation an = new AlgebraicNotation();

            string location = an.GetNotation((3, 5));
            CellLocation redLocation = an.GetLocation(location);

            Assert.IsTrue(redLocation.Equals((3, 5)));
        }

        [Test]
        public void LineBetweenTest()
        {
            List<(IList<CellLocation> sequence, List<CellLocation> expected)> tests = new()
            {
                (new CellLocation(3, 3).LineBetween((3, 6)), new(){(3, 4), (3, 5)}),
                (new CellLocation(0, 0).LineBetween((3, 3)), new(){(1, 1), (2, 2)}),
                (new CellLocation(3, 3).LineBetween((6, 3)), new(){(4, 3), (5, 3)}),
                (new CellLocation(0, 0).LineBetween((6, 3)), new(){(1,0), (2,1), (3,2), (4,2), (5,2)}),
            };

            foreach (var (sequence, expected) in tests)
                if (!expected.SequenceEqual(sequence))
                    Assert.Fail();

            Assert.Pass();
        } 
    }
}
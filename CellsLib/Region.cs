using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CellLib
{
    public class Region : IEnumerable<CellLocation>, IRectangle
    {
        public Region(CellLocation from, CellLocation to)
        {
            From = (Math.Min(from.Column, to.Column), Math.Min(from.Row, to.Row));
            To = (Math.Max(from.Column, to.Column), Math.Max(from.Row, to.Row));
        }

        public CellLocation From { get; }
        public CellLocation To { get; }

        public int Width => From.Column - To.Column + 1;
        public int Height => From.Row - To.Row + 1;

        public IEnumerator<CellLocation> GetEnumerator()
        {
            for (int col = From.Column; col <= To.Column; col++)
                for (int row = From.Row; row <= To.Row; row++)
                    yield return new CellLocation(col, row);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

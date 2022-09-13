using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CellLib
{
    public class RectangleCellMap<T> : IReadOnlyDictionary<CellLocation, T>, IRectangle, IEnumerable<T> where T : class, new()
    {
        protected T[][] fields;

        public int Width => fields.Length; //columns
        public int Height => fields[0].Length; //rows

        public int Count => Width * Height;

        public IEnumerable<CellLocation> Keys { get; }

        public RectangleCellMap(T[][] fields)
        {
            this.fields = fields;
            Keys = (0, 0).Region((Width - 1, Height - 1));
        }

        public RectangleCellMap(int width, int height)
        {
            fields = new T[width][];

            for(int col = 0; col < width; col++)
            {
                fields[col] = new T[height];

                for(int row = 0; row < height; row++)
                    fields[col][row] = new T();
            }

            Keys = (0, 0).Region((Width - 1, Height - 1));
        }

        public CellLocation? LocationOf(T value)
        {
            foreach (CellLocation location in Keys)
                if (this[location] == value)
                    return location;

            return null;
        }

        public virtual T this[CellLocation key]
        {
            get => fields[key.Column][Height - 1 - key.Row];
            protected set => fields[key.Column][Height - 1 - key.Row] = value;
        }

        public CellLocation CenterSymmetricKey(CellLocation key)
        {
            return new (
                column: Width - 1 - key.Column,
                row: Height - 1 - key.Row);
        }

        public virtual void Clear()
        {
            foreach (T[] lineofCells in fields)
                for (int i = 0; i < lineofCells.Length; i++)
                    lineofCells[i] = new();
        }

        public bool LocationIsOnTheMap(CellLocation l)
        {
            return l.Row >= 0 && l.Column >=0 && l.Column < Width && l.Row < Height;
        }

        public IEnumerator<T> GetEnumerator() => new CellsEnumerator<T>(fields);
        IEnumerator IEnumerable.GetEnumerator() => new CellsEnumerator<T>(fields);

        #region IReadonlyDictionary
        public IEnumerable<T> Values => this;
        public bool ContainsKey(CellLocation key) => LocationIsOnTheMap(key);

        public bool TryGetValue(CellLocation key, out T value)
        {
            if (ContainsKey(key))
            {
                value = this[key];
                return true;
            }
            value = default;
            return false;
        }

        IEnumerator<KeyValuePair<CellLocation, T>> IEnumerable<KeyValuePair<CellLocation, T>>.GetEnumerator()
        {
            foreach (CellLocation key in Keys)
                yield return new KeyValuePair<CellLocation, T>(key, this[key]);
        }
        #endregion
    }
}

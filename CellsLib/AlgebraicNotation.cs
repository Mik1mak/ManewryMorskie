using System;
using System.Collections.Generic;
using System.Text;

namespace CellLib
{
    public class AlgebraicNotation : INotation
    {
        private static readonly char[] alphabet = new char[27];

        static AlgebraicNotation()
        {
            alphabet[0] = 'a';
            for (int i = 1; i < 27; i++)
                alphabet[i] = (char)(96 + i);
        }

        public CellLocation GetLocation(string algebraicNotation)
        {
            SplitValues(algebraicNotation, out string column, out string row);

            return new CellLocation(ColumnInt(column), int.Parse(row));
        }

        private void SplitValues(string algebraicNotation, out string column, out string row)
        {
            int border = ValueBorderIndex(algebraicNotation);

            column = algebraicNotation[..border];
            row = algebraicNotation[border..];
        }

        private int ValueBorderIndex(string algebraicNotation)
        {
            for (int i = 0; i < algebraicNotation.Length; i++)
            {
                char iteratingChar = algebraicNotation[i];

                if (char.IsDigit(iteratingChar))
                    return i;
            }

            throw new ArgumentException("Invalid format of algebraic notation.");
        }

        //Source: https://stackoverflow.com/a/667902/12538521
        private int ColumnInt(string columnText)
        {
            columnText = columnText.ToUpperInvariant();

            int sum = 0;

            for (int i = 0; i < columnText.Length; i++)
            {
                sum *= 26;
                sum += (columnText[i] - 'A' + 1);
            }

            return sum;
        }


        public string GetNotation(CellLocation point)
        {
            return IntToDigits(point.Column, alphabet) + point.Row;
        }

        //Source: https://stackoverflow.com/a/923814/12538521
        private string IntToDigits(int value, char[] baseChars)
        {
            int i = 32;
            char[] buffer = new char[i];
            int targetBase = baseChars.Length;

            do
            {
                buffer[--i] = baseChars[value % targetBase];
                value /= targetBase;
            }
            while (value > 0);

            char[] result = new char[32 - i];
            Array.Copy(buffer, i, result, 0, 32 - i);

            return new string(result);
        }
    }
}

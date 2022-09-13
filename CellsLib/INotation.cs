using System;
using System.Collections.Generic;
using System.Text;

namespace CellLib
{
    public interface INotation
    {
        public CellLocation GetLocation(string value);
        public string GetNotation(CellLocation value);
    }
}

using System;
using System.Collections.Generic;

namespace ManewryMorskie
{
    public class Mina : Unit
    {
        public override uint Step => 0;
        public override bool IsSelectable => false;
        protected override IEnumerable<Type> StrongerUnits => Array.Empty<Type>();
        public override string ToString() => "Mina";
    }
}

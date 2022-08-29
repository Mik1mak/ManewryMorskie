using System;
using System.Collections.Generic;

namespace ManewryMorskie
{
    public class Tralowiec : Unit
    {
        public override uint Step => 2;
        public override bool IsAbleToSetMines => true;
        public override bool IsAbleToDisarmMines => true;

        protected override IEnumerable<Type> StrongerUnits => _strongerUnits;
        private readonly static Type[] _strongerUnits = new[]
        {
            typeof(Eskortowiec),
            typeof(Pancernik),
            typeof(OkretRakietowy),
            typeof(OkretPodwodny),
            typeof(Krazownik),
            typeof(Niszczyciel),
            typeof(Mina),
            typeof(Bateria),
        };

        public override string ToString() => "Trałowiec";
    }
}

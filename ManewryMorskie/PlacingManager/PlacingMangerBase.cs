using CellLib;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ManewryMorskie.PlacingManagerComponents
{
    public abstract class PlacingMangerBase
    {
        protected readonly RectangleCellMap<MapField> map;
        protected readonly Dictionary<Type, int> unitsToPlace;
        protected readonly PlayerManager players;

        protected PlacingMangerBase(Dictionary<Type, int> unitsToPlace, RectangleCellMap<MapField> map, PlayerManager players)
        {
            this.map = map;
            this.unitsToPlace = unitsToPlace;
            this.players = players;
        }

        protected async ValueTask PlaceUnit(CellLocation location, Type typeOfUnit, Player player)
        {
            Unit unit = (Unit)Activator.CreateInstance(typeOfUnit);

            player.Fleet.Set(unit);
            map[location].Unit = unit;
            unitsToPlace[unit.GetType()]--;

            bool isBattery = unit is Bateria;

            await player.UserInterface.PlacePawn(location, player.Color, isBattery, unit.ToString());

            if (players.GetOpositePlayer(player).UserInterface != player.UserInterface)
                await players.GetOpositePlayer(player).UserInterface.PlacePawn(location, player.Color, isBattery);
        }
    }

}

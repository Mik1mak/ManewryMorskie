using Microsoft.AspNetCore.SignalR;

namespace ManewryMorskie.Server
{
    public class Rooms
    {
        private readonly Dictionary<string, Room> randomRooms = new();
        private readonly Dictionary<string, Room> namedRooms = new();
        private readonly int roomsLimits;
        private readonly ILogger<Rooms> logger;

        public int RoomsCount => randomRooms.Count + namedRooms.Count;

        public Rooms(IConfiguration config, ILogger<Rooms> logger)
        {
            roomsLimits = int.Parse(config["Rooms:Limit"]);
            this.logger = logger;
        }

        public async Task CreateRandomRoom(IGroupManager groups, Client creator, IDictionary<object, object?> contextItems)
        {
            await CreateRoom(groups, creator, Guid.NewGuid().ToString(), contextItems, randomRooms);
        }

        public async Task CreateRoom(string name, IGroupManager groups, Client creator, IDictionary<object, object?> contextItems)
        {
            if(namedRooms.ContainsKey(name))
                await creator.Kick("Pokój o podanej nazwie już istnieje. Proszę podać inną nazwę.");
            else
                await CreateRoom(groups, creator, name, contextItems, namedRooms);
        }

        private async Task CreateRoom(IGroupManager groups, Client creator, string name, 
            IDictionary<object, object?> contextItems, IDictionary<string, Room> rooms)
        {
            if (RoomsCount < roomsLimits)
            {
                await groups.AddToGroupAsync(creator.Id, name);
                rooms.Add(name, new Room(logger){}.AddClient(creator, contextItems));
            }
            else
            {
                await creator.Kick("Osiągnięto maksymalną ilość pokoi. Proszę spróbować później.");
            }
        }

        public async Task JoinToRoom(string name, IGroupManager groups, Client newClient, IDictionary<object, object?> contextItems)
        {
            if(namedRooms.ContainsKey(name))
            {
                if (namedRooms[name].IsWaitingForPlayers)
                    await Join(namedRooms[name], newClient, groups, name, contextItems);
                else
                    await newClient.Kick("Pokój jest zajęty.");
            }
            else
            {
                await newClient.Kick("Szukany pokój nie istnieje.");
            }
        }

        public async Task JoinToRandomRoom(IGroupManager groups, Client newClient, IDictionary<object, object?> contextItems)
        {
            var randomRoom = randomRooms.Where(x => x.Value.IsWaitingForPlayers).FirstOrDefault();

            if(randomRoom.Value == default)
                await newClient.Kick("Brak wolnego losowego pokoju.");
            else
                await Join(randomRoom.Value, newClient, groups, randomRoom.Key, contextItems);
        }
        
        private async Task Join(Room room, Client newClient, IGroupManager groups, string groupName, IDictionary<object, object?> contextItems)
        {
            await groups.AddToGroupAsync(newClient.Id, groupName);
            room.AddClient(newClient, contextItems);

            if(!room.IsWaitingForPlayers)
                await room.RunGame();
        }

        public void ClearDisconnectedRooms()
        {
            foreach (var rooms in new[] {namedRooms, randomRooms})
            {
                var disconnectedKeys = rooms.Where(kpv => kpv.Value.ClientDisconnected).Select(kpv => kpv.Key);
                foreach (string key in disconnectedKeys)
                    rooms.Remove(key);
            }
        }
    }
}

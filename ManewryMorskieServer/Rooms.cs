using Microsoft.AspNetCore.SignalR;
using System;

namespace ManewryMorskie.Server
{
    public class Rooms
    {
        private readonly Dictionary<string, Room> randomRooms = new();
        private readonly Dictionary<string, Room> namedRooms = new();
        private readonly Dictionary<string, Room>[] allRooms;
        public int RoomsCount => allRooms.Sum(rms => rms.Count);

        private readonly int roomsLimit;
        private readonly TimeSpan inactivityTolerance;

        private readonly ILogger<Rooms> logger;
        private readonly ILoggerFactory loggerFactory;

        public Rooms(IConfiguration config, ILoggerFactory loggerFactory)
        {
            roomsLimit = int.Parse(config["Rooms:Limit"]);
            inactivityTolerance = TimeSpan.FromMinutes(double.Parse(config["Rooms:MaxPlayerInactivityMinutes"]));
            this.logger = loggerFactory.CreateLogger<Rooms>();
            this.loggerFactory = loggerFactory;
            allRooms = new[] { randomRooms, namedRooms };
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
            if (RoomsCount <= roomsLimit)
                await ClearInactiveRooms(additionalMsg: " w trakcie kiedy osiągnięta została maksymalna ilość utworzonych pokojów przez wszystkich użytkowników.");

            if (RoomsCount < roomsLimit)
            {
                await groups.AddToGroupAsync(creator.Id, name);
                ILogger roomLogger = this.loggerFactory.CreateLogger($"Room {name}");
                rooms.Add(name, new Room(roomLogger) {}.AddClient(creator, contextItems));
            }
            else
            {
                await creator.Kick("Osiągnięto maksymalną ilość pokoi utworzonych przez użytkowników. Proszę spróbować później.");
                logger.LogCritical("Max room count reached ({limit}). Creation the {name} room rejected.", roomsLimit, name);
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

        public async Task ClearInactiveRooms(string additionalMsg = ".")
        {
            DateTime utcNow = DateTime.UtcNow;
            foreach (var rooms in allRooms)
                foreach (var kpv in rooms)
                    if((utcNow - kpv.Value.LastClientCommonActivityUtc) > inactivityTolerance)
                    {
                        await kpv.Value.Terminate($"Przekroczono dozwolony czas nieaktywności ({inactivityTolerance.TotalMinutes} min){additionalMsg}");
                        rooms.Remove(kpv.Key);
                        logger?.LogInformation("Room {roomName} terminated due to exceeding the inactivity time ({time}min).",
                            kpv.Key, inactivityTolerance.TotalMinutes);
                    }
        }

        public void ClearDisconnectedRooms()
        {
            foreach (var rooms in allRooms)
            {
                var disconnectedKeys = rooms.Where(kpv => kpv.Value.ClientDisconnected).Select(kpv => kpv.Key);
                foreach (string key in disconnectedKeys)
                    rooms.Remove(key);
            }
        }
    }
}

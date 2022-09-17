using CellLib;
using Microsoft.AspNetCore.SignalR;

namespace ManewryMorskie.Server
{
    public class ManewryMorskieHub : Hub
    {
        private readonly Rooms rooms;
        private readonly Client newClient;
        private readonly ILogger<ManewryMorskieHub> logger;

        private Client Client => (Context.Items[nameof(Client)] as Client)!;

        public ManewryMorskieHub(Rooms rooms, Client newClient, ILogger<ManewryMorskieHub> logger)
        {
            this.rooms = rooms;
            this.newClient = newClient;
            this.logger = logger;
        }

        public Task<Dictionary<string, int[]>?> GetDestroyedUnitsTable()
        {
            if(Context.Items.TryGetValue(nameof(Room), out object? roomObj))
            {
                Room room = (Room)roomObj!;

                Dictionary<string, int[]> result = new();

                foreach (Player player in room.Clients)
                {
                    foreach (Unit destroyedUnit in player.Fleet.DestroyedUnits)
                    {
                        string key = destroyedUnit.ToString()!;

                        if (!result.ContainsKey(key))
                            result.Add(key, new int[] { 0, 0 });

                        result[key][player.Color]++;
                    }
                }

                return Task.FromResult<Dictionary<string, int[]>?>(result);
            }

            return Task.FromResult<Dictionary<string, int[]>?>(null);
        }

        public async Task CreateRoom(string? name)
        {
            if (name == null)
                await rooms.CreateRandomRoom(Groups, Client, Context.Items);
            else
                await rooms.CreateRoom(name, Groups, Client, Context.Items);
        }

        public async Task JoinToRoom(string? name)
        {
            if (name == null)
                await rooms.JoinToRandomRoom(Groups, Client, Context.Items);
            else
                await rooms.JoinToRoom(name, Groups, Client, Context.Items);
        }
   
        public Task ChoosenOptionId(int optionId)
        {
            Client.NetworkUserInterface.InvokeChoosenOptionId(optionId);
            logger.LogDebug("Client {clientId} choosed {optionId} loccation", Client.Id, optionId);
            return Task.CompletedTask;
        }

        public Task ClickedLocation(CellLocation location)
        {
            Client.NetworkUserInterface.InvokeClickedLocation(location);
            logger.LogDebug("Client {clientId} clicked {location} loccation", Client.Id, location);
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            Context.Items.Add(nameof(Client), newClient);
            Client.SetCallerContext(Context);
            logger.LogInformation("Client {clientId} connected", Client.Id);
            return Task.CompletedTask;
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null)
                logger.LogError("Client {clientId} disconnected with exeption {exception}", Client.Id, exception!.Message);
            else
                logger.LogInformation("Client {clientId} disconnected", Client.Id);

            await Client.Disconnect();
            rooms.ClearDisconnectedRooms();
        }
    }
}

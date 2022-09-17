using Microsoft.AspNetCore.SignalR;

namespace ManewryMorskie.Server
{
    public class Client : Player
    {
        private readonly IHubContext<ManewryMorskieHub> hubContext;

        public event Func<Task>? Disconnecting;
        public bool IsDisconnected { get; private set; }

        private IClientProxy ClientProxy => hubContext.Clients.Client(Id);
        public string Id { get; private set; } = string.Empty;
        public CancellationToken CancellationToken { get; private set; }
        public NetworkUserInterface NetworkUserInterface => (NetworkUserInterface)this.UserInterface;

        public Client(IHubContext<ManewryMorskieHub> hubContext) 
            : base(new NetworkUserInterface(hubContext))
        {
            this.hubContext = hubContext;
        }

        public void SetCallerContext(HubCallerContext context)
        {
            NetworkUserInterface.ConnectionId = Id = context.ConnectionId;
            CancellationToken = context.ConnectionAborted;
        }

        public async Task Kick(string? message = null)
        {
            if(message != null)
                await UserInterface.DisplayMessage(message);

            await ClientProxy.SendAsync("Kick");
            IsDisconnected = true;
        }

        public async Task GameStarted()
        {
            await ClientProxy.SendAsync(nameof(GameStarted));
        }

        public async Task Disconnect()
        {
            if(Disconnecting != null)
                await Disconnecting.Invoke();
            await Kick();
        }
    }
}

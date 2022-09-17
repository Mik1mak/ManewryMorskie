using CellLib;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManewryMorskie.Network
{

    public class ManewryMorskieNetworkClient : IManewryMorskieClient
    {
        private (bool create, string? roomName, bool isRandomRoom) room;

        private readonly HubConnection connection;
        private readonly IUserInterface clientInterface;
        private readonly ILogger? logger;

        public event Func<string?, Task>? GameClosed;
        public event Func<Task>? GameStarted;

        public event Func<Exception?, Task>? Reconnecting
        {
            add => connection.Reconnecting += value;
            remove => connection.Reconnecting -= value;
        }

        public event Func<string?, Task>? Reconnected
        {
            add => connection.Reconnected += value;
            remove => connection.Reconnected -= value;
        }

        public ManewryMorskieNetworkClient(IUserInterface ui, string url, ILogger? logger = null)
        {
            this.clientInterface = ui;
            this.logger = logger;

            connection = new HubConnectionBuilder()
                .AddJsonProtocol(options => {
                    options.PayloadSerializerOptions.Converters.Add(new CellLocationConverter());
                })
                .WithUrl(url)
                .WithAutomaticReconnect()
                .Build();

            clientInterface.ClickedLocation += InvokeClickedLocation;
            clientInterface.ChoosenOptionId += InvokeChoosenOptionId;
            connection.Closed += Connection_Closed;

            connection.On<string, string[]>(nameof(IUserInterface.DisplayOptionsMenu), async (title, options) =>
                await clientInterface.DisplayOptionsMenu(title, options));

            connection.On<string, MessageType>(nameof(IUserInterface.DisplayMessage), async (msg, type) =>
                await clientInterface.DisplayMessage(msg, type));

            connection.On<CellLocation, string[]>(nameof(IUserInterface.DisplayContextOptionsMenu), async (location, options) =>
                await clientInterface.DisplayContextOptionsMenu(location, options));

            connection.On<IEnumerable<CellLocation>, MarkOptions>(nameof(IUserInterface.MarkCells), async (locations, markOption) =>
                await clientInterface.MarkCells(locations, markOption));

            connection.On<Move>(nameof(IUserInterface.ExecuteMove), async mv => 
                await clientInterface.ExecuteMove(mv));

            connection.On<CellLocation>(nameof(IUserInterface.TakeOffPawn), async l => 
                await clientInterface.TakeOffPawn(l));

            connection.On<CellLocation, int, bool, string>(nameof(IUserInterface.PlacePawn), async (l, c, b, d) =>
                await clientInterface.PlacePawn(l, c, b, d));

            connection.On(nameof(GameStarted), async () => {
                if (GameStarted != null)
                    await GameStarted.Invoke();
                });

            connection.On("Kick", async () => await StopAsync());
        }

        private async Task Connection_Closed(Exception? arg)
        {
            if(arg != null)
                logger?.LogError("Connection on client closed {exc}", arg);

            if(GameClosed != null)
                await GameClosed.Invoke("Gra została zakończona.");
        }

        private async void InvokeChoosenOptionId(object? sender, int e)
        {
            await connection.InvokeAsync(nameof(IUserInterface.ChoosenOptionId), e);
        }

        private async void InvokeClickedLocation(object? sender, CellLocation e)
        {
            await connection.InvokeAsync(nameof(IUserInterface.ClickedLocation), e);
        }

        public void SetRoom(bool create, string? roomName, bool isRandomRoom)
        {
            room = (create, roomName, isRandomRoom);
        }

        public async Task RunGame(CancellationToken ct = default)
        {
            try
            {
                await connection.StartAsync(ct);

                await connection.InvokeAsync(
                    methodName: room.create ? "CreateRoom" : "JoinToRoom", 
                    arg1: room.isRandomRoom ? null : room.roomName,
                    cancellationToken: ct);

                while (connection.State != HubConnectionState.Disconnected && !ct.IsCancellationRequested)
                    await Task.Delay(800, ct);

                await StopAsync();
            }
            catch(HttpRequestException ex)
            {
#if DEBUG
                await clientInterface.DisplayMessage($"Wystąpił błąd {ex}");
                logger?.LogError("{ex}", ex);
#else
                await clientInterface.DisplayMessage($"Wystąpił błąd {ex.StatusCode?.ToString() ?? "nieznany"}.");
#endif
            }
            catch(TaskCanceledException){}
            catch (Exception ex)
            {
#if DEBUG
                await clientInterface.DisplayMessage($"Wystąpił nieoczekiwany błąd {ex}");
                logger?.LogError("{ex}", ex);
#else
                await clientInterface.DisplayMessage($"Wystąpił nieoczekiwany błąd.");
#endif
            }
            finally
            {
                GameClosed?.Invoke(null);
            }
        }

        private async ValueTask StopAsync()
        {
            if (connection.State != HubConnectionState.Disconnected)
            {
                await connection.StopAsync(CancellationToken.None);
            }
            await Connection_Closed(null);
        }

        public async Task<Dictionary<string, int[]>> GetDestroyedUnitsTable()
        {
            return (await connection.InvokeAsync<Dictionary<string, int[]>?>(nameof(GetDestroyedUnitsTable))) ?? new();
        }

        public async ValueTask DisposeAsync()
        {
            await StopAsync();
            connection.Closed -= Connection_Closed;
            clientInterface.ClickedLocation -= InvokeClickedLocation;
            clientInterface.ChoosenOptionId -= InvokeChoosenOptionId;
            await connection.DisposeAsync();
        }
    }
}
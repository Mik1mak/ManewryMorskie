using ManewryMorskie.Network;
using System;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ManewryMorskieRazor
{
    public class GameService
    {
        private readonly UserInterface ui;
        private readonly DialogService dialogService;
        private readonly ILogger<GameService> logger;
        private readonly string serverUrl;

        private CancellationTokenSource? tokenSource;
        private IManewryMorskieClient? client;

        public event Func<Task>? GameStarted;
        public event Func<string?, Task>? GameClosed;

        public GameService(UserInterface ui, DialogService dialogService, IConfiguration Configuration, ILogger<GameService> logger)
        {
            this.ui = ui;
            this.dialogService = dialogService;
            this.logger = logger;
            serverUrl = Configuration["ManewryMorskieServerUrl"];
        }

        public async Task<Dictionary<string, int[]>> DestroyedUnits()
        {
            if (client == null)
                return new();
            return await client.GetDestroyedUnitsTable();
        }

        public async ValueTask SetUpLocal()
        {
            await Clean();

            ManewryMorskieLocalClient localClient = new(ui, logger);
            localClient.TurnChanged += Manewry_TurnChanged;
            client = localClient;
        }

        public async ValueTask SetUpOnline(bool create, string? roomName, bool randomRoom)
        {
            await Clean();

            ManewryMorskieNetworkClient networkClient = new(ui, serverUrl);
            networkClient.Reconnecting += NetworkClient_Reconnecting;
            networkClient.Reconnected += HideSplashScreen;
            networkClient.GameStarted += HideSplashScreen;
            networkClient.GameClosed += HideSplashScreen;
            dialogService.SplashScreenDismissed += AbortGame; ;
            networkClient.SetRoom(create, roomName, randomRoom);

            client = networkClient;
        }

        private async ValueTask Clean()
        {
            if (client is ManewryMorskieLocalClient localClient)
            {
                localClient.TurnChanged -= Manewry_TurnChanged;
            }
            else if(client is ManewryMorskieNetworkClient networkClient)
            {
                networkClient.Reconnecting -= NetworkClient_Reconnecting;
                networkClient.Reconnected -= HideSplashScreen;
                networkClient.GameStarted -= HideSplashScreen;
                networkClient.GameClosed -= HideSplashScreen;
                dialogService.SplashScreenDismissed -= HideSplashScreen;
            }

            if (client != null)
            {
                client.GameClosed -= Client_GameClosed;
                client.GameClosed -= GameClosed;
                client.GameStarted -= GameStarted;
                await client.DisposeAsync();
            }

            await ui.Clean();
        }

        public async Task RunGame()
        {
            if (client != null)
            {
                client.GameClosed += Client_GameClosed;
                client.GameClosed += GameClosed;
                client.GameStarted += GameStarted;

                tokenSource?.Cancel();
                await Task.Delay(5);
                tokenSource = new();

                await client.RunGame(tokenSource.Token);
            }
        }

        private async Task Client_GameClosed(string? arg)
        {
            client!.GameClosed -= Client_GameClosed;
            await client.DisposeAsync();
            await ui!.Clean();
        }

        private async void Manewry_TurnChanged(object? sender, int e)
        {
            await dialogService.DisplaySplashScreen(new("Kliknij, aby kontynuować", true));
        }

        private async Task NetworkClient_Reconnecting(Exception? arg)
        {
            await dialogService.DisplaySplashScreen(new("Utracono połączenie. Próbujemy najwiązać je ponownie.", false));
        }
        private async Task HideSplashScreen(string? arg)
        {
            await dialogService.DisplaySplashScreen(null);
        }
        private async Task HideSplashScreen()
        {
            dialogService.SplashScreenDismissed -= AbortGame;
            await dialogService.DisplaySplashScreen(null);
        }

        private async Task AbortGame()
        {
            await Clean();
        }
    }
}

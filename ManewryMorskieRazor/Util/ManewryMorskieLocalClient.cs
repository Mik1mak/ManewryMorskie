using ManewryMorskie;
using ManewryMorskie.Network;
using Microsoft.Extensions.Logging;

namespace ManewryMorskieRazor
{
    public class ManewryMorskieLocalClient : IManewryMorskieClient
    {
        private readonly IUserInterface ui;
        private readonly ManewryMorskieGame game;
        private readonly Player playerOne;
        private readonly Player playerTwo;

        public event Func<string?, Task>? GameClosed;
        public event Func<Task>? GameStarted;

        public event EventHandler<int> TurnChanged
        {
            add => game.TurnChanged += value;
            remove => game.TurnChanged -= value;
        }

        public ManewryMorskieLocalClient(IUserInterface ui, ILogger? logger = null)
        {
            this.ui = ui;

            playerOne = new(ui)
            {
                Color = 1,
            };

            playerTwo = new(ui)
            {
                Color = 0,
            };

            game = new(playerOne, playerTwo, logger);
        }

        public async Task RunGame(CancellationToken ct = default)
        {
            try
            {
                if(GameStarted != null)
                    await GameStarted.Invoke();
                await game.Start(ct);
            }
            finally
            {
                GameClosed?.Invoke(string.Empty);
            }
        }

        public Task<Dictionary<string, int[]>> GetDestroyedUnitsTable()
        {
            Dictionary<string, int[]> result = new();

            foreach (Player player in new[] { playerOne, playerTwo })
            {
                foreach (Unit destroyedUnit in player.Fleet.DestroyedUnits)
                {
                    string key = destroyedUnit.ToString()!;

                    if (!result.ContainsKey(key))
                        result.Add(key, new int[] { 0, 0 });

                    result[key][player.Color]++;
                }
            }

            return Task.FromResult(result);
        }

        public async ValueTask DisposeAsync()
        {
            if(ui is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
        }
    }
}

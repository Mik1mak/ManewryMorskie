using CellLib;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManewryMorskie.GameEndManagerComponents;
using ManewryMorskie.TurnManagerComponents;
using ManewryMorskie.PlacingManagerComponents;

namespace ManewryMorskie
{
    public class ManewryMorskieGame : IDisposable
    {
        private readonly InternationalWaterManager internationalWaterManager;
        private readonly StandardMap map;
        private readonly PlayerManager playerManager;
        private readonly MoveExecutor executor;
        private readonly PawnHider pawnHider;
        private readonly InternHandler internHandler;
        private readonly ILogger? logger;
        private readonly TurnCounter turnManager = new();
        private GameEndManager? endManager;


        public bool AsyncGame { get; set; }

        public event EventHandler<int>? TurnChanged
        {
            add => turnManager.TurnChanged += value;
            remove => turnManager.TurnChanged -= value;
        }

        public ManewryMorskieGame(Player player1, Player player2, ILogger? logger = null)
        {
            this.logger = logger;
            playerManager = new PlayerManager(turnManager, player1, player2);

            map = StandardMap.DefaultMap(playerManager);

            internationalWaterManager = new InternationalWaterManager(map);
            turnManager.TurnChanging += TurnCounter_TurnChanging;

            internHandler = new InternHandler(internationalWaterManager, map, playerManager);
            executor = new MoveExecutor(map, playerManager);
            pawnHider = new PawnHider(map, executor, playerManager, turnManager);
        }

        private void TurnCounter_TurnChanging(object sender, int e)
        {
            internationalWaterManager.Iterate();
        }

        public async Task Start(CancellationToken token)
        {
            pawnHider.RegisterEvents(AsyncGame, turnManager);

            logger?.LogInformation("Game Started.");
            using (IPlacingManager currentPlacingMgr =
#if DEBUG
                new ComplexPlacingManager(map, playerManager, playerManager.CurrentPlayer, logger)
#else
                new ManualPlacingManagerWithStandardPawns(map, playerManager, playerManager.CurrentPlayer, logger)
#endif
            ){
                Task currentPlayerPlacingTask = currentPlacingMgr.PlacePawns(token);
                token.ThrowIfCancellationRequested();

                Player opositePlayer = playerManager.GetOpositePlayer();

                if (!AsyncGame)
                {
                    await Task.WhenAll(currentPlayerPlacingTask);
                    token.ThrowIfCancellationRequested();
                    logger?.LogInformation("First Player setup pawns in async game.");
                    turnManager.NextTurn();
                }

                using (IPlacingManager opositePlacingMgr =
#if DEBUG
                    new ComplexPlacingManager(map, playerManager, opositePlayer, logger)
#else
                    new ManualPlacingManagerWithStandardPawns(map, playerManager, opositePlayer, logger)
#endif
                ){
                    Task opositePlayerPlacingTask = opositePlacingMgr.PlacePawns(token);
                    token.ThrowIfCancellationRequested();
                    await Task.WhenAll(currentPlayerPlacingTask, opositePlayerPlacingTask);
                    token.ThrowIfCancellationRequested();
                    logger?.LogInformation("All Players setup pawns.");
                    turnManager.NextTurn();
                }
            }

            endManager = new GameEndManager(map, turnManager, playerManager, executor);

            using TurnManager turnMgr = new(map, playerManager, internationalWaterManager, logger);

            while (!endManager.GameIsEnded)
            {
                await playerManager.GetOpositePlayer()
                    .UserInterface.DisplayMessage("Poczekaj aż przeciwnik wykona ruch", MessageType.SideMessage);
                Move move = await turnMgr.MakeMove(token);
                logger?.LogInformation("Move Made.");
                await executor.Execute(move);
                token.ThrowIfCancellationRequested();
                logger?.LogInformation("Move Executed.");
                turnManager.NextTurn();
            }

            await playerManager.WriteToPlayers("Gra zakończona", MessageType.SideMessage);
            logger?.LogInformation("Game Finished.");
        }

        public void Dispose()
        {
            endManager?.Dispose();
            pawnHider.Dispose();
            internHandler.Dispose();
        }
    }
}

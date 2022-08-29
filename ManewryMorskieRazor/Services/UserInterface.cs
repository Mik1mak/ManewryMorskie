using CellLib;
using ManewryMorskie;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ManewryMorskieRazor
{
    public class UserInterface : IUserInterface
    {
        private readonly BoardService boardService;
        private readonly DialogService dialogService;
        private readonly ConcurrentQueue<Move> moveBuffer = new();

        public bool ActiveClickInput { get; set; } = true;

        public event EventHandler<CellLocation>? ClickedLocation;
        public event EventHandler<int>? ChoosenOptionId;

        public UserInterface(BoardService board, DialogService dialogs)
        {
            (boardService, dialogService) = (board, dialogs);
            dialogService.OptionChoosed += DialogService_OptionChoosed;
        }

        private void DialogService_OptionChoosed(object? sender, int e)
        {
            ChoosenOptionId?.Invoke(this, e);
            ActiveClickInput = true;
        }

        public void Click(CellLocation location)
        {
            if(ActiveClickInput)
                ClickedLocation?.Invoke(this, location);
        } 

        public async Task DisplayContextOptionsMenu(CellLocation location, params string[] options)
        {
            ActiveClickInput = false;
            await boardService[location].DisplayContextMenu(options);
        }

        public async Task DisplayMessage(string message, MessageType msgType = MessageType.Standard)
        {
            await dialogService.DisplayMessage(message, msgType);
        }

        public async Task DisplayOptionsMenu(string title, params string[] options)
        {
            await dialogService.DisplayOptions(title, options);
        }

        public async Task ExecuteMove(Move mv)
        {
            ActiveClickInput = false;

            if(!moveBuffer.IsEmpty)
            {
                moveBuffer.Enqueue(mv);
                return;
            }

            while(true)
            {
                int animationDuration = (mv.Path.Count() + 2) * 250;
                await boardService[mv.From].AnimatePawn(mv.From.Concat(mv.Path).Concat(mv.To), animationDuration);

                Pawn pawn = await boardService[mv.From].TakeOffPawn();
                BoardCellService toCell = boardService[mv.To];

                await toCell.PlacePawn(pawn);

                foreach (CellLocation minedLocation in mv.SetMines)
                {
                    await PlacePawn(minedLocation, pawn.Color, false, pawn.Color == mv.CurrentPlayerColor ? "mina" : string.Empty);
                    await Task.Delay(200);
                }
                    

                if (mv.Result != BattleResult.None)
                {
                    await toCell.PlacePawn(pawn.Copy(mv.SourceUnitDescription!));

                    BoardCellService targetCell = boardService[(mv.Attack ?? mv.Disarm!).Value];
                    await targetCell.PlacePawn(targetCell.Pawn!.Value.Copy(mv.TargetUnitDescription!));
                    await Task.Delay(1800);

                    if (mv.Result.HasFlag(BattleResult.SourceDestroyed))
                        await TakeOff(toCell, mv.To);

                    if (mv.Result.HasFlag(BattleResult.TargetDestroyed))
                        await TakeOff(targetCell, (mv.Attack ?? mv.Disarm!).Value);
                }
                else
                {
                    await Task.Delay(300);
                }

                await Task.Delay(200);

                if (moveBuffer.IsEmpty)
                    break;
                else
                    while(!moveBuffer.TryDequeue(out mv!));
            }

            ActiveClickInput = true;
        }

        private async ValueTask TakeOff(BoardCellService srvice, CellLocation start)
        {
            await srvice.AnimatePawn(start.Concat(new CellLocation(15, 25)), 500);
            await srvice.TakeOffPawn();
        }

        public async Task MarkCells(IEnumerable<CellLocation> cells, MarkOptions mode)
        {
            HashSet<Task> tasks = new();

            foreach (CellLocation l in cells)
                tasks.Add(boardService[l].MarkCell(mode));

            await Task.WhenAll(tasks);
        }

        public async Task PlacePawn(CellLocation location, int playerColor, bool battery = false, string pawnDescription = "")
        {
            await boardService[location].PlacePawn(new()
            {
                Color = playerColor,
                IsBattery = battery,
                Label = pawnDescription,
                LabelIsHidden = string.IsNullOrEmpty(pawnDescription),
            });
        }

        public async Task TakeOffPawn(CellLocation location)
        {
            await boardService[location].TakeOffPawn();
        }

        public async ValueTask Clean()
        {
            HashSet<Task> tasks = new();

            foreach (CellLocation l in boardService.Keys)
            {
                if (boardService[l].Pawn.HasValue)
                    tasks.Add(TakeOffPawn(l));
                tasks.Add(boardService[l].DisplayContextMenu(Array.Empty<string>()));
            }

            tasks.Add(MarkCells(boardService.Keys, MarkOptions.None));
            tasks.Add(DisplayMessage(string.Empty, MessageType.Empty));

            await Task.WhenAll(tasks);
        }
    }
}

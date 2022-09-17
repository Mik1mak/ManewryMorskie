namespace ManewryMorskie.Network
{
    public interface IManewryMorskieClient : IAsyncDisposable
    {
        public event Func<string?, Task>? GameClosed;
        public event Func<Task>? GameStarted;

        public Task RunGame(CancellationToken ct = default);

        public Task<Dictionary<string, int[]>> GetDestroyedUnitsTable();
    }
}
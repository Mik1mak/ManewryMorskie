using CellLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie
{
    public class CancellableLocationSelectionHandler : IDisposable
    {
        private readonly IUserInterface ui;
        private readonly IReadOnlyCollection<CellLocation> validLocations;

        public CancellableLocationSelectionHandler(IUserInterface ui, IReadOnlyCollection<CellLocation> validLocations) 
        {
            this.ui = ui;
            this.validLocations = validLocations;

            ui.ClickedLocation += Ui_ClickedLocation;
        }

        SemaphoreSlim? semaphore;
        CancellationTokenSource? tokenSource = null;
        CellLocation? selected = null;

        public async Task Handle(Func<CellLocation, CancellationToken, Task> afterSelection,
            Func<bool> until, CancellationToken token = default)
        {
            do
            {
                selected = null;
                tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token);
                CancellationToken localToken = tokenSource.Token;
                semaphore = new(0, 1);

                try
                {
                    await semaphore.WaitAsync(localToken);
                    await afterSelection.Invoke(selected!.Value, localToken);
                }
                catch (OperationCanceledException) {}

                if (token.IsCancellationRequested)
                    break;

            } while (until.Invoke());
        }

        private void Ui_ClickedLocation(object sender, CellLocation e)
        {
            if(selected.HasValue)
            {
                tokenSource?.Cancel();
            }
            else if(validLocations.Contains(e))
            {
                selected = e;
                semaphore!.Release();
            }
        }

        public void Dispose()
        {
            semaphore?.Dispose();
            ui.ClickedLocation -= Ui_ClickedLocation;
        }
    }
}

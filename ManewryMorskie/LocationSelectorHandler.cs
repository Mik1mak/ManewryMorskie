using CellLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie
{
    public class LocationSelectionHandler
    {
        private readonly IUserInterface ui;

        private SemaphoreSlim? semaphore;
        private CellLocation? selectedLocation;
        private IEnumerable<CellLocation>? validSelection;

        public LocationSelectionHandler(IUserInterface ui)
        {
            this.ui = ui;
        }

        public async Task<CellLocation> WaitForCorrectSelection(IEnumerable<CellLocation> validSelection, CancellationToken token = default)
        {
            this.validSelection = validSelection;
            semaphore = new SemaphoreSlim(0, 1);
            ui.ClickedLocation += Ui_ClickedLocation;

            try
            {
                await semaphore.WaitAsync(token);
            }
            finally
            {
                ui.ClickedLocation -= Ui_ClickedLocation;
            }

            return await Task.FromResult(selectedLocation!.Value);
        }

        private void Ui_ClickedLocation(object sender, CellLocation e)
        {
            if(validSelection.Contains(e))
            {
                selectedLocation = e;
                semaphore!.Release();
            }
        }
    }
}

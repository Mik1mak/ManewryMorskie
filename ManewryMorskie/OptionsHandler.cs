using CellLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ManewryMorskie
{
    public class OptionsHandler
    {
        private readonly IUserInterface ui;

        private SemaphoreSlim? semaphore;
        private int choosenOptionId;

        public OptionsHandler(IUserInterface ui)
        {
            this.ui = ui;
        }

        public async Task<T> ChooseOption<T>(IEnumerable<KeyValuePair<string, T>> options, string title = "",
            CellLocation? context = null, CancellationToken token = default) where T : class
        {
            semaphore = new SemaphoreSlim(0, 1);
            ui.ChoosenOptionId += Ui_ChoosenOptionId;

            if (context.HasValue)
                await ui.DisplayContextOptionsMenu(context.Value, options.Select(x => x.Key).ToArray());
            else
                await ui.DisplayOptionsMenu(title, options.Select(x => x.Key).ToArray());

            try
            {
                await semaphore.WaitAsync(token);
            }
            catch(OperationCanceledException e)
            {
                throw e;
            }
            finally
            {
                ui.ChoosenOptionId -= Ui_ChoosenOptionId;
            }

            return options.ElementAt(choosenOptionId).Value;
        }

        private void Ui_ChoosenOptionId(object sender, int e)
        {
            choosenOptionId = e;
            semaphore!.Release();
        }
    }
}

using Microsoft.AspNetCore.Components.WebView.Maui;
using ManewryMorskieRazor;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace ManewryMorskie.MAUI
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
#if DEBUG
		    builder.Services.AddBlazorWebViewDeveloperTools();

            builder.Services.AddLogging(configure =>
            {
                configure.AddDebug();
            });
#endif
            #region config
            Assembly a = Assembly.GetExecutingAssembly();

            string rscName =
#if ANDROID || IOS
                "ManewryMorskie.MAUI.appsettings.Mobile.json";
#else
                "ManewryMorskie.MAUI.appsettings.json";
#endif
            using var stream = a.GetManifestResourceStream(rscName);
            var config = new ConfigurationBuilder()
                .AddJsonStream(stream)
                .Build();
            builder.Configuration.AddConfiguration(config);
#endregion

            builder.Services.AddManewryMorskieGame();

            return builder.Build();
        }
    }
}
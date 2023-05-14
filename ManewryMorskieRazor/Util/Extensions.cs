using Microsoft.Extensions.DependencyInjection;

namespace ManewryMorskieRazor
{
    public static class Extensions
    {
        public static IServiceCollection AddManewryMorskieGame(this IServiceCollection services)
        {
            services.AddScoped<BoardTransformService>();
            services.AddScoped<BootstrapInterop>();
            services.AddScoped<DialogService>();
            services.AddScoped<BoardService>();
            services.AddScoped<UserInterface>();
            services.AddScoped<GameService>();
            services.AddScoped<DragToScrollService>();
            services.AddScoped<PawnAnimatingService>();

            return services;
        }
    }
}

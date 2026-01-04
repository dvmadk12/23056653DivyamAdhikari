using Microsoft.Extensions.Logging;

namespace JUpdate
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
    		builder.Logging.AddDebug();
#endif

            // Register services
            builder.Services.AddSingleton<JUpdate.Services.DatabaseService>();
            builder.Services.AddScoped<JUpdate.Services.JournalService>();
            builder.Services.AddScoped<JUpdate.Services.AuthService>();
            builder.Services.AddScoped<JUpdate.Services.ThemeService>();
            builder.Services.AddScoped<JUpdate.Services.AnalyticsService>();

            return builder.Build();
        }
    }
}

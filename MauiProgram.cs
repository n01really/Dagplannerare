using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using CommunityToolkit.Maui; // Ensure this using is present

namespace PlannerApp
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit() // Chain this directly after UseMauiApp
                .ConfigureSyncfusionToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            string dbFolder = Path.Combine(FileSystem.AppDataDirectory, "DB");
            Directory.CreateDirectory(dbFolder);
            string dbPath = Path.Combine(dbFolder, "plannerapp.db3"); //C:\Users\trygg\AppData\Local\PlannerApp\DB\

            builder.Services.AddSingleton(new DB.dbContext(dbPath));

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<WeatherPage>();
            builder.Services.AddSingleton<AnalysisPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

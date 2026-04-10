using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using CommunityToolkit.Maui;

namespace PlannerApp.SRC
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
            string dbPath = Path.Combine(dbFolder, "plannerapp.db3"); //C:\Users\trygg\AppData\Local\User Name\com.companyname.plannerapp\Data
            builder.Services.AddSingleton(new DB.dbContext(dbPath));

            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddSingleton<WeatherPage>();
            builder.Services.AddSingleton<AnalysisPage>();
            builder.Services.AddSingleton<PlannerApp.SRC.Backend.AppService>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

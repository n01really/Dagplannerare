using PlannerApp.SRC.APIs;
using PlannerApp.SRC.DB;
using PlannerApp.SRC.Math;
using PlannerApp.SRC.Models;

namespace PlannerApp;

public partial class WeatherPage : ContentPage
{
	private readonly SmhiAPI _smhiAPI = new SmhiAPI();
	private readonly SeasonCalculator _seasonCalculator = new SeasonCalculator();
    private readonly dbContext _dbContext;
    private IDispatcherTimer _weatherTimer;

    public WeatherPage(dbContext database)
	{
		InitializeComponent();
        _dbContext = database;
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // Uppdatera direkt när sidan visas
        await UpdateWeatherAsync();
        
        // Starta timer för uppdatering varje timme (3600 sekunder)
        _weatherTimer = Dispatcher.CreateTimer();
        _weatherTimer.Interval = TimeSpan.FromHours(1);
        _weatherTimer.Tick += async (s, e) => await UpdateWeatherAsync();
        _weatherTimer.Start();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Stoppa timer när sidan lämnas
        _weatherTimer?.Stop();
    }

    private async Task UpdateWeatherAsync()
    {
        // Visa laddningsstatus
        TempLabel.Text = "Laddar...";
        SeasonLabel.Text = "Beräknar...";
        WeatherLabel.Text = "Hämtar...";

        // Uppdatera temperaturen i season calculator
        await _seasonCalculator.UpdateTemperature();
        
        // Uppdatera säsongsberäkningen
        await _seasonCalculator.UpdateSeasonAsync();

        double? temp = await _smhiAPI.GetCurrentTempratureAsync();

        if (temp.HasValue)
        {
            // Uppdatera temperatur
            TempLabel.Text = $"{temp.Value}°C";
            
            // Uppdatera årstid
            string currentSeason = _seasonCalculator.GetCurrentSeason();
            SeasonLabel.Text = currentSeason;
            
            // Uppdatera väderförhållanden (baserat på temperatur)
            string weatherDescription = GetWeatherDescription(temp.Value);
            WeatherLabel.Text = weatherDescription;

            // Spara väderdata till databasen
            await SaveCurrentWeatherLog(weatherDescription, temp.Value);
        }
        else
        {
            TempLabel.Text = "-- °C";
            SeasonLabel.Text = "Okänd";
            WeatherLabel.Text = "--";
            await DisplayAlert("Hoppsan", "Kunde inte hämta vädret. Kolla internet och platstjänster.", "OK");
        }
    }

    private async Task SaveCurrentWeatherLog(string weatherCondition, double temperature)
    {
        try
        {
            var weatherLog = new WeatherLoggingModel
            {
                WeatherCondition = weatherCondition,
                Temperature = temperature,
                DateTime = DateTime.Now
            };

            await _dbContext.SaveWeatherLogAsync(weatherLog);
        }
        catch (Exception ex)
        {
            // Logga eventuella fel, men fortsätt visa vädret för användaren
            System.Diagnostics.Debug.WriteLine($"Kunde inte spara väderlog: {ex.Message}");
        }
    }

    private string GetWeatherDescription(double temperature)
    {
        if (temperature < -10)
            return "Mycket kallt";
        else if (temperature < 0)
            return "Kallt";
        else if (temperature < 10)
            return "Svalt";
        else if (temperature < 20)
            return "Behagligt";
        else if (temperature < 25)
            return "Varmt";
        else
            return "Mycket varmt";
    }
}
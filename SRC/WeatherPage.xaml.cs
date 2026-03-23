using PlannerApp.SRC.APIs;
using PlannerApp.SRC.DB;
using PlannerApp.SRC.Math;

namespace PlannerApp;

public partial class WeatherPage : ContentPage
{
	private readonly SmhiAPI _smhiAPI = new SmhiAPI();
	private readonly SeasonCalculator _seasonCalculator = new SeasonCalculator();
    private readonly dbContext _dbContext;

    public WeatherPage(dbContext database)
	{
		InitializeComponent();
        _dbContext = database;
	}

    private async void OnUpdateWeatherClicked(object sender, EventArgs e)
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
            WeatherLabel.Text = GetWeatherDescription(temp.Value);
        }
        else
        {
            TempLabel.Text = "-- °C";
            SeasonLabel.Text = "Okänd";
            WeatherLabel.Text = "--";
            await DisplayAlert("Hoppsan", "Kunde inte hämta vädret. Kolla internet och platstjänster.", "OK");
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
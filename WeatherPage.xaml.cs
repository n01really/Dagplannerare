using PlannerApp.APIs;
namespace PlannerApp;

public partial class WeatherPage : ContentPage
{
	private readonly SmhiAPI _smhiAPI = new SmhiAPI();
    public WeatherPage()
	{
		InitializeComponent();
	}

    private async void OnUpdateWeatherClicked(object sender, EventArgs e)
    {
       
        TempLabel.Text = "Hämtar position och väder...";

        double? temp = await _smhiAPI.GetCurrentTempratureAsync();

        if (temp.HasValue)
        {
            TempLabel.Text = $"Just nu: {temp.Value}°C";
        }
        else
        {
            await DisplayAlert("Hoppsan", "Kunde inte hämta vädret. Kolla internet och platstjänster.", "OK");
        }
    }
}
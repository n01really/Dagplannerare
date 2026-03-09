using Microsoft.Maui.Devices.Sensors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace PlannerApp.APIs
{
    public class SmhiAPI
    {
        private readonly HttpClient _httpClient;
        private const string BaseUrl = "https://opendata-download-metfcst.smhi.se/api/category/pmp3g/version/2/geotype/point";

        public SmhiAPI()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PlannerApp/1.0");
        }

        public async Task<double?> GetCurrentTempratureAsync()
        {
            try
            {
                // Position
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
                if (location == null) return null;

                // URL
                string lat = location.Latitude.ToString("F6", CultureInfo.InvariantCulture); //F: fast decimalpunkt, 6: 6 decimaler, InvariantCulture: punkt som decimalavgränsare
                string lon = location.Longitude.ToString("F6", CultureInfo.InvariantCulture);
                string url = $"{BaseUrl}/lon/{lon}/lat/{lat}/data.json";

                var response = await _httpClient.GetFromJsonAsync<SmhiResponse>(url);

                var currentForecast = response?.TimeSeries?.FirstOrDefault();
                var tempParam = currentForecast?.Parameters?.FirstOrDefault(p => p.Name == "t");

                return tempParam?.Values?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Väderfel: {ex.Message}");
                return null;
            }
        }
    }

    public record SmhiResponse(List<ForecastTime> TimeSeries);
    public record ForecastTime(DateTime ValidTime, List<WeatherParameter> Parameters);
    public record WeatherParameter(string Name, List<double> Values);
}

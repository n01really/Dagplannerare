using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace PlannerApp.SRC.APIs
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

        // Testhjälp: fråga med specifika koordinater (använd för felsökning)
        public async Task<double?> GetCurrentTemperatureForCoordsAsync(double latitude, double longitude)
        {
            var response = await QuerySmhiAsync(latitude, longitude);
            var currentForecast = response?.TimeSeries?.FirstOrDefault();
            var tempParam = currentForecast?.Parameters?.FirstOrDefault(p => p.Name == "t");
            if (tempParam?.Values?.FirstOrDefault() is double t) return t;

            // Fallback to Open-Meteo
            return await GetCurrentFromOpenMeteoAsync(latitude, longitude);
        }

        public async Task<double?> GetCurrentTempratureAsync()
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    Debug.WriteLine("Location permission not granted.");
                    return null;
                }

                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
                if (location == null)
                {
                    Debug.WriteLine("Geolocation returned null.");
                    return null;
                }

                // Försök SMHI först (exakt punkt)
                var response = await QuerySmhiAsync(location.Latitude, location.Longitude);
                var temp = response?.TimeSeries?.FirstOrDefault()?.Parameters?.FirstOrDefault(p => p.Name == "t")?.Values?.FirstOrDefault();
                if (temp.HasValue) return temp.Value;

                // Om tom/ingen data => prova närliggande punkter (bredare snapping)
                double[] offsets = { 0.01, -0.01, 0.02, -0.02, 0.05, -0.05, 0.1, -0.1, 0.2, -0.2 };
                foreach (var dLat in offsets)
                {
                    foreach (var dLon in offsets)
                    {
                        var r = await QuerySmhiAsync(location.Latitude + dLat, location.Longitude + dLon);
                        var t = r?.TimeSeries?.FirstOrDefault()?.Parameters?.FirstOrDefault(p => p.Name == "t")?.Values?.FirstOrDefault();
                        if (t.HasValue) return t.Value;
                    }
                }

                // Om SMHI fortfarande saknar data -> fallback till Open-Meteo
                Debug.WriteLine("SMHI saknar data för punkt + närområde — använder Open-Meteo fallback.");
                return await GetCurrentFromOpenMeteoAsync(location.Latitude, location.Longitude);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Väderfel: {ex.Message}");
                return null;
            }
        }

        public async Task<List<double>?> GetTemperatureForecastAsync(int days)
        {
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                    status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

                if (status != PermissionStatus.Granted)
                {
                    Debug.WriteLine("Location permission not granted.");
                    return null;
                }

                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
                if (location == null) return null;

                var response = await QuerySmhiAsync(location.Latitude, location.Longitude);

                if (response == null || response.TimeSeries == null || !response.TimeSeries.Any())
                {
                    // Prova närområdet
                    double[] offsets = { 0.01, -0.01, 0.02, -0.02, 0.05, -0.05, 0.1, -0.1 };
                    foreach (var dLat in offsets)
                    {
                        foreach (var dLon in offsets)
                        {
                            var r = await QuerySmhiAsync(location.Latitude + dLat, location.Longitude + dLon);
                            if (r != null && r.TimeSeries != null && r.TimeSeries.Any())
                            {
                                response = r;
                                goto haveResponse;
                            }
                        }
                    }
                }

            haveResponse:
                if (response?.TimeSeries != null && response.TimeSeries.Any())
                {
                    var dailyTemperatures = new List<double>();
                    var today = DateTime.Today;

                    for (int i = 0; i < days; i++)
                    {
                        var targetDate = today.AddDays(i);
                        var dayForecasts = response.TimeSeries
                            .Where(ts => ts.ValidTime.Date == targetDate)
                            .SelectMany(ts => ts.Parameters)
                            .Where(p => p.Name == "t")
                            .SelectMany(p => p.Values)
                            .ToList();

                        if (dayForecasts.Any())
                        {
                            dailyTemperatures.Add(dayForecasts.Average());
                        }
                    }

                    if (dailyTemperatures.Any()) return dailyTemperatures;
                }

                // Fallback till Open-Meteo om SMHI inte levererar forecast
                Debug.WriteLine("Använder Open-Meteo för prognosfallback.");
                return await GetForecastFromOpenMeteoAsync(location.Latitude, location.Longitude, days);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Väderfel: {ex.Message}");
                return null;
            }
        }

        public async Task<List<double>?> GetHistoricalTemperaturesAsync(int daysBack)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.High));
                if (location == null) return null;

                Debug.WriteLine("Historisk data kräver Metobs API implementation (kan implementeras senare).");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Historisk data fel: {ex.Message}");
                return null;
            }
        }

        // Utför HTTP GET mot SMHI, loggar status och body, och deserialiserar säkert
        private async Task<SmhiResponse?> QuerySmhiAsync(double latitude, double longitude)
        {
            string lat = latitude.ToString("F6", CultureInfo.InvariantCulture);
            string lon = longitude.ToString("F6", CultureInfo.InvariantCulture);
            string url = $"{BaseUrl}/lon/{lon}/lat/{lat}/data.json";

            try
            {
                Debug.WriteLine($"Trying SMHI: {url}");
                var resp = await _httpClient.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();
                Debug.WriteLine($"SMHI GET {url} => {(int)resp.StatusCode} {resp.ReasonPhrase}");
                if (!resp.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"SMHI error body: {body}");
                    return null;
                }

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var parsed = JsonSerializer.Deserialize<SmhiResponse>(body, options);

                if (parsed == null || parsed.TimeSeries == null || !parsed.TimeSeries.Any())
                {
                    Debug.WriteLine("SMHI returned empty TimeSeries for coordinates: " + lat + "," + lon);
                    return parsed;
                }

                return parsed;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"QuerySmhiAsync exception: {ex.Message}");
                return null;
            }
        }

        // Open-Meteo fallback: hentar aktuell temp
        private async Task<double?> GetCurrentFromOpenMeteoAsync(double latitude, double longitude)
        {
            try
            {
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&current_weather=true&hourly=temperature_2m&timezone=auto";
                Debug.WriteLine($"OpenMeteo GET {url}");
                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode)
                {
                    Debug.WriteLine($"OpenMeteo status {(int)resp.StatusCode}");
                    return null;
                }

                var body = await resp.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var om = JsonSerializer.Deserialize<OpenMeteoResponse>(body, options);
                if (om?.Current_Weather != null) return om.Current_Weather.Temperature;
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenMeteo exception: {ex.Message}");
                return null;
            }
        }

        // Open-Meteo fallback: dagliga medelvärden beräknade från timvis data
        private async Task<List<double>?> GetForecastFromOpenMeteoAsync(double latitude, double longitude, int days)
        {
            try
            {
                // begär timvis prognos flera dagar fram
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude.ToString(CultureInfo.InvariantCulture)}&longitude={longitude.ToString(CultureInfo.InvariantCulture)}&hourly=temperature_2m&forecast_days={System.Math.Max(1, days)}&timezone=auto";
                Debug.WriteLine($"OpenMeteo GET {url}");
                var resp = await _httpClient.GetAsync(url);
                if (!resp.IsSuccessStatusCode) return null;
                var body = await resp.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var om = JsonSerializer.Deserialize<OpenMeteoResponse>(body, options);
                if (om?.Hourly == null || om.Hourly.Time == null || om.Hourly.Temperature_2m == null) return null;

                var daily = new List<double>();
                var times = om.Hourly.Time;
                var temps = om.Hourly.Temperature_2m;

                var grouped = new Dictionary<DateTime, List<double>>();
                for (int i = 0; i < times.Length && i < temps.Length; i++)
                {
                    if (!DateTime.TryParse(times[i], out var dt)) continue;
                    var date = dt.Date;
                    if (!grouped.ContainsKey(date)) grouped[date] = new List<double>();
                    grouped[date].Add(temps[i]);
                }

                var today = DateTime.Today;
                for (int i = 0; i < days; i++)
                {
                    var key = today.AddDays(i);
                    if (grouped.TryGetValue(key, out var list) && list.Any())
                        daily.Add(list.Average());
                }

                return daily;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"OpenMeteo forecast exception: {ex.Message}");
                return null;
            }
        }

        // Open-Meteo DTOs
        private class OpenMeteoResponse
        {
            public OpenMeteoCurrent Current_Weather { get; set; }
            public OpenMeteoHourly Hourly { get; set; }
        }

        private class OpenMeteoCurrent
        {
            public double Temperature { get; set; }
            // other fields omitted
        }

        private class OpenMeteoHourly
        {
            public string[] Time { get; set; }
            public double[] Temperature_2m { get; set; }
        }
    }

    public record SmhiResponse(List<ForecastTime> TimeSeries);
    public record ForecastTime(DateTime ValidTime, List<WeatherParameter> Parameters);
    public record WeatherParameter(string Name, List<double> Values);
}

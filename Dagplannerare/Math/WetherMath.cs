using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlannerApp.APIs;

namespace PlannerApp.Math
{

        // metrologisk vår är när dygnsmedeltemperaturen är över 0°C men under 10°C i minst 7 dagar i rad.
        // metrologisk somar är när dygnsmedeltemperaturen är över 10°C i minst 7 dagar i rad.
        // metrologisk höst är när dygnsmedeltemperaturen är under 10°C men över 0°C i minst 7 dagar i rad efter att ha varit över 10°C.
        // metrologisk vinter är när dygnsmedeltemperaturen är under 0°C i minst 7 dagar i rad.
        // det är den första dagen av de 7 dagarna som räknas som startdagen för den nya årstiden.
        // snön bör ha smält bort när medeltemperaturen är över 0°C i 14 dagar i rad.
        // det är mattematiken jag kommer att använda i min app för att räkna ut när årstiderna börjar och slutar, samt när snön smälter bort.

    public class SeasonCalculator
    {
        readonly SmhiAPI _smhiAPI = new SmhiAPI();

        public int temperature { get; set; }

        int daysAbove0 = 0;
        int daysAbove10 = 0;
        int daysBelow0 = 0;
        

        bool isSpring = false;
        bool isSummer = false;
        bool isAutumn = false;
        bool isWinter = false;

        // Lagra temperaturer lokalt för att kunna kolla bakåt
        private List<(DateTime Date, double Temperature)> _historicalTemperatures = new();

        public async Task UpdateTemperature()
        {
            var temp = await _smhiAPI.GetCurrentTempratureAsync();
            if (temp.HasValue)
            {
                temperature = (int)temp.Value;
                
                // Spara dagens temperatur
                var today = DateTime.Today;
                var existing = _historicalTemperatures.FirstOrDefault(t => t.Date == today);
                if (existing != default)
                {
                    _historicalTemperatures.Remove(existing);
                }
                _historicalTemperatures.Add((today, temp.Value));
                
                // Behåll bara senaste 30 dagarna
                _historicalTemperatures = _historicalTemperatures
                    .Where(t => t.Date >= DateTime.Today.AddDays(-30))
                    .OrderBy(t => t.Date)
                    .ToList();
            }
        }

        public async Task UpdateSeasonAsync(int daysBack = 7, int daysForward = 10)
        {
            // Hämta framtida temperaturer
            var futureTemperatures = await _smhiAPI.GetTemperatureForecastAsync(daysForward);
            if (futureTemperatures == null)
                futureTemperatures = new List<double>();

            // Kombinera historisk och framtida data
            var allTemperatures = new List<double>();
            
            // Lägg till historiska temperaturer
            var historicalData = _historicalTemperatures
                .Where(t => t.Date >= DateTime.Today.AddDays(-daysBack) && t.Date < DateTime.Today)
                .OrderBy(t => t.Date)
                .Select(t => t.Temperature)
                .ToList();
            
            allTemperatures.AddRange(historicalData);
            allTemperatures.AddRange(futureTemperatures);

            if (allTemperatures.Count == 0)
                return;

            // Reset counters
            daysAbove0 = 0;
            daysAbove10 = 0;
            daysBelow0 = 0;

            // Count consecutive days for each temperature range
            foreach (var temp in allTemperatures)
            {
                if (temp > 10)
                {
                    daysAbove10++;
                    daysAbove0++;
                    daysBelow0 = 0;
                }
                else if (temp > 0)
                {
                    daysAbove0++;
                    daysAbove10 = 0;
                    daysBelow0 = 0;
                }
                else // temp <= 0
                {
                    daysBelow0++;
                    daysAbove0 = 0;
                    daysAbove10 = 0;
                }

                // Check season conditions after each day
                if (daysBelow0 >= 7)
                {
                    isWinter = true;
                    isSpring = false;
                    isSummer = false;
                    isAutumn = false;
                }
                else if (daysAbove10 >= 7)
                {
                    isSummer = true;
                    isSpring = false;
                    isAutumn = false;
                    isWinter = false;
                }
                else if (daysAbove0 >= 7 && daysAbove10 < 7)
                {
                    if (isSummer)
                    {
                        isAutumn = true;
                        isSpring = false;
                    }
                    else
                    {
                        isSpring = true;
                        isAutumn = false;
                    }
                    isSummer = false;
                    isWinter = false;
                }
            }
        }

        public string GetCurrentSeason()
        {
            if (isWinter) return "Vinter";
            if (isSpring) return "Vår";
            if (isSummer) return "Sommar";
            if (isAutumn) return "Höst";
            return "Okänd";
        }
    }
}

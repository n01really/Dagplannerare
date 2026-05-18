using PlannerApp.SRC.Backend;
using PlannerApp.SRC.DB;
using PlannerApp.SRC.Models;

namespace PlannerApp;

public partial class AnalysisPage : ContentPage
{
    private readonly ProcessService _processService = new ProcessService();
    private readonly dbContext _dbContext;
    private Dictionary<int, DateTime> _trackedProcesses = new Dictionary<int, DateTime>();

    public AnalysisPage(dbContext database)
    {
        InitializeComponent();
        _dbContext = database;
    }

    private async void OnShowTopAppsClicked(object sender, EventArgs e)
    {
        App1NameLabel.Text = "Laddar...";
        App2NameLabel.Text = "Laddar...";
        App3NameLabel.Text = "Laddar...";
        App4NameLabel.Text = "Laddar...";
        App5NameLabel.Text = "Laddar...";

        App1TimeLabel.Text = "-- minuter";
        App2TimeLabel.Text = "-- minuter";
        App3TimeLabel.Text = "-- minuter";
        App4TimeLabel.Text = "-- minuter";
        App5TimeLabel.Text = "-- minuter";

        // 1. Hämta de appar som körs JUST NU (aktiva)
        var activeProcesses = _processService.GetUserApplications();

        // 2. Uppdatera vår spårning och logga de appar som har STÄNGTS sedan sist
        await LogClosedProcessesToDatabase(activeProcesses);

        // 3. Hämta all historik från databasen (redan stängda appar)
        var dbLogs = await _dbContext.GetProcessLogsAsync();

        // 4. Skapa en gemensam lista för att räkna ihop tid från både databasen OCH de som körs just nu
        var allAppTimes = dbLogs
            .GroupBy(log => log.AppName)
            .Select(group => new
            {
                AppName = group.Key,
                TotalMinutes = group.Sum(log => (log.EndTime - log.StartTime).TotalMinutes)
            })
            .ToList();

        // 5. Lägg till tiden för de appar som körs JUST NU live (utan att spara dem i DB än)
        foreach (var process in activeProcesses)
        {
            var existing = allAppTimes.FirstOrDefault(a => a.AppName == process.Name);
            double currentActiveMinutes = (DateTime.Now - process.StartTime).TotalMinutes;

            if (existing != null)
            {
                // Om appen redan har historik i DB, plussa på den live-tid som körs nu
                int index = allAppTimes.IndexOf(existing);
                allAppTimes[index] = new { AppName = process.Name, TotalMinutes = existing.TotalMinutes + currentActiveMinutes };
            }
            else
            {
                // Om appen inte finns i DB än, lägg till den med dess nuvarande live-tid
                allAppTimes.Add(new { AppName = process.Name, TotalMinutes = currentActiveMinutes });
            }
        }

        // 6. Sortera listan så de mest använda hamnar först och ta topp 5
        var topApps = allAppTimes
            .OrderByDescending(app => app.TotalMinutes)
            .Take(5)
            .ToList();

        ClearPanels();

        // Uppdatera UI
        if (topApps.Count > 0)
        {
            App1NameLabel.Text = topApps[0].AppName;
            App1TimeLabel.Text = $"{Math.Round(topApps[0].TotalMinutes, 1)} minuter";
        }
        if (topApps.Count > 1)
        {
            App2NameLabel.Text = topApps[1].AppName;
            App2TimeLabel.Text = $"{Math.Round(topApps[1].TotalMinutes, 1)} minuter";
        }
        if (topApps.Count > 2)
        {
            App3NameLabel.Text = topApps[2].AppName;
            App3TimeLabel.Text = $"{Math.Round(topApps[2].TotalMinutes, 1)} minuter";
        }
        if (topApps.Count > 3)
        {
            App4NameLabel.Text = topApps[3].AppName;
            App4TimeLabel.Text = $"{Math.Round(topApps[3].TotalMinutes, 1)} minuter";
        }
        if (topApps.Count > 4)
        {
            App5NameLabel.Text = topApps[4].AppName;
            App5TimeLabel.Text = $"{Math.Round(topApps[4].TotalMinutes, 1)} minuter";
        }
    }

    // ÄNDRAD METOD: Denna loggar nu ENBART appar som faktiskt har stängts!
    private async Task LogClosedProcessesToDatabase(List<ProcessItem> currentProcesses)
    {
        var currentProcessIds = new HashSet<int>();

        foreach (var process in currentProcesses)
        {
            currentProcessIds.Add(process.Id);

            if (!_trackedProcesses.ContainsKey(process.Id))
            {
                _trackedProcesses[process.Id] = process.StartTime;
            }
        }

        // Hitta vilka ID:n som fanns förut men INTE finns i datorn nu (alltså stängda appar)
        var closedProcesses = _trackedProcesses.Keys
            .Where(id => !currentProcessIds.Contains(id))
            .ToList();

        foreach (var closedProcessId in closedProcesses)
        {
            try
            {
                // Hitta namnet på appen som stängdes genom att kolla bakåt (eller sätt ett standardnamn)
                string closedAppName = "Avslutad app";

                // Skapa loggen för den stängda appen
                var processLog = new ProcessLoggingModel
                {
                    AppName = closedAppName,
                    AppId = closedProcessId,
                    StartTime = _trackedProcesses[closedProcessId],
                    EndTime = DateTime.Now
                };

                await _dbContext.SaveProcessLogAsync(processLog);
                _trackedProcesses.Remove(closedProcessId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Kunde inte logga stängd process {closedProcessId}: {ex.Message}");
            }
        }
    }

    private async void OnClearHistoryClicked(object sender, EventArgs e)
    {
        bool answer = await DisplayAlert("Rensa historik", "Är du säker på att du vill ta bort all app-statistik?", "Ja", "Nej");

        if (answer)
        {
            await _dbContext.ClearProcessLogsAsync();
            _trackedProcesses.Clear(); // Töm även interna spårningen
            ClearPanels();
            await DisplayAlert("Klart", "Historiken har rensats!", "OK");
        }
    }

    private void ClearPanels()
    {
        App1NameLabel.Text = "--"; App1TimeLabel.Text = "-- minuter";
        App2NameLabel.Text = "--"; App2TimeLabel.Text = "-- minuter";
        App3NameLabel.Text = "--"; App3TimeLabel.Text = "-- minuter";
        App4NameLabel.Text = "--"; App4TimeLabel.Text = "-- minuter";
        App5NameLabel.Text = "--"; App5TimeLabel.Text = "-- minuter";
    }
}
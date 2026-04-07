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

		// Hämta aktuella processer och logga dem först
		var processes = _processService.GetUserApplications();
		await LogProcessesToDatabase(processes);

		// Hämta alla processloggar från databasen
		var allProcessLogs = await _dbContext.GetProcessLogsAsync();

		// Gruppera per app och beräkna total tid
		var appUsageStats = allProcessLogs
			.GroupBy(p => p.AppName)
			.Select(g => new
			{
				AppName = g.Key,
				TotalMinutes = g.Sum(p =>
				{
					var duration = p.EndTime - p.StartTime;
					return duration.TotalMinutes;
				})
			})
			.OrderByDescending(a => a.TotalMinutes)
			.Take(5)
			.ToList();

		
		if (appUsageStats.Count > 0)
		{
			App1NameLabel.Text = appUsageStats[0].AppName;
			App1TimeLabel.Text = $"{Math.Round(appUsageStats[0].TotalMinutes, 1)} minuter";
		}
		else
		{
			App1NameLabel.Text = "--";
			App1TimeLabel.Text = "-- minuter";
		}

		if (appUsageStats.Count > 1)
		{
			App2NameLabel.Text = appUsageStats[1].AppName;
			App2TimeLabel.Text = $"{Math.Round(appUsageStats[1].TotalMinutes, 1)} minuter";
		}
		else
		{
			App2NameLabel.Text = "--";
			App2TimeLabel.Text = "-- minuter";
		}

		if (appUsageStats.Count > 2)
		{
			App3NameLabel.Text = appUsageStats[2].AppName;
			App3TimeLabel.Text = $"{Math.Round(appUsageStats[2].TotalMinutes, 1)} minuter";
		}
		else
		{
			App3NameLabel.Text = "--";
			App3TimeLabel.Text = "-- minuter";
		}

		if (appUsageStats.Count > 3)
		{
			App4NameLabel.Text = appUsageStats[3].AppName;
			App4TimeLabel.Text = $"{Math.Round(appUsageStats[3].TotalMinutes, 1)} minuter";
		}
		else
		{
			App4NameLabel.Text = "--";
			App4TimeLabel.Text = "-- minuter";
		}

		if (appUsageStats.Count > 4)
		{
			App5NameLabel.Text = appUsageStats[4].AppName;
			App5TimeLabel.Text = $"{Math.Round(appUsageStats[4].TotalMinutes, 1)} minuter";
		}
		else
		{
			App5NameLabel.Text = "--";
			App5TimeLabel.Text = "-- minuter";
		}
	}

	private async Task LogProcessesToDatabase(List<ProcessItem> processes)
	{
		var currentProcessIds = new HashSet<int>();

		foreach (var process in processes)
		{
			currentProcessIds.Add(process.Id);

			
			if (!_trackedProcesses.ContainsKey(process.Id))
			{
				_trackedProcesses[process.Id] = process.StartTime;
			}
		}

		
		var closedProcesses = _trackedProcesses.Keys
			.Where(id => !currentProcessIds.Contains(id))
			.ToList();

		// Logga avslutade processer
		foreach (var closedProcessId in closedProcesses)
		{
			try
			{
				var processLog = new ProcessLoggingModel
				{
					AppName = $"Process {closedProcessId}", 
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

		
		foreach (var process in processes)
		{
			try
			{
				var processLog = new ProcessLoggingModel
				{
					AppName = process.Name,
					AppId = process.Id,
					StartTime = _trackedProcesses[process.Id],
					EndTime = DateTime.Now 
				};

				await _dbContext.SaveProcessLogAsync(processLog);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"Kunde inte logga process {process.Name}: {ex.Message}");
			}
		}
	}
}
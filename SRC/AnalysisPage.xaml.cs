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

	private async void OnRefreshProcessesClicked(object sender, EventArgs e)
	{
		var processes = _processService.GetUserApplications();

		ProcessList.ItemsSource = processes;

		// Logga processer till databasen
		await LogProcessesToDatabase(processes);

		await DisplayAlert("Processer loggade", $"{processes.Count} användarappar har sparats i databasen.", "OK");
	}

	private async Task LogProcessesToDatabase(List<ProcessItem> processes)
	{
		var currentProcessIds = new HashSet<int>();

		foreach (var process in processes)
		{
			currentProcessIds.Add(process.Id);

			// Om vi inte redan spårar denna process, lägg till den
			if (!_trackedProcesses.ContainsKey(process.Id))
			{
				_trackedProcesses[process.Id] = process.StartTime;
			}
		}

		// Hitta processer som har stängts (fanns förut men inte nu)
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
					AppName = $"Process {closedProcessId}", // Vi har inte namnet längre
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

		// Logga aktiva processer (spara aktuell session)
		foreach (var process in processes)
		{
			try
			{
				var processLog = new ProcessLoggingModel
				{
					AppName = process.Name,
					AppId = process.Id,
					StartTime = _trackedProcesses[process.Id],
					EndTime = DateTime.Now // Uppdateras varje gång
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
using PlannerApp.Backend;
using PlannerApp.DB;

namespace PlannerApp;

public partial class AnalysisPage : ContentPage
{
	private readonly ProcessService _processService = new ProcessService();
	private readonly dbContext _dbContext;

    public AnalysisPage(dbContext database)
	{
		InitializeComponent();
		_dbContext = database;
	}

	private void OnRefreshProcessesClicked(object sender, EventArgs e)
	{
		var processes = _processService.GetCurrentProcesses();

		ProcessList.ItemsSource = processes;
    }
}
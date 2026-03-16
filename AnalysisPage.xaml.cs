using PlannerApp.Backend;

namespace PlannerApp;

public partial class AnalysisPage : ContentPage
{
	private readonly ProcessService _processService = new ProcessService();

    public AnalysisPage()
	{
		InitializeComponent();
	}

	private void OnRefreshProcessesClicked(object sender, EventArgs e)
	{
		var processes = _processService.GetCurrentProcesses();

		ProcessList.ItemsSource = processes;
    }
}
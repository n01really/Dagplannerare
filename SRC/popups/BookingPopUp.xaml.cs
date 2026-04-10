using CommunityToolkit.Maui.Views;
using PlannerApp.SRC.Backend;
using PlannerApp.SRC.Models;

namespace PlannerApp.popups;

public partial class BookingPopUp : Popup
{
    public BookingPopUp(DateTime selectedTime, List<AppsModels> apps)
    {
        InitializeComponent();
        SelectedTimeLabel.Text = $"{selectedTime:yyyy-MM-dd HH:mm}";
        AppPicker.ItemsSource = apps;
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {
        if (AppPicker.SelectedItem == null)
        {
            return;
        }
        Close();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close();
    }
}


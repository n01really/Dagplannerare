using CommunityToolkit.Maui.Views;

namespace PlannerApp.popups;

public partial class BookingPopUp : Popup
{
    public BookingPopUp(DateTime selectedTime)
    {
        InitializeComponent();
        SelectedTimeLabel.Text = $"{selectedTime:yyyy-MM-dd HH:mm}";
    }

    private void OnSaveClicked(object sender, EventArgs e)
    {

        Close();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close();
    }
}


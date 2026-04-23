using CommunityToolkit.Maui.Views;
using PlannerApp.SRC.Backend;
using PlannerApp.SRC.Models;
using PlannerApp.SRC.DB;
using Microsoft.Maui.Controls.Shapes;
using System.Diagnostics;

namespace PlannerApp.popups;

public partial class BookingPopUp : Popup
{
    private readonly DateTime _selectedDate;
    private readonly dbContext _dbContext;
    private string _selectedColor = "#2196F3";
    
    private readonly List<string> _availableColors = new List<string>
    {
        "#2196F3", "#4CAF50", "#FFC107", "#FF5722", "#9C27B0",
        "#F44336", "#00BCD4", "#FF9800", "#795548", "#607D8B"
    };

    public BookingPopUp(DateTime selectedTime, List<AppsModels> apps, dbContext dbContext)
    {
        InitializeComponent();
        
        _selectedDate = selectedTime;
        _dbContext = dbContext;
        
        SelectedTimeLabel.Text = $"{selectedTime:yyyy-MM-dd HH:mm}";
        StartTimePicker.Time = selectedTime.TimeOfDay;
        EndTimePicker.Time = selectedTime.AddHours(1).TimeOfDay;
        
        AppPicker.ItemsSource = apps;
        
        CreateColorPalette();
    }

    private void CreateColorPalette()
    {
        foreach (var color in _availableColors)
        {
            var colorButton = new Border
            {
                BackgroundColor = Color.FromArgb(color),
                WidthRequest = 50,
                HeightRequest = 50,
                Margin = 5,
                StrokeThickness = 3,
                Stroke = color == _selectedColor ? Colors.Black : Colors.Transparent
            };
            
            colorButton.StrokeShape = new RoundRectangle { CornerRadius = 25 };
            
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => OnColorSelected(color);
            colorButton.GestureRecognizers.Add(tapGesture);
            
            ColorPalette.Children.Add(colorButton);
        }
    }

    private void OnColorSelected(string color)
    {
        _selectedColor = color;
        
        foreach (var child in ColorPalette.Children)
        {
            if (child is Border border)
            {
                var borderColor = border.BackgroundColor.ToArgbHex();
                border.Stroke = borderColor == color ? Colors.Black : Colors.Transparent;
                border.StrokeThickness = borderColor == color ? 3 : 2;
            }
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (AppPicker.SelectedItem == null && string.IsNullOrWhiteSpace(TitleEntry.Text))
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Fel", "Välj en app eller skriv en titel", "OK");
            }
            return;
        }

        var startTime = _selectedDate.Date.Add(StartTimePicker.Time);
        var endTime = _selectedDate.Date.Add(EndTimePicker.Time);

        if (endTime <= startTime)
        {
            if (Shell.Current?.CurrentPage != null)
            {
                await Shell.Current.CurrentPage.DisplayAlert("Fel", "Sluttid måste vara efter starttid", "OK");
            }
            return;
        }

        var title = TitleEntry.Text;
        var appName = "";
        var appId = 0;

        if (AppPicker.SelectedItem != null)
        {
            var selectedApp = (AppsModels)AppPicker.SelectedItem;
            if (string.IsNullOrWhiteSpace(title))
            {
                title = selectedApp.Name;
            }
            // FIXED: Spara hela sökvägen till programmet, inte bara namnet
            appName = selectedApp.Path;  // Path innehåller fullständig sökväg
            appId = selectedApp.Id;
            
            Debug.WriteLine($"Schemalagt program: {title} ({appName}) för {startTime}");
        }

        var schedual = new SchedualModel
        {
            Title = title,
            AppName = appName,
            AppId = appId,
            DateTime = _selectedDate.Date,
            StartTime = startTime,
            EndTime = endTime,
            Description = DescriptionEditor.Text ?? "",
            BackgroundColor = _selectedColor,
            IsRunning = false  // FIXED: Sätt explicit till false så programmet kan köras
        };

        await _dbContext.SaveSchedualAsync(schedual);
        
        Debug.WriteLine($"Schemat sparat - IsRunning: {schedual.IsRunning}, AppId: {schedual.AppId}");
        
        Close(schedual);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close();
    }
}


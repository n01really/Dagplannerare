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

    private readonly SchedualModel? _existingBooking;
    
    private readonly List<string> _availableColors = new List<string>
    {
        "#2196F3", "#4CAF50", "#FFC107", "#FF5722", "#9C27B0",
        "#F44336", "#00BCD4", "#FF9800", "#795548", "#607D8B"
    };

    public BookingPopUp(DateTime selectedTime, List<AppsModels> apps, dbContext dbContext, SchedualModel? existingBooking = null)
    {
        InitializeComponent();
        
        _selectedDate = selectedTime;
        _dbContext = dbContext;
        _existingBooking = existingBooking;
        
        SelectedTimeLabel.Text = $"{selectedTime:yyyy-MM-dd HH:mm}";
        StartTimePicker.Time = selectedTime.TimeOfDay;
        EndTimePicker.Time = selectedTime.AddHours(1).TimeOfDay;
        
        AppPicker.ItemsSource = apps;
        
        CreateColorPalette();

        if (_existingBooking != null)
        {
            
            SelectedTimeLabel.Text = $"{_existingBooking.DateTime:yyyy-MM-dd} (Redigerar)";
            TitleEntry.Text = _existingBooking.Title;
            DescriptionEditor.Text = _existingBooking.Description;
            StartTimePicker.Time = _existingBooking.StartTime.TimeOfDay;
            EndTimePicker.Time = _existingBooking.EndTime.TimeOfDay;
            _selectedColor = _existingBooking.BackgroundColor ?? "#2196F3";

            // Välj rätt app i pickern om det finns en sparad
            if (apps != null && _existingBooking.AppId != 0)
            {
                AppPicker.SelectedItem = apps.FirstOrDefault(a => a.Id == _existingBooking.AppId);
            }
        }
        else
        {
            // Standardläge (Skapa ny)
            SelectedTimeLabel.Text = $"{selectedTime:yyyy-MM-dd HH:mm}";
            StartTimePicker.Time = selectedTime.TimeOfDay;
            EndTimePicker.Time = selectedTime.AddHours(1).TimeOfDay;
        }
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
            
            appName = selectedApp.Path;  
            appId = selectedApp.Id;
            
            Debug.WriteLine($"Schemalagt program: {title} ({appName}) för {startTime}");
        }

        SchedualModel schedual;
        if (_existingBooking != null)
        {
            schedual = _existingBooking; // Använd samma ID och objekt
            schedual.Title = title;
            schedual.AppName = appName;
            schedual.AppId = appId;
            schedual.StartTime = startTime;
            schedual.EndTime = endTime;
            schedual.Description = DescriptionEditor.Text ?? "";
            schedual.BackgroundColor = _selectedColor;

            // Använd din dbContext-metod för att uppdatera
            await _dbContext.UpdateScheduleAsync(schedual);
        }
        else
        {
            schedual = new SchedualModel
            {
                Title = title,
                AppName = appName,
                AppId = appId,
                DateTime = _selectedDate.Date,
                StartTime = startTime,
                EndTime = endTime,
                Description = DescriptionEditor.Text ?? "",
                BackgroundColor = _selectedColor,
                IsRunning = false
            };

            await _dbContext.SaveSchedualAsync(schedual);
        }

        Close(schedual);
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close();
    }
}


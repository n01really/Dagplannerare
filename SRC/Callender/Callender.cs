using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.Maui.Toolkit.Calendar;
using Microsoft.Maui.Controls.Shapes;
using PlannerApp.SRC.DB;
using PlannerApp.SRC.Models;

namespace PlannerApp.SRC.Callender
{
    // Kalender-komponent som visar en månadvy med integrerade timmar i varje dag.
    // Använder Syncfusion.Maui.Toolkit.Calendar för kalendervisning.
    
    internal class Callender
    {
        #region Properties

        
        // Syncfusion kalenderkontroll som visar dagarna
        
        public SfCalendar Calendar { get; set; }

        
        // Event som triggas när användaren väljer en ny dag i kalendern
        public event EventHandler<DateTime>? DateSelected;

        // Event som triggas när användaren klickar på en specifik timme
        
        public event EventHandler<DateTime>? HourSelected;

        private dbContext? _dbContext;
        private List<WeatherLoggingModel> _weatherLogs = new List<WeatherLoggingModel>();
        private List<SchedualModel> _scheduals = new List<SchedualModel>();

        #endregion

        #region Constructor

        // Initierar kalendern med standardinställningar
        
        public Callender()
        {
            // Konfigurerar huvudkalenderkontrollen
            Calendar = new SfCalendar
            {
                View = CalendarView.Month,                    // Visar månadvy
                SelectionMode = CalendarSelectionMode.Single, // Endast en dag kan väljas åt gången
                SelectedDate = DateTime.Today,                // Dagens datum är förvalt
                EnablePastDates = false,                      // Förhindrar val av tidigare datum
                ShowTrailingAndLeadingDates = false,          // Visar endast aktuell månads dagar
                SelectionShape = CalendarSelectionShape.Rectangle, // Rektangulär markering av vald dag
                SelectionBackground = new SolidColorBrush(Color.FromRgba(33, 150, 243, 0.5)),
                MonthView = new CalendarMonthView
                {
                    NumberOfVisibleWeeks = 1,                 // Visar endast en vecka i taget
                    FirstDayOfWeek = DateTime.Today.DayOfWeek, // Startar veckan med dagens veckodag
                    CellTemplate = new DataTemplate(() => CreateDayCellWithHours()) // Custom mall för varje dag-cell
                },
            };

            // Sätter dagens datum som valt
            Calendar.SelectedDate = DateTime.Today;

            // Lyssnar på när användaren väljer ett nytt datum
            Calendar.SelectionChanged += OnSelectionChanged;
        }

        #endregion

        #region Public DB methods


        // Sätter databas-konteksten som komponenten använder för att läsa in loggade vädervärden.
        // Anropa detta från Page/DI efter att dbContext finns tillgänglig.

        public void SetDatabase(dbContext database)
        {
            _dbContext = database;
            _ = RefreshWeatherLogsAsync();
            _ = RefreshSchedualsAsync();
        }

       
        // Laddar om alla väderloggar från databasen till minnet.

        public async Task RefreshWeatherLogsAsync()
        {
            if (_dbContext == null)
            {
                _weatherLogs = new List<WeatherLoggingModel>();
                return;
            }

            try
            {
                _weatherLogs = await _dbContext.GetWeatherLogsAsync() ?? new List<WeatherLoggingModel>();
            }
            catch
            {
                _weatherLogs = new List<WeatherLoggingModel>();
            }
        }

        public async Task RefreshSchedualsAsync()
        {
            if (_dbContext == null)
            {
                _scheduals = new List<SchedualModel>();
                return;
            }

            try
            {
                _scheduals = await _dbContext.GetSchedualsAsync() ?? new List<SchedualModel>();
                RefreshCalendarView();
            }
            catch
            {
                _scheduals = new List<SchedualModel>();
            }
        }

        private void RefreshCalendarView()
        {
            if (Calendar == null || Calendar.MonthView == null) return;

            // Eftersom databasen laddas asynkront måste vi se till att UI-uppdateringen 
            // sker på appens huvudtråd (UI-tråden)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Genom att sätta CellTemplate till null och sedan ge den en ny DataTemplate
                    // tvingar vi Syncfusion att rensa sin cache och bygga om alla celler från grunden.
                    Calendar.MonthView.CellTemplate = null;
                    Calendar.MonthView.CellTemplate = new DataTemplate(() => CreateDayCellWithHours());
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Fel vid uppdatering av kalendervy: {ex.Message}");
                }
            });
        }


        #endregion

        #region Private Methods


        // Skapar en custom cell för varje dag i kalendern.
        // Varje cell innehåller datumnumret och alla 24 timmar som scrollbara element.
        // Visar eventuellt temperaturvärde för varje timme från _weatherLogs.

        // som representerar innehållet i en dag-cell
        private View CreateDayCellWithHours()
        {
            // LÖSNING: Använd Grid istället för VerticalStackLayout för att tvinga fram en begränsad höjd
            var mainLayout = new Grid
            {
                RowDefinitions = new RowDefinitionCollection
        {
            new RowDefinition { Height = GridLength.Auto }, // För datum-etiketten
            new RowDefinition { Height = GridLength.Star }  // Tar resten av platsen och aktiverar scroll
        },
                RowSpacing = 5,
                Padding = new Thickness(2)
            };

            // Label som visar dagens nummer (1-31)
            var dateLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                TextColor = Colors.Black
            };
            dateLabel.SetBinding(Label.TextProperty, "Date.Day");

            // Placera datumet på rad 0 i gridden
            Grid.SetRow(dateLabel, 0);
            mainLayout.Add(dateLabel);

            // Layout som innehåller alla timmar
            var hoursLayout = new VerticalStackLayout
            {
                Spacing = 6
            };

            // Skapar 24 timmar (00:00 - 23:00)
            for (int hour = 0; hour < 24; hour++)
            {
                int capturedHour = hour;

                var hourContainer = new VerticalStackLayout
                {
                    Spacing = 0,
                    Padding = new Thickness(5, 2)
                };

                var hourLabel = new Label
                {
                    Text = $"{capturedHour:D2}:00",
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black,
                    Padding = new Thickness(5)
                };

                var tempLabel = new Label
                {
                    Text = string.Empty,
                    FontSize = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Gray,
                };

                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += (s, e) =>
                {
                    if (s is Label || s is VisualElement)
                    {
                        var bindingContext = hourLabel.BindingContext;
                        if (bindingContext is CalendarCellDetails cellDetails)
                        {
                            DateTime selectedHour = new DateTime(
                                cellDetails.Date.Year,
                                cellDetails.Date.Month,
                                cellDetails.Date.Day,
                                capturedHour, 0, 0);

                            HourSelected?.Invoke(this, selectedHour);
                        }
                    }
                };

                hourLabel.InputTransparent = true;
                tempLabel.InputTransparent = true;

                hourContainer.GestureRecognizers.Add(tapGesture);
                hourContainer.Add(hourLabel);
                hourContainer.Add(tempLabel);
                hoursLayout.Add(hourContainer);
            }

            // ScrollView gör att användaren kan scrolla genom alla 24 timmar
            var scrollView = new ScrollView
            {
                Content = hoursLayout,
                Orientation = ScrollOrientation.Vertical,
                VerticalScrollBarVisibility = ScrollBarVisibility.Always
            };

            // Placera ScrollView på rad 1 så den tvingas att hålla sig inom cellen
            Grid.SetRow(scrollView, 1);
            mainLayout.Add(scrollView);

            // Hanterar uppdatering av färg och bokningar/väder (Behålls exakt som du skrivit)
            mainLayout.BindingContextChanged += async (s, e) =>
            {
                if (mainLayout.BindingContext is not CalendarCellDetails cellDetails)
                    return;

                if ((_weatherLogs == null || !_weatherLogs.Any()) && _dbContext != null)
                {
                    await RefreshWeatherLogsAsync();
                }

                if ((_scheduals == null || !_scheduals.Any()) && _dbContext != null)
                {
                    await RefreshSchedualsAsync();
                }

                for (int i = 0; i < hoursLayout.Children.Count; i++)
                {
                    if (hoursLayout.Children[i] is VerticalStackLayout hourContainer && hourContainer.Children.Count >= 2)
                    {
                        var tempLabel = hourContainer.Children[1] as Label;
                        if (tempLabel == null) continue;

                        var currentHour = new DateTime(cellDetails.Date.Year, cellDetails.Date.Month, cellDetails.Date.Day, i, 0, 0);

                        var booking = _scheduals?.FirstOrDefault(s =>
                            currentHour >= s.StartTime && currentHour < s.EndTime);

                        if (booking != null)
                        {
                            hourContainer.BackgroundColor = Color.FromArgb(booking.BackgroundColor ?? "#2196F3");
                            tempLabel.Text = booking.Title ?? "";
                            tempLabel.TextColor = Colors.White;
                            tempLabel.FontAttributes = FontAttributes.Bold;
                        }
                        else
                        {
                            hourContainer.BackgroundColor = Colors.Transparent;
                            var log = _weatherLogs?.FirstOrDefault(w =>
                                w.DateTime.Date == cellDetails.Date.Date &&
                                w.DateTime.Hour == i);

                            if (log != null)
                            {
                                tempLabel.Text = $"{System.Math.Round(log.Temperature, 1)}°C";
                                tempLabel.TextColor = Colors.Gray;
                                tempLabel.FontAttributes = FontAttributes.None;
                            }
                            else
                            {
                                tempLabel.Text = string.Empty;
                            }
                        }

                        hourContainer.BindingContext = cellDetails;
                        if (hourContainer.Children[0] is VisualElement ve) ve.BindingContext = cellDetails;
                        if (hourContainer.Children[1] is VisualElement ve2) ve2.BindingContext = cellDetails;
                    }
                }
            };

            return mainLayout;
        }


        // Hanterar när användaren väljer ett nytt datum i kalendern

        private void OnSelectionChanged(object? sender, CalendarSelectionChangedEventArgs e)
        {
            if (e.NewValue != null)
            {
                DateTime selectedDate = (DateTime)e.NewValue;
                // Triggar DateSelected-eventet som andra komponenter kan lyssna på
                DateSelected?.Invoke(this, selectedDate);
            }
        }

        #endregion

        #region Public Methods

        
        // Sätter vilket datumintervall som är tillgängligt i kalendern.
        /// Användbart för att begränsa kalendern till t.ex. en specifik månad eller tidsperiod.
        
        
        public void SetDateRange(DateTime minDate, DateTime maxDate)
        {
            Calendar.MinimumDate = minDate;
            Calendar.MaximumDate = maxDate;
        }

        
        // Sätter vilket datum som ska vara valt i kalendern programmatiskt
        
        public void SetSelectedDate(DateTime date)
        {
            Calendar.SelectedDate = date;
        }

        #endregion
    }
}
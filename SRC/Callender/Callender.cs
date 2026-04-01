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

        #endregion

        #region Private Methods


        // Skapar en custom cell för varje dag i kalendern.
        // Varje cell innehåller datumnumret och alla 24 timmar som scrollbara element.
        // Visar eventuellt temperaturvärde för varje timme från _weatherLogs.

        // som representerar innehållet i en dag-cell
        private View CreateDayCellWithHours()
        {
            // Huvudlayout för dag-cellen
            var mainLayout = new VerticalStackLayout
            {
                Spacing = 5,              // Mellanrum mellan datumnummer och timmar
                Padding = new Thickness(2) // Padding runt innehållet
            };

            // Label som visar dagens nummer (1-31)
            var dateLabel = new Label
            {
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold, // Fet text för bättre synlighet
                FontSize = 14,
                TextColor = Colors.Black              // Svart färg för tydlig läsbarhet
            };
            // Binder till Date.Day från CalendarCellDetails (kommer från Syncfusion)
            dateLabel.SetBinding(Label.TextProperty, "Date.Day");
            mainLayout.Add(dateLabel);

            // Layout som innehåller alla timmar
            var hoursLayout = new VerticalStackLayout
            {
                Spacing = 6  // mindre mellanrum för bättre kompakt vy i cell
            };

            // Skapar 24 timmar (00:00 - 23:00)
            for (int hour = 0; hour < 24; hour++)
            {
                int capturedHour = hour; // Viktigt: sparar för closure

                // Container för timmen (innehåller timtext och temptext)
                var hourContainer = new VerticalStackLayout
                {
                    Spacing = 0,
                    Padding = new Thickness(0)
                };

                // Label för varje timme
                var hourLabel = new Label
                {
                    Text = $"{capturedHour:D2}:00",           // Formaterar som 00:00, 01:00, etc.
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black,          // Svart färg för tydlig läsbarhet
                    Padding = new Thickness(5)         // Padding för större klickyta
                };

                // Label för eventuellt temperaturvärde
                var tempLabel = new Label
                {
                    Text = string.Empty,
                    FontSize = 10,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Gray,
                };

                // Lägger till tap-gesture för att göra timmen klickbar
                var tapGesture = new TapGestureRecognizer();

                // Event handler när användaren klickar på en timme
                tapGesture.Tapped += (s, e) =>
                {
                    // Kontrollerar att vi har rätt context
                    // BindingContext på cellen blir en CalendarCellDetails och ärvs av barn
                    if (s is Label || s is VisualElement)
                    {
                        // Vi tar BindingContext från hourLabel då den är barn i cellens trädkedja
                        var bindingContext = hourLabel.BindingContext;
                        if (bindingContext is CalendarCellDetails cellDetails)
                        {
                            // Skapar en DateTime med valt datum och timme
                            DateTime selectedHour = new DateTime(
                                cellDetails.Date.Year,
                                cellDetails.Date.Month,
                                cellDetails.Date.Day,
                                capturedHour, 0, 0);

                            // Triggar HourSelected-eventet som MainPage kan lyssna på
                            HourSelected?.Invoke(this, selectedHour);
                        }
                    }
                };

                hourLabel.GestureRecognizers.Add(tapGesture);

                hourContainer.Add(hourLabel);
                hourContainer.Add(tempLabel);

                // Spara hourContainer så vi senare kan uppdatera tempLabel när BindingContext ändras
                hoursLayout.Add(hourContainer);
            }

            // ScrollView gör att användaren kan scrolla genom alla 24 timmar
            var scrollView = new ScrollView
            {
                Content = hoursLayout,
                Orientation = ScrollOrientation.Vertical,      // Vertikal scrollning
                VerticalScrollBarVisibility = ScrollBarVisibility.Always // Visar alltid scrollbar
            };

            mainLayout.Add(scrollView);

            // När cellens BindingContext sätts (CalendarCellDetails) uppdatera temperaturvisningen
            mainLayout.BindingContextChanged += async (s, e) =>
            {
                if (mainLayout.BindingContext is not CalendarCellDetails cellDetails)
                    return;

                // Se till att vi har weather-logs i minnet
                if ((_weatherLogs == null || !_weatherLogs.Any()) && _dbContext != null)
                {
                    await RefreshWeatherLogsAsync();
                }

                // För varje tim-kontainer uppdatera tempLabel enligt loggarna
                for (int i = 0; i < hoursLayout.Children.Count; i++)
                {
                    if (hoursLayout.Children[i] is VerticalStackLayout hourContainer && hourContainer.Children.Count >= 2)
                    {
                        var tempLabel = hourContainer.Children[1] as Label;
                        if (tempLabel == null) continue;

                        var log = _weatherLogs?.FirstOrDefault(w =>
                            w.DateTime.Date == cellDetails.Date.Date &&
                            w.DateTime.Hour == i);

                        if (log != null)
                        {
                            tempLabel.Text = $"{System.Math.Round(log.Temperature, 1)}°C";
                        }
                        else
                        {
                            tempLabel.Text = string.Empty;
                        }

                        // Se till att childernas BindingContext är samma som cellens så att tapp fungerar
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
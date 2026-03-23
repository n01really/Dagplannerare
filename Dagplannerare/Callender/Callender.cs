using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.Maui.Toolkit.Calendar;
using Microsoft.Maui.Controls.Shapes; 

namespace PlannerApp.Callender
{
    /// <summary>
    /// Kalender-komponent som visar en månadvy med integrerade timmar i varje dag.
    /// Använder Syncfusion.Maui.Toolkit.Calendar för kalendervisning.
    /// </summary>
    internal class Callender
    {
        #region Properties
        
        /// <summary>
        /// Syncfusion kalenderkontroll som visar dagarna
        /// </summary>
        public SfCalendar Calendar { get; set; }
        
        /// <summary>
        /// Event som triggas när användaren väljer en ny dag i kalendern
        /// </summary>
        public event EventHandler<DateTime>? DateSelected;
        
        /// <summary>
        /// Event som triggas när användaren klickar på en specifik timme
        /// </summary>
        public event EventHandler<DateTime>? HourSelected;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initierar kalendern med standardinställningar
        /// </summary>
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
        
        #region Private Methods
        
        /// <summary>
        /// Skapar en custom cell för varje dag i kalendern.
        /// Varje cell innehåller datumnumret och alla 24 timmar som scrollbara element.
        /// </summary>
        /// <returns>View som representerar innehållet i en dag-cell</returns>
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
                Spacing = 30  // 3cm mellanrum mellan timmarna (ca 30 enheter)
            };
            
            // Skapar 24 timmar (00:00 - 23:00)
            for (int hour = 0; hour < 24; hour++)
            {
                // Label för varje timme
                var hourLabel = new Label
                {
                    Text = $"{hour:D2}:00",           // Formaterar som 00:00, 01:00, etc.
                    FontSize = 12,
                    HorizontalOptions = LayoutOptions.Center,
                    TextColor = Colors.Black,          // Svart färg för tydlig läsbarhet
                    Padding = new Thickness(5)         // Padding för större klickyta
                };
                
                // Lägger till tap-gesture för att göra timmen klickbar
                var tapGesture = new TapGestureRecognizer();
                int capturedHour = hour; // Viktigt: Sparar värdet i closure för korrekt referens
                
                // Event handler när användaren klickar på en timme
                tapGesture.Tapped += (s, e) =>
                {
                    // Kontrollerar att vi har rätt context
                    if (s is Label label && label.BindingContext is CalendarCellDetails cellDetails)
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
                };
                hourLabel.GestureRecognizers.Add(tapGesture);
                
                hoursLayout.Add(hourLabel);
            }
            
            // ScrollView gör att användaren kan scrolla genom alla 24 timmar
            var scrollView = new ScrollView
            {
                Content = hoursLayout,
                Orientation = ScrollOrientation.Vertical,      // Vertikal scrollning
                VerticalScrollBarVisibility = ScrollBarVisibility.Always // Visar alltid scrollbar
            };
            
            mainLayout.Add(scrollView);
            
            return mainLayout;
        }
        
        /// <summary>
        /// Hanterar när användaren väljer ett nytt datum i kalendern
        /// </summary>
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
        
        /// <summary>
        /// Sätter vilket datumintervall som är tillgängligt i kalendern.
        /// Användbart för att begränsa kalendern till t.ex. en specifik månad eller tidsperiod.
        /// </summary>
        /// <param name="minDate">Tidigaste tillåtna datum</param>
        /// <param name="maxDate">Senaste tillåtna datum</param>
        public void SetDateRange(DateTime minDate, DateTime maxDate)
        {
            Calendar.MinimumDate = minDate;
            Calendar.MaximumDate = maxDate;
        }
        
        /// <summary>
        /// Sätter vilket datum som ska vara valt i kalendern programmatiskt
        /// </summary>
        /// <param name="date">Datumet som ska väljas</param>
        public void SetSelectedDate(DateTime date) 
        { 
            Calendar.SelectedDate = date;
        }
        
        #endregion
    }
}

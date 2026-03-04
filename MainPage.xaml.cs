using PlannerApp.Callender;

namespace PlannerApp
{
    /// <summary>
    /// Huvudsida för planeringsappen som visar kalendern med timmar
    /// </summary>
    public partial class MainPage : ContentPage
    {
        #region Fields
        
        /// <summary>
        /// Instans av kalenderkomponenten som hanterar visning och interaktion
        /// </summary>
        private Callender.Callender _calender;
        
        #endregion
        
        #region Constructor
        
        /// <summary>
        /// Initierar MainPage och konfigurerar kalendern
        /// </summary>
        public MainPage()
        {
            InitializeComponent();
            
            // Skapar en ny instans av kalenderkomponenten
            _calender = new Callender.Callender();

            // Sätter kalendern som innehåll i CalenderContainer (definierad i MainPage.xaml)
            CalenderContainer.Content = _calender.Calendar;
            
            // Prenumererar på HourSelected-eventet för att hantera när användaren väljer en timme
            // Detta event triggas när användaren klickar på en specifik timme i kalendern
            _calender.HourSelected += (sender, dateTime) =>
            {
                // Visar en bekräftelsedialog med valt datum och tid
                // dateTime innehåller både datum och timme användaren valde
                // Format: yyyy-MM-dd HH:mm (t.ex. 2024-01-15 14:00)
                DisplayAlert("Timme vald", $"Du valde: {dateTime:yyyy-MM-dd HH:mm}", "OK");
                
                // TODO: Här kan du lägga till funktionalitet för att:
                // - Spara en bokning/aktivitet för vald tid
                // - Navigera till en ny sida för att skapa en händelse
                // - Visa befintliga händelser för vald timme
                // - Uppdatera en lista med valda tider
            };
            
            // Du kan också prenumerera på DateSelected om du vill hantera dagval separat:
            // _calender.DateSelected += (sender, dateTime) =>
            // {
            //     // Kod som körs när användaren väljer en ny dag i kalendern
            // };
        }
        
        #endregion
        
        // Tips för vidareutveckling:
        // 1. Lägg till databas för att spara bokningar/aktiviteter
        // 2. Skapa en ny sida för att visa detaljer när en timme väljs
        // 3. Lägg till visuella markeringar för bokade timmar
        // 4. Implementera notifikationer för kommande händelser
        // 5. Lägg till möjlighet att välja flera timmar för en bokning
    }
}

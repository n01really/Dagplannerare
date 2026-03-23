using PlannerApp.popups;
using CommunityToolkit.Maui.Views;

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
            _calender.HourSelected += async (sender, dateTime) =>
            {
                var popup = new BookingPopUp(dateTime);
                var result = await this.ShowPopupAsync(popup);
                
            };
        }
        
        #endregion
    }
}

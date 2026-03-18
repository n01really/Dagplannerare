using PlannerApp.popups;
using CommunityToolkit.Maui.Views;
using PlannerApp.DB;

namespace PlannerApp
{

    // Huvudsida för planeringsappen som visar kalendern med timmar

    public partial class MainPage : ContentPage
    {
        #region Fields
        

        // Instans av kalenderkomponenten som hanterar visning och interaktion

        private Callender.Callender _calender;
        
        #endregion
        
        #region Constructor

        private readonly dbContext _dbContext;


        // Initierar MainPage och konfigurerar kalendern

        public MainPage(dbContext database)
        {
            InitializeComponent();
            _dbContext = database;
            
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

using PlannerApp.popups;
using CommunityToolkit.Maui.Views;
using PlannerApp.SRC.Callender;
using PlannerApp.SRC.DB;
using Microsoft.Maui.ApplicationModel;
using PlannerApp.SRC.Backend;

namespace PlannerApp
{

    // Huvudsida för planeringsappen som visar kalendern med timmar

    public partial class MainPage : ContentPage
    {
        #region Fields
        

        // Instans av kalenderkomponenten som hanterar visning och interaktion

        private SRC.Callender.Callender _calender;
        private readonly AppService _appService;

        #endregion

        #region Constructor

        private readonly dbContext _dbContext;


        // Initierar MainPage och konfigurerar kalendern

        public MainPage(dbContext database, AppService appService)
        {
            InitializeComponent();
            _dbContext = database;
            _appService = appService;
            // Skapar en ny instans av kalenderkomponenten
            _calender = new SRC.Callender.Callender();

            // Sätter kalendern som innehåll i CalenderContainer (definierad i MainPage.xaml)
            CalenderContainer.Content = _calender.Calendar;

            // Sätt databaskontext så Callender kan läsa väderloggar (startar intern laddning)
            _calender.SetDatabase(_dbContext);
            
            // Prenumererar på HourSelected-eventet för att hantera när användaren väljer en timme
            _calender.HourSelected += async (sender, dateTime) =>
            {
                var installedApps = _appService.GetInstalledApps();
                var popup = new BookingPopUp(dateTime, installedApps);
                var result = await this.ShowPopupAsync(popup);
                
            };
        }
        
        #endregion

        #region Lifecycle

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Säkerställ att väderloggar är laddade innan kalendern visas
            // SetDatabase kallas i konstruktorn men vi väntar här in laddningen så UI visar data direkt.
            if (_calender != null)
            {
                await _calender.RefreshWeatherLogsAsync();

                // Tvinga layoutuppdatering så cellerna binder om och visar temperaturer
                try
                {
                    _calender.Calendar.InvalidateMeasure();
                }
                catch
                {
                    // Ignore if control doesn't support it
                }
            }
        }

        #endregion
    }
}

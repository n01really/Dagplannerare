using CommunityToolkit.Maui.Views;
using PlannerApp.SRC.Callender;
using PlannerApp.SRC.DB;
using Microsoft.Maui.ApplicationModel;
using PlannerApp.SRC.Backend;
using PlannerApp.SRC.Models;
using PlannerApp.popups;

namespace PlannerApp
{

    // Huvudsida för planeringsappen som visar kalendern med timmar

    public partial class MainPage : ContentPage
    {
        #region Fields
        

        // Instans av kalenderkomponenten som hanterar visning och interaktion

        private SRC.Callender.Callender _calender;
        private readonly AppService _appService;
        private readonly dbContext _dbContext;
        private readonly LaunchService _launchService;

        #endregion

        #region Constructor


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

            // FIXED: Spara LaunchService som instansfält så den inte tas bort
            _launchService = new LaunchService(_dbContext);
            _launchService.StartMonitoring();

            // Prenumererar på HourSelected-eventet för att hantera när användaren väljer en timme
            _calender.HourSelected += async (sender, dateTime) =>
            {
                // 1. Hämta dina installerade appar (precis som du gjorde innan)
                var installedApps = _appService.GetInstalledApps();

                // 2. Hämta alla sparade scheman från databasen
                // (Dubbelkolla så att din metod i dbContext heter exakt GetSchedualsAsync eller liknande)
                var allSchedules = await _dbContext.GetSchedualsAsync();

                // 3. Kolla om det redan finns en bokning som krockar med den timme du klickade på
                var existingBooking = allSchedules?.FirstOrDefault(b =>
                    dateTime >= b.StartTime && dateTime < b.EndTime);

                // 4. Skicka med 'existingBooking' till popupen. 
                // Om den är null fattar popupen att det är en NY bokning, annars blir det ÄNDRA-läge.
                var popup = new BookingPopUp(dateTime, installedApps, _dbContext, existingBooking);

                var result = await this.ShowPopupAsync(popup);

                if (result is SchedualModel)
                {
                    await RefreshCalendar();
                }
            };
        }

        #endregion

            #region Lifecycle

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Säkerställ att väderloggar är laddade innan kalendern visas
            // SetDatabase kallas i konstruktorn men vi väntar här in laddningen så UI visar data direkt.
            await RefreshCalendar();
        }

        #endregion

        #region Event Handlers

       
        private async void OnRefreshClicked(object sender, EventArgs e)
        {
            await RefreshCalendar();
        }

        #endregion

        #region Private Methods

       
        private async Task RefreshCalendar()
        {
            if (_calender != null)
            {
                await _calender.RefreshWeatherLogsAsync();
                await _calender.RefreshSchedualsAsync();

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

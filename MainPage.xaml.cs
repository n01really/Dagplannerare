using PlannerApp.Callender;

namespace PlannerApp
{
    public partial class MainPage : ContentPage
    {
        private Callender.Callender _calender;
        public MainPage()
        {
            InitializeComponent();
            _calender = new Callender.Callender();

            CalenderContainer.Content = _calender.Calendar;
        }

    }
}

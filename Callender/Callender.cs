using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.Maui.Toolkit.Calendar;
using Microsoft.Maui.Controls.Shapes; 

namespace PlannerApp.Callender
{
    internal class Callender
    {
        public SfCalendar Calendar { get; set; }
        public event EventHandler<DateTime>? DateSelected;
        
        public Callender()
        {
            Calendar = new SfCalendar 
            {
                View = CalendarView.Month,
                SelectionMode = CalendarSelectionMode.Single,
                SelectedDate = DateTime.Today,
                EnablePastDates = false,
                ShowTrailingAndLeadingDates = false,
                SelectionShape = CalendarSelectionShape.Rectangle,
                MonthView = new CalendarMonthView
                {
                    NumberOfVisibleWeeks = 1,
                    FirstDayOfWeek = DateTime.Today.DayOfWeek
                },
                
                
            };
            Calendar.SelectedDate = DateTime.Today;
            Calendar.SelectionChanged += OnSelectionChanged;
        }
        
        private void OnSelectionChanged(object? sender, CalendarSelectionChangedEventArgs e) 
        { 
            if (e.NewValue != null)
            {
                DateTime selectedDate = (DateTime)e.NewValue;
                DateSelected?.Invoke(this, selectedDate);
            }
        }
        
        public void SetDateRange(DateTime minDate, DateTime maxDate)
        {
            Calendar.MinimumDate = minDate;
            Calendar.MaximumDate = maxDate;
        }
        
        public void SetSelectedDate(DateTime date) 
        { 
            Calendar.SelectedDate = date;
        }
    }
}

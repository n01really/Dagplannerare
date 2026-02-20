using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Syncfusion.Maui.Toolkit.Calendar;

namespace PlannerApp.Callender
{
    internal class Callender
    {
        public SfCalendar Calendar { get; set; }
        
        public Callender()
        {
            Calendar = new SfCalendar 
            {
                View = CalendarView.Month,
                SelectionMode = CalendarSelectionMode.Single,
                SelectedDate = DateTime.Today,
                EnablePastDates = true
            };
            Calendar.SelectedDate = DateTime.Today;
            Calendar.SelectionChanged += OnSelectionChanged;
        }
        
        private void OnSelectionChanged(object? sender, CalendarSelectionChangedEventArgs e) 
        { 
            if (e.NewValue != null)
            {
                DateTime selectedDate = (DateTime)e.NewValue;
                // Handle the selected date change here
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

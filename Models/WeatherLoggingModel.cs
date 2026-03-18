using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PlannerApp.Models
{
    public class WeatherLoggingModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string WeatherCondition { get; set; }
        public double Temperature { get; set; }
        public DateTime DateTime { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace PlannerApp.Models
{
    public class SchedualModel //I dont care if it is misspeled, i am dyslecic and it is funny. ps i wrote dyslexic wrong on purpose because it is also funny.
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string AppName { get; set; }
        public int AppId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Description { get; set; }
        public DateTime DateTime { get; set; }
    }
}

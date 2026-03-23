using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;
using PlannerApp.Backend;

namespace PlannerApp.Models
{
    public class ProcessLoggingModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string AppName { get; set; }
        public int AppId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}

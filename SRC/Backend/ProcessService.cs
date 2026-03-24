using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using PlannerApp.SRC.Backend;

namespace PlannerApp.SRC.Backend
{
    public class ProcessItem
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    class ProcessService
    {
        public List<ProcessItem> GetCurrentProcesses() 
        { 
            return Process.GetProcesses()
                .Where(p => p.MainWindowHandle != nint.Zero && !string.IsNullOrWhiteSpace(p.MainWindowTitle))
                .Select(p => new ProcessItem
                {
                    Name = p.ProcessName,
                    Id = p.Id
                })
                .OrderBy(p => p.Name)
                .ToList();
        }
    }
}

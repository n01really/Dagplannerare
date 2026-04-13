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
        public DateTime StartTime { get; set; }
    }

    class ProcessService
    {
        // Lista över processnamn som ska ignoreras (system- och bakgrundsprocesser)
        private readonly HashSet<string> _ignoredProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "smss", "lsass", "services", "winlogon", "wininit",
            "dwm", "explorer", "taskhostw", "RuntimeBroker", "sihost", "ctfmon",
            "SearchIndexer", "SearchHost", "StartMenuExperienceHost", "ShellExperienceHost",
            "TextInputHost", "SecurityHealthSystray", "SecurityHealthService",
            "MsMpEng", "NisSrv", "SgrmBroker", "fontdrvhost", "conhost",
            "ApplicationFrameHost", "SystemSettings", "dllhost", "spoolsv",
            "audiodg", "WUDFHost", "dasHost", "Memory Compression", "Registry",
            "SearchUI", "Widgets", "WidgetService"
        };

        // Vanliga systemprocesser som börjar med dessa prefix
        private readonly string[] _ignoredPrefixes = { "Microsoft.", "Windows.", "System" };

        public List<ProcessItem> GetCurrentProcesses() 
        { 
            return Process.GetProcesses()
                .Where(p => IsUserApplication(p))
                .Select(p => new ProcessItem
                {
                    Name = p.ProcessName,
                    Id = p.Id,
                    StartTime = GetProcessStartTime(p)
                })
                .OrderBy(p => p.Name)
                .ToList();
        }

        public List<ProcessItem> GetUserApplications()
        {
            return GetCurrentProcesses();
        }

        private bool IsUserApplication(Process process)
        {
            try
            {
                // Måste ha ett synligt fönster
                if (process.MainWindowHandle == nint.Zero || string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    return false;

                string processName = process.ProcessName;

                // Filtrera bort kända systemprocesser
                if (_ignoredProcesses.Contains(processName))
                    return false;

                // Filtrera bort processer med system-prefix
                if (_ignoredPrefixes.Any(prefix => processName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    return false;

                // Filtrera bort Windows-appar i SystemApps-mappen
                string fileName = process.MainModule?.FileName ?? "";
                if (fileName.Contains("SystemApps", StringComparison.OrdinalIgnoreCase) ||
                    fileName.Contains("WindowsApps\\Microsoft.", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Om processen har kommit hit är det troligen en användarapp
                return true;
            }
            catch
            {
                // Om vi inte kan läsa processinfo (säkerhetsskäl), skippa den
                return false;
            }
        }

        private DateTime GetProcessStartTime(Process process)
        {
            try
            {
                return process.StartTime;
            }
            catch
            {
                // Om vi inte kan få starttid, använd nuvarande tid
                return DateTime.Now;
            }
        }
    }
}

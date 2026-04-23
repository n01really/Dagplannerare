using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PlannerApp.SRC.DB;
using PlannerApp.SRC.Models;

namespace PlannerApp.SRC.Backend
{
    internal class LaunchService
    {
        private IDispatcherTimer? _timer;
        private readonly dbContext _dbContext;
        private readonly string _currentAppPath;

        // Windows API för att hantera fönster
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;

        public LaunchService(dbContext dbContext)
        {
            _dbContext = dbContext;
            // Hämta den nuvarande appens sökväg/namn
            _currentAppPath = GetCurrentAppExecutableName();
        }

        private string GetCurrentAppExecutableName()
        {
            try
            {
                // Hämta namnet på den körande processen
                var currentProcess = Process.GetCurrentProcess();
                return currentProcess.ProcessName;
            }
            catch
            {
                return "PlannerApp"; // Fallback till appnamn
            }
        }

        public void StartMonitoring()
        {
            // Skapa en timer som körs periodiskt (var 60:e sekund)
            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick += async (sender, e) => await CheckAndLaunchAsync();
            _timer.Start();
        }

        public void StopMonitoring()
        {
            _timer?.Stop();
        }

        private async Task CheckAndLaunchAsync()
        {
            try
            {
                // Hämta schemalagda program
                var schedules = await _dbContext.GetSchedualsAsync();
                var now = DateTime.Now;

                // Uppdatera IsRunning status för alla scheman
                foreach (var schedule in schedules)
                {
                    bool isRunning = IsProgramRunning(schedule.AppName);
                    
                    // Uppdatera om status ändrats
                    if (schedule.IsRunning != isRunning)
                    {
                        schedule.IsRunning = isRunning;
                        await _dbContext.UpdateScheduleAsync(schedule);
                    }
                }

                var toExecute = schedules
                    .Where(s => s.StartTime <= now 
                             && now < s.EndTime  // Kontrollera att vi är INOM tidsfönstret
                             && s.AppId != 0 
                             && !s.IsRunning)  // Kör bara om programmet INTE körs
                    .ToList();

                foreach (var schedule in toExecute)
                {
                    // FÖRHINDRA att appen startar sig själv
                    if (IsSelfReference(schedule.AppName))
                    {
                        Debug.WriteLine($"Skippade {schedule.AppName} - kan inte starta sig själv");
                        continue;
                    }

                    try
                    {
                        // Minimera alla andra appar FÖRE start av ny app
                        MinimizeOtherApps(schedule.AppName);

                        // Starta programmet
                        var processInfo = new ProcessStartInfo
                        {
                            FileName = schedule.AppName,
                            UseShellExecute = true
                        };
                        Process.Start(processInfo);

                        // Markera som körande
                        schedule.IsRunning = true;
                        await _dbContext.UpdateScheduleAsync(schedule);

                        // Logga körningen
                        await LogProgramStartAsync(schedule);
                        
                        Debug.WriteLine($"Startade {schedule.AppName} för schema ID: {schedule.Id}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Kunde inte starta {schedule.AppName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fel i CheckAndLaunchAsync: {ex.Message}");
            }
        }

        private bool IsProgramRunning(string appName)
        {
            try
            {
                string? appNameToCheck = GetAppNameFromPath(appName);
                if (string.IsNullOrEmpty(appNameToCheck))
                    return false;

                var runningProcesses = Process.GetProcesses();
                return runningProcesses.Any(p => 
                    p.ProcessName.Equals(appNameToCheck, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fel vid kontroll av körande program: {ex.Message}");
                return false;
            }
        }

        private void MinimizeOtherApps(string appToLaunch)
        {
            try
            {
                var currentProcess = Process.GetCurrentProcess();
                var currentProcessId = currentProcess.Id;
                
                // Hämta namnet på appen som ska startas (för att inte minimera den om den redan körs)
                string? appToLaunchName = GetAppNameFromPath(appToLaunch);

                // Hämta alla processer med fönster
                var allProcesses = Process.GetProcesses()
                    .Where(p => p.MainWindowHandle != IntPtr.Zero && IsWindowVisible(p.MainWindowHandle))
                    .ToList();

                foreach (var process in allProcesses)
                {
                    try
                    {
                        // Minimera INTE:
                        // 1. Den nuvarande PlannerApp-processen
                        // 2. Appen som ska startas (om den redan körs)
                        if (process.Id == currentProcessId)
                            continue;

                        if (!string.IsNullOrEmpty(appToLaunchName) && 
                            process.ProcessName.Equals(appToLaunchName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Minimera fönstret
                        ShowWindow(process.MainWindowHandle, SW_MINIMIZE);
                        Debug.WriteLine($"Minimerade: {process.ProcessName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Kunde inte minimera {process.ProcessName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fel vid minimering av appar: {ex.Message}");
            }
        }

        private string? GetAppNameFromPath(string appPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(appPath))
                    return null;

                // Om det är en fullständig sökväg
                if (Path.HasExtension(appPath))
                {
                    return Path.GetFileNameWithoutExtension(appPath);
                }

                // Om det bara är ett namn
                return appPath.Replace(".exe", "");
            }
            catch
            {
                return null;
            }
        }

        private bool IsSelfReference(string appName)
        {
            if (string.IsNullOrWhiteSpace(appName))
                return false;

            try
            {
                // Kontrollera olika varianter av appnamnet
                var appNameLower = appName.ToLower();
                var currentAppLower = _currentAppPath.ToLower();

                // Om appName innehåller nuvarande appens namn
                if (appNameLower.IndexOf(currentAppLower, StringComparison.Ordinal) >= 0)
                    return true;

                // Om appName innehåller "plannerapp" eller "dagplannerare"
                if (appNameLower.IndexOf("plannerapp", StringComparison.Ordinal) >= 0 || 
                    appNameLower.IndexOf("dagplannerare", StringComparison.Ordinal) >= 0)
                    return true;

                // Jämför filnamn (om det är en fullständig sökväg)
                if (Path.HasExtension(appName))
                {
                    var fileName = Path.GetFileNameWithoutExtension(appName)?.ToLower();
                    if (fileName == currentAppLower)
                        return true;
                }

                return false;
            }
            catch
            {
                // Om något går fel, anta att det KAN vara en självreferens (säkrare)
                return true;
            }
        }

        private async Task LogProgramStartAsync(SchedualModel schedule)
        {
            // Logga körningen
            var log = new ProcessLoggingModel
            {
                AppName = schedule.AppName,
                AppId = schedule.AppId,
                StartTime = schedule.StartTime,
                EndTime = DateTime.Now,
            };
            await _dbContext.AddProcessLogAsync(log);
        }
    }
}

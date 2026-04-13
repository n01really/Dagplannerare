using Microsoft.Win32;
using PlannerApp.SRC.Models;

namespace PlannerApp.SRC.Backend
{
    public class AppService
    {
        // Lista över exe-filer som ska ignoreras
        private readonly string[] _ignoredExecutables = {
            "uninstall.exe",
            "uninst.exe",
            "unins000.exe",
            "helper.exe",
            "updater.exe",
            "launcher.exe",
            "install.exe",
            "setup.exe"
        };

        public List<AppsModels> GetInstalledApps()
        {
            var apps = new List<AppsModels>();
            int idCounter = 1;
            
            string[] registryKeys = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            
            foreach (string Key in registryKeys)
            {
                using (var hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var subKey = hklm.OpenSubKey(Key))
                {
                    if (subKey != null)
                    {
                        foreach (var subKeyName in subKey.GetSubKeyNames())
                        {
                            using (var appKey = subKey.OpenSubKey(subKeyName))
                            {
                                var name = appKey.GetValue("DisplayName") as string;
                                if (!string.IsNullOrEmpty(name))
                                {
                                    var exePath = GetExecutablePath(appKey, name);
                                    if (!string.IsNullOrEmpty(exePath))
                                    {
                                        apps.Add(new AppsModels
                                        {
                                            Id = idCounter++,
                                            Name = name,
                                            Version = appKey.GetValue("DisplayVersion") as string,
                                            Publisher = appKey.GetValue("Publisher") as string,
                                            Path = exePath
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return apps.OrderBy(app => app.Name).ToList();
        }

        private string GetExecutablePath(RegistryKey appKey, string appName)
        {
            // 1. Kolla DisplayIcon först (pekar ofta direkt på exe-filen)
            var displayIcon = appKey.GetValue("DisplayIcon") as string;
            if (!string.IsNullOrEmpty(displayIcon))
            {
                var iconPath = displayIcon.Split(',')[0].Trim('"');
                if (File.Exists(iconPath) && 
                    iconPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) &&
                    !IsIgnoredExecutable(iconPath))
                {
                    return iconPath;
                }
            }

            // 2. Kolla InstallLocation och hitta rätt exe-fil
            var installLocation = appKey.GetValue("InstallLocation") as string;
            if (!string.IsNullOrEmpty(installLocation))
            {
                // FIXED: Sök både i TopDirectoryOnly OCH AllDirectories (max 2 nivåer djup)
                var exePath = FindExecutableInDirectory(installLocation, appName);
                if (!string.IsNullOrEmpty(exePath))
                    return exePath;
            }

            // 3. ADDED: Kolla vanliga installationsplatser som fallback
            var commonLocations = new[]
            {
                @"C:\Program Files\{0}\{0}.exe",
                @"C:\Program Files (x86)\{0}\{0}.exe",
                @"C:\Program Files\{0}\bin\{0}.exe",
                @"C:\Program Files (x86)\{0}\bin\{0}.exe"
            };

            foreach (var locationTemplate in commonLocations)
            {
                var path = string.Format(locationTemplate, appName);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        // ADDED: Hitta exe-fil i mapp med smart sökning
        private string FindExecutableInDirectory(string directory, string appName)
        {
            if (!Directory.Exists(directory))
                return null;

            try
            {
                // Sök först i huvudmappen
                var exeFiles = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly)
                    .Where(f => !IsIgnoredExecutable(f))
                    .ToArray();

                var match = FindBestMatch(exeFiles, appName);
                if (match != null)
                    return match;

                // FIXED: Sök sedan i undermappar (max 2 nivåer djup för att undvika långsam sökning)
                var subDirectories = Directory.GetDirectories(directory)
                    .Take(10); // Begränsa till första 10 undermapparna

                foreach (var subDir in subDirectories)
                {
                    var subExeFiles = Directory.GetFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(f => !IsIgnoredExecutable(f))
                        .ToArray();

                    match = FindBestMatch(subExeFiles, appName);
                    if (match != null)
                        return match;
                }
            }
            catch
            {
                // Om det blir fel med filåtkomst, returnera null
                return null;
            }

            return null;
        }

        // ADDED: Hitta bästa matchning bland exe-filer
        private string FindBestMatch(string[] exeFiles, string appName)
        {
            if (exeFiles.Length == 0)
                return null;

            // PRIORITET 1: Exakt matchning
            var exactMatch = exeFiles.FirstOrDefault(f => 
                Path.GetFileNameWithoutExtension(f).Equals(appName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
                return exactMatch;

            // PRIORITET 2: Partiell matchning (steam.exe matchar "Steam")
            var partialMatch = exeFiles.FirstOrDefault(f => 
                Path.GetFileNameWithoutExtension(f).Contains(appName, StringComparison.OrdinalIgnoreCase));
            if (partialMatch != null)
                return partialMatch;

            // PRIORITET 3: Första exe-filen
            return exeFiles[0];
        }

        // Kontrollera om exe-filen ska ignoreras
        private bool IsIgnoredExecutable(string exePath)
        {
            var fileName = Path.GetFileName(exePath).ToLower();
            return _ignoredExecutables.Any(ignored => fileName.Equals(ignored, StringComparison.OrdinalIgnoreCase));
        }
    }
}





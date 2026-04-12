using Microsoft.Win32;
using System.Collections.Generic;
using PlannerApp.SRC.Models;
using System.IO;

namespace PlannerApp.SRC.Backend
{
    public class AppService
    {
        public List<AppsModels> GetInstalledApps()
        {
            var apps = new List<AppsModels>();
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
                                    // Kontrollera om appen har en exe-fil
                                    if (HasExecutable(appKey))
                                    {
                                        apps.Add(new AppsModels
                                        {
                                            Name = name,
                                            Version = appKey.GetValue("DisplayVersion") as string,
                                            Publisher = appKey.GetValue("Publisher") as string
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
                
            }
            return apps;
        }

        private bool HasExecutable(RegistryKey appKey)
        {
            // Kolla InstallLocation
            var installLocation = appKey.GetValue("InstallLocation") as string;
            if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
            {
                if (Directory.GetFiles(installLocation, "*.exe", SearchOption.TopDirectoryOnly).Length > 0)
                {
                    return true;
                }
            }

            // Kolla DisplayIcon (pekar ofta direkt på exe-filen)
            var displayIcon = appKey.GetValue("DisplayIcon") as string;
            if (!string.IsNullOrEmpty(displayIcon))
            {
                // Ta bort eventuella index (t.ex. ",0" i slutet)
                var iconPath = displayIcon.Split(',')[0].Trim('"');
                if (File.Exists(iconPath) && iconPath.EndsWith(".exe", System.StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}





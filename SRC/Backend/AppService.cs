using Microsoft.Win32;
using System.Collections.Generic;
using PlannerApp.SRC.Models;


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
            return apps;
        }
    }
}





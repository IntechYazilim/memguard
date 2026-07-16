using System;
using System.IO;
using System.Text.Json;

namespace MemGuard.Services
{
    public class DisabledRegistryStartup
    {
        public string KeyName { get; set; } = string.Empty;
        public string Command { get; set; } = string.Empty;
        public string RegistryPath { get; set; } = string.Empty; // HKCU or HKLM path
        public bool IsLocalMachine { get; set; } // true if HKLM, false if HKCU
    }

    public class DisabledFolderStartup
    {
        public string KeyName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DisabledPath { get; set; } = string.Empty;
        public string OriginalFolderPath { get; set; } = string.Empty;
        public bool IsLocalMachine { get; set; }
    }

    public class AppSettings
    {
        public bool StartWithWindows { get; set; } = false;
        public bool MinimizeToTray { get; set; } = true;
        public bool CheckForUpdates { get; set; } = true;
        public bool HardwareAcceleration { get; set; } = true;
        public bool NotificationsEnabled { get; set; } = true;
        public bool AutomaticMonitoring { get; set; } = true;
        public bool AutoOptimizeEnabled { get; set; } = false;
        public bool AutoOptimizeAdaptiveInterval { get; set; } = true;
        public int AutoOptimizeThresholdPercent { get; set; } = 82;
        public int AutoOptimizeIntervalMinutes { get; set; } = 5;
        public string SelectedTheme { get; set; } = "Dark";
        public string SelectedLanguage { get; set; } = "en";
        public System.Collections.Generic.List<DisabledRegistryStartup> DisabledRegistryStartups { get; set; } = new();
        public System.Collections.Generic.List<DisabledFolderStartup> DisabledFolderStartups { get; set; } = new();
    }

    public interface ISettingsService
    {
        AppSettings Current { get; }
        void Load();
        void Save();
        void Update(Action<AppSettings> updateAction);
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFile;
        private readonly ILoggerService _logger;
        public AppSettings Current { get; private set; } = new();

        public SettingsService(ILoggerService logger)
        {
            _logger = logger;
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _settingsFile = Path.Combine(appData, "MemGuard", "settings.json");
            Load();
        }

        public void Load()
        {
            try
            {
                if (File.Exists(_settingsFile))
                {
                    var json = File.ReadAllText(_settingsFile);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        Current = settings;
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load settings, using defaults.", ex);
            }
            Current = new AppSettings();
        }

        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(_settingsFile);
                if (dir != null && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Current, options);
                File.WriteAllText(_settingsFile, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save settings.", ex);
            }
        }

        public void Update(Action<AppSettings> updateAction)
        {
            updateAction(Current);
            Save();
        }
    }
}

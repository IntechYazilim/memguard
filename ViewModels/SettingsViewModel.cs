using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Win32;
using MemGuard.Localization;
using MemGuard.Models;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly ILoggerService _logger;
        private readonly IThemeService _themeService;

        public ObservableCollection<int> AutoOptimizeIntervals { get; } = new() { 1, 3, 5, 10, 15, 30 };
        public ObservableCollection<int> ThresholdOptions { get; } = new() { 65, 70, 75, 80, 85, 90, 95 };
        public ObservableCollection<ThemeOption> Themes { get; }
        public ObservableCollection<LanguageOption> Languages { get; } = new()
        {
            new LanguageOption { Code = "tr", DisplayKey = "Language.Turkish" },
            new LanguageOption { Code = "en", DisplayKey = "Language.English" },
            new LanguageOption { Code = "es", DisplayKey = "Language.Spanish" },
            new LanguageOption { Code = "fr", DisplayKey = "Language.French" }
        };

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppRegistryName = "MemGuardOptimizer";

        public bool StartWithWindows
        {
            get => _settingsService.Current.StartWithWindows;
            set
            {
                if (_settingsService.Current.StartWithWindows != value)
                {
                    _settingsService.Update(s => s.StartWithWindows = value);
                    OnPropertyChanged();
                    ConfigureStartup(value);
                }
            }
        }

        public bool MinimizeToTray
        {
            get => _settingsService.Current.MinimizeToTray;
            set
            {
                if (_settingsService.Current.MinimizeToTray != value)
                {
                    _settingsService.Update(s => s.MinimizeToTray = value);
                    OnPropertyChanged();
                }
            }
        }

        public bool CheckForUpdates
        {
            get => _settingsService.Current.CheckForUpdates;
            set
            {
                if (_settingsService.Current.CheckForUpdates != value)
                {
                    _settingsService.Update(s => s.CheckForUpdates = value);
                    OnPropertyChanged();
                }
            }
        }

        public bool HardwareAcceleration
        {
            get => _settingsService.Current.HardwareAcceleration;
            set
            {
                if (_settingsService.Current.HardwareAcceleration != value)
                {
                    _settingsService.Update(s => s.HardwareAcceleration = value);
                    OnPropertyChanged();
                    OnSettingsChanged?.Invoke("Settings Updated", "Restart may be required to apply graphics settings.");
                }
            }
        }

        public bool NotificationsEnabled
        {
            get => _settingsService.Current.NotificationsEnabled;
            set
            {
                if (_settingsService.Current.NotificationsEnabled != value)
                {
                    _settingsService.Update(s => s.NotificationsEnabled = value);
                    OnPropertyChanged();
                }
            }
        }

        public bool AutomaticMonitoring
        {
            get => _settingsService.Current.AutomaticMonitoring;
            set
            {
                if (_settingsService.Current.AutomaticMonitoring != value)
                {
                    _settingsService.Update(s => s.AutomaticMonitoring = value);
                    OnPropertyChanged();
                }
            }
        }

        public string SelectedTheme
        {
            get => _settingsService.Current.SelectedTheme;
            set
            {
                if (_settingsService.Current.SelectedTheme != value)
                {
                    _settingsService.Update(s => s.SelectedTheme = value);
                    _themeService.ApplyTheme(value);
                    OnPropertyChanged();
                    var themeName = LocalizationService.Instance[$"Theme.{value}"];
                    OnSettingsChanged?.Invoke(
                        LocalizationService.Instance["Toast.ThemeUpdated"],
                        string.Format(LocalizationService.Instance["Toast.ThemeApplied"], themeName));
                }
            }
        }

        public string SelectedLanguage
        {
            get => _settingsService.Current.SelectedLanguage;
            set
            {
                if (_settingsService.Current.SelectedLanguage != value)
                {
                    _settingsService.Update(s => s.SelectedLanguage = value);
                    LocalizationService.Instance.SetLanguage(value);
                    OnPropertyChanged();
                    var languageName = Languages.FirstOrDefault(language => language.Code.Equals(value, StringComparison.OrdinalIgnoreCase))?.DisplayName ?? value.ToUpperInvariant();
                    OnSettingsChanged?.Invoke(
                        LocalizationService.Instance["Toast.LanguageUpdated"],
                        string.Format(LocalizationService.Instance["Toast.LanguageApplied"], languageName));
                }
            }
        }

        public bool AutoOptimizeEnabled
        {
            get => _settingsService.Current.AutoOptimizeEnabled;
            set
            {
                if (_settingsService.Current.AutoOptimizeEnabled != value)
                {
                    _settingsService.Update(s => s.AutoOptimizeEnabled = value);
                    OnPropertyChanged();
                    OnSettingsChanged?.Invoke("Auto Optimize", value ? "Automatic RAM optimization enabled." : "Automatic RAM optimization disabled.");
                }
            }
        }

        public bool AutoOptimizeAdaptiveInterval
        {
            get => _settingsService.Current.AutoOptimizeAdaptiveInterval;
            set
            {
                if (_settingsService.Current.AutoOptimizeAdaptiveInterval != value)
                {
                    _settingsService.Update(s => s.AutoOptimizeAdaptiveInterval = value);
                    OnPropertyChanged();
                }
            }
        }

        public int AutoOptimizeThresholdPercent
        {
            get => _settingsService.Current.AutoOptimizeThresholdPercent;
            set
            {
                if (_settingsService.Current.AutoOptimizeThresholdPercent != value)
                {
                    _settingsService.Update(s => s.AutoOptimizeThresholdPercent = value);
                    OnPropertyChanged();
                }
            }
        }

        public int AutoOptimizeIntervalMinutes
        {
            get => _settingsService.Current.AutoOptimizeIntervalMinutes;
            set
            {
                if (_settingsService.Current.AutoOptimizeIntervalMinutes != value)
                {
                    _settingsService.Update(s => s.AutoOptimizeIntervalMinutes = value);
                    OnPropertyChanged();
                }
            }
        }

        public SettingsViewModel(ISettingsService settingsService, ILoggerService logger, IThemeService themeService)
        {
            _settingsService = settingsService;
            _logger = logger;
            _themeService = themeService;
            Themes = new ObservableCollection<ThemeOption>(_themeService.AvailableThemes);
            RefreshOptionLabels();
            LocalizationService.Instance.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Item[]" || args.PropertyName == nameof(LocalizationService.CurrentLanguage))
                {
                    RefreshOptionLabels();
                }
            };
        }

        private void RefreshOptionLabels()
        {
            foreach (var theme in Themes)
            {
                theme.DisplayName = LocalizationService.Instance[theme.DisplayKey];
            }

            foreach (var language in Languages)
            {
                language.DisplayName = LocalizationService.Instance[language.DisplayKey];
            }
        }

        private void ConfigureStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
                if (key == null) return;

                if (enable)
                {
                    var exePath = ResolveStartupExecutablePath();
                    if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                    {
                        key.SetValue(AppRegistryName, $"\"{exePath}\" --minimized");
                        _logger.LogInfo("Registered application to run on Windows startup.");
                    }
                }
                else
                {
                    key.DeleteValue(AppRegistryName, throwOnMissingValue: false);
                    _logger.LogInfo("Deregistered application from Windows startup.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to configure Windows startup registry.", ex);
                OnSettingsChanged?.Invoke("Registry Error", "Windows startup registration failed.");
            }
        }

        private static string? ResolveStartupExecutablePath()
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(exePath))
            {
                return null;
            }

            if (!exePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            {
                return exePath;
            }

            var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return exePath;
            }

            var publishedExe = Path.Combine(projectRoot, "publish", "win-x64", "MemGuard.exe");
            return File.Exists(publishedExe) ? publishedExe : exePath;
        }

        public event Action<string, string>? OnSettingsChanged;
    }
}

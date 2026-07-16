using System;
using System.Windows.Input;
using System.Windows.Threading;
using MemGuard.Localization;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        // Core Services
        public ILoggerService Logger { get; }
        public ISettingsService Settings { get; }
        public ISystemMonitoringService Monitor { get; }
        public IMemoryOptimizationService MemoryOptimizer { get; }
        public IProcessService ProcessManager { get; }
        public ICleanerService Cleaner { get; }
        public IStartupService Startup { get; }
        public IThemeService ThemeManager { get; }

        // Sub ViewModels
        public DashboardViewModel DashboardVM { get; }
        public MemoryViewModel MemoryVM { get; }
        public ProcessesViewModel ProcessesVM { get; }
        public CleanerViewModel CleanerVM { get; }
        public StartupViewModel StartupVM { get; }
        public GameModeViewModel GameModeVM { get; }
        public SettingsViewModel SettingsVM { get; }

        private ViewModelBase _currentViewModel;
        private string _currentPageName = "Dashboard";
        private readonly DispatcherTimer _autoOptimizeTimer;
        private DateTime _lastAutoOptimizeRun = DateTime.MinValue;
        private bool _autoOptimizeInProgress;

        public ViewModelBase CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        public string CurrentPageName
        {
            get => _currentPageName;
            set => SetProperty(ref _currentPageName, value);
        }

        // Window interaction commands
        public ICommand NavigateCommand { get; }

        public MainViewModel()
        {
            // 1. Initialize core services
            Logger = new LoggerService();
            Settings = new SettingsService(Logger);
            Monitor = new SystemMonitoringService(Logger);
            MemoryOptimizer = new MemoryOptimizationService(Monitor, Logger);
            ProcessManager = new ProcessService(Logger);
            Cleaner = new CleanerService(Logger);
            Startup = new StartupService(Settings, Logger);
            ThemeManager = new ThemeService();

            Logger.LogInfo("MemGuard core services initialized successfully.");

            // 2. Initialize sub viewmodels
            DashboardVM = new DashboardViewModel(Monitor);
            MemoryVM = new MemoryViewModel(MemoryOptimizer, Monitor);
            ProcessesVM = new ProcessesViewModel(ProcessManager);
            CleanerVM = new CleanerViewModel(Cleaner);
            StartupVM = new StartupViewModel(Startup);
            GameModeVM = new GameModeViewModel(ProcessManager, MemoryOptimizer);
            SettingsVM = new SettingsViewModel(Settings, Logger, ThemeManager);
            LocalizationService.Instance.SetLanguage(Settings.Current.SelectedLanguage);
            ThemeManager.ApplyTheme(Settings.Current.SelectedTheme);

            // Set default view
            _currentViewModel = DashboardVM;

            // 3. Register navigation command
            NavigateCommand = new RelayCommand((page) => Navigate(page?.ToString() ?? "Dashboard"));

            // 4. Wire notifications
            WireNotificationEvents();

            _autoOptimizeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            _autoOptimizeTimer.Tick += async (_, _) => await TryAutoOptimizeAsync();
            _autoOptimizeTimer.Start();
        }

        private void Navigate(string pageName)
        {
            CurrentPageName = pageName;
            CurrentViewModel = pageName switch
            {
                "Dashboard" => DashboardVM,
                "Memory" => MemoryVM,
                "Processes" => ProcessesVM,
                "Cleaner" => CleanerVM,
                "Startup" => StartupVM,
                "GameMode" => GameModeVM,
                "Settings" => SettingsVM,
                _ => DashboardVM
            };

            // If switching to Memory or Processes, update immediately
            if (CurrentViewModel == MemoryVM)
            {
                MemoryVM.RefreshStats();
            }
        }

        private void WireNotificationEvents()
        {
            // Setup hooks so any viewmodel can request displaying an in-app toast
            MemoryVM.OnOptimizationFinished += (title, msg) => ShowToastNotification(title, msg, Controls.NotificationType.Success);
            ProcessesVM.OnProcessTerminated += (title, msg) => ShowToastNotification(title, msg, title.Contains("Error") ? Controls.NotificationType.Error : Controls.NotificationType.Info);
            CleanerVM.OnCleanFinished += (title, msg) => ShowToastNotification(title, msg, Controls.NotificationType.Success);
            StartupVM.OnStartupToggled += (title, msg) => ShowToastNotification(title, msg, title.Contains("Blocked") ? Controls.NotificationType.Warning : Controls.NotificationType.Success);
            GameModeVM.OnGameModeNotification += (title, msg) => ShowToastNotification(title, msg, Controls.NotificationType.Success);
            SettingsVM.OnSettingsChanged += (title, msg) => ShowToastNotification(title, msg, title.Contains("Error") ? Controls.NotificationType.Error : Controls.NotificationType.Info);
        }

        // Global Event that MainWindow will hook into to display actual in-app popup
        public event Action<string, string, Controls.NotificationType>? RequestShowNotification;

        public void ShowToastNotification(string title, string message, Controls.NotificationType type)
        {
            if (Settings.Current.NotificationsEnabled)
            {
                RequestShowNotification?.Invoke(title, message, type);
            }
        }

        private async System.Threading.Tasks.Task TryAutoOptimizeAsync()
        {
            if (_autoOptimizeInProgress || !Settings.Current.AutoOptimizeEnabled)
            {
                return;
            }

            var (_, _, usagePercent) = Monitor.GetMemoryStatus();
            if (usagePercent < Settings.Current.AutoOptimizeThresholdPercent)
            {
                return;
            }

            var minutes = ResolveOptimizationInterval(usagePercent);
            if (DateTime.Now - _lastAutoOptimizeRun < TimeSpan.FromMinutes(minutes))
            {
                return;
            }

            try
            {
                _autoOptimizeInProgress = true;
                await MemoryVM.RunAutomaticOptimizationAsync(minutes, usagePercent);
                _lastAutoOptimizeRun = DateTime.Now;
            }
            finally
            {
                _autoOptimizeInProgress = false;
            }
        }

        private int ResolveOptimizationInterval(double usagePercent)
        {
            if (!Settings.Current.AutoOptimizeAdaptiveInterval)
            {
                return Math.Max(1, Settings.Current.AutoOptimizeIntervalMinutes);
            }

            if (usagePercent >= 95) return 1;
            if (usagePercent >= 90) return 3;
            if (usagePercent >= 85) return 5;
            if (usagePercent >= 75) return 10;
            return Math.Max(1, Settings.Current.AutoOptimizeIntervalMinutes);
        }

        public void Shutdown()
        {
            // Stop timers and clean up
            DashboardVM.StopTimer();
            ProcessesVM.StopTimer();
            _autoOptimizeTimer.Stop();
            Logger.LogInfo("MemGuard shutdown completed.");
        }
    }
}

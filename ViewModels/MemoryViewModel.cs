using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MemGuard.Localization;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class MemoryViewModel : ViewModelBase
    {
        private readonly IMemoryOptimizationService _optimizationService;
        private readonly ISystemMonitoringService _monitoringService;
        
        private string _totalMemory = "0.0 GB";
        private string _usedMemory = "0.0 GB";
        private string _availableMemory = "0.0 GB";
        private double _usagePercentage;
        private bool _isOptimizing;
        private string _statusLog = "Ready for memory optimization.";
        private string _resultText = string.Empty;
        private bool _showResult;

        public string TotalMemory
        {
            get => _totalMemory;
            set => SetProperty(ref _totalMemory, value);
        }

        public string UsedMemory
        {
            get => _usedMemory;
            set => SetProperty(ref _usedMemory, value);
        }

        public string AvailableMemory
        {
            get => _availableMemory;
            set => SetProperty(ref _availableMemory, value);
        }

        public double UsagePercentage
        {
            get => _usagePercentage;
            set => SetProperty(ref _usagePercentage, value);
        }

        public bool IsOptimizing
        {
            get => _isOptimizing;
            set => SetProperty(ref _isOptimizing, value);
        }

        public string StatusLog
        {
            get => _statusLog;
            set => SetProperty(ref _statusLog, value);
        }

        public string ResultText
        {
            get => _resultText;
            set => SetProperty(ref _resultText, value);
        }

        public bool ShowResult
        {
            get => _showResult;
            set => SetProperty(ref _showResult, value);
        }

        public ICommand OptimizeCommand { get; }

        public MemoryViewModel(IMemoryOptimizationService optimizationService, ISystemMonitoringService monitoringService)
        {
            _optimizationService = optimizationService;
            _monitoringService = monitoringService;

            OptimizeCommand = new RelayCommand(async () => await OptimizeMemoryAsync());

            RefreshStats();
            StatusLog = LocalizationService.Instance.CurrentLanguage == "tr"
                ? "Bellek optimizasyonu hazir."
                : "Ready for memory optimization.";
        }

        public void RefreshStats()
        {
            var (total, avail, pct) = _monitoringService.GetMemoryStatus();
            
            double totalGb = (double)total / (1024 * 1024 * 1024);
            double availGb = (double)avail / (1024 * 1024 * 1024);
            double usedGb = totalGb - availGb;

            TotalMemory = $"{totalGb:F2} GB";
            AvailableMemory = $"{availGb:F2} GB";
            UsedMemory = $"{usedGb:F2} GB";
            UsagePercentage = pct;
        }

        private async Task OptimizeMemoryAsync()
        {
            if (IsOptimizing) return;

            IsOptimizing = true;
            ShowResult = false;
            var sb = new StringBuilder();
            StatusLog = LocalizationService.Instance.CurrentLanguage == "tr"
                ? "RAM optimizasyonu baslatiliyor..."
                : "Starting RAM Optimization...";

            void ProgressLogger(string message)
            {
                sb.AppendLine($"[{DateTime.Now:HH:mm:ss}] {message}");
                // UI update on dispatcher thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    StatusLog = sb.ToString();
                });
            }

            try
            {
                // Run optimization service
                var result = await _optimizationService.OptimizeMemoryAsync(ProgressLogger);

                // Re-evaluate RAM stats
                RefreshStats();

                // Format results
                double beforeGb = (double)result.MemoryBefore / (1024 * 1024 * 1024);
                double afterGb = (double)result.MemoryAfter / (1024 * 1024 * 1024);
                double freedGb = (double)result.FreedMemory / (1024 * 1024 * 1024);

                ResultText = $"Before: {beforeGb:F2} GB Used\nAfter: {afterGb:F2} GB Used\nFreed: {freedGb:F2} GB";
                ShowResult = true;

                // Send notification through MainWindow if needed
                RaiseNotification(freedGb);
            }
            catch (Exception ex)
            {
                ProgressLogger($"Error during optimization: {ex.Message}");
                ResultText = LocalizationService.Instance.CurrentLanguage == "tr"
                    ? "Optimizasyon tamamlanamadi.\nSistemde degisiklik yapilmadi."
                    : "Optimization could not be completed.\nNo changes were made to your system.";
                ShowResult = true;
            }
            finally
            {
                IsOptimizing = false;
            }
        }

        public async Task RunAutomaticOptimizationAsync(int intervalMinutes, double usagePercent)
        {
            if (IsOptimizing)
            {
                return;
            }

            StatusLog = $"Auto optimization triggered at {usagePercent:F1}% RAM usage. Interval: {intervalMinutes} min.";
            await OptimizeMemoryAsync();
        }

        // Event/Callback hook to show in-app toast notifications via MainWindow
        public event Action<string, string>? OnOptimizationFinished;

        private void RaiseNotification(double freedGb)
        {
            if (freedGb > 0.05)
            {
                OnOptimizationFinished?.Invoke(
                    LocalizationService.Instance["Toast.MemoryOptimized"], 
                    $"{freedGb:F2} GB of memory was successfully released."
                );
            }
            else
            {
                OnOptimizationFinished?.Invoke(
                    LocalizationService.Instance["Toast.MemoryChecked"], 
                    "Your memory is already running at peak efficiency."
                );
            }
        }
    }
}

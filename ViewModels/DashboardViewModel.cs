using System;
using System.Windows.Threading;
using MemGuard.Localization;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly ISystemMonitoringService _monitoringService;
        private readonly DispatcherTimer _timer;

        private double _cpuUsage;
        private double _ramUsage;
        private double _diskUsage;
        private string _systemStatus = LocalizationService.Instance["Status.Analyzing"];
        private double _systemHealth = 100;
        private string _uptime = "0h 0m 0s";
        private string _ramText = "0 GB / 0 GB";

        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public double RamUsage
        {
            get => _ramUsage;
            set => SetProperty(ref _ramUsage, value);
        }

        public double DiskUsage
        {
            get => _diskUsage;
            set => SetProperty(ref _diskUsage, value);
        }

        public string SystemStatus
        {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        public double SystemHealth
        {
            get => _systemHealth;
            set => SetProperty(ref _systemHealth, value);
        }

        public string Uptime
        {
            get => _uptime;
            set => SetProperty(ref _uptime, value);
        }

        public string RamText
        {
            get => _ramText;
            set => SetProperty(ref _ramText, value);
        }

        public DashboardViewModel(ISystemMonitoringService monitoringService)
        {
            _monitoringService = monitoringService;

            // Trigger immediate update
            UpdateStats();

            // Setup real-time updates every 1 second
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateStats();
            _timer.Start();
        }

        public void StopTimer()
        {
            _timer.Stop();
        }

        private void UpdateStats()
        {
            // Fetch CPU
            CpuUsage = _monitoringService.GetCpuUsage();

            // Fetch RAM
            var (totalRam, availRam, ramPct) = _monitoringService.GetMemoryStatus();
            RamUsage = ramPct;
            double usedGb = (double)(totalRam - availRam) / (1024 * 1024 * 1024);
            double totalGb = (double)totalRam / (1024 * 1024 * 1024);
            RamText = $"{usedGb:F1} GB / {totalGb:F1} GB {LocalizationService.Instance["Common.Used"]}";

            // Fetch Disk
            var (_, _, diskPct) = _monitoringService.GetDiskStatus();
            DiskUsage = diskPct;

            // Fetch Uptime
            var uptimeSpan = _monitoringService.GetSystemUptime();
            if (uptimeSpan.Days > 0)
            {
                Uptime = $"{uptimeSpan.Days}d {uptimeSpan.Hours}h {uptimeSpan.Minutes}m";
            }
            else
            {
                Uptime = $"{uptimeSpan.Hours}h {uptimeSpan.Minutes}m {uptimeSpan.Seconds}s";
            }

            // Calculate overall System Health index (0-100)
            // Health starts at 100, drops as RAM and CPU usage get higher
            double health = 100.0;
            
            // Deduct for RAM usage above 50%
            if (RamUsage > 50)
            {
                health -= (RamUsage - 50) * 0.8;
            }
            
            // Deduct for CPU usage above 40%
            if (CpuUsage > 40)
            {
                health -= (CpuUsage - 40) * 0.5;
            }

            SystemHealth = Math.Clamp(health, 10.0, 100.0);

            // Set state text
            if (RamUsage > 85 || CpuUsage > 85)
            {
                SystemStatus = LocalizationService.Instance["Status.HighMemory"];
            }
            else if (SystemHealth < 60)
            {
                SystemStatus = LocalizationService.Instance["Status.Recommended"];
            }
            else if (SystemHealth < 80)
            {
                SystemStatus = LocalizationService.Instance["Status.Good"];
            }
            else
            {
                SystemStatus = LocalizationService.Instance["Status.Excellent"];
            }
        }
    }
}

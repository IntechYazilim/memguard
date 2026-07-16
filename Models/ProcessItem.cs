using System;
using MemGuard.Localization;

namespace MemGuard.Models
{
    public class ProcessItem
    {
        public string Name { get; set; } = string.Empty;
        public int Id { get; set; }
        public long MemoryBytes { get; set; }
        public double CpuUsage { get; set; }
        public string ExecutablePath { get; set; } = string.Empty;
        public string Publisher { get; set; } = "Unknown";
        public DateTime? StartTime { get; set; }
        public string Status => IsCritical
            ? LocalizationService.Instance["Processes.StatusSystemProtected"]
            : LocalizationService.Instance["Processes.StatusUserApplication"];
        public bool IsCritical { get; set; }

        public string MemoryFormatted
        {
            get
            {
                double mb = (double)MemoryBytes / (1024 * 1024);
                if (mb >= 1024)
                {
                    return $"{(mb / 1024):F2} GB";
                }
                return $"{mb:F1} MB";
            }
        }
    }
}

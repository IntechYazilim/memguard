using System.Collections.Generic;

namespace MemGuard.Models
{
    public class CleanCategory
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public int FileCount { get; set; }
        public bool IsSelected { get; set; } = true;
        public string Status { get; set; } = "Ready to Scan";
        public List<string> Targets { get; set; } = new();

        public string SizeFormatted
        {
            get
            {
                double mb = (double)SizeBytes / (1024 * 1024);
                if (mb >= 1024)
                {
                    return $"{(mb / 1024):F2} GB";
                }
                return $"{mb:F1} MB";
            }
        }
    }
}

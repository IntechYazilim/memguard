using MemGuard.ViewModels;

namespace MemGuard.Models
{
    public class StartupItem : ViewModelBase
    {
        public string Name { get; set; } = string.Empty;
        public string Publisher { get; set; } = "Unknown";
        public string Command { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty; // e.g., "Registry (CurrentUser)", "Startup Folder"
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set => SetProperty(ref _isEnabled, value);
        }
        public string Impact { get; set; } = "Medium"; // Low, Medium, High
        public string KeyName { get; set; } = string.Empty; // Registry value name or shortcut filename
        public string FilePath { get; set; } = string.Empty; // Location path (registry path or folder path)
    }
}

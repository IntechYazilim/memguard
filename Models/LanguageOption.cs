using MemGuard.ViewModels;

namespace MemGuard.Models
{
    public class LanguageOption : ViewModelBase
    {
        public string Code { get; set; } = string.Empty;
        public string DisplayKey { get; set; } = string.Empty;
        private string _displayName = string.Empty;
        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public override string ToString() => DisplayName;
    }
}

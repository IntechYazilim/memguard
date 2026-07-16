using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MemGuard.Models;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class StartupViewModel : ViewModelBase
    {
        private readonly IStartupService _startupService;

        private ObservableCollection<StartupItem> _startupItems = new();
        private bool _isLoading;

        public ObservableCollection<StartupItem> StartupItems
        {
            get => _startupItems;
            set => SetProperty(ref _startupItems, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoadCommand { get; }
        public ICommand ToggleCommand { get; }

        public StartupViewModel(IStartupService startupService)
        {
            _startupService = startupService;

            LoadCommand = new RelayCommand(async () => await LoadStartupItemsAsync());
            ToggleCommand = new RelayCommand(async (param) =>
            {
                if (param is StartupItem item)
                {
                    await ToggleStartupItemAsync(item);
                }
            });

            // Initial load
            _ = LoadStartupItemsAsync();
        }

        private async Task LoadStartupItemsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var list = await _startupService.GetStartupItemsAsync();
                App.Current.Dispatcher.Invoke(() =>
                {
                    // Sort enabled first, then impact High->Medium->Low
                    var sorted = list
                        .OrderByDescending(x => x.IsEnabled)
                        .ThenBy(x => GetImpactWeight(x.Impact))
                        .ToList();

                    StartupItems = new ObservableCollection<StartupItem>(sorted);
                });
            }
            catch (Exception ex)
            {
                _loggerError("Failed to load startup items", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ToggleStartupItemAsync(StartupItem item)
        {
            bool oldState = item.IsEnabled;
            bool requestedState = item.IsEnabled;

            try
            {
                oldState = !item.IsEnabled;

                bool success = await _startupService.ToggleStartupItemAsync(item, requestedState);
                if (success)
                {
                    string action = requestedState ? "enabled" : "disabled";
                    OnStartupToggled?.Invoke("Startup Configured", $"Successfully {action} {item.Name}.");
                    
                    // Reload to sort correctly
                    await LoadStartupItemsAsync();
                }
                else
                {
                    // Restore visual checkbox state on failure
                    item.IsEnabled = oldState;
                    OnStartupToggled?.Invoke("Action Blocked", $"Could not toggle {item.Name}. Elevated rights may be required.");
                }
            }
            catch (UnauthorizedAccessException)
            {
                item.IsEnabled = oldState;
                OnStartupToggled?.Invoke("Access Denied", "Run MemGuard as Administrator to modify system startup keys.");
            }
            catch (Exception)
            {
                item.IsEnabled = oldState;
                OnStartupToggled?.Invoke("Error", $"An error occurred trying to modify {item.Name}.");
            }
        }

        private int GetImpactWeight(string impact)
        {
            return impact switch
            {
                "High" => 1,
                "Medium" => 2,
                "Low" => 3,
                _ => 4
            };
        }

        // Standard way to log since logger isn't injected directly but can be queried
        private void _loggerError(string msg, Exception ex)
        {
            // Fail silently or call global app logic
        }

        public event Action<string, string>? OnStartupToggled;
    }
}

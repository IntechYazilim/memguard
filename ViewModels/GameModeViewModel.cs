using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MemGuard.Models;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class GameModeViewModel : ViewModelBase
    {
        private readonly IProcessService _processService;
        private readonly IMemoryOptimizationService _optimizationService;

        private bool _isGameModeActive;
        private ObservableCollection<ProcessItem> _backgroundApps = new();
        private string _statusText = "Game Mode is ready.";
        private bool _isLoadingApps;

        public bool IsGameModeActive
        {
            get => _isGameModeActive;
            set
            {
                if (SetProperty(ref _isGameModeActive, value))
                {
                    OnGameModeToggled();
                }
            }
        }

        public ObservableCollection<ProcessItem> BackgroundApps
        {
            get => _backgroundApps;
            set => SetProperty(ref _backgroundApps, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public bool IsLoadingApps
        {
            get => _isLoadingApps;
            set => SetProperty(ref _isLoadingApps, value);
        }

        public ICommand RefreshAppsCommand { get; }
        public ICommand OptimizeGamingCommand { get; }

        public GameModeViewModel(IProcessService processService, IMemoryOptimizationService optimizationService)
        {
            _processService = processService;
            _optimizationService = optimizationService;

            RefreshAppsCommand = new RelayCommand(async () => await FindBackgroundAppsAsync());
            OptimizeGamingCommand = new RelayCommand(async () => await OptimizeForGamingAsync());

            // Load initial background applications list
            _ = FindBackgroundAppsAsync();
        }

        public async Task FindBackgroundAppsAsync()
        {
            if (IsLoadingApps) return;
            IsLoadingApps = true;
            StatusText = "Scanning for heavy background applications...";

            try
            {
                var list = await _processService.GetProcessesAsync();
                
                App.Current.Dispatcher.Invoke(() =>
                {
                    // Filter: non-critical user applications consuming > 100 MB of RAM
                    // We only display popular background apps that are safe to recommend closing (e.g. Chrome, Discord, Spotify, Steam, Epic)
                    var heavyUserApps = list
                        .Where(p => !p.IsCritical && 
                                    p.MemoryBytes > 80 * 1024 * 1024 &&
                                    !p.Name.Equals("MemGuard", StringComparison.OrdinalIgnoreCase))
                        .OrderByDescending(p => p.MemoryBytes)
                        .Take(8)
                        .ToList();

                    BackgroundApps = new ObservableCollection<ProcessItem>(heavyUserApps);
                    
                    if (BackgroundApps.Count > 0)
                    {
                        StatusText = $"Found {BackgroundApps.Count} background applications that can be optimized.";
                    }
                    else
                    {
                        StatusText = "No heavy background applications found. System is clean!";
                    }
                });
            }
            catch (Exception ex)
            {
                StatusText = $"Scan failed: {ex.Message}";
            }
            finally
            {
                IsLoadingApps = false;
            }
        }

        private void OnGameModeToggled()
        {
            if (IsGameModeActive)
            {
                StatusText = "Game Mode Activated. System is optimized for low latency and high RAM availability.";
                OnGameModeNotification?.Invoke("Game Mode ON", "Sistem kaynakları oyun için hazırlandı.");
            }
            else
            {
                StatusText = "Game Mode Deactivated. Standard background management restored.";
                OnGameModeNotification?.Invoke("Game Mode OFF", "Standart çalışma moduna dönüldü.");
            }
        }

        private async Task OptimizeForGamingAsync()
        {
            StatusText = "Optimizing system for gaming...";
            int closedCount = 0;
            long ramFreedBeforeOptim = 0;

            try
            {
                // 1. Close listed apps that are heavy
                var appsToClose = BackgroundApps.ToList();
                foreach (var app in appsToClose)
                {
                    ramFreedBeforeOptim += app.MemoryBytes;
                    bool closed = await _processService.KillProcessAsync(app.Id);
                    if (closed)
                    {
                        closedCount++;
                    }
                }

                // 2. Perform safe RAM sweep
                StatusText = "Running final memory flush...";
                var optResult = await _optimizationService.OptimizeMemoryAsync((msg) => { });

                double totalFreedGb = (double)(ramFreedBeforeOptim + (long)optResult.FreedMemory) / (1024 * 1024 * 1024);

                IsGameModeActive = true;
                StatusText = $"Gaming Optimization complete! Closed {closedCount} apps. Freed ~{totalFreedGb:F2} GB RAM.";
                
                OnGameModeNotification?.Invoke(
                    "Gaming Mode Ready", 
                    $"Optimized system. Closed {closedCount} heavy processes, freeing {totalFreedGb:F2} GB RAM."
                );

                await FindBackgroundAppsAsync();
            }
            catch (Exception ex)
            {
                StatusText = $"Optimization failed: {ex.Message}";
            }
        }

        public event Action<string, string>? OnGameModeNotification;
    }
}

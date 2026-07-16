using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using MemGuard.Models;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class ProcessesViewModel : ViewModelBase
    {
        private readonly IProcessService _processService;
        private readonly DispatcherTimer _autoRefreshTimer;

        private ObservableCollection<ProcessItem> _processes = new();
        private ObservableCollection<ProcessItem> _highMemoryApps = new();
        private ProcessItem? _selectedProcess;
        private string _searchText = string.Empty;
        private string _sortBy = "Memory"; // "Memory" or "Cpu"
        private bool _isLoading;
        private bool _showDetails;

        public ObservableCollection<ProcessItem> Processes
        {
            get => _processes;
            set => SetProperty(ref _processes, value);
        }

        public ObservableCollection<ProcessItem> HighMemoryApps
        {
            get => _highMemoryApps;
            set => SetProperty(ref _highMemoryApps, value);
        }

        public ProcessItem? SelectedProcess
        {
            get => _selectedProcess;
            set
            {
                if (SetProperty(ref _selectedProcess, value))
                {
                    ShowDetails = value != null;
                }
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterProcesses();
                }
            }
        }

        public string SortBy
        {
            get => _sortBy;
            set
            {
                if (SetProperty(ref _sortBy, value))
                {
                    SortProcesses();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool ShowDetails
        {
            get => _showDetails;
            set => SetProperty(ref _showDetails, value);
        }

        public ICommand RefreshCommand { get; }
        public ICommand TerminateCommand { get; }
        public ICommand CloseDetailsCommand { get; }

        public ProcessesViewModel(IProcessService processService)
        {
            _processService = processService;

            RefreshCommand = new RelayCommand(async () => await LoadProcessesAsync(showIndicator: true));
            TerminateCommand = new RelayCommand(async () => await TerminateProcessAsync(), () => SelectedProcess != null && !SelectedProcess.IsCritical);
            CloseDetailsCommand = new RelayCommand(() => SelectedProcess = null);

            // Initial load
            _ = LoadProcessesAsync(showIndicator: true);

            // Auto refresh process lists every 4 seconds
            _autoRefreshTimer = new DispatcherTimer();
            _autoRefreshTimer.Interval = TimeSpan.FromSeconds(4);
            _autoRefreshTimer.Tick += async (s, e) => await LoadProcessesAsync(showIndicator: false);
            _autoRefreshTimer.Start();
        }

        public void StopTimer()
        {
            _autoRefreshTimer.Stop();
        }

        private async Task LoadProcessesAsync(bool showIndicator)
        {
            if (IsLoading) return;

            if (showIndicator)
            {
                IsLoading = true;
            }

            try
            {
                var list = await _processService.GetProcessesAsync();
                
                // Keep UI thread updates smooth
                App.Current.Dispatcher.Invoke(() =>
                {
                    // Merge or replace list
                    // Since it's sorted, we can replace the collection
                    Processes = new ObservableCollection<ProcessItem>(list);

                    // Extract High Memory user processes (> 300MB) for recommendations
                    var highMem = list
                        .Where(p => !p.IsCritical && p.MemoryBytes > 300 * 1024 * 1024)
                        .OrderByDescending(p => p.MemoryBytes)
                        .Take(3)
                        .ToList();

                    HighMemoryApps = new ObservableCollection<ProcessItem>(highMem);

                    // Re-apply filters/sorts
                    FilterProcesses();
                    SortProcesses();

                    // Restore selected process reference if still alive
                    if (SelectedProcess != null)
                    {
                        var matching = Processes.FirstOrDefault(p => p.Id == SelectedProcess.Id);
                        if (matching != null)
                        {
                            SelectedProcess = matching;
                        }
                        else
                        {
                            SelectedProcess = null; // process died/closed
                        }
                    }
                });
            }
            catch
            {
                // Silently log/handle
            }
            finally
            {
                if (showIndicator)
                {
                    IsLoading = false;
                }
            }
        }

        private void FilterProcesses()
        {
            var view = CollectionViewSource.GetDefaultView(Processes);
            if (view == null) return;

            if (string.IsNullOrWhiteSpace(SearchText))
            {
                view.Filter = null;
            }
            else
            {
                var query = SearchText.Trim().ToLowerInvariant();
                view.Filter = obj =>
                {
                    if (obj is ProcessItem item)
                    {
                        return item.Name.ToLowerInvariant().Contains(query) ||
                               item.Id.ToString().Contains(query) ||
                               item.Publisher.ToLowerInvariant().Contains(query);
                    }
                    return false;
                };
            }
        }

        private void SortProcesses()
        {
            var view = CollectionViewSource.GetDefaultView(Processes);
            if (view == null) return;

            view.SortDescriptions.Clear();
            if (SortBy == "Cpu")
            {
                view.SortDescriptions.Add(new SortDescription(nameof(ProcessItem.CpuUsage), ListSortDirection.Descending));
            }
            else
            {
                view.SortDescriptions.Add(new SortDescription(nameof(ProcessItem.MemoryBytes), ListSortDirection.Descending));
            }
        }

        private async Task TerminateProcessAsync()
        {
            if (SelectedProcess == null || SelectedProcess.IsCritical) return;

            int pid = SelectedProcess.Id;
            string name = SelectedProcess.Name;

            bool success = await _processService.KillProcessAsync(pid);
            if (success)
            {
                SelectedProcess = null;
                // Wait slightly and refresh
                await Task.Delay(500);
                await LoadProcessesAsync(showIndicator: false);
                
                OnProcessTerminated?.Invoke("Process Terminated", $"Successfully closed {name}.");
            }
            else
            {
                OnProcessTerminated?.Invoke("Error", $"Could not close process {name}. Access denied.");
            }
        }

        public event Action<string, string>? OnProcessTerminated;
    }
}

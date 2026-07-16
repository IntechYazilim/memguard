using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using MemGuard.Models;
using MemGuard.Services;

namespace MemGuard.ViewModels
{
    public class CleanerViewModel : ViewModelBase
    {
        private readonly ICleanerService _cleanerService;

        private ObservableCollection<CleanCategory> _categories = new();
        private bool _isScanning;
        private bool _isCleaning;
        private string _cleanableSizeText = "0.0 MB";
        private string _cleanedResultText = string.Empty;
        private bool _showCleanedResult;

        public ObservableCollection<CleanCategory> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        public bool IsScanning
        {
            get => _isScanning;
            set => SetProperty(ref _isScanning, value);
        }

        public bool IsCleaning
        {
            get => _isCleaning;
            set => SetProperty(ref _isCleaning, value);
        }

        public string CleanableSizeText
        {
            get => _cleanableSizeText;
            set => SetProperty(ref _cleanableSizeText, value);
        }

        public string CleanedResultText
        {
            get => _cleanedResultText;
            set => SetProperty(ref _cleanedResultText, value);
        }

        public bool ShowCleanedResult
        {
            get => _showCleanedResult;
            set => SetProperty(ref _showCleanedResult, value);
        }

        public ICommand ScanCommand { get; }
        public ICommand CleanCommand { get; }

        public CleanerViewModel(ICleanerService cleanerService)
        {
            _cleanerService = cleanerService;

            ScanCommand = new RelayCommand(async () => await ScanSystemAsync());
            CleanCommand = new RelayCommand(async () => await CleanSystemAsync(), () => Categories.Any(c => c.IsSelected && c.SizeBytes > 0));

            // Initialize categories
            _ = InitializeCategories();
        }

        private async Task InitializeCategories()
        {
            var list = await _cleanerService.GetCategoriesAsync();
            Categories = new ObservableCollection<CleanCategory>(list);
            UpdateCleanableTotal();
        }

        private async Task ScanSystemAsync()
        {
            if (IsScanning || IsCleaning) return;

            IsScanning = true;
            ShowCleanedResult = false;
            UpdateCleanableTotal();

            try
            {
                foreach (var cat in Categories)
                {
                    if (cat.IsSelected)
                    {
                        await _cleanerService.ScanCategoryAsync(cat);
                        UpdateCleanableTotal();
                    }
                    else
                    {
                        cat.Status = "Skipped";
                        cat.SizeBytes = 0;
                        cat.FileCount = 0;
                    }
                }
            }
            catch
            {
                // handle error
            }
            finally
            {
                IsScanning = false;
                UpdateCleanableTotal();
            }
        }

        private async Task CleanSystemAsync()
        {
            if (IsScanning || IsCleaning) return;

            // Check if any selected category actually has files to clean
            var selected = Categories.Where(c => c.IsSelected && c.SizeBytes > 0).ToList();
            if (selected.Count == 0) return;

            IsCleaning = true;
            ShowCleanedResult = false;
            long totalBytesFreed = 0;

            try
            {
                foreach (var cat in selected)
                {
                    long freed = await _cleanerService.CleanCategoryAsync(cat);
                    totalBytesFreed += freed;
                    UpdateCleanableTotal();
                }

                double mb = (double)totalBytesFreed / (1024 * 1024);
                if (mb >= 1024)
                {
                    CleanedResultText = $"Successfully cleaned {(mb / 1024):F2} GB of junk files.";
                }
                else
                {
                    CleanedResultText = $"Successfully cleaned {mb:F1} MB of junk files.";
                }

                ShowCleanedResult = true;
                OnCleanFinished?.Invoke("System Cleaned", CleanedResultText);
            }
            catch (Exception ex)
            {
                CleanedResultText = $"Clean failed: {ex.Message}";
                ShowCleanedResult = true;
            }
            finally
            {
                IsCleaning = false;
                UpdateCleanableTotal();
            }
        }

        public void UpdateCleanableTotal()
        {
            long total = Categories.Where(c => c.IsSelected).Sum(c => c.SizeBytes);
            double mb = (double)total / (1024 * 1024);
            if (mb >= 1024)
            {
                CleanableSizeText = $"{(mb / 1024):F2} GB";
            }
            else
            {
                CleanableSizeText = $"{mb:F1} MB";
            }
        }

        public event Action<string, string>? OnCleanFinished;
    }
}

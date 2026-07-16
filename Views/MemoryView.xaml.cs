using System;
using System.Windows.Controls;
using System.Windows.Threading;
using MemGuard.ViewModels;

namespace MemGuard.Views
{
    public partial class MemoryView : UserControl
    {
        private readonly DispatcherTimer _refreshTimer;

        public MemoryView()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _refreshTimer.Tick += (_, _) =>
            {
                if (DataContext is MemoryViewModel vm && !vm.IsOptimizing)
                {
                    vm.RefreshStats();
                }
            };

            Loaded += (_, _) => _refreshTimer.Start();
            Unloaded += (_, _) => _refreshTimer.Stop();
        }
    }
}

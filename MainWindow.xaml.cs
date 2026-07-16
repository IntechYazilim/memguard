using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using MemGuard.Controls;
using MemGuard.ViewModels;

namespace MemGuard
{
    public partial class MainWindow : Window
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _isExiting;
        private bool _showTrayHint = true;

        public MainWindow()
        {
            InitializeComponent();
            ApplyBrandingAssets();

            var viewModel = new MainViewModel();
            DataContext = viewModel;
            viewModel.RequestShowNotification += ViewModel_RequestShowNotification;

            _notifyIcon = new NotifyIcon
            {
                Icon = LoadBrandIcon(),
                Text = "MemGuard",
                Visible = true
            };
            _notifyIcon.DoubleClick += (_, _) => RestoreWindow();

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Paneli Ac", null, (_, _) => RestoreWindow());
            contextMenu.Items.Add("Bellegi Optimize Et", null, (_, _) =>
            {
                RestoreWindow();
                viewModel.NavigateCommand.Execute("Memory");
                viewModel.MemoryVM.OptimizeCommand.Execute(null);
            });
            contextMenu.Items.Add("Oyun Modunu Degistir", null, (_, _) =>
            {
                viewModel.GameModeVM.IsGameModeActive = !viewModel.GameModeVM.IsGameModeActive;
            });
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("Cikis", null, (_, _) => ExitApplication());
            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        public void RestoreFromExternalLaunch()
        {
            RestoreWindow();
        }

        public void StartHiddenInTray()
        {
            WindowState = WindowState.Minimized;
            Hide();
        }

        private void ApplyBrandingAssets()
        {
            try
            {
                Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/brand-mark.png", UriKind.Absolute));
            }
            catch
            {
                // Branding load failures should not block app startup.
            }
        }

        private void ViewModel_RequestShowNotification(string title, string message, NotificationType type)
        {
            Dispatcher.Invoke(() => ToastNotification.Show(title, message, type));
        }

        private static Icon LoadBrandIcon()
        {
            try
            {
                var exePath = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath))
                {
                    return System.Drawing.Icon.ExtractAssociatedIcon(exePath) ?? SystemIcons.Application;
                }
            }
            catch
            {
                // Fall back below.
            }

            return SystemIcons.Application;
        }

        private void RestoreWindow()
        {
            Show();
            ShowInTaskbar = true;

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void ExitApplication()
        {
            _isExiting = true;
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (DataContext is MainViewModel vm && vm.Settings.Current.MinimizeToTray && !_isExiting)
            {
                e.Cancel = true;
                Hide();
                ShowInTaskbar = false;

                if (_showTrayHint)
                {
                    _notifyIcon.ShowBalloonTip(
                        3000,
                        "MemGuard",
                        "MemGuard arka planda calismaya devam ediyor. Acmak icin cift tiklayin.",
                        ToolTipIcon.Info);
                    _showTrayHint = false;
                }
            }
            else
            {
                (DataContext as MainViewModel)?.Shutdown();
                _notifyIcon.Dispose();
            }

            base.OnClosing(e);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

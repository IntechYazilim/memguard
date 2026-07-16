using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace MemGuard.Controls
{
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    public partial class NotificationToast : UserControl
    {
        private readonly DispatcherTimer _timer;
        private Storyboard? _showStoryboard;
        private Storyboard? _hideStoryboard;

        public NotificationToast()
        {
            InitializeComponent();

            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(4);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer.Stop();
            Dismiss();
        }

        public void Show(string title, string message, NotificationType type)
        {
            // Stop any running timer
            _timer.Stop();

            // Set content
            TitleText.Text = title;
            MessageText.Text = message;

            // Configure type visual styles
            ConfigureToastType(type);

            // Fetch animations
            _showStoryboard = Resources["ShowStoryboard"] as Storyboard;
            _hideStoryboard = Resources["HideStoryboard"] as Storyboard;

            if (_showStoryboard != null)
            {
                _showStoryboard.Begin(this);
            }
            else
            {
                Visibility = Visibility.Visible;
                Opacity = 1.0;
            }

            // Start auto-dismiss timer
            _timer.Start();
        }

        public void Dismiss()
        {
            _timer.Stop();
            _hideStoryboard = Resources["HideStoryboard"] as Storyboard;

            if (_hideStoryboard != null)
            {
                _hideStoryboard.Begin(this);
            }
            else
            {
                Visibility = Visibility.Collapsed;
            }
        }

        private void ConfigureToastType(NotificationType type)
        {
            // Hide all icons
            IconInfo.Visibility = Visibility.Collapsed;
            IconSuccess.Visibility = Visibility.Collapsed;
            IconWarning.Visibility = Visibility.Collapsed;
            IconError.Visibility = Visibility.Collapsed;

            Color accentColor;

            switch (type)
            {
                case NotificationType.Success:
                    IconSuccess.Visibility = Visibility.Visible;
                    accentColor = Color.FromRgb(16, 185, 129); // Success green
                    break;
                case NotificationType.Warning:
                    IconWarning.Visibility = Visibility.Visible;
                    accentColor = Color.FromRgb(251, 191, 36); // Warning yellow
                    break;
                case NotificationType.Error:
                    IconError.Visibility = Visibility.Visible;
                    accentColor = Color.FromRgb(239, 68, 68); // Danger red
                    break;
                case NotificationType.Info:
                default:
                    IconInfo.Visibility = Visibility.Visible;
                    accentColor = Color.FromRgb(0, 240, 255); // Info electric blue
                    break;
            }

            ColorIndicatorBorder.BorderBrush = new SolidColorBrush(accentColor);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Dismiss();
        }
    }
}

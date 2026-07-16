using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace MemGuard.Controls
{
    public partial class CircularHealthGauge : UserControl
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(CircularHealthGauge),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Clamp(value, 0.0, 100.0));
        }

        public CircularHealthGauge()
        {
            InitializeComponent();
            Loaded += (s, e) => UpdateGauge(Value);
            SizeChanged += (s, e) => UpdateGauge(Value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularHealthGauge gauge)
            {
                gauge.UpdateGauge((double)e.NewValue);
            }
        }

        private void UpdateGauge(double value)
        {
            if (!IsLoaded) return;

            value = Math.Clamp(value, 0.0, 100.0);
            ValueText.Text = $"{Math.Round(value)}";
            double size = Math.Max(80.0, Math.Min(ActualWidth > 0 ? ActualWidth : Width, ActualHeight > 0 ? ActualHeight : Height));
            if (double.IsNaN(size) || size <= 0)
            {
                size = 220.0;
            }

            double stroke = Math.Max(10.0, size * 0.064);
            double ringSize = size * 0.764;
            double radius = (ringSize / 2.0) - (stroke / 2.0);
            double centerX = size / 2.0;
            double centerY = size / 2.0;

            ArcCanvas.Width = size;
            ArcCanvas.Height = size;
            TrackRing.Width = ringSize;
            TrackRing.Height = ringSize;
            TrackRing.StrokeThickness = stroke;
            ProgressPath.StrokeThickness = stroke;
            GlowPath.StrokeThickness = stroke;

            if (GlowPath.Effect is BlurEffect blur)
            {
                blur.Radius = Math.Max(6.0, stroke * 0.58);
            }

            ValueText.FontSize = size * 0.21;
            LabelText.Width = size * 0.58;
            LabelText.FontSize = Math.Max(8.0, size * 0.048);
            LabelText.LineHeight = LabelText.FontSize * 0.95;

            double startAngle = 130.0;
            double sweepRange = 280.0;
            double currentAngle = startAngle + (value / 100.0) * sweepRange;

            Point startPoint = GetArcPoint(centerX, centerY, radius, startAngle);
            Point endPoint = GetArcPoint(centerX, centerY, radius, currentAngle);
            bool isLargeArc = (value / 100.0) * sweepRange > 180.0;

            var progressFigure = new PathFigure { StartPoint = startPoint, IsClosed = false };
            progressFigure.Segments.Add(new ArcSegment(endPoint, new Size(radius, radius), 0, isLargeArc, SweepDirection.Clockwise, true));
            ProgressPath.Data = new PathGeometry(new[] { progressFigure });
            GlowPath.Data = ProgressPath.Data.Clone();

            if (value < 30)
            {
                ValueText.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // DangerRed
            }
            else if (value < 50)
            {
                ValueText.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36)); // WarningYellow
            }
            else
            {
                ValueText.Foreground = Application.Current.Resources["TextWhiteBrush"] as Brush ?? new SolidColorBrush(Colors.White);
            }
        }

        private static Point GetArcPoint(double centerX, double centerY, double radius, double angleDegrees)
        {
            double angleRad = angleDegrees * Math.PI / 180.0;
            double x = centerX + radius * Math.Cos(angleRad);
            double y = centerY + radius * Math.Sin(angleRad);
            return new Point(x, y);
        }
    }
}

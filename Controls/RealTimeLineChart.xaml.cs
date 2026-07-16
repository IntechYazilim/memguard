using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MemGuard.Controls
{
    public partial class RealTimeLineChart : UserControl
    {
        private readonly List<double> _history = new();
        private const int MaxDataPoints = 60;

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(double),
                typeof(RealTimeLineChart),
                new PropertyMetadata(0.0, OnValueChanged));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, Math.Clamp(value, 0.0, 100.0));
        }

        public static readonly DependencyProperty LineBrushProperty =
            DependencyProperty.Register(
                nameof(LineBrush),
                typeof(Brush),
                typeof(RealTimeLineChart),
                new PropertyMetadata(Brushes.DodgerBlue, OnBrushChanged));

        public Brush LineBrush
        {
            get => (Brush)GetValue(LineBrushProperty);
            set => SetValue(LineBrushProperty, value);
        }

        public RealTimeLineChart()
        {
            InitializeComponent();
            
            // Populate history with initial 0s
            for (int i = 0; i < MaxDataPoints; i++)
            {
                _history.Add(0);
            }

            SizeChanged += (s, e) => Redraw();
            Loaded += (s, e) => Redraw();
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RealTimeLineChart chart)
            {
                chart.AddDataPoint((double)e.NewValue);
            }
        }

        private static void OnBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is RealTimeLineChart chart)
            {
                chart.UpdateColors();
            }
        }

        private void AddDataPoint(double val)
        {
            val = Math.Clamp(val, 0.0, 100.0);
            _history.RemoveAt(0);
            _history.Add(val);

            CurrentValueText.Text = $"{Math.Round(val)}%";
            Redraw();
        }

        private void UpdateColors()
        {
            LinePath.Stroke = LineBrush;
            GlowLinePath.Stroke = LineBrush;

            if (LineBrush is SolidColorBrush solid)
            {
                AreaGradientStart.Color = Color.FromArgb(40, solid.Color.R, solid.Color.G, solid.Color.B);
            }
        }

        private void Redraw()
        {
            double w = ChartCanvas.ActualWidth;
            double h = ChartCanvas.ActualHeight;

            if (w <= 0 || h <= 0 || _history.Count < 2) return;

            UpdateColors();

            // 1. Draw Grid Lines
            GridLine25.X1 = 0; GridLine25.Y1 = h * 0.75; GridLine25.X2 = w; GridLine25.Y2 = h * 0.75;
            GridLine50.X1 = 0; GridLine50.Y1 = h * 0.50; GridLine50.X2 = w; GridLine50.Y2 = h * 0.50;
            GridLine75.X1 = 0; GridLine75.Y1 = h * 0.25; GridLine75.X2 = w; GridLine75.Y2 = h * 0.25;

            // 2. Generate line coordinates
            double xStep = w / (MaxDataPoints - 1);
            var points = new PointCollection();

            for (int i = 0; i < _history.Count; i++)
            {
                double x = i * xStep;
                // Height minus value percentage to invert coordinate (0% at bottom, 100% at top)
                double y = h - (_history[i] / 100.0) * h;
                
                // Keep points inside bounds
                y = Math.Clamp(y, 1.0, h - 1.0);
                points.Add(new Point(x, y));
            }

            // 3. Update line paths
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure { StartPoint = points[0], IsClosed = false };

            var segmentCollection = new PathSegmentCollection();
            for (int i = 1; i < points.Count; i++)
            {
                segmentCollection.Add(new LineSegment(points[i], true));
            }
            pathFigure.Segments = segmentCollection;
            pathGeometry.Figures.Add(pathFigure);

            LinePath.Data = pathGeometry;
            GlowLinePath.Data = pathGeometry;

            // 4. Update area gradient path (closes back down to bottom corners)
            var areaGeometry = new PathGeometry();
            var areaFigure = new PathFigure { StartPoint = new Point(0, h), IsClosed = true, IsFilled = true };
            
            var areaSegments = new PathSegmentCollection();
            areaSegments.Add(new LineSegment(points[0], true));
            for (int i = 1; i < points.Count; i++)
            {
                areaSegments.Add(new LineSegment(points[i], true));
            }
            areaSegments.Add(new LineSegment(new Point(w, h), true));
            
            areaFigure.Segments = areaSegments;
            areaGeometry.Figures.Add(areaFigure);

            AreaPath.Data = areaGeometry;
        }
    }
}

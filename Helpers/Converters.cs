using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MemGuard.Helpers
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return !b;
            }
            return true;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility v)
            {
                return v == Visibility.Visible;
            }
            return false;
        }
    }

    public class ImpactColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string impact = value?.ToString() ?? "Low";
            string type = parameter?.ToString() ?? "fg";

            if (type == "bg")
            {
                return impact switch
                {
                    "High" => new SolidColorBrush(Color.FromArgb(50, 239, 68, 68)),    // semi-trans red
                    "Medium" => new SolidColorBrush(Color.FromArgb(50, 251, 191, 36)),  // semi-trans yellow
                    "Low" => new SolidColorBrush(Color.FromArgb(50, 16, 185, 129)),    // semi-trans green
                    _ => new SolidColorBrush(Color.FromArgb(30, 156, 163, 175))       // grey
                };
            }
            else // fg
            {
                return impact switch
                {
                    "High" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),    // bright red
                    "Medium" => new SolidColorBrush(Color.FromRgb(251, 191, 36)),  // bright yellow
                    "Low" => new SolidColorBrush(Color.FromRgb(16, 185, 129)),    // bright green
                    _ => new SolidColorBrush(Color.FromRgb(243, 244, 246))        // white
                };
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StringEqualsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var current = value?.ToString() ?? string.Empty;
            var expected = parameter?.ToString() ?? string.Empty;
            return string.Equals(current, expected, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isChecked && isChecked)
            {
                return parameter?.ToString() ?? string.Empty;
            }

            return Binding.DoNothing;
        }
    }
}

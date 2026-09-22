using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Finite.App;

/// <summary>true → subtle blue highlight brush for today's cell.</summary>
public class BoolToTodayBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? new SolidColorBrush(Color.FromArgb(40, 0x3B, 0x82, 0xF6))
            : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>true → full opacity, false → faded (out-of-month days).</summary>
public class InMonthOpacityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 1.0 : 0.3;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

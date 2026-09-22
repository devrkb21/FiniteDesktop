using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Finite.App;

/// <summary>true → red brush, false → secondary gray (for overdue labels).</summary>
public class BoolToRedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? new SolidColorBrush(Color.FromRgb(0xF8, 0x71, 0x71))
                         : new SolidColorBrush(Color.FromRgb(0xA0, 0xA0, 0xA0));

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

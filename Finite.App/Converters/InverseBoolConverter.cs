using System.Globalization;
using System.Windows.Data;

namespace Finite.App;

/// <summary>bool → !bool (for IsSystem delete-button disabling etc).</summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}

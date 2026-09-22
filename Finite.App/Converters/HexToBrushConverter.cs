using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Finite.App;

/// <summary>
/// "#RRGGBB" → SolidColorBrush, never throwing (invalid/missing → neutral gray).
/// Used for account/category color swatches.
/// </summary>
public class HexToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string hex && !string.IsNullOrWhiteSpace(hex))
        {
            try
            {
                var c = (Color)ColorConverter.ConvertFromString(hex.Trim());
                return new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B));
            }
            catch (FormatException)
            {
                // fall through
            }
        }
        return new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

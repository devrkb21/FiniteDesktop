using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace Finite.App.Controls;

/// <summary>
/// Base for hand-rolled charts: re-renders when the bound collection's *contents* change
/// (ObservableCollection items added/removed), not just when the reference changes —
/// this was why charts appeared blank after data loaded.
/// </summary>
public abstract class ChartBase : FrameworkElement
{
    static ChartBase()
    {
        // Charts paint their own background, so re-render every chart when the
        // app theme changes (otherwise they keep the old theme's colors).
        Wpf.Ui.Appearance.ApplicationThemeManager.Changed += (_, _) =>
        {
            var handler = ThemeChanged;
            handler?.Invoke(null, EventArgs.Empty);
        };
    }

    /// <summary>Raised app-wide whenever the theme changes; charts subscribe to re-render.</summary>
    public static event EventHandler? ThemeChanged;

    protected ChartBase()
    {
        Loaded += (_, _) => ThemeChanged += OnThemeChangedInstance;
        Unloaded += (_, _) => ThemeChanged -= OnThemeChangedInstance;
    }

    private void OnThemeChangedInstance(object? sender, EventArgs e) => InvalidateVisual();

    protected static Brush? TryParseBrush(string? hex)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return null;
            var color = (Color)ColorConverter.ConvertFromString(hex.Trim());
            return new SolidColorBrush(color);
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>Card background from the active WPF-UI theme (falls back to transparent).</summary>
    protected static Brush FindBackgroundBrush()
    {
        if (Application.Current?.TryFindResource("ApplicationBackgroundBrush") is Brush brush)
            return brush;
        return Brushes.Transparent;
    }

    protected static Typeface DefaultTypeface { get; } =
        new(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

    protected static string FormatCompact(decimal value) =>
        value >= 1_000_000 ? $"{value / 1_000_000:0.#}M"
        : value >= 1_000 ? $"{value / 1_000:0.#}k"
        : $"{value:0}";

    /// <summary>Subscribe to collection-changed so OnRender re-runs as items stream in.</summary>
    protected void Watch(object? newValue, ref INotifyCollectionChanged? subscribed)
    {
        if (subscribed is not null)
        {
            subscribed.CollectionChanged -= WatchedCollectionChanged;
            subscribed = null;
        }

        if (newValue is INotifyCollectionChanged changed)
        {
            changed.CollectionChanged += WatchedCollectionChanged;
            subscribed = changed;
        }
    }

    protected void Unwatch(ref INotifyCollectionChanged? subscribed)
    {
        if (subscribed is not null)
        {
            subscribed.CollectionChanged -= WatchedCollectionChanged;
            subscribed = null;
        }
    }

    private void WatchedCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        => InvalidateVisual();
}

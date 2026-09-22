using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Finite.App.Controls;

/// <summary>Dependency-free grouped bar chart (1-2 series) with hover tooltips.</summary>
public class SimpleBarChart : ChartBase
{
    public record NamedSeries(string Label, string Color, IReadOnlyList<decimal> Values);

    private readonly List<(Rect Bar, string Tooltip)> _hitBoxes = [];
    private INotifyCollectionChanged? _watchedSeries;
    private INotifyCollectionChanged? _watchedLabels;

    public static readonly DependencyProperty SeriesProperty = DependencyProperty.Register(
        nameof(Series), typeof(IReadOnlyList<NamedSeries>), typeof(SimpleBarChart),
        new FrameworkPropertyMetadata(Array.Empty<NamedSeries>(),
            FrameworkPropertyMetadataOptions.AffectsRender, OnSeriesChanged));

    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels), typeof(IReadOnlyList<string>), typeof(SimpleBarChart),
        new FrameworkPropertyMetadata(Array.Empty<string>(),
            FrameworkPropertyMetadataOptions.AffectsRender, OnLabelsChanged));

    public IReadOnlyList<NamedSeries> Series
    {
        get => (IReadOnlyList<NamedSeries>)GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public IReadOnlyList<string> Labels
    {
        get => (IReadOnlyList<string>)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    private static void OnSeriesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (SimpleBarChart)d;
        chart.Unwatch(ref chart._watchedSeries);
        chart.Watch(e.NewValue, ref chart._watchedSeries);
    }

    private static void OnLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (SimpleBarChart)d;
        chart.Unwatch(ref chart._watchedLabels);
        chart.Watch(e.NewValue, ref chart._watchedLabels);
    }

    protected override void OnRender(DrawingContext dc)
    {
        _hitBoxes.Clear();
        var width = ActualWidth;
        var height = ActualHeight;
        if (width < 10 || height < 10) return;

        dc.DrawRectangle(FindBackgroundBrush(), null, new Rect(0, 0, width, height));

        var series = Series ?? Array.Empty<NamedSeries>();
        var labels = Labels ?? Array.Empty<string>();
        if (series.Count == 0) return;

        var count = Math.Max(labels.Count, series.Max(s => s.Values.Count));
        if (count == 0) return;

        var max = Math.Max(1m, series.SelectMany(s => s.Values).DefaultIfEmpty(0).Max());
        var left = 52d;
        var bottom = 28d;
        var plotW = Math.Max(10, width - left - 8);
        var plotH = Math.Max(10, height - bottom - 8);

        var gridBrush = new SolidColorBrush(Color.FromArgb(38, 148, 163, 184));
        var textBrush = new SolidColorBrush(Color.FromArgb(190, 148, 163, 184));
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var i = 0; i <= 4; i++)
        {
            var y = 8 + plotH * i / 4;
            dc.DrawLine(new Pen(gridBrush, 1), new Point(left, y), new Point(width - 8, y));
            var value = max * (4 - i) / 4;
            dc.DrawText(new FormattedText(FormatCompact(value), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, DefaultTypeface, 10, textBrush, dpi), new Point(4, y - 7));
        }

        var groupW = plotW / count;
        var barW = Math.Max(3, groupW * 0.7 / series.Count);

        for (var i = 0; i < count; i++)
        {
            for (var s = 0; s < series.Count; s++)
            {
                var values = series[s].Values;
                if (i >= values.Count) continue;
                var v = values[i];
                var barH = (double)(v / max) * plotH;
                var x = left + groupW * i + (groupW - barW * series.Count) / 2 + barW * s;
                var y = 8 + plotH - barH;

                var brush = TryParseBrush(series[s].Color) ?? Brushes.SteelBlue;
                var rect = new Rect(x, y, Math.Max(1, barW - 1), Math.Max(0, barH));
                dc.DrawRoundedRectangle(brush, null, rect, 2, 2);
                _hitBoxes.Add((rect, $"{series[s].Label}: ৳{v:N2}"));
            }

            if (i < labels.Count)
            {
                var text = new FormattedText(labels[i], CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                    DefaultTypeface, 10, textBrush, dpi);
                dc.DrawText(text, new Point(left + groupW * i + (groupW - text.Width) / 2, height - bottom + 8));
            }
        }

        // Legend (top-left, inside plot margin)
        var lx = left;
        foreach (var s in series)
        {
            var brush = TryParseBrush(s.Color) ?? Brushes.SteelBlue;
            dc.DrawRectangle(brush, null, new Rect(lx, 0, 10, 10));
            var text = new FormattedText(s.Label, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                DefaultTypeface, 10, textBrush, dpi);
            dc.DrawText(text, new Point(lx + 14, -1));
            lx += 14 + text.Width + 14;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var pos = e.GetPosition(this);
        foreach (var (rect, tooltip) in _hitBoxes)
        {
            if (rect.Contains(pos))
            {
                ToolTip = tooltip;
                return;
            }
        }
        ToolTip = null;
    }
}

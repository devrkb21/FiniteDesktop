using System.Collections.Specialized;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Finite.App.Controls;

/// <summary>Dependency-free line/area chart with gradient fill, hover tooltip and end-dot.</summary>
public class SimpleLineChart : ChartBase
{
    private (Rect Area, string Tooltip)[] _hitBoxes = [];
    private INotifyCollectionChanged? _watchedValues;
    private INotifyCollectionChanged? _watchedLabels;

    public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
        nameof(Values), typeof(IReadOnlyList<decimal>), typeof(SimpleLineChart),
        new FrameworkPropertyMetadata(Array.Empty<decimal>(),
            FrameworkPropertyMetadataOptions.AffectsRender, OnValuesChanged));

    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels), typeof(IReadOnlyList<string>), typeof(SimpleLineChart),
        new FrameworkPropertyMetadata(Array.Empty<string>(),
            FrameworkPropertyMetadataOptions.AffectsRender, OnLabelsChanged));

    public static readonly DependencyProperty StrokeColorProperty = DependencyProperty.Register(
        nameof(StrokeColor), typeof(string), typeof(SimpleLineChart),
        new FrameworkPropertyMetadata("#10B981", FrameworkPropertyMetadataOptions.AffectsRender));

    public IReadOnlyList<decimal> Values
    {
        get => (IReadOnlyList<decimal>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public IReadOnlyList<string> Labels
    {
        get => (IReadOnlyList<string>)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    public string StrokeColor
    {
        get => (string)GetValue(StrokeColorProperty);
        set => SetValue(StrokeColorProperty, value);
    }

    private static void OnValuesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (SimpleLineChart)d;
        chart.Unwatch(ref chart._watchedValues);
        chart.Watch(e.NewValue, ref chart._watchedValues);
    }

    private static void OnLabelsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var chart = (SimpleLineChart)d;
        chart.Unwatch(ref chart._watchedLabels);
        chart.Watch(e.NewValue, ref chart._watchedLabels);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width < 10 || height < 10) return;

        dc.DrawRectangle(FindBackgroundBrush(), null, new Rect(0, 0, width, height));

        var values = Values ?? Array.Empty<decimal>();
        var labels = Labels ?? Array.Empty<string>();
        if (values.Count < 2) return;

        var max = values.Max();
        var min = values.Min();

        // Baseline: chart from zero when all values share a sign; pad when mixed/negative.
        if (min >= 0)
        {
            min = 0;
            max = Math.Max(max, 1m);
        }
        else
        {
            min = Math.Min(min, 0m);
            max = Math.Max(max, min + 1m);
        }
        var range = max - min;
        if (range <= 0) range = 1;

        var left = 52d;
        var bottom = 24d;
        var top = 8d;
        var plotW = Math.Max(10, width - left - 8);
        var plotH = Math.Max(10, height - bottom - top);
        var baseY = top + plotH; // y coordinate of value `min` (the zero line)

        var gridBrush = new SolidColorBrush(Color.FromArgb(38, 148, 163, 184));
        var textBrush = new SolidColorBrush(Color.FromArgb(190, 148, 163, 184));
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

        for (var i = 0; i <= 4; i++)
        {
            var y = top + plotH * i / 4;
            dc.DrawLine(new Pen(gridBrush, 1), new Point(left, y), new Point(width - 8, y));
            var value = max - range * i / 4;
            dc.DrawText(new FormattedText(FormatCompact(value), CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight, DefaultTypeface, 10, textBrush, dpi), new Point(4, y - 7));
        }

        var baseColor = TryParseBrush(StrokeColor) is SolidColorBrush sc ? sc.Color : Color.FromRgb(0x10, 0xB9, 0x81);

        Point PointAt(int index)
        {
            var x = left + plotW * index / (values.Count - 1);
            var y = baseY - (float)((values[index] - min) / range) * plotH;
            return new Point(x, y);
        }

        // Build line AND area in one open context — re-opening a frozen/cloned
        // StreamGeometry without BeginFigure threw "BeginFigure must be called
        // before this API", which killed the whole trend chart render.
        var lineGeometry = new StreamGeometry();
        var areaGeometry = new StreamGeometry();
        using (var lineCtx = lineGeometry.Open())
        using (var areaCtx = areaGeometry.Open())
        {
            var first = PointAt(0);
            lineCtx.BeginFigure(first, false, false);
            areaCtx.BeginFigure(new Point(first.X, baseY), false, true);
            areaCtx.LineTo(first, true, false);
            for (var i = 1; i < values.Count; i++)
            {
                var p = PointAt(i);
                lineCtx.LineTo(p, true, false);
                areaCtx.LineTo(p, true, false);
            }
            areaCtx.LineTo(new Point(left + plotW, baseY), true, false);
            areaCtx.LineTo(new Point(left, baseY), true, false);
        }
        lineGeometry.Freeze();
        areaGeometry.Freeze();

        dc.DrawGeometry(
            new LinearGradientBrush(
                Color.FromArgb(80, baseColor.R, baseColor.G, baseColor.B),
                Color.FromArgb(4, baseColor.R, baseColor.G, baseColor.B), 90),
            null, areaGeometry);
        dc.DrawGeometry(null, new Pen(new SolidColorBrush(baseColor), 2), lineGeometry);

        // Zero baseline so negative dips read correctly.
        dc.DrawLine(new Pen(gridBrush, 1), new Point(left, baseY), new Point(width - 8, baseY));

        // End-dot marker on today's value
        var last = PointAt(values.Count - 1);
        dc.DrawEllipse(new SolidColorBrush(baseColor), null, last, 3.5, 3.5);
        dc.DrawEllipse(null, new Pen(new SolidColorBrush(baseColor), 1.5), last, 6.5, 6.5);

        // X labels: first, middle, last
        if (labels.Count > 0)
        {
            foreach (var idx in new[] { 0, labels.Count / 2, labels.Count - 1 })
            {
                var text = new FormattedText(labels[idx], CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight, DefaultTypeface, 10, textBrush, dpi);
                var x = left + plotW * idx / Math.Max(1, labels.Count - 1);
                dc.DrawText(text, new Point(Math.Min(x, width - 8 - text.Width), height - bottom + 4));
            }
        }

        _hitBoxes = values.Select((v, i) =>
        {
            var p = PointAt(i);
            return (new Rect(p.X - 6, p.Y - 6, 12, 12), $"৳{v:N2}");
        }).ToArray();
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

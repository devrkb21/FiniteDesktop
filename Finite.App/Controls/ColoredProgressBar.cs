using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Finite.App.Controls;

/// <summary>
/// Simple progress bar whose fill honors the Foreground property
/// (WPF-UI's themed ProgressBar overrides custom Foreground colors,
/// which broke the budget/goal color coding).
/// NOTE: DefaultStyleKey metadata is overridden in the static ctor —
/// overriding dependency-property metadata per-instance (instance ctor)
/// throws "invocation of the constructor ... threw an exception".
/// </summary>
[TemplatePart(Name = "PART_Track", Type = typeof(Border))]
[TemplatePart(Name = "PART_Fill", Type = typeof(Rectangle))]
[System.Windows.Markup.ContentProperty(nameof(Value))]
public class ColoredProgressBar : Control
{
    private Rectangle? _fill;
    private bool _templateApplied;

    static ColoredProgressBar()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColoredProgressBar),
            new FrameworkPropertyMetadata(typeof(ColoredProgressBar)));
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(ColoredProgressBar),
        new FrameworkPropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(ColoredProgressBar),
        new FrameworkPropertyMetadata(100.0, OnValueChanged));

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(ColoredProgressBar),
        new FrameworkPropertyMetadata(0.0, OnValueChanged));

    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(ColoredProgressBar),
        new FrameworkPropertyMetadata(new CornerRadius(3), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double Minimum { get => (double)GetValue(MinimumProperty); set => SetValue(MinimumProperty, value); }
    public double Maximum { get => (double)GetValue(MaximumProperty); set => SetValue(MaximumProperty, value); }
    public double Value { get => (double)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
    public CornerRadius CornerRadius { get => (CornerRadius)GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public ColoredProgressBar()
    {
        SizeChanged += (_, _) => UpdateFill();
    }

    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _fill = GetTemplateChild("PART_Fill") as Rectangle;
        _templateApplied = true;
        UpdateFill();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ColoredProgressBar)d).UpdateFill();

    private void UpdateFill()
    {
        if (!_templateApplied) return;
        if (_fill is null) _fill = GetTemplateChild("PART_Fill") as Rectangle;
        if (_fill is null) return;

        var range = Maximum - Minimum;
        var fraction = range <= 0 ? 0 : Math.Clamp((Value - Minimum) / range, 0, 1);
        _fill.Width = Math.Max(0, fraction * ActualWidth);
    }
}

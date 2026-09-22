using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Finite.App.Controls;

/// <summary>
/// A compact button that opens a color picker popup: saturation/value box,
/// hue slider, preset swatches and a hex field. Shows the current color as
/// its swatch. Replaces raw "#RRGGBB" text boxes for account/category colors.
/// </summary>
[TemplatePart(Name = "PART_Swatch", Type = typeof(Border))]
[TemplatePart(Name = "PART_SwatchFill", Type = typeof(Rectangle))]
[TemplatePart(Name = "PART_HexText", Type = typeof(TextBlock))]
[TemplatePart(Name = "PART_Preview", Type = typeof(Border))]
[TemplatePart(Name = "PART_Popup", Type = typeof(Popup))]
[TemplatePart(Name = "PART_SvBox", Type = typeof(Border))]
[TemplatePart(Name = "PART_SvCursor", Type = typeof(Ellipse))]
[TemplatePart(Name = "PART_HueTrack", Type = typeof(Border))]
[TemplatePart(Name = "PART_HueCursor", Type = typeof(Rectangle))]
public class ColorPickerButton : Control
{
    private Border? _swatch;
    private Rectangle? _swatchFill;
    private TextBlock? _hexText;
    private Border? _preview;
    private Popup? _popup;
    private Border? _svBox;
    private Ellipse? _svCursor;
    private Border? _hueTrack;
    private Rectangle? _hueCursor;
    private TextBox? _hexInput;
    private bool _updating;
    private bool _presetsBuilt;

    static ColorPickerButton()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPickerButton),
            new FrameworkPropertyMetadata(typeof(ColorPickerButton)));
    }

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(Color), typeof(ColorPickerButton),
        new FrameworkPropertyMetadata(Color.FromRgb(0x3B, 0x82, 0xF6),
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    /// <summary>Selected color as #RRGGBB string (binds to the VM's string property).</summary>
    public static readonly DependencyProperty HexTextProperty = DependencyProperty.Register(
        nameof(HexText), typeof(string), typeof(ColorPickerButton),
        new FrameworkPropertyMetadata("#3B82F6",
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHexTextChanged));

    public string HexText
    {
        get => (string)GetValue(HexTextProperty);
        set => SetValue(HexTextProperty, value);
    }

    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(
        nameof(Presets), typeof(IReadOnlyList<Color>), typeof(ColorPickerButton),
        new FrameworkPropertyMetadata(Array.Empty<Color>()));

    public IReadOnlyList<Color> Presets
    {
        get => (IReadOnlyList<Color>)GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    private static readonly IReadOnlyList<Color> DefaultPresets = new[]
    {
        Color.FromRgb(0x3B, 0x82, 0xF6), Color.FromRgb(0x10, 0xB9, 0x81),
        Color.FromRgb(0xF5, 0x9E, 0x0B), Color.FromRgb(0xEF, 0x44, 0x44),
        Color.FromRgb(0x8B, 0x5C, 0xF6), Color.FromRgb(0xEC, 0x48, 0x99),
        Color.FromRgb(0x06, 0xB6, 0xD4), Color.FromRgb(0x84, 0xCC, 0x16),
        Color.FromRgb(0xF9, 0x73, 0x16), Color.FromRgb(0x64, 0x74, 0x8B),
    };

    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ColorPickerButton)d;
        picker.UpdateVisuals();
        if (!picker._updating)
        {
            picker._updating = true;
            picker.SetCurrentValue(HexTextProperty, ColorToHex((Color)e.NewValue));
            picker._updating = false;
        }
    }

    private static void OnHexTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var picker = (ColorPickerButton)d;
        picker.UpdateVisuals();
        if (picker._updating) return;

        if (TryParseHex(e.NewValue as string, out var color))
        {
            picker._updating = true;
            picker.SetCurrentValue(ColorProperty, color);
            picker._updating = false;
        }
    }

    public static string ColorToHex(Color c) =>
        $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static bool TryParseHex(string? text, out Color color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text)) return false;
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(text.Trim());
            color = Color.FromRgb(c.R, c.G, c.B);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public override void OnApplyTemplate()
    {
        if (_popup is not null) _popup.Closed -= OnPopupClosed;
        if (_swatch is not null) _swatch.MouseLeftButtonUp -= OnSwatchMouseUp;
        if (_hexText is not null) _hexText.MouseLeftButtonUp -= OnSwatchMouseUp;
        if (_svBox is not null)
        {
            _svBox.MouseDown -= OnSvMouseDown;
            _svBox.MouseMove -= OnSvMouseMove;
            _svBox.MouseUp -= OnSvMouseUp;
        }
        if (_hueTrack is not null)
        {
            _hueTrack.MouseDown -= OnHueMouseDown;
            _hueTrack.MouseMove -= OnHueMouseMove;
            _hueTrack.MouseUp -= OnHueMouseUp;
        }
        if (_hexInput is not null) _hexInput.TextChanged -= OnHexInputChanged;

        base.OnApplyTemplate();

        _swatch = GetTemplateChild("PART_Swatch") as Border;
        _swatchFill = GetTemplateChild("PART_SwatchFill") as Rectangle;
        _hexText = GetTemplateChild("PART_HexText") as TextBlock;
        _preview = GetTemplateChild("PART_Preview") as Border;
        _popup = GetTemplateChild("PART_Popup") as Popup;
        _svBox = GetTemplateChild("PART_SvBox") as Border;
        _svCursor = GetTemplateChild("PART_SvCursor") as Ellipse;
        _hueTrack = GetTemplateChild("PART_HueTrack") as Border;
        _hueCursor = GetTemplateChild("PART_HueCursor") as Rectangle;
        _hexInput = GetTemplateChild("PART_HexInput") as TextBox;

        // IMPORTANT: open on MouseUp, not MouseDown — a StaysOpen=False popup
        // captures the mouse the moment it opens, so the *release* of that same
        // click registers as an outside click and instantly closes the popup
        // (the "picker closes immediately" bug).
        if (_swatch is not null) _swatch.MouseLeftButtonUp += OnSwatchMouseUp;
        if (_hexText is not null) _hexText.MouseLeftButtonUp += OnSwatchMouseUp;
        if (_popup is not null) _popup.Closed += OnPopupClosed;

        // SV box and hue track capture the mouse on press so a drag that leaves
        // the control keeps updating the color until the button is released.
        if (_svBox is not null)
        {
            _svBox.MouseDown += OnSvMouseDown;
            _svBox.MouseMove += OnSvMouseMove;
            _svBox.MouseUp += OnSvMouseUp;
        }
        if (_hueTrack is not null)
        {
            _hueTrack.MouseDown += OnHueMouseDown;
            _hueTrack.MouseMove += OnHueMouseMove;
            _hueTrack.MouseUp += OnHueMouseUp;
        }
        if (_hexInput is not null) _hexInput.TextChanged += OnHexInputChanged;

        // Seed hue slider + hex input from the current color.
        var (h, _, _) = ToHsv(Color);
        _hue = h;
        if (_hexInput is not null) _hexInput.Text = ColorToHex(Color);
        BuildPresets();
        UpdateVisuals();
    }

    private void BuildPresets()
    {
        if (_presetsBuilt) return;
        var host = GetTemplateChild("PART_Presets") as ItemsControl;
        if (host is null) return;
        foreach (var color in Presets.Count > 0 ? Presets : DefaultPresets)
        {
            var chip = new Border
            {
                Width = 22,
                Height = 22,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x80, 0x80, 0x80)),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Margin = new Thickness(3),
                ToolTip = ColorToHex(color),
            };
            var captured = color;
            chip.MouseLeftButtonUp += (_, ev) => { _hue = ToHsv(captured).H; MoveHueCursor(); SetColor(captured); ev.Handled = true; };
            host.Items.Add(chip);
        }
        _presetsBuilt = true;
    }

    private double _hue;

    private void OnSwatchMouseUp(object sender, MouseButtonEventArgs e)
    {
        TogglePopup();
        e.Handled = true;
    }

    private void TogglePopup()
    {
        if (_popup is null) return;
        _popup.IsOpen = !_popup.IsOpen;
    }

    private void OnPopupClosed(object? sender, EventArgs e)
    {
        // Commit typed-but-uncommitted hex on close.
        if (_hexInput is not null && TryParseHex(_hexInput.Text, out var c) && c != Color)
        {
            _updating = true;
            SetCurrentValue(ColorProperty, c);
            SetCurrentValue(HexTextProperty, ColorToHex(c));
            _updating = false;
        }
    }

    private void OnHexInputChanged(object sender, TextChangedEventArgs e)
    {
        if (_updating) return;
        if (_hexInput is null || !TryParseHex(_hexInput.Text, out var c)) return;
        _updating = true;
        SetCurrentValue(ColorProperty, c);
        SetCurrentValue(HexTextProperty, ColorToHex(c));
        _updating = false;
    }

    private void OnSvMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _svBox is null) return;
        _svBox.CaptureMouse();
        PickSv(e);
        e.Handled = true;
    }

    private void OnSvMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _svBox is null) return;
        if (Mouse.Captured != _svBox && !_svBox.IsMouseOver) return;
        PickSv(e);
    }

    private void OnSvMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_svBox is not null && Mouse.Captured == _svBox) _svBox.ReleaseMouseCapture();
    }

    private void PickSv(MouseEventArgs e)
    {
        if (_svBox is null) return;
        var pos = e.GetPosition(_svBox);
        var s = Math.Clamp(pos.X / Math.Max(1, _svBox.ActualWidth), 0, 1);
        var v = 1 - Math.Clamp(pos.Y / Math.Max(1, _svBox.ActualHeight), 0, 1);
        MoveSvCursor(s, v);
        SetColor(FromHsv(_hue, s, v));
    }

    private void OnHueMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || _hueTrack is null) return;
        _hueTrack.CaptureMouse();
        PickHue(e);
        e.Handled = true;
    }

    private void OnHueMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _hueTrack is null) return;
        if (Mouse.Captured != _hueTrack && !_hueTrack.IsMouseOver) return;
        PickHue(e);
    }

    private void OnHueMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_hueTrack is not null && Mouse.Captured == _hueTrack) _hueTrack.ReleaseMouseCapture();
    }

    private void PickHue(MouseEventArgs e)
    {
        if (_hueTrack is null) return;
        var pos = e.GetPosition(_hueTrack);
        _hue = Math.Clamp(pos.X / Math.Max(1, _hueTrack.ActualWidth), 0, 1) * 360;
        MoveHueCursor();
        SetColor(FromHsv(_hue, CurrentS, CurrentV));
    }

    private double CurrentS { get { var (_, s, v) = ToHsv(Color); return s; } }
    private double CurrentV { get { var (_, s, v) = ToHsv(Color); return v; } }

    private void SetColor(Color color)
    {
        SetCurrentValue(ColorProperty, color); // OnColorChanged syncs HexText
    }

    private void MoveSvCursor(double s, double v)
    {
        if (_svCursor is null || _svBox is null) return;
        var w = Math.Max(0, _svBox.ActualWidth - 12);
        var h = Math.Max(0, _svBox.ActualHeight - 12);
        Canvas.SetLeft(_svCursor, 6 + s * w);
        Canvas.SetTop(_svCursor, 6 + (1 - v) * h);
    }

    private void MoveHueCursor()
    {
        if (_hueCursor is null || _hueTrack is null) return;
        var w = Math.Max(0, _hueTrack.ActualWidth - 12);
        Canvas.SetLeft(_hueCursor, 6 + _hue / 360 * w);
    }

    private void UpdateVisuals()
    {
        var brush = new SolidColorBrush(Color);
        if (_swatchFill is not null) _swatchFill.Fill = brush;
        if (_preview is not null) _preview.Background = brush;
        if (_hexText is not null) _hexText.Text = ColorToHex(Color);
        if (_hexInput is not null && !_hexInput.IsFocused && _hexInput.Text != ColorToHex(Color))
            _hexInput.Text = ColorToHex(Color);
        MoveSvCursor(CurrentS, CurrentV);
        MoveHueCursor();
    }

    public static (double H, double S, double V) ToHsv(Color c)
    {
        var r = c.R / 255.0;
        var g = c.G / 255.0;
        var b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        double h = 0;
        if (delta > 0)
        {
            if (max == r) h = 60 * (((g - b) / delta) % 6);
            else if (max == g) h = 60 * ((b - r) / delta + 2);
            else h = 60 * ((r - g) / delta + 4);
        }
        if (h < 0) h += 360;
        var s = max <= 0 ? 0 : delta / max;
        return (h, s, max);
    }

    public static Color FromHsv(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        var c = v * s;
        var x = c * (1 - Math.Abs(h / 60 % 2 - 1));
        var m = v - c;
        (double r, double g, double b) = (h / 60) switch
        {
            < 1 => (c, x, 0.0),
            < 2 => (x, c, 0.0),
            < 3 => (0.0, c, x),
            < 4 => (0.0, x, c),
            < 5 => (x, 0.0, c),
            _ => (c, 0.0, x),
        };
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }
}

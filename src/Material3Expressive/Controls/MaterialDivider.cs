using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>The direction in which a <see cref="MaterialDivider" /> occupies layout space.</summary>
public enum DividerOrientation { Horizontal, Vertical }

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25, Divider.kt and
// tokens/DividerTokens.kt (VERSION v0_117). HorizontalDivider/VerticalDivider draw a centred
// 1dp OutlineVariant stroke; this Avalonia control uses Orientation rather than two types.

/// <summary>A Material 3 horizontal or vertical divider.</summary>
public sealed class MaterialDivider : TemplatedControl
{
    public static readonly StyledProperty<DividerOrientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialDivider, DividerOrientation>(nameof(Orientation));
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<MaterialDivider, double>(nameof(Thickness), 1);
    public static readonly DirectProperty<MaterialDivider, IBrush?> DividerBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialDivider, IBrush?>(nameof(DividerBrush), o => o.DividerBrush, (o, value) => o.SetDividerBrush(value));

    private readonly MaterialThemeWatcher _theme;
    private IBrush? _dividerBrush;
    private bool _hasExplicitBrush;

    public MaterialDivider()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    public DividerOrientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
    public IBrush? DividerBrush { get => _dividerBrush; set => SetDividerBrush(value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var thickness = Math.Max(0, Thickness);
        return Orientation == DividerOrientation.Horizontal
            ? new Size(0, thickness)
            : new Size(thickness, 0);
    }

    public override void Render(DrawingContext context)
    {
        var brush = DividerBrush;
        if (brush is null || Thickness <= 0)
            return;

        var pen = new Pen(brush, Thickness);
        if (Orientation == DividerOrientation.Horizontal)
            context.DrawLine(pen, new Point(0, Thickness / 2), new Point(Bounds.Width, Thickness / 2));
        else
            context.DrawLine(pen, new Point(Thickness / 2, 0), new Point(Thickness / 2, Bounds.Height));
    }

    static MaterialDivider()
    {
        AffectsMeasure<MaterialDivider>(OrientationProperty, ThicknessProperty);
        AffectsRender<MaterialDivider>(OrientationProperty, ThicknessProperty);
    }

    private void ApplyTheme()
    {
        if (!_hasExplicitBrush)
            SetAndRaise(DividerBrushProperty, ref _dividerBrush, new SolidColorBrush(_theme.Theme.ColorScheme?.GetColor(SysColorToken.OutlineVariant, _theme.IsDark) ?? Colors.Gray));
    }

    private void SetDividerBrush(IBrush? value)
    {
        _hasExplicitBrush = true;
        SetAndRaise(DividerBrushProperty, ref _dividerBrush, value);
        InvalidateVisual();
    }
}

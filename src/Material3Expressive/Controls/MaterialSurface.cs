using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// Surface.kt:100-130, 550-620 and ColorScheme.kt:1151-1154. The alpha22 executable reference
// can observe a single surface but cannot validate alpha25's public tonal-elevation switch or
// the expressive ListItem behaviour that consumes it. Avalonia intentionally adapts only the
// non-interactive Surface overload: click/select/toggle overloads need distinct command and
// accessibility contracts and are represented by explicit controls, not implicit container modes.

/// <summary>A non-interactive Material surface that supplies Surface and OnSurface defaults.</summary>
public sealed class MaterialSurface : ContentControl
{
    public static readonly StyledProperty<double> TonalElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(TonalElevation));

    public static readonly StyledProperty<double> ShadowElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(ShadowElevation));

    internal static readonly DirectProperty<MaterialSurface, double> AbsoluteTonalElevationProperty =
        AvaloniaProperty.RegisterDirect<MaterialSurface, double>(
            nameof(AbsoluteTonalElevation),
            surface => surface.AbsoluteTonalElevation);

    private readonly MaterialThemeWatcher _theme;
    private MaterialSurface? _tonalParent;
    private double _absoluteTonalElevation;

    public MaterialSurface()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTheme();
        AttachedToVisualTree += (_, _) => UpdateTonalParent();
        DetachedFromVisualTree += (_, _) => SetTonalParent(null);
        UpdateAbsoluteTonalElevation();
    }

    /// <summary>
    /// The elevation used to tint this surface when its effective background is the theme Surface
    /// colour. Nested <see cref="MaterialSurface" /> values accumulate, while shadow elevation does not.
    /// </summary>
    public double TonalElevation
    {
        get => GetValue(TonalElevationProperty);
        set => SetValue(TonalElevationProperty, value);
    }

    /// <summary>Point-light shadow elevation in DIPs.</summary>
    public double ShadowElevation
    {
        get => GetValue(ShadowElevationProperty);
        set => SetValue(ShadowElevationProperty, value);
    }

    /// <summary>The accumulated tonal elevation supplied to descendant material surfaces.</summary>
    internal double AbsoluteTonalElevation => _absoluteTonalElevation;

    public override void Render(DrawingContext context)
    {
        var surface = EffectiveSurfaceBrush();
        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;
        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            CornerRadius,
            surface,
            BorderBrush,
            EffectiveBorderThickness(),
            Math.Max(0, ShadowElevation),
            shadow);
        base.Render(context);
    }

    static MaterialSurface()
    {
        AffectsRender<MaterialSurface>(
            TonalElevationProperty,
            ShadowElevationProperty,
            BackgroundProperty,
            BorderBrushProperty,
            BorderThicknessProperty,
            CornerRadiusProperty);
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSurface>(false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TonalElevationProperty)
            UpdateAbsoluteTonalElevation();
        else if (change.Property == VisualParentProperty)
            UpdateTonalParent();
    }

    private void ApplyTheme() => InvalidateVisual();

    private void UpdateTonalParent()
    {
        var parent = Avalonia.VisualTree.VisualExtensions
            .GetVisualAncestors(this)
            .OfType<MaterialSurface>()
            .FirstOrDefault();
        SetTonalParent(parent);
    }

    private void SetTonalParent(MaterialSurface? parent)
    {
        if (ReferenceEquals(_tonalParent, parent))
        {
            UpdateAbsoluteTonalElevation();
            return;
        }

        if (_tonalParent is not null)
            _tonalParent.PropertyChanged -= OnTonalParentPropertyChanged;

        _tonalParent = parent;

        if (_tonalParent is not null)
            _tonalParent.PropertyChanged += OnTonalParentPropertyChanged;

        UpdateAbsoluteTonalElevation();
    }

    private void OnTonalParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == AbsoluteTonalElevationProperty)
            UpdateAbsoluteTonalElevation();
    }

    private void UpdateAbsoluteTonalElevation()
    {
        var inherited = _tonalParent?.AbsoluteTonalElevation ?? 0;
        var resolved = Math.Max(0, inherited) + Math.Max(0, TonalElevation);
        if (Math.Abs(resolved - _absoluteTonalElevation) < 0.000001)
            return;

        SetAndRaise(AbsoluteTonalElevationProperty, ref _absoluteTonalElevation, resolved);
        InvalidateVisual();
    }

    private IBrush? EffectiveSurfaceBrush()
    {
        var background = Background;
        var scheme = _theme.Theme.ColorScheme;
        if (background is not ISolidColorBrush solid || scheme is null)
            return background;

        var surface = scheme.GetColor(SysColorToken.Surface, _theme.IsDark);
        if (solid.Color != surface || _absoluteTonalElevation <= 0)
            return background;

        var tint = scheme.GetColor(SysColorToken.SurfaceTint, _theme.IsDark);
        var alpha = ((4.5 * Math.Log(_absoluteTonalElevation + 1)) + 2) / 100;
        return new ImmutableSolidColorBrush(CompositeOver(tint, alpha, surface), solid.Opacity);
    }

    private double EffectiveBorderThickness()
    {
        // Compose BorderStroke has one width. Avalonia exposes an asymmetric Thickness on every
        // templated control; when supplied here, the largest side is intentionally used as the
        // single Material border width rather than silently omitting the border.
        var thickness = BorderThickness;
        return Math.Max(0, Math.Max(Math.Max(thickness.Left, thickness.Top), Math.Max(thickness.Right, thickness.Bottom)));
    }

    private static Color CompositeOver(Color foreground, double foregroundAlpha, Color background)
    {
        var fgAlpha = Math.Clamp((foreground.A / 255d) * foregroundAlpha, 0, 1);
        var bgAlpha = background.A / 255d;
        var outAlpha = fgAlpha + (bgAlpha * (1 - fgAlpha));
        if (outAlpha <= 0)
            return Colors.Transparent;

        byte Channel(byte foregroundChannel, byte backgroundChannel) =>
            (byte)Math.Round(
                (((foregroundChannel / 255d) * fgAlpha) + ((backgroundChannel / 255d) * bgAlpha * (1 - fgAlpha))) / outAlpha * 255,
                MidpointRounding.AwayFromZero);

        return Color.FromArgb(
            (byte)Math.Round(outAlpha * 255, MidpointRounding.AwayFromZero),
            Channel(foreground.R, background.R),
            Channel(foreground.G, background.G),
            Channel(foreground.B, background.B));
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25, Surface.kt.
// Avalonia adapts Compose's non-interactive Surface overload to a ContentControl. Clickable,
// selectable and toggleable Surface overloads remain explicit controls rather than implicit
// behavior on a visual container.

/// <summary>A non-interactive Material surface that supplies Surface and OnSurface defaults.</summary>
public sealed class MaterialSurface : ContentControl
{
    public static readonly StyledProperty<double> TonalElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(TonalElevation));

    public static readonly StyledProperty<double> ShadowElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(ShadowElevation));

    private readonly MaterialThemeWatcher _theme;

    public MaterialSurface()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTheme();
        ApplyTheme();
    }

    /// <summary>Tonal elevation reserved for nested-surface composition.</summary>
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

    public override void Render(DrawingContext context)
    {
        var surface = new SolidColorBrush(_theme.Theme.ColorScheme?.GetColor(SysColorToken.Surface, _theme.IsDark) ?? Colors.White);
        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;
        MaterialContainer.Draw(context, this, Bounds.Size, CornerRadius, surface, null, 0, ShadowElevation, shadow);
        base.Render(context);
    }

    static MaterialSurface()
    {
        AffectsRender<MaterialSurface>(TonalElevationProperty, ShadowElevationProperty, CornerRadiusProperty);
        ClipToBoundsProperty.OverrideDefaultValue<MaterialSurface>(true);
    }

    private void ApplyTheme()
    {
        Foreground = new SolidColorBrush(_theme.Theme.ColorScheme?.GetColor(SysColorToken.OnSurface, _theme.IsDark) ?? Colors.Black);
        InvalidateVisual();
    }
}

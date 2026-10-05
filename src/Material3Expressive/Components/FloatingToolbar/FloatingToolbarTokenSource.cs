using System.Collections.Frozen;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// Fixed FloatingToolbarTokens.kt, version 12_0_0, and the entry-point configuration defaults.
public sealed class FloatingToolbarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.FloatingToolbar.ExpandedShadowElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.FloatingToolbar.ExpandedShadowElevationWithFab] = new TokenRef(Sys.Elevation.Level1),
        [Comp.FloatingToolbar.CollapsedShadowElevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.FloatingToolbar.ExpansionAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.FloatingToolbar.ElevationAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.FloatingToolbar.SideContentAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.FloatingToolbar.BalancedPaddingAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.FloatingToolbar.ScrollSnapAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.FloatingToolbar.ContainerBetweenSpace] = 4d,
        [Comp.FloatingToolbar.ContainerExternalPadding] = 16d,
        [Comp.FloatingToolbar.ContainerHeight] = 64d,
        [Comp.FloatingToolbar.ContainerLeadingSpace] = 8d,
        [Comp.FloatingToolbar.ContainerTrailingSpace] = 8d,
        [Comp.FloatingToolbar.ContainerShape] = new TokenRef(Sys.Shape.Full),
        [Comp.FloatingToolbar.StandardContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.FloatingToolbar.VibrantContainerColor] = new TokenRef(Sys.Color.PrimaryContainer),
        [Comp.FloatingToolbar.VibrantButtonSelectedContainerColor] = new TokenRef(Sys.Color.SurfaceContainer),
        [Comp.FloatingToolbar.VibrantButtonSelectedIconColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.FloatingToolbar.VibrantButtonSelectedTextColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.FloatingToolbar.VibrantButtonUnselectedIconColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.FloatingToolbar.VibrantButtonUnselectedTextColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

internal sealed class FloatingToolbarPaddingConverter : IMultiValueConverter
{
    public static IMultiValueConverter Instance { get; } = new FloatingToolbarPaddingConverter();
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [double start, double top, double end, double bottom] ? new Thickness(start, top, end, bottom) : AvaloniaProperty.UnsetValue;
}

// The public FAB animation input accepts arbitrary finite specs; its default remains a live system-spring reference.
internal sealed class FloatingToolbarAnimationConverter : IMultiValueConverter
{
    public static FloatingToolbarAnimationConverter Instance { get; } = new();
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [Foundation.Motion.SpringSpec spring]
            ? new ComposeAnimation.Runtime.SpringSpec<float>((float)spring.DampingRatio, (float)spring.Stiffness, .01f)
            : AvaloniaProperty.UnsetValue;
}

using System.Collections.Frozen;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// Fixed FloatingToolbarTokens.kt, version 12_0_0. Composable defaults stay in FloatingToolbarDefaults.
public sealed class FloatingToolbarTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
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

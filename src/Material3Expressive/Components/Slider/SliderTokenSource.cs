using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Defaults consumed by Slider.kt at the fixed AndroidX baseline.</summary>
public sealed class SliderTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Slider.VerticalEnableCornerShrinking] = true,
        [Comp.Slider.EnableCornerShrinking] = false,
        [Comp.Slider.TrackInsideCornerSize] = 2d,
        [Comp.Slider.HandleColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Slider.ActiveTrackColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Slider.InactiveTrackColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.Slider.DisabledHandleColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Slider.DisabledActiveTrackColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Slider.DisabledInactiveTrackColor] = new WithAlpha(Sys.Color.OnSurface, 0.12),
        [Comp.Slider.HandleWidth] = 4d,
        [Comp.Slider.HandleHeight] = 44d,
        [Comp.Slider.HandleShape] = new TokenRef(Sys.Shape.Full),
        [Comp.Slider.InactiveTrackHeight] = 16d,
        [Comp.Slider.ActiveHandleLeadingSpace] = 6d,
        [Comp.Slider.StopIndicatorSize] = 4d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

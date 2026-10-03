using System.Collections.Frozen;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class SwitchTokenSource : ITokenSource
{
    // Disabled tokens retain their alpha until the consuming component composites over Surface,
    // as SwitchDefaults.colors does. Explicit SwitchColors already contains final colors.
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Switch.CheckedThumbColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.Switch.CheckedTrackColor] = new TokenRef(Sys.Color.Primary),
        [Comp.Switch.CheckedBorderColor] = Color.FromArgb(0, 0, 0, 0),
        [Comp.Switch.CheckedIconColor] = new TokenRef(Sys.Color.OnPrimaryContainer),
        [Comp.Switch.UncheckedThumbColor] = new TokenRef(Sys.Color.Outline),
        [Comp.Switch.UncheckedTrackColor] = new TokenRef(Sys.Color.SurfaceContainerHighest),
        [Comp.Switch.UncheckedBorderColor] = new TokenRef(Sys.Color.Outline),
        [Comp.Switch.UncheckedIconColor] = new TokenRef(Sys.Color.SurfaceContainerHighest),
        [Comp.Switch.DisabledCheckedThumbColor] = new TokenRef(Sys.Color.Surface),
        [Comp.Switch.DisabledCheckedTrackColor] = new WithAlpha(Sys.Color.OnSurface, 0.12),
        [Comp.Switch.DisabledCheckedBorderColor] = Color.FromArgb(0, 0, 0, 0),
        [Comp.Switch.DisabledCheckedIconColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Switch.DisabledUncheckedThumbColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.Switch.DisabledUncheckedTrackColor] = new WithAlpha(Sys.Color.SurfaceContainerHighest, 0.12),
        [Comp.Switch.DisabledUncheckedBorderColor] = new WithAlpha(Sys.Color.OnSurface, 0.12),
        [Comp.Switch.DisabledUncheckedIconColor] = new WithAlpha(Sys.Color.SurfaceContainerHighest, 0.38),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

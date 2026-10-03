using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// Fixed BadgeTokens v0_103. Badge() uses Color for both content branches, and
// contentColorFor for the foreground; LargeColor/LabelTextColor are retained tokens.
public sealed class BadgeTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Badge.Color] = new TokenRef(Sys.Color.Error),
        [Comp.Badge.LargeColor] = new TokenRef(Sys.Color.Error),
        [Comp.Badge.LargeLabelTextColor] = new TokenRef(Sys.Color.OnError),
        [Comp.Badge.LargeLabelTextFont] = new TokenRef(Sys.TypeScale.LabelSmall.Style),
        [Comp.Badge.LargeShape] = new TokenRef(Sys.Shape.Full),
        [Comp.Badge.LargeSize] = 16d,
        [Comp.Badge.Shape] = new TokenRef(Sys.Shape.Full),
        [Comp.Badge.Size] = 6d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

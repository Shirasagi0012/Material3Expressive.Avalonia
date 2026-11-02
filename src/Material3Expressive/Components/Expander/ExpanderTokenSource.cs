using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ExpanderTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.Expander.HeaderMinHeight] = 56d,
        [Comp.Expander.HeaderPadding] = new Thickness(16, 10),
        [Comp.Expander.HeaderIconSpacing] = 12d,
        [Comp.Expander.HeaderIconSize] = 24d,
        [Comp.Expander.ContentPadding] = new Thickness(16, 0),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);

    public event EventHandler? Changed { add { } remove { } }
}

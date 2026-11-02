using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class GroupBoxTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.GroupBox.ContentPadding] = new Thickness(16),
        [Comp.GroupBox.HeaderBottomSpacing] = 8d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);

    public event EventHandler? Changed { add { } remove { } }
}

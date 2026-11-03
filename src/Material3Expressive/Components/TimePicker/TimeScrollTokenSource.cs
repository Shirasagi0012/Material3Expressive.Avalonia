using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class TimeScrollTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TimeScroll.Margin] = 12.0d,
        [Comp.TimeScroll.DisplaySpacing] = 16.0d,
        [Comp.TimeScroll.TimeFieldContainerWidth] = 100.0d,
        [Comp.TimeScroll.TimeFieldContainerHeight] = 120.0d,
        [Comp.TimeScroll.TimeFieldWithToggleContainerHeight] = 140.0d,
        [Comp.TimeScroll.SeparatorWidth] = 16.0d,
        [Comp.TimeScroll.PeriodSelectorContainerWidth] = 56.0d,
        [Comp.TimeScroll.PeriodSelectorSpacing] = 8.0d,
        [Comp.TimeScroll.ItemHeight] = 40.0d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

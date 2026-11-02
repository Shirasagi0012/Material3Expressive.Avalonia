using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class DatePickerDialogTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.DatePickerDialog.ActionEndPadding] = 6d,
        [Comp.DatePickerDialog.ActionBottomPadding] = 8d,
        [Comp.DatePickerDialog.ActionHorizontalSpacing] = 8d,
        [Comp.DatePickerDialog.ActionVerticalSpacing] = 12d,
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

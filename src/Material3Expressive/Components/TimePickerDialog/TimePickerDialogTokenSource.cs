using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class TimePickerDialogTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TimePickerDialog.HorizontalPadding] = 24.0d,
        [Comp.TimePickerDialog.VerticalPadding] = 24.0d,
        [Comp.TimePickerDialog.LandscapeAdditionalHeight] = 28.0d,
        [Comp.TimePickerDialog.LandscapeContentTopOffset] = 16.0d,
        [Comp.TimePickerDialog.LandscapeActionsSpacing] = 4.0d,
        [Comp.TimePickerDialog.LandscapeTallActionsOffset] = 16.0d,
        [Comp.TimePickerDialog.ActionsSpacing] = 8.0d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

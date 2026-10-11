using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// The subset of fixed DatePickerModalTokens consumed by DatePickerDialog.
// DatePickerDefaults.TonalElevation is Level0, not the modal elevation token.
public sealed class DatePickerDialogTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.DatePickerModal.ContainerColor] = new TokenRef(Sys.Color.SurfaceContainerHigh),
        [Comp.DatePickerModal.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.DatePickerModal.ContainerWidth] = 360d,
        [Comp.DatePickerModal.ContainerHeight] = 568d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

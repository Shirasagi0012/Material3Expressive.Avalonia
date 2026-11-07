using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

// MenuDefaults' expressive arrangement and selectable content padding are live getters.
// Keep selection in the theme so caller styles and local values retain their priority.
internal static class MenuPrecisionDefaults
{
    public static readonly IMultiValueConverter Value = new FuncMultiValueConverter<object?, object?>(values =>
        values.ToArray() is [bool precision, var normal, var precise]
            ? precision ? precise : normal : AvaloniaProperty.UnsetValue);
}

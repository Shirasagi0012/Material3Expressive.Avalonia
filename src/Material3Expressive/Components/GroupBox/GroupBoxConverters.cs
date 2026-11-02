using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal static class GroupBoxConverters
{
    public static IMultiValueConverter HeaderMargin { get; } = new FuncMultiValueConverter<object?, Thickness>(values =>
        values.ToArray() is [double spacing] ? new Thickness(0, 0, 0, spacing) : default);

    // Header presence: a null or empty-string Header reserves no space (no row, no gap). The built-in
    // StringConverters.IsNotNullOrEmpty only answers for strings, so a control header would rely on
    // its UnsetValue fallback.
    public static IValueConverter HasHeader { get; } = new FuncValueConverter<object?, bool>(value => value is not null and not "");
}

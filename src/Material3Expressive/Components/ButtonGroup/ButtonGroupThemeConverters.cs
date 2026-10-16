using Avalonia;
using Avalonia.Data.Converters;

namespace Material3Expressive.Components;

internal static class ButtonGroupThemeConverters
{
    public static readonly FuncMultiValueConverter<object, object?> ContentPadding = new(values =>
        values is [var icon, var template, Thickness padding, Thickness iconPadding]
            ? icon is not null || template is not null ? iconPadding : padding
            : AvaloniaProperty.UnsetValue);

    public static readonly FuncMultiValueConverter<object, object?> LeadingCorners = new(values =>
        values is [CornerRadius outer, CornerRadius inner]
            ? new CornerRadius(outer.TopLeft, inner.TopRight, inner.BottomRight, outer.BottomLeft)
            : AvaloniaProperty.UnsetValue);

    public static readonly FuncMultiValueConverter<object, object?> TrailingCorners = new(values =>
        values is [CornerRadius outer, CornerRadius inner]
            ? new CornerRadius(inner.TopLeft, outer.TopRight, outer.BottomRight, inner.BottomLeft)
            : AvaloniaProperty.UnsetValue);
}

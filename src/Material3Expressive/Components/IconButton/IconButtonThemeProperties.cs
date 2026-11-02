using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Components;

// A theme candidate, like ButtonThemeProperties: the owner and ButtonLayout consume the same
// width, while native Width/MinWidth retain their normal override priority.
internal static class IconButtonThemeProperties
{
    // Keep contentColorFor tied to the checked container candidate, rather than the effective
    // (possibly disabled or explicitly overridden) Background.
    public static readonly AttachedProperty<IBrush?> CheckedContentColorProperty =
        AvaloniaProperty.RegisterAttached<IconButton, IBrush?>("CheckedContentColor", typeof(IconButtonThemeProperties));
    public static IBrush? GetCheckedContentColor(IconButton owner) => owner.GetValue(CheckedContentColorProperty);
    public static void SetCheckedContentColor(IconButton owner, IBrush? value) => owner.SetValue(CheckedContentColorProperty, value);

    // outlinedIconToggleButtonBorder returns null whenever checked, including when disabled.
    public static readonly FuncMultiValueConverter<object, object> ToggleBorderThicknessConverter = new(values =>
        values.Count == 2 && values[0] is bool selected && values[1] is Thickness width
            ? selected ? default(Thickness) : width : AvaloniaProperty.UnsetValue);

    public static readonly AttachedProperty<CornerRadius?> CheckedCornerRadiusProperty =
        AvaloniaProperty.RegisterAttached<IconButton, CornerRadius?>("CheckedCornerRadius", typeof(IconButtonThemeProperties));
    public static CornerRadius? GetCheckedCornerRadius(IconButton owner) => owner.GetValue(CheckedCornerRadiusProperty);
    public static void SetCheckedCornerRadius(IconButton owner, CornerRadius? value) => owner.SetValue(CheckedCornerRadiusProperty, value);

    // IconToggleButtonColors.containerColor/contentColor use rememberUpdatedState, not animation.
    public static readonly FuncMultiValueConverter<object, object> ToggleColorConverter = new(values =>
    {
        if (values.Count != 5 || values[0] is not bool enabled || values[1] is not bool selected)
            return AvaloniaProperty.UnsetValue;
        return values[!enabled ? 3 : selected ? 4 : 2] is Color color
            ? new ImmutableSolidColorBrush(color) : AvaloniaProperty.UnsetValue;
    });

    public static readonly AttachedProperty<double> ContainerWidthProperty =
        AvaloniaProperty.RegisterAttached<IconButton, double>("ContainerWidth", typeof(IconButtonThemeProperties));
    public static double GetContainerWidth(IconButton owner) => owner.GetValue(ContainerWidthProperty);
    public static void SetContainerWidth(IconButton owner, double value) => owner.SetValue(ContainerWidthProperty, value);

    // IconButtonDefaults.{extraSmall,small,medium,large,extraLarge}ContainerSize. Uniform uses
    // DefaultLeadingSpace twice upstream (UniformLeadingSpace in the older Large token table).
    public static readonly FuncMultiValueConverter<object, object> ContainerWidthConverter = new(values =>
    {
        if (values.Count != 7 || values[0] is not IconButtonWidthOption option ||
            values.Skip(1).Any(value => value is not double))
            return AvaloniaProperty.UnsetValue;
        var space = option switch
        {
            IconButtonWidthOption.Narrow => (double)values[2]! + (double)values[3]!,
            IconButtonWidthOption.Uniform => (double)values[4]! + (double)values[4]!,
            IconButtonWidthOption.Wide => (double)values[5]! + (double)values[6]!,
            _ => 0d
        };
        return (double)values[1]! + space;
    });
}

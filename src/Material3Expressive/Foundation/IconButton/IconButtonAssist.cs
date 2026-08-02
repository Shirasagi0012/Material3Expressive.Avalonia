using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation;

/// <summary>How much horizontal room an icon button leaves around its icon.</summary>
public enum IconButtonWidth
{
    Narrow,
    Default,
    Wide
}

/// <summary>Canonical Material icon-button options for native controls and CLR facades.</summary>
public static class IconButtonAssist
{
    public static readonly AttachedProperty<IconButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonVariant>(
            "Variant", typeof(IconButtonAssist), IconButtonVariant.Standard, inherits: true);

    public static readonly AttachedProperty<IconButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonSize>(
            "Size", typeof(IconButtonAssist), IconButtonSize.Small, inherits: true);

    public static readonly AttachedProperty<IconButtonWidth> ButtonWidthProperty =
        AvaloniaProperty.RegisterAttached<Button, IconButtonWidth>(
            "ButtonWidth", typeof(IconButtonAssist), IconButtonWidth.Default, inherits: true);

    public static readonly AttachedProperty<bool> IsSquareProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsSquare", typeof(IconButtonAssist));

    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.RegisterAttached<Button, RoundedCornerShape?>(
            "ContainerShapeOverride", typeof(IconButtonAssist));

    public static readonly AttachedProperty<double> IconSizeProperty =
        AvaloniaProperty.RegisterAttached<Button, double>(
            "IconSize", typeof(IconButtonAssist), 24);

    /// <summary>Marks a Button/ToggleButton as using the icon-button semantic visual contract.</summary>
    public static readonly AttachedProperty<bool> IsIconButtonProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>(
            "IsIconButton", typeof(IconButtonAssist));

    public static IconButtonVariant GetVariant(Button button) => button.GetValue(VariantProperty);
    public static void SetVariant(Button button, IconButtonVariant value) => button.SetValue(VariantProperty, value);
    public static IconButtonSize GetSize(Button button) => button.GetValue(SizeProperty);
    public static void SetSize(Button button, IconButtonSize value) => button.SetValue(SizeProperty, value);
    public static IconButtonWidth GetButtonWidth(Button button) => button.GetValue(ButtonWidthProperty);
    public static void SetButtonWidth(Button button, IconButtonWidth value) => button.SetValue(ButtonWidthProperty, value);
    public static bool GetIsSquare(Button button) => button.GetValue(IsSquareProperty);
    public static void SetIsSquare(Button button, bool value) => button.SetValue(IsSquareProperty, value);
    public static RoundedCornerShape? GetContainerShapeOverride(Button button) =>
        button.GetValue(ContainerShapeOverrideProperty);
    public static void SetContainerShapeOverride(Button button, RoundedCornerShape? value) =>
        button.SetValue(ContainerShapeOverrideProperty, value);
    public static double GetIconSize(Button button) => button.GetValue(IconSizeProperty);
    public static bool GetIsIconButton(Button button) => button.GetValue(IsIconButtonProperty);
    public static void SetIsIconButton(Button button, bool value) => button.SetValue(IsIconButtonProperty, value);
}

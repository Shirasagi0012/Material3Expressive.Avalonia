using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation;

/// <summary>
/// Material button options shared by native <see cref="Button" /> controls and CLR facades.
/// </summary>
/// <remarks>
/// These attached properties are the canonical state for the button implementation. Named
/// control themes only provide defaults for them, and facade properties add themselves as owners
/// of these same property instances rather than copying values into a second state store.
/// </remarks>
public static class ButtonAssist
{
    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonVariant>(
            "Variant", typeof(ButtonAssist), ButtonVariant.Filled);

    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.RegisterAttached<Button, ButtonSize>(
            "Size", typeof(ButtonAssist), ButtonSize.Small);

    public static readonly AttachedProperty<bool> IsSquareProperty =
        AvaloniaProperty.RegisterAttached<Button, bool>("IsSquare", typeof(ButtonAssist));

    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        AvaloniaProperty.RegisterAttached<Button, RoundedCornerShape?>(
            "ContainerShapeOverride", typeof(ButtonAssist));

    public static ButtonVariant GetVariant(Button button) => button.GetValue(VariantProperty);

    public static void SetVariant(Button button, ButtonVariant value) =>
        button.SetValue(VariantProperty, value);

    public static ButtonSize GetSize(Button button) => button.GetValue(SizeProperty);

    public static void SetSize(Button button, ButtonSize value) => button.SetValue(SizeProperty, value);

    public static bool GetIsSquare(Button button) => button.GetValue(IsSquareProperty);

    public static void SetIsSquare(Button button, bool value) => button.SetValue(IsSquareProperty, value);

    public static RoundedCornerShape? GetContainerShapeOverride(Button button) =>
        button.GetValue(ContainerShapeOverrideProperty);

    public static void SetContainerShapeOverride(Button button, RoundedCornerShape? value) =>
        button.SetValue(ContainerShapeOverrideProperty, value);
}

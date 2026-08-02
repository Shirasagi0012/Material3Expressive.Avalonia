using Avalonia;
using Avalonia.Controls.Primitives;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>CLR-property facade over a native Material-themed ToggleButton.</summary>
public sealed class MaterialToggleButton : ToggleButton
{
    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        ButtonAssist.VariantProperty.AddOwner<MaterialToggleButton>();
    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        ButtonAssist.SizeProperty.AddOwner<MaterialToggleButton>();
    public static readonly AttachedProperty<bool> IsSquareProperty =
        ButtonAssist.IsSquareProperty.AddOwner<MaterialToggleButton>();
    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        ButtonAssist.ContainerShapeOverrideProperty.AddOwner<MaterialToggleButton>();

    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public bool IsSquare
    {
        get => GetValue(IsSquareProperty);
        set => SetValue(IsSquareProperty, value);
    }

    public RoundedCornerShape? ContainerShapeOverride
    {
        get => GetValue(ContainerShapeOverrideProperty);
        set => SetValue(ContainerShapeOverrideProperty, value);
    }
}

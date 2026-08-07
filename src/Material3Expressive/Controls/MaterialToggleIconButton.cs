using Avalonia;
using Avalonia.Controls.Primitives;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>CLR-property facade over a native ToggleButton using a Material icon-button theme.</summary>
public sealed class MaterialToggleIconButton : ToggleButton
{
    public MaterialToggleIconButton()
    {
        MaterialElementContext.GetOrCreate(this);
    }

    public static readonly AttachedProperty<IconButtonVariant> VariantProperty =
        IconButtonAssist.VariantProperty.AddOwner<MaterialToggleIconButton>();
    public static readonly AttachedProperty<IconButtonSize> SizeProperty =
        IconButtonAssist.SizeProperty.AddOwner<MaterialToggleIconButton>();
    public static readonly AttachedProperty<IconButtonWidth> ButtonWidthProperty =
        IconButtonAssist.ButtonWidthProperty.AddOwner<MaterialToggleIconButton>();
    public static readonly AttachedProperty<bool> IsSquareProperty =
        IconButtonAssist.IsSquareProperty.AddOwner<MaterialToggleIconButton>();
    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        IconButtonAssist.ContainerShapeOverrideProperty.AddOwner<MaterialToggleIconButton>();
    public static readonly AttachedProperty<double> IconSizeProperty =
        IconButtonAssist.IconSizeProperty.AddOwner<MaterialToggleIconButton>();

    public IconButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public IconButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public IconButtonWidth ButtonWidth
    {
        get => GetValue(ButtonWidthProperty);
        set => SetValue(ButtonWidthProperty, value);
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

    public double IconSize => GetValue(IconSizeProperty);
}

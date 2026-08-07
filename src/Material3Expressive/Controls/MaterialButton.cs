using Avalonia;
using Avalonia.Controls;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// An ergonomic CLR facade over a native <see cref="Button" /> with the Material button theme.
/// </summary>
/// <remarks>
/// This type adds no rendering, interaction or token logic. Its CLR properties own the same
/// attached properties used by an ordinary Button, so the facade and native APIs cannot diverge.
/// </remarks>
public sealed class MaterialButton : Button
{
    public MaterialButton()
    {
        MaterialElementContext.GetOrCreate(this);
    }

    public static readonly AttachedProperty<ButtonVariant> VariantProperty =
        ButtonAssist.VariantProperty.AddOwner<MaterialButton>();

    public static readonly AttachedProperty<ButtonSize> SizeProperty =
        ButtonAssist.SizeProperty.AddOwner<MaterialButton>();

    /// <summary>
    /// Squares the resting container off. Material pairs a round button with a square one in
    /// toggle groups, so the two shapes are a property rather than two variants.
    /// </summary>
    public static readonly AttachedProperty<bool> IsSquareProperty =
        ButtonAssist.IsSquareProperty.AddOwner<MaterialButton>();

    /// <inheritdoc cref="ContainerShapeOverride" />
    public static readonly AttachedProperty<RoundedCornerShape?> ContainerShapeOverrideProperty =
        ButtonAssist.ContainerShapeOverrideProperty.AddOwner<MaterialButton>();

    /// <summary>The colour variant: filled, tonal, elevated, outlined or text.</summary>
    public ButtonVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>The size, which sets height, padding, icon size and type scale together.</summary>
    public ButtonSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <inheritdoc cref="IsSquareProperty" />
    public bool IsSquare
    {
        get => GetValue(IsSquareProperty);
        set => SetValue(IsSquareProperty, value);
    }

    /// <summary>
    /// Replaces the resting shape outright, corner by corner.
    /// </summary>
    /// <remarks>
    /// The escape hatch for containers that shape their children: a connected button group squares
    /// only the corners where two segments meet, and a split button's two halves each keep one
    /// round end. Neither can be said with <see cref="IsSquare" />, which is all four corners or
    /// none. The press morph still applies on top.
    /// </remarks>
    public RoundedCornerShape? ContainerShapeOverride
    {
        get => GetValue(ContainerShapeOverrideProperty);
        set => SetValue(ContainerShapeOverrideProperty, value);
    }

}

using System;
using Avalonia;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// Sparse override for one system type-scale slot.
/// </summary>
/// <remarks>
/// Type scale belongs to <see cref="TypographyScheme"/>, not the component override collection.
/// The class remains a <see cref="ComponentTokenOverrides"/> for source compatibility with the
/// alpha API; new code should add it to <see cref="TypographyScheme.TypeScaleOverrides"/>.
/// </remarks>
[Obsolete("TypeScaleOverrides belongs in TypographyScheme.TypeScaleOverrides; the Components collection form is retained for compatibility.")]
public sealed class TypeScaleOverrides : ComponentTokenOverrides
{
    public static readonly StyledProperty<TypographyRole> VariantProperty =
        AvaloniaProperty.Register<TypeScaleOverrides, TypographyRole>(nameof(Variant));

    public static readonly StyledProperty<double> FontSizeProperty =
        AvaloniaProperty.Register<TypeScaleOverrides, double>(nameof(FontSize));

    public static readonly StyledProperty<double> LineHeightProperty =
        AvaloniaProperty.Register<TypeScaleOverrides, double>(nameof(LineHeight));

    public static readonly StyledProperty<double> LetterSpacingProperty =
        AvaloniaProperty.Register<TypeScaleOverrides, double>(nameof(LetterSpacing));

    public static readonly StyledProperty<FontFamilyToken> FontProperty =
        AvaloniaProperty.Register<TypeScaleOverrides, FontFamilyToken>(nameof(Font));

    public static readonly StyledProperty<FontWeightToken> WeightProperty =
        AvaloniaProperty.Register<TypeScaleOverrides, FontWeightToken>(nameof(Weight));

    public TypographyRole Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public double FontSize
    {
        get => GetValue(FontSizeProperty);
        set => SetValue(FontSizeProperty, value);
    }

    public double LineHeight
    {
        get => GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    public double LetterSpacing
    {
        get => GetValue(LetterSpacingProperty);
        set => SetValue(LetterSpacingProperty, value);
    }

    public FontFamilyToken Font
    {
        get => GetValue(FontProperty);
        set => SetValue(FontProperty, value);
    }

    public FontWeightToken Weight
    {
        get => GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    public TypeScale ApplyTo(TypeScale baseTokens)
    {
        return new TypeScale(
            IsSet(FontSizeProperty) ? FontSize : baseTokens.FontSize,
            IsSet(LineHeightProperty) ? LineHeight : baseTokens.LineHeight,
            IsSet(LetterSpacingProperty) ? LetterSpacing : baseTokens.LetterSpacing,
            IsSet(FontProperty) ? Font : baseTokens.Font,
            IsSet(WeightProperty) ? Weight : baseTokens.Weight);
    }
}

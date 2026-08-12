using Avalonia.Metadata;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens.Runtime;

namespace Material3Expressive.Markups;

/// <summary>Returns a strongly typed system type-scale key for use by other XAML extensions.</summary>
/// <example>
/// <c>FontSize="{m3:TokenRef Token={m3:TypeScaleToken BodyLarge, Field=FontSize}}"</c>
/// </example>
public sealed class TypeScaleTokenExtension
{
    public TypeScaleTokenExtension()
    {
    }

    public TypeScaleTokenExtension(TypographyRole role)
    {
        Role = role;
    }

    [ConstructorArgument("role")]
    public TypographyRole Role { get; set; } = TypographyRole.BodyMedium;

    public SystemTypeScaleField Field { get; set; } = SystemTypeScaleField.FontSize;

    public MaterialToken ProvideValue()
    {
        var tokens = SystemTypeScaleTokens.For(Role);
        return Field switch
        {
            SystemTypeScaleField.FontSize => tokens.FontSize,
            SystemTypeScaleField.LineHeight => tokens.LineHeight,
            SystemTypeScaleField.LetterSpacing => tokens.LetterSpacing,
            SystemTypeScaleField.Font => tokens.Font,
            SystemTypeScaleField.Weight => tokens.Weight,
            _ => throw new ArgumentOutOfRangeException(nameof(Field), Field, null)
        };
    }
}

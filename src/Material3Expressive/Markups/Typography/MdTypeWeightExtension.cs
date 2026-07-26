using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>The weight a type scale slot is set in, from the theme in scope at the target.</summary>
/// <inheritdoc cref="MdTypeFamilyExtension" path="/remarks" />
public class MdTypeWeightExtension
{
    public MdTypeWeightExtension()
    {
    }

    public MdTypeWeightExtension(TypographyRole role)
    {
        Role = role;
    }

    [ConstructorArgument("role")]
    public TypographyRole Role { get; set; } = TypographyRole.BodyMedium;

    public BindingBase ProvideValue()
    {
        return TypographyResolver.FontWeight(Role);
    }
}

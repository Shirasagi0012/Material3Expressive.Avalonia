using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>The typeface a type scale slot is set in, from the theme in scope at the target.</summary>
/// <remarks>
/// Takes a role rather than a face, because which of the theme's four faces a slot uses is itself
/// a token — restating it at the call site is how the type scale's data ended up duplicated across
/// the style sheet.
/// </remarks>
[Obsolete(
    "Legacy ResolvedTheme typography markup. Use typed MaterialValueKey<T> resources.",
    DiagnosticId = "M3E1004")]
public class MdTypeFamilyExtension
{
    public MdTypeFamilyExtension()
    {
    }

    public MdTypeFamilyExtension(TypographyRole role)
    {
        Role = role;
    }

    [ConstructorArgument("role")]
    public TypographyRole Role { get; set; } = TypographyRole.BodyMedium;

    public BindingBase ProvideValue()
    {
        return TypographyResolver.FontFamily(Role);
    }
}

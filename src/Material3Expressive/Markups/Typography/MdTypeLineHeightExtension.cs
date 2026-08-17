using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>Line height of a type scale, from the theme in scope at the target element.</summary>
[Obsolete(
    "Legacy ResolvedTheme typography markup. Use typed MaterialValueKey<T> resources.",
    DiagnosticId = "M3E1004")]
public class MdTypeLineHeightExtension
{
    public MdTypeLineHeightExtension()
    {
    }

    public MdTypeLineHeightExtension(TypographyRole token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public TypographyRole Token { get; set; } = TypographyRole.BodyMedium;

    public BindingBase ProvideValue()
    {
        return TypographyResolver.LineHeight(Token);
    }
}

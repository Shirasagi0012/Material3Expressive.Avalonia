using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>Font size of a type scale, from the theme in scope at the target element.</summary>
public class MdTypeSizeExtension
{
    public MdTypeSizeExtension()
    {
    }

    public MdTypeSizeExtension(TypographyRole token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public TypographyRole Token { get; set; } = TypographyRole.BodyMedium;

    public BindingBase ProvideValue()
    {
        return TypographyResolver.FontSize(Token);
    }
}

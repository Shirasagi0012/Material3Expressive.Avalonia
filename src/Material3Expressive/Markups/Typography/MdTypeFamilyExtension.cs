using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Markups;

/// <summary>A typeface from the theme in scope at the target element.</summary>
public class MdTypeFamilyExtension
{
    public MdTypeFamilyExtension()
    {
    }

    public MdTypeFamilyExtension(FontFamilyToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public FontFamilyToken Token { get; set; } = FontFamilyToken.Plain;

    public BindingBase ProvideValue()
    {
        return TypographyResolver.FontFamily(Token);
    }
}

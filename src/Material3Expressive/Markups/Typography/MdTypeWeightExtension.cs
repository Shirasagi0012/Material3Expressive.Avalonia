using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Markups;

/// <summary>A font weight from the theme in scope at the target element.</summary>
public class MdTypeWeightExtension
{
    public MdTypeWeightExtension()
    {
    }

    public MdTypeWeightExtension(FontWeightToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public FontWeightToken Token { get; set; } = FontWeightToken.Regular;

    public BindingBase ProvideValue()
    {
        return TypographyResolver.FontWeight(Token);
    }
}

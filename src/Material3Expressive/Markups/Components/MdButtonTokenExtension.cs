using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>
/// Resolves a button token against the theme in scope at the target element.
/// </summary>
/// <remarks>
/// App-author sugar only: the library's own controls read their tokens in C#. The variant and size
/// default to the filled small button, which is what a bare <c>&lt;Button/&gt;</c> renders as.
/// </remarks>
public class MdButtonTokenExtension
{
    public MdButtonTokenExtension()
    {
    }

    public MdButtonTokenExtension(ButtonToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public ButtonToken Token { get; set; } = ButtonToken.ContainerColor;

    public ButtonVariant Variant { get; set; } = ButtonVariant.Filled;

    public ButtonSize Size { get; set; } = ButtonSize.Small;

    public BindingBase ProvideValue()
    {
        return ButtonTokenResolver.Resolve(Token, Variant, Size);
    }
}

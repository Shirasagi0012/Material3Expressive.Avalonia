using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Controls.ButtonRuntime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>
/// Resolves a button token against the theme in scope at the target element.
/// </summary>
/// <remarks>
/// The returned binding observes <see cref="ButtonAssist.VariantProperty" /> and
/// <see cref="ButtonAssist.SizeProperty" /> on its target, so attached-property changes remain
/// live and there is no second variant or size source in markup.
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

    public BindingBase ProvideValue()
    {
        return ButtonTokenResolver.Resolve(Token);
    }
}

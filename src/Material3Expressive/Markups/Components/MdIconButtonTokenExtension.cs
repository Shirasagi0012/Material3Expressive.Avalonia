using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

/// <summary>Resolves an icon-button token against live IconButtonAssist properties.</summary>
public sealed class MdIconButtonTokenExtension
{
    public MdIconButtonTokenExtension()
    {
    }

    public MdIconButtonTokenExtension(IconButtonToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public IconButtonToken Token { get; set; } = IconButtonToken.ContainerColor;

    public BindingBase ProvideValue() => IconButtonTokenResolver.Resolve(Token);
}

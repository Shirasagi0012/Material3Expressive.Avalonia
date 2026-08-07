using Avalonia.Data;
using Avalonia.Metadata;

namespace Material3Expressive.Markups;

using Controls;

/// <summary>Resolves a ToggleButton-owned state-aware token against the live target.</summary>
public sealed class MdToggleButtonTokenExtension
{
    public MdToggleButtonTokenExtension()
    {
    }

    public MdToggleButtonTokenExtension(ToggleButtonToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public ToggleButtonToken Token { get; set; } = ToggleButtonToken.UncheckedContainerColor;

    public BindingBase ProvideValue()
    {
        return ToggleButtonTokenResolver.Resolve(Token);
    }
}

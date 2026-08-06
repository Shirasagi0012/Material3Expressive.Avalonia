using Avalonia.Data;
using Avalonia.Metadata;
using Material3Expressive.Controls.ButtonRuntime;

namespace Material3Expressive.Markups;

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

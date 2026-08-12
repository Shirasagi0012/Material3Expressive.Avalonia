using Avalonia.Metadata;
using Material3Expressive.Foundation.Tokens.Runtime;

namespace Material3Expressive.Markups;

/// <summary>Creates a requester-local alias definition for use in typed token override properties.</summary>
public sealed class TokenRefExtension
{
    /// <summary>Creates an empty extension for XAML property syntax.</summary>
    public TokenRefExtension()
    {
    }

    /// <summary>Creates an extension referring to <paramref name="token" />.</summary>
    public TokenRefExtension(MaterialToken token)
    {
        Token = token;
    }

    /// <summary>Gets or sets the target token.</summary>
    [ConstructorArgument("token")]
    public MaterialToken? Token { get; set; }

    /// <summary>Returns a <c>TokenDefinition&lt;T&gt;</c> matching the target token's value type.</summary>
    public object ProvideValue()
    {
        var target = Token ?? throw new InvalidOperationException("TokenRef requires a target token.");
        return typeof(Material3Expressive.Foundation.Tokens.Runtime.Token)
            .GetMethod(nameof(Material3Expressive.Foundation.Tokens.Runtime.Token.Alias))!
            .MakeGenericMethod(target.ValueType)
            .Invoke(null, [target])!;
    }
}

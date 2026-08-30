using Avalonia.Metadata;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Markups;

/// <summary>Creates a Raw alias definition for a <see cref="MaterialToken" />.</summary>
public sealed class TokenRefExtension
{
    /// <summary>Creates an empty extension for XAML property syntax.</summary>
    public TokenRefExtension()
    {
    }

    /// <summary>Creates an extension referring to <paramref name="token" />.</summary>
    public TokenRefExtension(object token)
    {
        Token = token;
    }

    /// <summary>Gets or sets the target token.</summary>
    [ConstructorArgument("token")]
    public object? Token { get; set; }

    /// <summary>Returns a <c>TokenDefinition&lt;T&gt;</c> matching the target token's value type.</summary>
    public object ProvideValue()
    {
        return Token is MaterialToken target
            ? target.CreateAliasDefinition()
            : throw new InvalidOperationException(
                Token is null
                    ? "TokenRef requires a target token."
                    : $"TokenRef cannot target '{Token.GetType().FullName}'.");
    }
}

using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace Material3ExpressiveRefactor.Foundation.Tokens;

/// <summary>
/// <c>{m3:TokenRef}</c> — authors a <see cref="TokenRef"/> alias value for use as a token
/// entry's value (design doc §5.3). Only the key-object form is supported here
/// (<c>{m3:TokenRef {x:Static ...}}</c>); family-name lookup is deferred (§12).
/// </summary>
public class TokenRefExtension : MarkupExtension
{
    public TokenRefExtension() { }

    public TokenRefExtension(MaterialTokenKey key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public MaterialTokenKey? Key { get; set; }

    public override TokenRef ProvideValue(IServiceProvider serviceProvider)
    {
        var key = Key ?? throw new InvalidOperationException("TokenRefExtension requires a key.");
        return new TokenRef(key);
    }
}

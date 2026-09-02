using Avalonia.Markup.Xaml;

using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Metadata;

namespace Material3ExpressiveRefactor.Foundation;

public class TokenExtension : MarkupExtension
{
    public TokenExtension(MaterialTokenKey key)
    {
        Key = key;
    }

    [ConstructorArgument("key")]
    public MaterialTokenKey? Key { get; set; }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (Key is null)
            throw new InvalidOperationException("Token key is required.");

        return new DynamicResourceExtension(Key);
    }
}

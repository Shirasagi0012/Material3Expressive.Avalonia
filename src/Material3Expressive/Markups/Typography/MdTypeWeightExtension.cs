using System;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Markups;

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

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentStack = (IAvaloniaXamlIlParentStackProvider?)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider));
        if (parentStack is null) return FontWeight.Regular;
        return TypographyResolver.ProvideFontWeightBinding(parentStack, Token);
    }
}

using System;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Markups;

public class MdTypeFamilyExtension
{
    public MdTypeFamilyExtension()
    {
    }

    public MdTypeFamilyExtension(FontFamilyToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")] 
    public FontFamilyToken Token { get; set; } = FontFamilyToken.Plain;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentStack = (IAvaloniaXamlIlParentStackProvider?)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider));
        if (parentStack is null) return TypefaceTokens.Plain;
        return TypographyResolver.ProvideFontFamilyBinding(parentStack, Token);
    }
}

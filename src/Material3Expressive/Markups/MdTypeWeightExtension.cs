using System;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Material3Expressive.Themes;
using Material3Expressive.Themes.Tokens;

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
        return TypographyHelper.ProvideFontWeightBinding(parentStack, Token);
    }
}

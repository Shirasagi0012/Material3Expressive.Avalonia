using System;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Themes;
using Material3Expressive.Tokens;

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

    [ConstructorArgument("token")] public FontFamilyToken Token { get; set; } = FontFamilyToken.Plain;

    public ThemeVariant? Theme { get; set; }

    public FontFamily? Fallback { get; set; }

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentStack = (IAvaloniaXamlIlParentStackProvider?)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider));
        if (parentStack is null) return FontFamilyToken.Plain;
        return TypographyHelper.ProvideFontFamilyBinding(parentStack, Token);
    }
}

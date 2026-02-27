using System;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Material3Expressive.Themes;
using Material3Expressive.Tokens;

namespace Material3Expressive.Markups;

public class MdTypeSizeExtension
{
    public MdTypeSizeExtension()
    {
    }

    public MdTypeSizeExtension(TypographyRoleToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")] public TypographyRoleToken Token { get; set; } = TypographyRoleToken.BodyMedium;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentStack = (IAvaloniaXamlIlParentStackProvider?)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider));
        if (parentStack is null) return TypeScaleTokens.BodyMedium.FontSize;
        return TypographyHelper.ProvideTypeScaleFontSizeBinding(parentStack, Token);
    }
}

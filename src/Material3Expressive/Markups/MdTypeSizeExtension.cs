using System;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Avalonia.Styling;

namespace Material3Expressive.Themes;

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

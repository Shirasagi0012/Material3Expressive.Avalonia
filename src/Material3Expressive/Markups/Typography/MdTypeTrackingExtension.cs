using System;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Markups;

public class MdTypeTrackingExtension
{
    public MdTypeTrackingExtension()
    {
    }

    public MdTypeTrackingExtension(TypographyRole token)
    {
        Token = token;
    }

    [ConstructorArgument("token")] 
    public TypographyRole Token { get; set; } = TypographyRole.BodyMedium;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentStack = (IAvaloniaXamlIlParentStackProvider?)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider));
        if (parentStack is null) return TypeScaleTokens.BodyMedium.LetterSpacing;
        return TypographyResolver.ProvideTypeScaleLetterSpacingBinding(parentStack, Token);
    }
}

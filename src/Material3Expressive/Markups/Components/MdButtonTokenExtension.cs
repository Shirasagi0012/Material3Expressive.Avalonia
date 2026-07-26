using System;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Markups;

public class MdButtonTokenExtension
{
    public MdButtonTokenExtension()
    {
    }

    public MdButtonTokenExtension(ButtonToken token)
    {
        Token = token;
    }

    [ConstructorArgument("token")]
    public ButtonToken Token { get; set; } = ButtonToken.ContainerBrush;

    public object ProvideValue(IServiceProvider serviceProvider)
    {
        var parentStack = (IAvaloniaXamlIlParentStackProvider?)serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider));
        if (parentStack is null)
            return GetFallbackValue();

        return ButtonTokenResolver.ProvideBinding(parentStack, Token);
    }

    private object GetFallbackValue()
    {
        return Token switch
        {
            ButtonToken.ContainerBrush => new SolidColorBrush(Color.Parse("#FF6750A4")),
            ButtonToken.ForegroundBrush => new SolidColorBrush(Colors.White),
            ButtonToken.DisabledContainerBrush => new SolidColorBrush(Color.Parse("#FFE7E0EC")),
            ButtonToken.DisabledForegroundBrush => new SolidColorBrush(Color.Parse("#FF49454F")),
            ButtonToken.StateLayerBrush => new SolidColorBrush(Colors.White),
            ButtonToken.CornerRadius => new CornerRadius(20),
            ButtonToken.Padding => new Thickness(24, 10),
            ButtonToken.MinHeight => 40d,
            ButtonToken.HoverStateLayerOpacity => 0.08d,
            ButtonToken.PressedStateLayerOpacity => 0.12d,
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}

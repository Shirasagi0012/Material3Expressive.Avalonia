using Avalonia;
using Avalonia.Media;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

[GenerateAttachedProperty<AvaloniaObject, ButtonTokens>("ButtonHost", Inherits = true)]
public partial class ButtonTokens : AvaloniaObject
{
    public ButtonTokens()
    {
        ContainerBrush = new SolidColorBrush(Color.Parse("#FF6750A4"));
        ForegroundBrush = new SolidColorBrush(Colors.White);
        DisabledContainerBrush = new SolidColorBrush(Color.Parse("#FFE7E0EC"));
        DisabledForegroundBrush = new SolidColorBrush(Color.Parse("#FF49454F"));
        StateLayerBrush = new SolidColorBrush(Colors.White);
        CornerRadius = new CornerRadius(20);
        Padding = new Thickness(24, 10);
        MinHeight = 40;
        HoverStateLayerOpacity = 0.08;
        PressedStateLayerOpacity = 0.12;
    }

    [GeneratedStyledProperty] public partial IBrush ContainerBrush { get; set; }
    [GeneratedStyledProperty] public partial IBrush ForegroundBrush { get; set; }
    [GeneratedStyledProperty] public partial IBrush DisabledContainerBrush { get; set; }
    [GeneratedStyledProperty] public partial IBrush DisabledForegroundBrush { get; set; }
    [GeneratedStyledProperty] public partial IBrush StateLayerBrush { get; set; }
    [GeneratedStyledProperty] public partial CornerRadius CornerRadius { get; set; }
    [GeneratedStyledProperty] public partial Thickness Padding { get; set; }
    [GeneratedStyledProperty] public partial double MinHeight { get; set; }
    [GeneratedStyledProperty] public partial double HoverStateLayerOpacity { get; set; }
    [GeneratedStyledProperty] public partial double PressedStateLayerOpacity { get; set; }

    public object ProvideValue()
    {
        return this;
    }
}

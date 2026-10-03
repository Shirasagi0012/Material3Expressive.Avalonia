using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Typography;
namespace Material3Expressive.Components;
internal sealed partial class TextFieldLayout
{
    public static readonly StyledProperty<IBrush?> ContainerBrushProperty = AvaloniaProperty.Register<TextFieldLayout, IBrush?>(nameof(ContainerBrush));
    public IBrush? ContainerBrush { get => GetValue(ContainerBrushProperty); set => SetValue(ContainerBrushProperty, value); }
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty = AvaloniaProperty.Register<TextFieldLayout, IBrush?>(nameof(IndicatorBrush));
    public IBrush? IndicatorBrush { get => GetValue(IndicatorBrushProperty); set => SetValue(IndicatorBrushProperty, value); }
    public static readonly StyledProperty<IBrush?> LabelBrushProperty = AvaloniaProperty.Register<TextFieldLayout, IBrush?>(nameof(LabelBrush));
    public IBrush? LabelBrush { get => GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }
    public static readonly StyledProperty<TextStyle?> LargeTextStyleProperty = AvaloniaProperty.Register<TextFieldLayout, TextStyle?>(nameof(LargeTextStyle));
    public TextStyle? LargeTextStyle { get => GetValue(LargeTextStyleProperty); set => SetValue(LargeTextStyleProperty, value); }
    public static readonly StyledProperty<TextStyle?> SmallTextStyleProperty = AvaloniaProperty.Register<TextFieldLayout, TextStyle?>(nameof(SmallTextStyle));
    public TextStyle? SmallTextStyle { get => GetValue(SmallTextStyleProperty); set => SetValue(SmallTextStyleProperty, value); }
}

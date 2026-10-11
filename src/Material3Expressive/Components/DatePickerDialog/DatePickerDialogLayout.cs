using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Components;

// Core DatePickerDialog.skiko.kt: requiredWidth is inside BasicAlertDialog's caller
// constraints. It can overflow those constraints, centered in its reported allocation.
internal sealed class DatePickerDialogContent : Decorator
{
    public static readonly StyledProperty<double> ContainerWidthProperty = AvaloniaProperty.Register<DatePickerDialogContent, double>(nameof(ContainerWidth));
    public static readonly StyledProperty<double> ContainerHeightProperty = AvaloniaProperty.Register<DatePickerDialogContent, double>(nameof(ContainerHeight));
    public double ContainerWidth { get => GetValue(ContainerWidthProperty); set => SetValue(ContainerWidthProperty, value); }
    public double ContainerHeight { get => GetValue(ContainerHeightProperty); set => SetValue(ContainerHeightProperty, value); }
    static DatePickerDialogContent() => AffectsMeasure<DatePickerDialogContent>(ContainerWidthProperty, ContainerHeightProperty);
    protected override bool BypassFlowDirectionPolicies => true;
    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(new Size(ContainerWidth, Math.Min(ContainerHeight, availableSize.Height)));
        return new Size(ContainerWidth, Child?.DesiredSize.Height ?? 0);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        // modifier.wrapContentHeight removes the incoming minimum height and
        // centers the wrapped Surface within any larger explicit allocation.
        var height = Child?.DesiredSize.Height ?? 0;
        Child?.Arrange(new Rect((finalSize.Width - ContainerWidth) / 2, (finalSize.Height - height) / 2, ContainerWidth, height));
        return finalSize;
    }
}

internal sealed class DatePickerDialogColumn : Panel
{
    protected override bool BypassFlowDirectionPolicies => true;
    protected override Size MeasureOverride(Size availableSize)
    {
        var content = Children[0];
        var actions = Children[1];
        actions.Measure(new Size(Math.Max(0, availableSize.Width - 6), Math.Max(0, availableSize.Height - 8)));
        content.Measure(new Size(availableSize.Width, Math.Max(0, availableSize.Height - actions.DesiredSize.Height - 8)));
        return new Size(availableSize.Width, content.DesiredSize.Height + actions.DesiredSize.Height + 8);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var content = Children[0];
        var actions = Children[1];
        content.Arrange(new Rect(0, 0, finalSize.Width, content.DesiredSize.Height));
        // Resolve Alignment.End and PaddingValues(end=6,bottom=8) physically,
        // without putting the already direction-aware FlowRow inside a mirrored Border.
        var x = FlowDirection == FlowDirection.RightToLeft ? 6 : finalSize.Width - 6 - actions.DesiredSize.Width;
        actions.Arrange(new Rect(x, finalSize.Height - actions.DesiredSize.Height - 8, actions.DesiredSize.Width, actions.DesiredSize.Height));
        return finalSize;
    }
}

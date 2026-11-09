using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Interactivity;
using ComposeAnimation;
using Material3Expressive.Components;

namespace Material3Expressive.Catalog;

public partial class ProgressIndicatorDemo : Window
{
    public ProgressIndicatorDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        ComposedStop.DrawStopIndicator = (context, _) =>
        {
            ProgressIndicatorDefaults.DrawStopIndicator(context, ComposedStop, 12, ComposedStop.Color, PenLineCap.Square);
            ProgressIndicatorDefaults.DrawStopIndicator(context, ComposedStop, 5, Colors.OrangeRed, PenLineCap.Round);
        };
    }

    private void ReverseStopDirection(object? sender, RoutedEventArgs e)
    {
        var direction = ComposedStop.FlowDirection == FlowDirection.LeftToRight ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        ComposedStop.FlowDirection = direction; IndependentStop.FlowDirection = direction;
    }
}

public sealed class ProgressStopPreview : Control
{
    public override void Render(DrawingContext context) =>
        ProgressIndicatorDefaults.DrawStopIndicator(context, this, 8, Colors.Teal, PenLineCap.Round);
}

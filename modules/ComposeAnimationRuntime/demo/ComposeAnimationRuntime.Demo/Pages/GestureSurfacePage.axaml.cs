using System.Numerics;
using Avalonia.Controls;
using Avalonia.Media;

namespace ComposeAnimationRuntime.Demo.Pages;

internal sealed partial class GestureSurfacePage : MotionPageBase
{
    public GestureSurfacePage() => InitializeComponent();
    protected override void OnAttached()
    {
        Surface.Minimum = Vector2.Zero;
        Surface.Maximum = new Vector2(560, 170);
        Surface.PositionChanged += OnPositionChanged;
        Surface.InteractiveTransition.ProgressChanged += OnProgressChanged;
    }
    protected override void OnDetached()
    {
        Surface.PositionChanged -= OnPositionChanged;
        Surface.InteractiveTransition.ProgressChanged -= OnProgressChanged;
    }
    private void OnPositionChanged(object? sender, Vector2 position)
    {
        Surface.RenderTransform = new TranslateTransform(position.X, position.Y);
        Status.Text = $"position = {position.X:0}, {position.Y:0}";
    }
    private void OnProgressChanged(double progress) => Progress.Value = progress;
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ComposeAnimation;

// Outer suppress / inner capture. Opacity and hit testing on the outer layer do not change
// user Opacity / RenderTransform on the captured content.
internal sealed class SharedCaptureSurface : SharedLayoutControl
{
    public SharedCaptureSurface() => UseLayoutRounding = false;
    protected override Control? LayoutChild => _child;
    internal bool OwnsLogicalChild { get; set; } = true;
    private Control? _child;
    public Control? Child
    {
        get => _child;
        set
        {
            if (_child == value) return;
            if (_child is not null)
            {
                VisualChildren.Remove(_child);
                if (OwnsLogicalChild) LogicalChildren.Remove(_child);
            }
            _child = value;
            if (value is not null)
            {
                VisualChildren.Add(value);
                if (OwnsLogicalChild) LogicalChildren.Add(value);
            }
            InvalidateMeasure();
        }
    }
}

internal sealed class SharedSuppressLayer : SharedLayoutControl
{
    protected override Control LayoutChild => Surface;
    internal SharedCaptureSurface Surface { get; } = new();
    private bool _suppressed;
    private int _suppressGeneration;
    internal int SuppressGeneration => _suppressGeneration;
    internal bool IsSuppressed => _suppressed;

    public SharedSuppressLayer()
    {
        UseLayoutRounding = false;
        VisualChildren.Add(Surface);
        LogicalChildren.Add(Surface);
    }

    internal void SetSuppressed(bool value, int generation)
    {
        if (value)
        {
            _suppressGeneration = generation;
            if (_suppressed) return;
            _suppressed = true;
            Opacity = 0;
            IsHitTestVisible = false;
            return;
        }
        if (!_suppressed || generation != _suppressGeneration) return;
        _suppressed = false;
        Opacity = 1;
        IsHitTestVisible = true;
    }

}

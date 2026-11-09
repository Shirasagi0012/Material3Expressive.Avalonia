using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ComposeAnimation;

internal sealed class ContentViewport : Decorator
{
    internal AnimatedContent? Host { get; set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        Child?.Measure(availableSize);
        return Child?.DesiredSize ?? default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is null || Host is null) return finalSize;
        var size = Child.DesiredSize;
        // Start/End stay logical here; the native visual-tree mirror resolves RTL once.
        var offset = EnterExitMotion.Align(size, finalSize, Host.ContentAlignment, FlowDirection.LeftToRight);
        Child.Arrange(new Rect(offset, size));
        return finalSize;
    }
}

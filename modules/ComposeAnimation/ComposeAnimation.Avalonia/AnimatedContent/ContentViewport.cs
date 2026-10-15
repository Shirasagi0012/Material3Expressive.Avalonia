using Avalonia;
using Avalonia.Controls;

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
        var offset = EnterExitMotion.Align(size, finalSize, Host.ContentAlignment, FlowDirection);
        Child.Arrange(new Rect(offset, size));
        return finalSize;
    }
}

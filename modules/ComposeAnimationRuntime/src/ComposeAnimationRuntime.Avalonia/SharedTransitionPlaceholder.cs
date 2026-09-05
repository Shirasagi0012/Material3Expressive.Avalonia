using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class SharedTransitionPlaceholder : Control
{
    public SharedTransitionPlaceholder(Size size, object? key = null)
    {
        Key = key;
        Width = size.Width;
        Height = size.Height;
        IsHitTestVisible = false;
    }

    public object? Key { get; }
}

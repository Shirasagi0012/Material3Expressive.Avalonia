using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Foundation;

// InteractiveComponentSize.kt's two minimum-interactive alignment lines. Their merge
// operation is min, and each descendant's line is translated into the queried layout.
internal interface IMinimumInteractiveLayout
{
    Point MinimumInteractivePadding { get; }
}

internal static class MinimumInteractiveAlignment
{
    internal static Point Read(Control root)
    {
        var lines = root.GetVisualDescendants().Prepend(root).OfType<Control>()
            .Where(control => control is IMinimumInteractiveLayout)
            .Select(control => control.TranslatePoint(((IMinimumInteractiveLayout)control).MinimumInteractivePadding, root))
            .OfType<Point>().ToArray();
        return lines.Length == 0 ? default : new Point(lines.Min(point => point.X), lines.Min(point => point.Y));
    }
}

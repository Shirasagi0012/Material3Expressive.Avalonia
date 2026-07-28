using Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Exposes a named rectangle rendered by a control but not represented by a visual child.
/// </summary>
public interface IVisualBoundsProbe
{
    /// <summary>Gets the requested rectangle in the implementing control's local coordinates.</summary>
    bool TryGetVisualBounds(string id, out Rect bounds);
}

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Controls;

/// <summary>
/// Draws the surface every Material component sits on: a rounded rectangle, optionally outlined,
/// optionally casting a shadow.
/// </summary>
/// <remarks>
/// Shared because the geometry has one subtlety worth writing once. A pen straddles the path it
/// strokes, so an outline drawn on the control's bounds would be half outside them and clipped;
/// both the rectangle and its corner radii have to be inset by half the border width.
/// </remarks>
public static class MaterialContainer
{
    public static void Draw(
        DrawingContext context,
        Size size,
        CornerRadius corners,
        IBrush? fill,
        IImmutableBrush? outline = null,
        double outlineWidth = 0,
        BoxShadows shadows = default)
    {
        if (size.Width <= 0 || size.Height <= 0)
            return;

        var pen = outline is { } && outlineWidth > 0 ? new ImmutablePen(outline, outlineWidth) : null;
        var inset = pen is { } ? outlineWidth / 2 : 0;

        context.DrawRectangle(
            fill,
            pen,
            new RoundedRect(new Rect(size).Deflate(inset), Deflate(corners, inset)),
            shadows);
    }

    /// <summary>Shrinks corner radii to match a rectangle that has been inset by the same amount.</summary>
    public static CornerRadius Deflate(CornerRadius radius, double amount)
    {
        if (amount <= 0)
            return radius;

        return new CornerRadius(
            Math.Max(0, radius.TopLeft - amount),
            Math.Max(0, radius.TopRight - amount),
            Math.Max(0, radius.BottomRight - amount),
            Math.Max(0, radius.BottomLeft - amount));
    }
}

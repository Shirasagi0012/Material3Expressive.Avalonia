using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Foundation.Interaction;

// Fixed material3-ripple Border.kt, restricted to rectangular/rounded outlines. Each stroke
// has its own cache; both animation parameters are read before comparing the physical geometry.
internal sealed class InsetRingBorder
{
    private (Size Size, CornerRadius Corners, float Scale, float Width, float Inset)? _key;
    private Geometry? _geometry;

    internal void Clear() { _key = null; _geometry = null; }

    internal void Draw(DrawingContext context, IBrush brush, Size size, CornerRadius corners,
        float scale, float widthDip, float insetDip)
    {
        var width = (float)size.Width * scale;
        var height = (float)size.Height * scale;
        var inset = MathF.Ceiling(insetDip * scale);
        var stroke = MathF.Max(0, MathF.Min(widthDip == 0 ? 1 : MathF.Ceiling(widthDip * scale),
            MathF.Ceiling((MathF.Min(width, height) - inset * 2) / 2)));
        var fill = inset == 0 && stroke * 2 > MathF.Min(width, height);
        var key = (size, corners, scale, stroke, inset);

        if (corners.IsUniform)
        {
            var radius = MathF.Max(0, (float)corners.TopLeft * scale - inset);
            if (fill)
            {
                context.DrawRectangle(brush, null, new RoundedRect(new Rect(size), radius / scale));
                return;
            }
            if (corners == default || radius >= stroke / 2)
            {
                var offset = inset + stroke / 2;
                if (width - stroke - inset * 2 <= 0 || height - stroke - inset * 2 <= 0) return;
                context.DrawRectangle(null, new Pen(brush, stroke / scale), new RoundedRect(
                    new Rect(offset / scale, offset / scale,
                        (width - stroke - inset * 2) / scale, (height - stroke - inset * 2) / scale),
                    MathF.Max(0, radius - stroke / 2) / scale));
                return;
            }
            // Keep Border.kt's small-radius branch: the outer fill and interior cutout
            // use the original bounds, even when a nonzero inset was supplied.
            if (_key != key)
            {
                var outer = new RectangleGeometry(new Rect(size), radius / scale, radius / scale);
                var inside = new Rect(stroke / scale, stroke / scale,
                    Math.Max(0, (width - stroke * 2) / scale), Math.Max(0, (height - stroke * 2) / scale));
                _geometry = inside.Width > 0 && inside.Height > 0
                    ? new CombinedGeometry(GeometryCombineMode.Exclude, outer, new RectangleGeometry(inside)) : outer;
                _key = key;
            }
        }
        else if (_key != key)
        {
            Geometry InsetPath(float amount)
            {
                var bounds = new Rect(amount / scale, amount / scale,
                    Math.Max(0, (width - amount * 2) / scale), Math.Max(0, (height - amount * 2) / scale));
                var shrunk = new CornerRadius(
                    MathF.Max(0, (float)corners.TopLeft * scale - amount) / scale,
                    MathF.Max(0, (float)corners.TopRight * scale - amount) / scale,
                    MathF.Max(0, (float)corners.BottomRight * scale - amount) / scale,
                    MathF.Max(0, (float)corners.BottomLeft * scale - amount) / scale);
                return RoundedRectangleGeometry.Create(bounds, shrunk);
            }
            var outer = InsetPath(inset);
            _geometry = fill || width <= (stroke + inset) * 2 || height <= (stroke + inset) * 2
                ? outer : new CombinedGeometry(GeometryCombineMode.Exclude, outer, InsetPath(stroke + inset));
            _key = key;
        }
        if (_geometry is not null) context.DrawGeometry(brush, null, _geometry);
    }
}

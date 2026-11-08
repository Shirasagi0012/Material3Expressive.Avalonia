using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Foundation.Interaction;

// Fixed material3-ripple Border.kt. The presenter owns the generic layer replay order;
// both animation parameters are read before comparing the physical geometry.
internal sealed class InsetRingBorder
{
    private (Size Size, CornerRadius Corners, float Scale, float Width, float Inset)? _key;
    private Geometry? _geometry;
    private readonly PathBorderMask _maskCache = new();
    private WriteableBitmap? _mask;
    private (PathGeometry Shape, Size Size, float Scale, float Width, float Inset, Color Color)? _pathKey;

    internal void Clear() { _key = null; _geometry = null; _pathKey = null; _mask?.Dispose(); _mask = null; _maskCache.Dispose(); }

    internal void Draw(DrawingContext context, IBrush brush, Size size, CornerRadius corners,
        float scale, float widthDip, float insetDip, PathGeometry? shape = null)
    {
        var width = (float)size.Width * scale;
        var height = (float)size.Height * scale;
        var inset = MathF.Ceiling(insetDip * scale);
        var stroke = MathF.Max(0, MathF.Min(widthDip == 0 ? 1 : MathF.Ceiling(widthDip * scale),
            MathF.Ceiling((MathF.Min(width, height) - inset * 2) / 2)));
        var fill = inset == 0 && stroke * 2 > MathF.Min(width, height);
        var key = (size, corners, scale, stroke, inset);

        if (shape is not null)
        {
            if (fill) { context.DrawGeometry(brush, null, shape); return; }
            using var path = ShapeGeometry.ToSkiaPath(shape, scale);
            var bounds = path.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0) return;
            var solid = (ISolidColorBrush)brush;
            var color = Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
            var pathKey = (shape, size, scale, stroke, inset, color);
            if (_mask is null || _pathKey != pathKey)
            {
                _mask?.Dispose();
                _mask = _maskCache.Update(path, stroke, scale, false, size, color, inset, eraseInset: true);
                _pathKey = pathKey;
            }
            context.DrawImage(_mask, new Rect(0, 0, MathF.Ceiling(bounds.Width), MathF.Ceiling(bounds.Height)),
                new Rect(bounds.Left / scale, bounds.Top / scale, MathF.Ceiling(bounds.Width) / scale, MathF.Ceiling(bounds.Height) / scale));
            return;
        }

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

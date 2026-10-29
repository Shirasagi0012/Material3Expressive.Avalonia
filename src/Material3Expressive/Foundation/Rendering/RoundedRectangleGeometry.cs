using Avalonia;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Rendering;

internal static class RoundedRectangleGeometry
{
    internal static StreamGeometry Create(Rect bounds, CornerRadius corners)
    {
        // Skia's addRoundRect/SetRectRadii scales overlapping radii together before
        // adding arcs. StreamGeometry requires that fit to be explicit on each path.
        double fit = 1;
        void Limit(double first, double second, double extent)
        {
            if (first + second > extent) fit = Math.Min(fit, extent / (first + second));
        }
        Limit(corners.TopLeft, corners.TopRight, bounds.Width);
        Limit(corners.TopRight, corners.BottomRight, bounds.Height);
        Limit(corners.BottomRight, corners.BottomLeft, bounds.Width);
        Limit(corners.BottomLeft, corners.TopLeft, bounds.Height);
        if (fit < 1)
            corners = new((float)(corners.TopLeft * fit), (float)(corners.TopRight * fit),
                (float)(corners.BottomRight * fit), (float)(corners.BottomLeft * fit));
        var geometry = new StreamGeometry();
        using var path = geometry.Open();
        path.BeginFigure(new Point(bounds.Left + corners.TopLeft, bounds.Top), true);
        path.LineTo(new Point(bounds.Right - corners.TopRight, bounds.Top));
        path.ArcTo(new Point(bounds.Right, bounds.Top + corners.TopRight), new Size(corners.TopRight, corners.TopRight), 0, false, SweepDirection.Clockwise);
        path.LineTo(new Point(bounds.Right, bounds.Bottom - corners.BottomRight));
        path.ArcTo(new Point(bounds.Right - corners.BottomRight, bounds.Bottom), new Size(corners.BottomRight, corners.BottomRight), 0, false, SweepDirection.Clockwise);
        path.LineTo(new Point(bounds.Left + corners.BottomLeft, bounds.Bottom));
        path.ArcTo(new Point(bounds.Left, bounds.Bottom - corners.BottomLeft), new Size(corners.BottomLeft, corners.BottomLeft), 0, false, SweepDirection.Clockwise);
        path.LineTo(new Point(bounds.Left, bounds.Top + corners.TopLeft));
        path.ArcTo(new Point(bounds.Left + corners.TopLeft, bounds.Top), new Size(corners.TopLeft, corners.TopLeft), 0, false, SweepDirection.Clockwise);
        path.EndFigure(true);
        return geometry;
    }
}

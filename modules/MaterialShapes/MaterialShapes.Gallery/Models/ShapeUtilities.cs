using Avalonia;
using MaterialShapes;

namespace MaterialShapes.Gallery.Models;

internal static class ShapeUtilities
{
    public static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }

    public static Point RadialToCartesian(double radius, double angleRadians, Point center = default)
    {
        return new Point(
            Math.Cos(angleRadians) * radius + center.X,
            Math.Sin(angleRadians) * radius + center.Y
        );
    }

    public static string ToSvgString(RoundedPolygon polygon)
    {
        var cubics = polygon.Cubics;
        if (cubics.Count == 0)
            return "d=\"\"";

        var sb = new System.Text.StringBuilder();
        sb.Append("d=\"M ");
        sb.Append(cubics[0].Anchor0.X);
        sb.Append(", ");
        sb.Append(cubics[0].Anchor0.Y);

        foreach (var c in cubics)
        {
            sb.Append(" C ");
            sb.Append(c.Control0.X);
            sb.Append(", ");
            sb.Append(c.Control0.Y);
            sb.Append(", ");
            sb.Append(c.Control1.X);
            sb.Append(", ");
            sb.Append(c.Control1.Y);
            sb.Append(", ");
            sb.Append(c.Anchor1.X);
            sb.Append(", ");
            sb.Append(c.Anchor1.Y);
        }

        sb.Append("\"");
        return sb.ToString();
    }
}

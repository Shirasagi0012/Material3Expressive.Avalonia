using Avalonia;
using Avalonia.Media;

namespace MaterialShapes;

public static class AvaloniaExtensions
{
    /// <summary>
    /// The start angle upstream uses by default: 12 o'clock in screen coordinates.
    /// </summary>
    /// <remarks>
    /// Our own default is <c>0</c> (no rotation). Upstream's <c>toPath</c> is internal and every
    /// caller re-centres the result afterwards, so a non-zero default is harmless there; on a
    /// public API it would silently rotate — and, since the rotation is about the origin rather
    /// than the shape's centre, translate — every shape drawn through it. Pass this explicitly
    /// when porting material3 drawing code.
    /// </remarks>
    public const int StartAngleTop = 270;

    extension(StreamGeometryContext ctx)
    {
        /// <summary>
        /// Writes a rounded polygon as one closed, filled figure.
        /// </summary>
        /// <param name="polygon">The rounded polygon to draw.</param>
        /// <param name="startAngle">
        /// Rotates the figure so that its first anchor sits at this angle, measured from the
        /// polygon's centre. <c>0</c> leaves it unrotated; see <see cref="StartAngleTop" />.
        /// </param>
        /// <param name="repeatPath">
        /// Emits the cubics a second time, joined by a line — how upstream builds the two-ring
        /// path that a stroked wavy progress indicator needs.
        /// </param>
        /// <param name="closePath">Whether to close the figure.</param>
        public void DrawRoundedPolygon(
            RoundedPolygon polygon,
            int startAngle = 0,
            bool repeatPath = false,
            bool closePath = true
        )
        {
            WriteCubics(ctx, polygon.Cubics, startAngle, repeatPath, closePath, polygon.Center);
        }

        /// <summary>
        /// Writes a morph at the given progress as one closed, filled figure.
        /// </summary>
        /// <param name="morph">The morph to draw.</param>
        /// <param name="progress">Morph progress, 0..1.</param>
        /// <param name="startAngle">See <see cref="DrawRoundedPolygon" />.</param>
        /// <param name="repeatPath">See <see cref="DrawRoundedPolygon" />.</param>
        /// <param name="closePath">Whether to close the figure.</param>
        /// <param name="rotationPivot">
        /// The point <paramref name="startAngle" /> is measured from. Upstream defaults this to
        /// the origin for morphs, since a morph has no single centre.
        /// </param>
        public void DrawMorph(
            Morph morph,
            double progress,
            int startAngle = 0,
            bool repeatPath = false,
            bool closePath = true,
            Point rotationPivot = default
        )
        {
            WriteCubics(
                ctx,
                // Match graphics-shapes Morph.asCubics(): its final cubic deliberately reuses the
                // first anchor so the contour closes exactly. EnumerateCubics exposes the raw
                // interpolation stream and can leave a sub-pixel closing line at the seam.
                morph.AsCubics(progress),
                startAngle,
                repeatPath,
                closePath,
                rotationPivot);
        }
    }

    extension(RoundedPolygon polygon)
    {
        /// <summary>
        /// Converts a rounded polygon to a geometry. The polygon is expected to be normalized to a
        /// 1×1 box; those from <see cref="MaterialShapeCatalog" /> already are.
        /// </summary>
        public StreamGeometry ToGeometry(int startAngle = 0, bool repeatPath = false, bool closePath = true)
        {
            var geometry = new StreamGeometry();
            using var ctx = geometry.Open();
            ctx.DrawRoundedPolygon(polygon, startAngle, repeatPath, closePath);
            return geometry;
        }
    }

    extension(Morph morph)
    {
        /// <summary>Converts a morph at the given progress to a geometry.</summary>
        public StreamGeometry ToGeometry(
            double progress,
            int startAngle = 0,
            bool repeatPath = false,
            bool closePath = true,
            Point rotationPivot = default
        )
        {
            var geometry = new StreamGeometry();
            using var ctx = geometry.Open();
            ctx.DrawMorph(morph, progress, startAngle, repeatPath, closePath, rotationPivot);
            return geometry;
        }
    }

    private static void WriteCubics(
        StreamGeometryContext ctx,
        IReadOnlyList<CubicBezier> cubics,
        int startAngle,
        bool repeatPath,
        bool closePath,
        Point rotationPivot
    )
    {
        var (figure, repeatIndex) = BuildFigure(cubics, startAngle, repeatPath, rotationPivot);
        if (figure.Count == 0)
            return;

        ctx.BeginFigure(figure[0].Anchor0, true);

        for (var i = 0; i < figure.Count; i++)
        {
            // Upstream joins the two passes with an explicit line; for a closed shape it is
            // degenerate, but it matters when the figure is left open.
            if (i == repeatIndex)
                ctx.LineTo(figure[i].Anchor0);

            ctx.CubicBezierTo(figure[i].Control0, figure[i].Control1, figure[i].Anchor1);
        }

        ctx.EndFigure(closePath);
    }

    /// <summary>
    /// Produces the cubics of the figure in emission order, already rotated, along with the index
    /// at which the repeated pass begins (or -1).
    /// </summary>
    /// <remarks>
    /// Ported from androidx <c>compose/material3/internal/ShapeUtil.kt</c> (<c>pathFromCubics</c>).
    /// Note that the rotation is applied about the origin, not about the pivot — the pivot only
    /// decides where the first anchor's angle is measured from. That is upstream's behaviour, and
    /// its callers compensate by re-centring afterwards.
    /// </remarks>
    internal static (List<CubicBezier> Cubics, int RepeatIndex) BuildFigure(
        IReadOnlyList<CubicBezier> cubics,
        int startAngle,
        bool repeatPath,
        Point rotationPivot
    )
    {
        if (cubics.Count == 0)
            return ([], -1);

        var transform = CreateStartAngleTransform(cubics, startAngle, rotationPivot);

        var figure = new List<CubicBezier>(repeatPath ? cubics.Count * 2 : cubics.Count);
        for (var pass = 0; pass < (repeatPath ? 2 : 1); pass++)
            foreach (var cubic in cubics)
                figure.Add(cubic.Transformed(point => point.Transform(transform)));

        return (figure, repeatPath ? cubics.Count : -1);
    }

    /// <summary>
    /// The rotation that brings the first anchor to <paramref name="startAngle" />, as measured
    /// from <paramref name="rotationPivot" />. Identity when no start angle is requested.
    /// </summary>
    internal static Matrix CreateStartAngleTransform(
        IReadOnlyList<CubicBezier> cubics,
        int startAngle,
        Point rotationPivot
    )
    {
        if (startAngle == 0 || cubics.Count == 0)
            return Matrix.Identity;

        var first = cubics[0].Anchor0;
        var angleToFirstCubic = Math.Atan2(first.Y - rotationPivot.Y, first.X - rotationPivot.X) * 180.0 / Math.PI;

        var radians = (-angleToFirstCubic + startAngle) * Math.PI / 180.0;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return new Matrix(cos, sin, -sin, cos, 0, 0);
    }
}

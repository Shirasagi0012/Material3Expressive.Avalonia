using Avalonia;

namespace Material3Expressive.Foundation.Interaction;

// RoundedCornerShape's CornerSize values retain their size dependency across interpolation.
// Only the public CornerRadius adapter knows about the legacy Full=1000 convention.
internal abstract record CornerShape
{
    public abstract CornerRadius Resolve(Size size);

    internal sealed record Defined(CornerRadius Corners, float FullFraction = .5f) : CornerShape
    {
        // Unit metadata must not distinguish two identical all-DIP shapes.
        public float FullFraction { get; } = Corners.TopLeft == 1000 || Corners.TopRight == 1000
            || Corners.BottomRight == 1000 || Corners.BottomLeft == 1000 ? FullFraction : .5f;

        public override CornerRadius Resolve(Size size)
        {
            float Corner(double value) => value == 1000 && size.Width > 0 && size.Height > 0
                ? (float)Math.Min(size.Width, size.Height) * FullFraction : (float)value;
            return new(Corner(Corners.TopLeft), Corner(Corners.TopRight),
                Corner(Corners.BottomRight), Corner(Corners.BottomLeft));
        }
    }

    internal sealed record Blend(CornerShape From, CornerShape To, float Progress) : CornerShape
    {
        public override CornerRadius Resolve(Size size) =>
            AnimatedCornerRadius.Lerp(From.Resolve(size), To.Resolve(size), Progress);
    }

    // CornerBasedShape.createOutline: each vertical pair is limited by the smaller extent.
    // Skia's subsequent round-rect fit still handles overlaps along horizontal edges.
    public static CornerRadius Normalize(CornerRadius corners, Size size)
    {
        // A template can publish its target before its first nonempty arrange.
        if (size.Width <= 0 || size.Height <= 0) return corners;
        var extent = (float)Math.Min(size.Width, size.Height);
        var left = (float)corners.TopLeft + (float)corners.BottomLeft;
        var right = (float)corners.TopRight + (float)corners.BottomRight;
        var leftScale = left > extent ? extent / left : 1f;
        var rightScale = right > extent ? extent / right : 1f;
        return new((float)corners.TopLeft * leftScale, (float)corners.TopRight * rightScale,
            (float)corners.BottomRight * rightScale, (float)corners.BottomLeft * leftScale);
    }
}

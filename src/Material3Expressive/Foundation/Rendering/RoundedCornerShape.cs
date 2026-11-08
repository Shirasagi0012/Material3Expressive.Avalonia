using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>A rounded rectangle with logical DIP or percentage corners. Percentages use the smaller side.</summary>
public sealed class RoundedCornerShape : MaterialShape
{
    public static readonly StyledProperty<RelativeScalar> TopStartProperty = CutCornerShape.TopStartProperty.AddOwner<RoundedCornerShape>();
    public static readonly StyledProperty<RelativeScalar> TopEndProperty = CutCornerShape.TopEndProperty.AddOwner<RoundedCornerShape>();
    public static readonly StyledProperty<RelativeScalar> BottomEndProperty = CutCornerShape.BottomEndProperty.AddOwner<RoundedCornerShape>();
    public static readonly StyledProperty<RelativeScalar> BottomStartProperty = CutCornerShape.BottomStartProperty.AddOwner<RoundedCornerShape>();
    public RelativeScalar TopStart { get => GetValue(TopStartProperty); set => SetValue(TopStartProperty, value); }
    public RelativeScalar TopEnd { get => GetValue(TopEndProperty); set => SetValue(TopEndProperty, value); }
    public RelativeScalar BottomEnd { get => GetValue(BottomEndProperty); set => SetValue(BottomEndProperty, value); }
    public RelativeScalar BottomStart { get => GetValue(BottomStartProperty); set => SetValue(BottomStartProperty, value); }

    public RoundedCornerShape() { }
    public RoundedCornerShape(double size) : this(new RelativeScalar(size, RelativeUnit.Absolute)) { }
    public RoundedCornerShape(RelativeScalar size) => TopStart = TopEnd = BottomEnd = BottomStart = size;

    public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
    {
        // CornerSize.toPx and CornerBasedShape's two independent vertical-pair fits.
        var scale = (float)density;
        var physical = new Size((float)size.Width * scale, (float)size.Height * scale);
        var minimum = (float)Math.Min(physical.Width, physical.Height);
        float Pixels(RelativeScalar corner) => corner.Unit == RelativeUnit.Relative
            ? minimum * (((float)corner.Scalar * 100f) / 100f) : (float)corner.Scalar * scale;
        var corners = CornerShape.Normalize(new(Pixels(TopStart), Pixels(TopEnd), Pixels(BottomEnd), Pixels(BottomStart)), physical);
        corners = new((float)corners.TopLeft / scale, (float)corners.TopRight / scale,
            (float)corners.BottomRight / scale, (float)corners.BottomLeft / scale);
        if (layoutDirection == FlowDirection.RightToLeft)
            corners = new(corners.TopRight, corners.TopLeft, corners.BottomLeft, corners.BottomRight);
        return RoundedRectangleGeometry.Create(new Rect(size), corners);
    }
}

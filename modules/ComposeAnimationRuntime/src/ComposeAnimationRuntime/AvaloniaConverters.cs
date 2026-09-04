using Avalonia;

namespace ComposeAnimationRuntime;

public sealed class PointVectorConverter : IAnimationVectorConverter<Point>
{
    public Point VisibilityThreshold { get; }

    public PointVectorConverter(double threshold = 0.001) => VisibilityThreshold = new Point(threshold, threshold);

    public AnimationVector ConvertToVector(Point value) => new AnimationVector2D((float)value.X, (float)value.Y);

    public Point ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}

public sealed class SizeVectorConverter : IAnimationVectorConverter<Size>
{
    public Size VisibilityThreshold { get; }

    public SizeVectorConverter(double threshold = 0.001) => VisibilityThreshold = new Size(threshold, threshold);

    public AnimationVector ConvertToVector(Size value) => new AnimationVector2D((float)value.Width, (float)value.Height);

    public Size ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}

public sealed class VectorVectorConverter : IAnimationVectorConverter<Vector>
{
    public Vector VisibilityThreshold { get; }

    public VectorVectorConverter(double threshold = 0.001) => VisibilityThreshold = new Vector(threshold, threshold);

    public AnimationVector ConvertToVector(Vector value) => new AnimationVector2D((float)value.X, (float)value.Y);

    public Vector ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}

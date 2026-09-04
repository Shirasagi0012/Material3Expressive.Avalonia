using Avalonia;
using ComposeAnimationRuntime;

namespace ComposeAnimationRuntime.Avalonia;

public sealed class PointVectorConverter(double threshold = 0.001) : IAnimationVectorConverter<Point>
{
    public Point VisibilityThreshold { get; } = new(threshold, threshold);
    public AnimationVector ConvertToVector(Point value) => new AnimationVector2D((float)value.X, (float)value.Y);
    public Point ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}

public sealed class SizeVectorConverter(double threshold = 0.001) : IAnimationVectorConverter<Size>
{
    public Size VisibilityThreshold { get; } = new(threshold, threshold);
    public AnimationVector ConvertToVector(Size value) => new AnimationVector2D((float)value.Width, (float)value.Height);
    public Size ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}

public sealed class VectorVectorConverter(double threshold = 0.001) : IAnimationVectorConverter<Vector>
{
    public Vector VisibilityThreshold { get; } = new(threshold, threshold);
    public AnimationVector ConvertToVector(Vector value) => new AnimationVector2D((float)value.X, (float)value.Y);
    public Vector ConvertFromVector(AnimationVector vector) => new(vector[0], vector[1]);
}

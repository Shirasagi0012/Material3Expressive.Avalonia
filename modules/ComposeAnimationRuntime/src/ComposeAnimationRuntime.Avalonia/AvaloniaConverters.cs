using Avalonia;
using Avalonia.Media;
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

public sealed class ColorVectorConverter : IAnimationVectorConverter<Color>
{
    public Color VisibilityThreshold { get; } = new(1, 1, 1, 1);
    public AnimationVector ConvertToVector(Color value) => new AnimationVector4D(value.A, value.R, value.G, value.B);
    public Color ConvertFromVector(AnimationVector vector) => Color.FromArgb(
        Clamp(vector[0]), Clamp(vector[1]), Clamp(vector[2]), Clamp(vector[3]));

    private static byte Clamp(float value) => (byte)Math.Clamp(MathF.Round(value), 0, 255);
}

public sealed class MatrixVectorConverter(double threshold = 0.001) : IAnimationVectorConverter<Matrix>
{
    public Matrix VisibilityThreshold { get; } = new(threshold, threshold, threshold, threshold, threshold, threshold);
    public AnimationVector ConvertToVector(Matrix value) => new AnimationVectorN(new[]
        { (float)value.M11, (float)value.M12, (float)value.M21, (float)value.M22, (float)value.M31, (float)value.M32 });
    public Matrix ConvertFromVector(AnimationVector vector) => new(
        vector[0], vector[1], vector[2], vector[3], vector[4], vector[5]);
}

public sealed class CornerRadiusVectorConverter(double threshold = 0.001) : IAnimationVectorConverter<CornerRadius>
{
    public CornerRadius VisibilityThreshold { get; } = new(threshold);
    public AnimationVector ConvertToVector(CornerRadius value) => new AnimationVector4D(
        (float)value.TopLeft, (float)value.TopRight, (float)value.BottomRight, (float)value.BottomLeft);
    public CornerRadius ConvertFromVector(AnimationVector vector) => new(
        vector[0], vector[1], vector[2], vector[3]);
}

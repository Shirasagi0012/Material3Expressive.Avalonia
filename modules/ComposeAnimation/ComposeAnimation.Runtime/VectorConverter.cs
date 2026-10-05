namespace ComposeAnimation.Runtime;

public interface ITwoWayConverter<T>
{
    int Dimension { get; }

    void To(T value, Span<double> destination);

    T From(ReadOnlySpan<double> vector);
}

public sealed class DoubleConverter : ITwoWayConverter<double>
{
    public int Dimension => 1;

    public void To(double value, Span<double> destination)
    {
        destination[0] = value;
    }

    public double From(ReadOnlySpan<double> vector) => vector[0];
}

public sealed class FloatConverter : ITwoWayConverter<float>
{
    public int Dimension => 1;

    public void To(float value, Span<double> destination)
    {
        destination[0] = value;
    }

    public float From(ReadOnlySpan<double> vector) => (float)vector[0];
}

public sealed class Int32Converter : ITwoWayConverter<int>
{
    public int Dimension => 1;

    public void To(int value, Span<double> destination)
    {
        destination[0] = value;
    }

    public int From(ReadOnlySpan<double> vector) => (int)vector[0];
}

public sealed class PointConverter : ITwoWayConverter<Avalonia.Point>
{
    public int Dimension => 2;

    public void To(Avalonia.Point value, Span<double> destination)
    {
        destination[0] = value.X;
        destination[1] = value.Y;
    }

    public Avalonia.Point From(ReadOnlySpan<double> vector) =>
        new(vector[0], vector[1]);
}

public sealed class VectorConverter : ITwoWayConverter<Avalonia.Vector>
{
    public int Dimension => 2;

    public void To(Avalonia.Vector value, Span<double> destination)
    {
        destination[0] = value.X;
        destination[1] = value.Y;
    }

    public Avalonia.Vector From(ReadOnlySpan<double> vector) =>
        new(vector[0], vector[1]);
}

public sealed class SizeConverter : ITwoWayConverter<Avalonia.Size>
{
    public int Dimension => 2;

    public void To(Avalonia.Size value, Span<double> destination)
    {
        destination[0] = value.Width;
        destination[1] = value.Height;
    }

    public Avalonia.Size From(ReadOnlySpan<double> vector) =>
        new(vector[0], vector[1]);
}

/// <summary>
/// Converts Avalonia rectangles to Compose's [left, top, right, bottom] animation vectors.
/// The public Rect still stores X/Y/Width/Height; arc specs animate its two corners, and
/// vector velocities, bounds and spring thresholds apply to its four edges.
/// Use new Rect(left, top, right - left, bottom - top) when specifying edge values.
/// </summary>
public sealed class RectConverter : ITwoWayConverter<Avalonia.Rect>
{
    public int Dimension => 4;

    public void To(Avalonia.Rect value, Span<double> destination)
    {
        destination[0] = value.Left;
        destination[1] = value.Top;
        destination[2] = value.Right;
        destination[3] = value.Bottom;
    }

    public Avalonia.Rect From(ReadOnlySpan<double> vector) =>
        new(vector[0], vector[1], vector[2] - vector[0], vector[3] - vector[1]);
}

public sealed class ThicknessConverter : ITwoWayConverter<Avalonia.Thickness>
{
    public int Dimension => 4;

    public void To(Avalonia.Thickness value, Span<double> destination)
    {
        destination[0] = value.Left;
        destination[1] = value.Top;
        destination[2] = value.Right;
        destination[3] = value.Bottom;
    }

    public Avalonia.Thickness From(ReadOnlySpan<double> vector) =>
        new(vector[0], vector[1], vector[2], vector[3]);
}

public sealed class CornerRadiusConverter : ITwoWayConverter<Avalonia.CornerRadius>
{
    public int Dimension => 4;

    public void To(Avalonia.CornerRadius value, Span<double> destination)
    {
        destination[0] = value.TopLeft;
        destination[1] = value.TopRight;
        destination[2] = value.BottomRight;
        destination[3] = value.BottomLeft;
    }

    public Avalonia.CornerRadius From(ReadOnlySpan<double> vector) =>
        new(vector[0], vector[1], vector[2], vector[3]);
}

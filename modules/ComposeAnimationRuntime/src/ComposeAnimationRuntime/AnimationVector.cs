namespace ComposeAnimationRuntime;

/// <summary>Mutable numeric vector used by the animation engine.</summary>
public abstract class AnimationVector
{
    public abstract int Dimension { get; }
    public abstract float this[int index] { get; set; }
    public abstract AnimationVector Copy();
    public abstract void Reset();
}

public sealed class AnimationVector1D : AnimationVector
{
    public AnimationVector1D(float value = 0) => Value = value;
    public float Value { get; set; }
    public override int Dimension => 1;

    public override float this[int index]
    {
        get => index == 0 ? Value : throw new IndexOutOfRangeException();
        set
        {
            if (index != 0) throw new IndexOutOfRangeException();
            Value = value;
        }
    }

    public override AnimationVector Copy() => new AnimationVector1D(Value);
    public override void Reset() => Value = 0;
}

public sealed class AnimationVector2D : AnimationVector
{
    public AnimationVector2D(float x = 0, float y = 0)
    {
        X = x;
        Y = y;
    }

    public float X { get; set; }
    public float Y { get; set; }
    public override int Dimension => 2;

    public override float this[int index]
    {
        get => index switch { 0 => X, 1 => Y, _ => throw new IndexOutOfRangeException() };
        set
        {
            if (index == 0) X = value;
            else if (index == 1) Y = value;
            else throw new IndexOutOfRangeException();
        }
    }

    public override AnimationVector Copy() => new AnimationVector2D(X, Y);

    public override void Reset()
    {
        X = 0;
        Y = 0;
    }
}

public sealed class AnimationVector3D : AnimationVector
{
    public AnimationVector3D(float x = 0, float y = 0, float z = 0) { X = x; Y = y; Z = z; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public override int Dimension => 3;
    public override float this[int index] { get => index switch { 0 => X, 1 => Y, 2 => Z, _ => throw new IndexOutOfRangeException() }; set { if (index == 0) X = value; else if (index == 1) Y = value; else if (index == 2) Z = value; else throw new IndexOutOfRangeException(); } }
    public override AnimationVector Copy() => new AnimationVector3D(X, Y, Z);
    public override void Reset() { X = 0; Y = 0; Z = 0; }
}

public sealed class AnimationVector4D : AnimationVector
{
    public AnimationVector4D(float x = 0, float y = 0, float z = 0, float w = 0) { X = x; Y = y; Z = z; W = w; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float W { get; set; }
    public override int Dimension => 4;
    public override float this[int index] { get => index switch { 0 => X, 1 => Y, 2 => Z, 3 => W, _ => throw new IndexOutOfRangeException() }; set { if (index == 0) X = value; else if (index == 1) Y = value; else if (index == 2) Z = value; else if (index == 3) W = value; else throw new IndexOutOfRangeException(); } }
    public override AnimationVector Copy() => new AnimationVector4D(X, Y, Z, W);
    public override void Reset() { X = 0; Y = 0; Z = 0; W = 0; }
}

public sealed class AnimationVectorN : AnimationVector
{
    private readonly float[] _values;

    public AnimationVectorN(int dimension)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(dimension, 0);
        _values = new float[dimension];
    }

    public AnimationVectorN(IEnumerable<float> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        _values = values.ToArray();
        if (_values.Length == 0) throw new ArgumentException("At least one component is required.", nameof(values));
    }

    public override int Dimension => _values.Length;
    public override float this[int index] { get => _values[index]; set => _values[index] = value; }
    public override AnimationVector Copy() => new AnimationVectorN(_values);
    public override void Reset() => Array.Clear(_values);
}

public static class VectorConverter
{
    public static IAnimationVectorConverter<T> Create<T>(Func<T, AnimationVector> toVector,
        Func<AnimationVector, T> fromVector, T visibilityThreshold)
    {
        ArgumentNullException.ThrowIfNull(toVector);
        ArgumentNullException.ThrowIfNull(fromVector);
        return new DelegateVectorConverter<T>(toVector, fromVector, visibilityThreshold);
    }

    private sealed class DelegateVectorConverter<T>(Func<T, AnimationVector> toVector,
        Func<AnimationVector, T> fromVector, T visibilityThreshold) : IAnimationVectorConverter<T>
    {
        public T VisibilityThreshold { get; } = visibilityThreshold;
        public AnimationVector ConvertToVector(T value) => toVector(value);
        public T ConvertFromVector(AnimationVector vector) => fromVector(vector);
    }
}

public interface IAnimationVectorConverter<T>
{
    AnimationVector ConvertToVector(T value);
    T ConvertFromVector(AnimationVector vector);
    T VisibilityThreshold { get; }
}

public sealed class ScalarVectorConverter : IAnimationVectorConverter<float>
{
    public ScalarVectorConverter(float visibilityThreshold = 0.01f) => VisibilityThreshold = visibilityThreshold;
    public float VisibilityThreshold { get; }
    public AnimationVector ConvertToVector(float value) => new AnimationVector1D(value);
    public float ConvertFromVector(AnimationVector vector) => vector[0];
}

public sealed class DoubleVectorConverter : IAnimationVectorConverter<double>
{
    public DoubleVectorConverter(float visibilityThreshold = 0.001f) => VisibilityThreshold = visibilityThreshold;
    public double VisibilityThreshold { get; }
    public AnimationVector ConvertToVector(double value) => new AnimationVector1D((float)value);
    public double ConvertFromVector(AnimationVector vector) => vector[0];
}

public static class Easings
{
    public static Func<double, double> Linear { get; } = value => value;
    public static Func<double, double> FastOutSlowIn { get; } = value => value * value * (3 - 2 * value);
    public static Func<double, double> EaseIn { get; } = value => value * value;
    public static Func<double, double> EaseOut { get; } = value => 1 - (1 - value) * (1 - value);
}

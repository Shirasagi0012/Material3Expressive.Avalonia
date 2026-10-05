namespace ComposeAnimation.Runtime;


public class AnimationVector
{
    private readonly double[] _components;

    public AnimationVector(int dimension)
    {
        if (dimension <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(dimension),
                "Dimension must be greater than zero.");
        }

        _components = new double[dimension];
    }

    public int Dimension => _components.Length;

    public double this[int index]
    {
        get => _components[index];
        set => _components[index] = value;
    }

    public Span<double> AsSpan() => _components;

    public ReadOnlySpan<double> AsReadOnlySpan() => _components;

    public void Clear()
    {
        AsSpan().Clear();
    }   
    
    public void CopyFrom(ReadOnlySpan<double> source)
    {
        if (source.Length != Dimension)
        {
            throw new ArgumentException(
                "Source dimension must match the vector dimension.",
                nameof(source));
        }

        source.CopyTo(AsSpan());
    }

    public AnimationVector Clone()
    {
        return From(AsReadOnlySpan());
    }

    public AnimationVector CreateZero()
    {
        return new AnimationVector(Dimension);
    }
    
    public static AnimationVector From(
        ReadOnlySpan<double> components)
    {
        var result = new AnimationVector(components.Length);
        result.CopyFrom(components);
        return result;
    }
}

internal static class VectorValidation
{
    /// <summary>Validates a pair of same-dimension input vectors and an optional destination.</summary>
    public static void Validate(
        ReadOnlySpan<double> first,
        ReadOnlySpan<double> second,
        int destinationLength = 0)
    {
        if (first.Length == 0 || second.Length != first.Length)
            throw new ArgumentException("All input vectors must have the same positive dimension.");

        if (destinationLength != 0 && destinationLength != first.Length)
            throw new ArgumentException("Destination dimension must match the input vectors.");
    }

    /// <summary>Validates initial/target/velocity input vectors and an optional destination.</summary>
    public static void Validate(
        ReadOnlySpan<double> initial,
        ReadOnlySpan<double> target,
        ReadOnlySpan<double> initialVelocity,
        int destinationLength = 0)
    {
        if (initial.Length == 0 ||
            target.Length != initial.Length ||
            initialVelocity.Length != initial.Length)
            throw new ArgumentException("All input vectors must have the same positive dimension.");

        if (destinationLength != 0 && destinationLength != initial.Length)
            throw new ArgumentException("Destination dimension must match the input vectors.");
    }
}

using System;
using Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// The size of one corner, resolved against the shape it belongs to rather than baked into a
/// number up front.
/// </summary>
/// <remarks>
/// Deferring resolution is what makes <c>CornerFull</c> (50%) interpolate correctly against an
/// absolute size such as 8dp: the interpolation happens in resolved units, at the size the shape
/// is actually drawn at. Compose does the same thing by interpolating inside
/// <c>CornerSize.toPx</c> (<c>foundation/shape/RoundedCornerShape.kt:121</c>); Phase 3's press
/// morph depends on it.
/// </remarks>
public abstract record CornerSize
{
    /// <summary>The corner radius in device-independent pixels for a shape of this size.</summary>
    public abstract double Resolve(Size shapeSize);

    public static CornerSize Zero { get; } = new AbsoluteCornerSize(0);

    /// <summary>Half of the shape's smaller side — a fully rounded (pill or circle) corner.</summary>
    public static CornerSize Full { get; } = new PercentCornerSize(50);

    public static CornerSize Absolute(double radius)
    {
        return radius == 0 ? Zero : new AbsoluteCornerSize(radius);
    }

    public static CornerSize Percent(double percent)
    {
        return new PercentCornerSize(percent);
    }

    /// <summary>
    /// A corner that interpolates between two others when it is resolved. Boundary fractions
    /// collapse to the operand itself so that repeated retargeting cannot nest indefinitely.
    /// </summary>
    public static CornerSize Lerp(CornerSize start, CornerSize stop, double fraction)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(stop);

        return fraction switch
        {
            <= 0 => start,
            >= 1 => stop,
            _ when start == stop => start,
            _ => new LerpCornerSize(start, stop, fraction)
        };
    }

    public static implicit operator CornerSize(double radius)
    {
        return Absolute(radius);
    }
}

public sealed record AbsoluteCornerSize(double Radius) : CornerSize
{
    public override double Resolve(Size shapeSize)
    {
        return Radius;
    }
}

public sealed record PercentCornerSize(double Percentage) : CornerSize
{
    public override double Resolve(Size shapeSize)
    {
        return Math.Min(shapeSize.Width, shapeSize.Height) * (Percentage / 100d);
    }
}

public sealed record LerpCornerSize(CornerSize Start, CornerSize Stop, double Fraction) : CornerSize
{
    public override double Resolve(Size shapeSize)
    {
        var start = Start.Resolve(shapeSize);
        return start + (Stop.Resolve(shapeSize) - start) * Fraction;
    }
}

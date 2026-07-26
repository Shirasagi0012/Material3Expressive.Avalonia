using System;
using Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// Base for every shape a token can name. Phase 1 ships only the corner-based family; the polygon
/// family (the 35-shape catalog behind LoadingIndicator and wavy progress) joins it in Phase 3.
/// </summary>
public abstract record MaterialShape;

/// <summary>
/// A rectangle with four independently sized corners. Corners are named by writing direction, as
/// in the Material spec, and are mapped onto Avalonia's physical corners when resolved.
/// </summary>
public sealed record RoundedCornerShape(
    CornerSize TopStart,
    CornerSize TopEnd,
    CornerSize BottomEnd,
    CornerSize BottomStart
) : MaterialShape
{
    /// <summary>A square-cornered rectangle.</summary>
    public static RoundedCornerShape Rectangle { get; } = Uniform(CornerSize.Zero);

    /// <summary>Fully rounded: a circle when square, a pill otherwise.</summary>
    public static RoundedCornerShape Circle { get; } = Uniform(CornerSize.Full);

    public static RoundedCornerShape Uniform(CornerSize size)
    {
        return new RoundedCornerShape(size, size, size, size);
    }

    public static RoundedCornerShape Uniform(double radius)
    {
        return Uniform(CornerSize.Absolute(radius));
    }

    /// <summary>This shape with its bottom corners squared off.</summary>
    public RoundedCornerShape Top()
    {
        return this with { BottomEnd = CornerSize.Zero, BottomStart = CornerSize.Zero };
    }

    /// <summary>This shape with its top corners squared off.</summary>
    public RoundedCornerShape Bottom()
    {
        return this with { TopStart = CornerSize.Zero, TopEnd = CornerSize.Zero };
    }

    /// <summary>This shape with its start-side corners squared off.</summary>
    public RoundedCornerShape End()
    {
        return this with { TopStart = CornerSize.Zero, BottomStart = CornerSize.Zero };
    }

    /// <summary>This shape with its end-side corners squared off.</summary>
    public RoundedCornerShape Start()
    {
        return this with { TopEnd = CornerSize.Zero, BottomEnd = CornerSize.Zero };
    }

    /// <summary>
    /// Resolves to Avalonia's physical corner radii. Start and end swap under right-to-left flow.
    /// </summary>
    public CornerRadius ToCornerRadius(Size shapeSize, bool isRightToLeft = false)
    {
        var topStart = TopStart.Resolve(shapeSize);
        var topEnd = TopEnd.Resolve(shapeSize);
        var bottomEnd = BottomEnd.Resolve(shapeSize);
        var bottomStart = BottomStart.Resolve(shapeSize);

        return isRightToLeft
            ? new CornerRadius(topEnd, topStart, bottomStart, bottomEnd)
            : new CornerRadius(topStart, topEnd, bottomEnd, bottomStart);
    }

    /// <summary>
    /// Interpolates corner-wise, deferring each corner's interpolation to resolution time. Returns
    /// one of the operands at the boundaries so that a shape animated back and forth cannot build
    /// up a chain of nested interpolations.
    /// </summary>
    public static RoundedCornerShape Lerp(RoundedCornerShape start, RoundedCornerShape stop, double fraction)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(stop);

        return fraction switch
        {
            <= 0 => start,
            >= 1 => stop,
            _ => new RoundedCornerShape(
                CornerSize.Lerp(start.TopStart, stop.TopStart, fraction),
                CornerSize.Lerp(start.TopEnd, stop.TopEnd, fraction),
                CornerSize.Lerp(start.BottomEnd, stop.BottomEnd, fraction),
                CornerSize.Lerp(start.BottomStart, stop.BottomStart, fraction))
        };
    }
}

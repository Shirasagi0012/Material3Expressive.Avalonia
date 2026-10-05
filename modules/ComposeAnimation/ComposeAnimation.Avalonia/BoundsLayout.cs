using Avalonia;

namespace ComposeAnimation;

/// <summary>Experimental single-child intermediate layout between the two bounds animations.
/// Size is reported to the outer boundary; ChildOffset is the default local child placement.</summary>
public readonly record struct BoundsLayoutResult(Size Size, Point ChildOffset)
{
    /// <summary>Optional synchronous placement policy replacing ChildOffset. Called independently
    /// in target and actual placement, potentially many times for one measurement. Capture only
    /// immutable measurement data; do not measure/arrange controls or read live Bounds here.</summary>
    public Func<BoundsPlacementContext, Point>? Placement { get; init; }

    internal Point Place(BoundsPlacementContext context)
    {
        var offset = Placement?.Invoke(context) ?? ChildOffset;
        if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
            throw new InvalidOperationException("BoundsLayout placement must return a finite child offset.");
        return offset;
    }
}

/// <summary>One intermediate layer's placement inputs in physical DIP. Size and ChildSize are
/// its cached measured sizes (not the outer animated host size). Origin is this layer's current
/// root-relative origin; outside a LookaheadRoot it is relative to the AnimatedBounds host.
/// Target origins exclude motion frames; actual origins include them. IsLookingAhead identifies
/// the independent target pass. Returning an offset does not change either measured size.</summary>
public readonly record struct BoundsPlacementContext(Size Size, Size ChildSize, Point Origin, bool IsLookingAhead);

/// <summary>
/// Experimental counterpart of animateBounds' intermediate layout modifier. Implement a pure,
/// synchronous policy that measures its child exactly once and returns a finite size within the
/// incoming constraints and a finite child offset or placement policy. Measurement and placement
/// each run for target and actual layout. Do not retain the measurement callback or read/write live layout. Instances should
/// be immutable; replace AnimatedBounds.IntermediateLayout when inputs change.
/// </summary>
public abstract class BoundsLayout
{
    public abstract BoundsLayoutResult Measure(LookaheadConstraints constraints,
        Func<LookaheadConstraints, Size> measureChild);

    public static BoundsLayout Empty { get; } = new IdentityLayout();

    /// <summary>Compose outer-to-inner. a.Then(b) measures a around b.</summary>
    public BoundsLayout Then(BoundsLayout inner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        return new CombinedLayout(this, inner);
    }

    public static BoundsLayout FixedSize(Size size) => new SizeLayout(LookaheadConstraints.Fixed(size));
    public static BoundsLayout Padding(Thickness padding)
    {
        if (!double.IsFinite(padding.Left) || !double.IsFinite(padding.Top) ||
            !double.IsFinite(padding.Right) || !double.IsFinite(padding.Bottom) ||
            padding.Left < 0 || padding.Top < 0 || padding.Right < 0 || padding.Bottom < 0)
            throw new ArgumentOutOfRangeException(nameof(padding));
        return new PaddingLayout(padding);
    }
    /// <summary>Physical DIP offset; does not change reported size or perform RTL mirroring.</summary>
    public static BoundsLayout Offset(Vector offset)
    {
        if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
            throw new ArgumentOutOfRangeException(nameof(offset));
        return new OffsetLayout(offset);
    }

    internal BoundsLayoutResult Evaluate(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
    {
        var calls = 0;
        var active = true;
        BoundsLayoutResult result;
        try
        {
            result = Measure(constraints, childConstraints =>
            {
                if (!active || ++calls != 1)
                    throw new InvalidOperationException("BoundsLayout must measure its child exactly once, synchronously.");
                return measureChild(childConstraints);
            });
        }
        finally { active = false; }
        if (calls != 1 || !double.IsFinite(result.Size.Width) || !double.IsFinite(result.Size.Height) ||
            result.Size.Width < 0 || result.Size.Height < 0 || constraints.Constrain(result.Size) != result.Size ||
            !double.IsFinite(result.ChildOffset.X) || !double.IsFinite(result.ChildOffset.Y))
            throw new InvalidOperationException("BoundsLayout must measure once and return a finite constrained size and finite child offset.");
        return result;
    }

    private sealed class IdentityLayout : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
            => new(constraints.Constrain(measureChild(constraints)), default);
    }
    private sealed class SizeLayout(LookaheadConstraints preferred) : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
        {
            var childConstraints = constraints.Constrain(preferred);
            return new(childConstraints.Constrain(measureChild(childConstraints)), default);
        }
    }
    private sealed class PaddingLayout(Thickness padding) : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
            => new(constraints.Constrain(measureChild(constraints.Deflate(padding)).Inflate(padding)),
                new Point(padding.Left, padding.Top));
    }
    private sealed class OffsetLayout(Vector offset) : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
            => new(constraints.Constrain(measureChild(constraints)), new Point(offset.X, offset.Y));
    }
    private sealed class CombinedLayout(BoundsLayout outer, BoundsLayout inner) : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
        {
            BoundsLayoutResult inside = default;
            var outside = outer.Evaluate(constraints, next =>
            {
                inside = inner.Evaluate(next, measureChild);
                return inside.Size;
            });
            var result = new BoundsLayoutResult(outside.Size,
                outside.ChildOffset + new Vector(inside.ChildOffset.X, inside.ChildOffset.Y));
            if (outside.Placement is null && inside.Placement is null) return result;
            // Preserve every layer's measured size and origin rather than flattening dynamic
            // placement into a measurement-time offset. No measurement callback is retained.
            return result with
            {
                Placement = context =>
                {
                    var outerOffset = outside.Place(context with { Size = outside.Size, ChildSize = inside.Size });
                    var innerOffset = inside.Place(context with
                    {
                        Size = inside.Size,
                        Origin = context.Origin + new Vector(outerOffset.X, outerOffset.Y)
                    });
                    return outerOffset + new Vector(innerOffset.X, innerOffset.Y);
                }
            };
        }
    }
}

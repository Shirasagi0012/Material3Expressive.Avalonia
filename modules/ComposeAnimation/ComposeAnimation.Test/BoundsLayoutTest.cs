using Avalonia;

namespace ComposeAnimation.Test;

public class BoundsLayoutTest
{
    [Fact]
    public void CompositionOrder_ChangesChildConstraintsAndReportedSize()
    {
        var constraints = LookaheadConstraints.AtMost(new Size(400, 300));
        LookaheadConstraints seen = default;
        Size Measure(LookaheadConstraints incoming) { seen = incoming; return incoming.Constrain(new Size(30, 20)); }
        var sizeThenPadding = BoundsLayout.FixedSize(new Size(120, 100)).Then(BoundsLayout.Padding(new Thickness(10)));
        Assert.Equal(new BoundsLayoutResult(new Size(120, 100), new Point(10, 10)), sizeThenPadding.Evaluate(constraints, Measure));
        Assert.Equal(LookaheadConstraints.Fixed(new Size(100, 80)), seen);
        var paddingThenSize = BoundsLayout.Padding(new Thickness(10)).Then(BoundsLayout.FixedSize(new Size(120, 100)));
        Assert.Equal(new BoundsLayoutResult(new Size(140, 120), new Point(10, 10)), paddingThenSize.Evaluate(constraints, Measure));
        Assert.Equal(LookaheadConstraints.Fixed(new Size(120, 100)), seen);
    }

    [Fact]
    public void TightIncomingRange_WinsIntermediateSizePreference()
    {
        var policy = BoundsLayout.FixedSize(new Size(300, 200)).Then(BoundsLayout.Padding(new Thickness(10)));
        var result = policy.Evaluate(LookaheadConstraints.Fixed(new Size(120, 100)), constraints =>
        {
            Assert.Equal(LookaheadConstraints.Fixed(new Size(100, 80)), constraints);
            return constraints.MinSize;
        });
        Assert.Equal(new BoundsLayoutResult(new Size(120, 100), new Point(10, 10)), result);
    }

    private sealed class CallbackLayout(Func<LookaheadConstraints, Func<LookaheadConstraints, Size>, BoundsLayoutResult> measure) : BoundsLayout
    {
        public override BoundsLayoutResult Measure(LookaheadConstraints constraints, Func<LookaheadConstraints, Size> measureChild)
            => measure(constraints, measureChild);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void InvalidPolicies_RejectMissingRepeatedOrInvalidMeasurements(int mode)
    {
        var calls = 0;
        var policy = new CallbackLayout((constraints, child) =>
        {
            var size = mode == 0 ? default : child(constraints);
            if (mode == 2) child(constraints);
            if (mode == 3) size = new Size(double.NaN, 10);
            if (mode == 4) size = new Size(1000, 1000);
            return new(size, mode == 5 ? new Point(double.PositiveInfinity, 0) : default);
        });
        Assert.Throws<InvalidOperationException>(() => policy.Evaluate(LookaheadConstraints.AtMost(new Size(100, 100)),
            _ => { calls++; return new Size(30, 20); }));
        Assert.Equal(mode == 0 ? 0 : 1, calls); // A second real measurement is never performed.
    }

    [Fact]
    public void RetainedMeasureCallback_CannotBeUsedAfterPolicyReturns()
    {
        Func<LookaheadConstraints, Size>? retained = null;
        var policy = new CallbackLayout((constraints, child) =>
        {
            retained = child;
            return new(child(constraints), default);
        });
        policy.Evaluate(LookaheadConstraints.Unbounded, _ => new Size(30, 20));
        Assert.Throws<InvalidOperationException>(() => retained!(LookaheadConstraints.Unbounded));
    }

    [Fact]
    public void InvalidFactoryInputs_AreRejectedBeforeLayout()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BoundsLayout.FixedSize(new Size(double.PositiveInfinity, 20)));
        Assert.Throws<ArgumentOutOfRangeException>(() => BoundsLayout.Padding(new Thickness(-1)));
        Assert.Throws<ArgumentOutOfRangeException>(() => BoundsLayout.Offset(new Vector(double.NaN, 0)));
        Assert.Throws<ArgumentNullException>(() => BoundsLayout.Empty.Then(null!));
    }

    [Fact]
    public void PlacementComposition_PreservesLayerSizesOrderAndCurrentOriginsWithoutMeasuringAgain()
    {
        var measures = 0;
        var contexts = new List<(string Layer, BoundsPlacementContext Context)>();
        var outer = new CallbackLayout((constraints, child) =>
        {
            measures++;
            var size = child(constraints.Deflate(new Thickness(10)));
            return new(size.Inflate(new Thickness(10)), default)
            {
                Placement = context => { contexts.Add(("outer", context)); return new Point(10, 10); }
            };
        });
        var inner = new CallbackLayout((constraints, child) =>
        {
            measures++;
            child(constraints);
            return new(new Size(100, 80), default)
            {
                Placement = context =>
                {
                    contexts.Add(("inner", context));
                    return new Point(context.Size.Width - context.ChildSize.Width, 5);
                }
            };
        });
        var result = outer.Then(inner).Evaluate(LookaheadConstraints.AtMost(new Size(400, 300)), _ => new Size(30, 20));
        Assert.Empty(contexts); // Measurement must not invoke placement.
        var target = new BoundsPlacementContext(result.Size, new Size(30, 20), new Point(20, 40), true);
        Assert.Equal(new Point(80, 15), result.Place(target));
        Assert.Equal(("outer", target with { ChildSize = new Size(100, 80) }), contexts[0]);
        Assert.Equal(("inner", target with { Size = new Size(100, 80), Origin = new Point(30, 50) }), contexts[1]);
        contexts.Clear();
        result.Place(target with { Origin = new Point(70, 90), IsLookingAhead = false });
        Assert.Equal(new Point(80, 100), contexts[1].Context.Origin);
        Assert.False(contexts[1].Context.IsLookingAhead);
        Assert.Equal(2, measures);
    }

    [Fact]
    public void DynamicPlacement_RejectsNonFiniteIntermediateOffsetEvenIfNextLayerWouldIgnoreIt()
    {
        var invalid = new CallbackLayout((constraints, child) => new(child(constraints), default)
        {
            Placement = _ => new Point(double.NaN, 0)
        });
        var result = invalid.Then(BoundsLayout.Offset(new Vector(10, 20)))
            .Evaluate(LookaheadConstraints.Unbounded, _ => new Size(30, 20));
        Assert.Throws<InvalidOperationException>(() => result.Place(new(result.Size, new Size(30, 20), default, true)));
    }
}

using Avalonia;
using ComposeAnimation;

namespace ComposeAnimation.Test;

public class LookaheadConstraintsTest
{
    [Fact]
    public void DefaultsAndFactories_DistinguishUnboundedAndFixedZero()
    {
        Assert.Equal(LookaheadConstraints.Unbounded, new LookaheadConstraints());
        Assert.Equal(LookaheadConstraints.Fixed(default), default(LookaheadConstraints));
        Assert.Equal(new Size(30, 40), LookaheadConstraints.Fixed(new Size(30, 40)).MinSize);
        Assert.False(LookaheadConstraints.AtMost(new Size(30, 40)).HasMinimum);
        Assert.True(LookaheadConstraints.Fixed(new Size(30, 40)).HasFixedWidth);
    }

    [Theory]
    [InlineData(-1, 100, 0, 100)]
    [InlineData(double.NaN, 100, 0, 100)]
    [InlineData(double.PositiveInfinity, double.PositiveInfinity, 0, 100)]
    [InlineData(101, 100, 0, 100)]
    [InlineData(0, double.NaN, 0, 100)]
    [InlineData(0, -1, 0, 100)]
    [InlineData(0, 100, -1, 100)]
    [InlineData(0, 100, 0, double.NegativeInfinity)]
    public void RejectsInvalidRanges(double minW, double maxW, double minH, double maxH)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new LookaheadConstraints(minW, maxW, minH, maxH));

    [Fact]
    public void DeflateAndOffset_PreserveUnboundedAxesAndClampAtZero()
    {
        var constraints = new LookaheadConstraints(100, 300, 80, double.PositiveInfinity);
        Assert.Equal(new LookaheadConstraints(70, 270, 10, double.PositiveInfinity),
            constraints.Deflate(new Thickness(10, 30, 20, 40)));
        Assert.Equal(new LookaheadConstraints(0, 0, 0, double.PositiveInfinity), constraints.Offset(-400, -400));
        Assert.Equal(new LookaheadConstraints(120, 320, 100, double.PositiveInfinity), constraints.Offset(20, 20));
        Assert.Throws<ArgumentOutOfRangeException>(() => constraints.Offset(double.NaN, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LookaheadConstraints.Fixed(new Size(double.MaxValue, 10)).Offset(double.MaxValue, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => LookaheadConstraints.Fixed(new Size(double.PositiveInfinity, 10)));
    }

    [Fact]
    public void Constrain_FitsPreferencesInsideIncomingRanges()
    {
        var incoming = new LookaheadConstraints(100, 200, 50, 150);
        Assert.Equal(new Size(100, 150), incoming.Constrain(new Size(20, 300)));
        Assert.Equal(new LookaheadConstraints(200, 200, 50, 150),
            incoming.Constrain(new LookaheadConstraints(300, 400, 10, 500)));
        Assert.Equal(new LookaheadConstraints(0, 200, 0, 150), incoming.Loosen());
        Assert.Throws<ArgumentException>(() => incoming.Constrain(new Size(double.NaN, 0)));
    }
}

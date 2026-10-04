using Avalonia;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class TooltipPositionTests
{
    [Fact]
    public void TokenSource_PreservesFixedPlainTooltipTokens() =>
        TokenScan.AssertComplete(new PlainTooltipTokenSource(), typeof(Comp.PlainTooltip));

    // Explicit expected coordinates cover each branch of the pinned provider; these
    // are not calculated with a second copy of the production algorithm.
    [Theory]
    [InlineData(0, false, 88, 76)]
    [InlineData(1, false, 88, 124)]
    [InlineData(2, false, 52, 100)]
    [InlineData(3, false, 124, 100)]
    [InlineData(4, false, 52, 100)]
    [InlineData(4, true, 124, 100)]
    [InlineData(5, false, 124, 100)]
    [InlineData(5, true, 52, 100)]
    public void PreferredSide_AndRelativeSides(int side, bool rtl, int x, int y) =>
        Assert.Equal(new PixelPoint(x, y), TooltipPosition.Calculate(new(100, 100, 20, 20),
            new(300, 240), new(44, 20), (TooltipSide)side, 4, rtl));

    [Theory]
    [InlineData(0, 100, 0, 88, 24)]
    [InlineData(1, 100, 220, 88, 196)]
    [InlineData(2, 0, 100, 24, 100)]
    [InlineData(3, 280, 100, 232, 100)]
    [InlineData(0, 0, 100, 0, 76)]
    [InlineData(0, 280, 100, 256, 76)]
    [InlineData(2, 100, 0, 52, 0)]
    [InlineData(2, 100, 230, 52, 220)]
    public void WindowEdges_FlipPreferredSideThenClamp(int side, int left, int top, int x, int y) =>
        Assert.Equal(new PixelPoint(x, y), TooltipPosition.Calculate(new(left, top, 20, 20),
            new(300, 240), new(44, 20), (TooltipSide)side, 4, false));

    [Fact]
    public void OversizedPopup_AndZeroViewport_ClampToOrigin()
    {
        Assert.Equal(default, TooltipPosition.Calculate(new(10, 10, 20, 20), new(40, 40),
            new(80, 80), TooltipSide.Above, 4, false));
        Assert.Equal(default, TooltipPosition.Calculate(new(0, 0, 0, 0), default,
            new(40, 24), TooltipSide.Right, 4, false));
    }

    [Fact]
    public void OddPixelExtent_UsesKotlinIntegerDivision() =>
        Assert.Equal(new PixelPoint(96, 72), TooltipPosition.Calculate(new(100, 100, 20, 20),
            new(300, 240), new(29, 24), TooltipSide.Above, 4, false));
}

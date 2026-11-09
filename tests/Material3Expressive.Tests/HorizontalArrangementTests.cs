using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Xunit;

namespace Material3Expressive.Tests;

public class HorizontalArrangementTests
{
    [Theory]
    [InlineData(false, HorizontalAlignment.Left, 0, 17, 44)]
    [InlineData(false, HorizontalAlignment.Center, 13, 30, 57)]
    [InlineData(false, HorizontalAlignment.Right, 26, 43, 70)]
    [InlineData(true, HorizontalAlignment.Left, 90, 63, 26)]
    [InlineData(true, HorizontalAlignment.Center, 77, 50, 13)]
    [InlineData(true, HorizontalAlignment.Right, 64, 37, 0)]
    public void SpacedGroupAlignmentIsLogical(bool rtl, HorizontalAlignment alignment, int first, int second, int third)
    {
        var positions = new int[3];
        MaterialHorizontalArrangement.SpacedBy(7, alignment).Arrange(1, 100, [10, 20, 30],
            rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, positions);
        Assert.Equal(new[] { first, second, third }, positions);
    }

    [Theory]
    [InlineData(false, 0, 6, 6)]
    [InlineData(true, 16, 0, 0)]
    public void ConstrainedGapsClampEachChildInInputOrder(bool rtl, int first, int second, int third)
    {
        var positions = new int[3];
        MaterialHorizontalArrangement.SpacedBy(12).Arrange(1, 26, [10, 20, 20],
            rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, positions);
        Assert.Equal(new[] { first, second, third }, positions);
    }

    [Fact]
    public void NegativeSpacingOverlapsAndUsesPhysicalDensityRounding()
    {
        var positions = new int[3];
        MaterialHorizontalArrangement.SpacedBy(-2.25).Arrange(2, 100, [20, 30, 40], FlowDirection.LeftToRight, positions);
        // -4.5 rounds toward positive infinity to -4, exactly like Dp.roundToPx.
        Assert.Equal(new[] { 0, 16, 42 }, positions);
    }

    [Fact]
    public void AbsoluteOrderDoesNotMakeGroupAlignmentAbsolute()
    {
        var positions = new int[2];
        MaterialHorizontalArrangement.Absolute.SpacedBy(10).Arrange(1, 100, [10, 20], FlowDirection.RightToLeft, positions);
        Assert.Equal(new[] { 60, 80 }, positions);
        MaterialHorizontalArrangement.Absolute.Left.Arrange(1, 100, [10, 20], FlowDirection.RightToLeft, positions);
        Assert.Equal(new[] { 0, 10 }, positions);
    }

    [Theory]
    [InlineData(false, 0)] [InlineData(true, 70)]
    public void SpaceBetweenWithOneChildUsesLogicalStart(bool rtl, int expected)
    {
        var positions = new int[1];
        MaterialHorizontalArrangement.SpaceBetween.Arrange(1, 100, [30],
            rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, positions);
        Assert.Equal(expected, positions[0]);
    }

    [Fact]
    public void EmptyChildrenAreValidAndDoNotWriteOutput()
    {
        var positions = new[] { 42 };
        foreach (var arrangement in new[] { MaterialHorizontalArrangement.Start, MaterialHorizontalArrangement.SpaceBetween,
                     MaterialHorizontalArrangement.SpaceAround, MaterialHorizontalArrangement.SpaceEvenly,
                     MaterialHorizontalArrangement.SpacedBy(8) })
            arrangement.Arrange(1, 100, [], FlowDirection.RightToLeft, positions);
        Assert.Equal(42, positions[0]);
    }
}

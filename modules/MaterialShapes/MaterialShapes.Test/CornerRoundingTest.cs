namespace MaterialShapes.Test;

public class CornerRoundingTest
{
    [Fact]
    public void CornerRounding_Test()
    {
        var defaultCorner = new CornerRounding();
        Assert.Equal(0D, defaultCorner.Radius);
        Assert.Equal(0D, defaultCorner.Smoothing);

        var unrounded = CornerRounding.Unrounded;
        Assert.Equal(0D, unrounded.Radius);
        Assert.Equal(0D, unrounded.Smoothing);

        var rounded = new CornerRounding(5D);
        Assert.Equal(5D, rounded.Radius);
        Assert.Equal(0D, rounded.Smoothing);

        var smoothed = new CornerRounding(smoothing: .5D);
        Assert.Equal(0D, smoothed.Radius);
        Assert.Equal(.5D, smoothed.Smoothing);

        var roundedAndSmoothed = new CornerRounding(5D, .5D);
        Assert.Equal(5D, roundedAndSmoothed.Radius);
        Assert.Equal(.5D, roundedAndSmoothed.Smoothing);
    }
}
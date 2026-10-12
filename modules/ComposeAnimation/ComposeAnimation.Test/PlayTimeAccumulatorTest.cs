using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class PlayTimeAccumulatorTest
{
    // 16ms frames at scale 3 do not divide evenly (53333.3 ticks per frame). Rounding each frame
    // would lose 7.5ms over an hour; the carried remainder keeps it exact.
    [Fact]
    public void OneHourAtScaleThree_IsExactlyTwentyMinutes()
    {
        var clock = new PlayTimeAccumulator();
        var frame = TimeSpan.FromMilliseconds(16);
        var now = TimeSpan.Zero;
        clock.Advance(now, 3);
        var frames = (int)(TimeSpan.FromHours(1).Ticks / frame.Ticks);
        for (var i = 0; i < frames; i++) clock.Advance(now += frame, 3);
        Assert.Equal(TimeSpan.FromHours(1), now);
        Assert.Equal(TimeSpan.FromMinutes(20).Ticks, clock.PlayTime.Ticks);
    }

    [Fact]
    public void ScaleChange_AppliesFromTheNextFrame_AndPauseDoesNotCountInactiveTime()
    {
        var clock = new PlayTimeAccumulator();
        clock.Advance(TimeSpan.Zero, 1);
        clock.Advance(TimeSpan.FromMilliseconds(100), 1);
        clock.Advance(TimeSpan.FromMilliseconds(200), 2);
        Assert.Equal(TimeSpan.FromMilliseconds(150), clock.PlayTime);
        clock.Pause();
        clock.Advance(TimeSpan.FromSeconds(10), 1);
        Assert.Equal(TimeSpan.FromMilliseconds(150), clock.PlayTime);
        clock.Advance(TimeSpan.FromSeconds(10) + TimeSpan.FromMilliseconds(50), 0);
        Assert.Equal(TimeSpan.FromMilliseconds(150), clock.PlayTime);
        Assert.True(clock.IsBeforeLast(TimeSpan.FromSeconds(1)));
        clock.Reset();
        Assert.Equal(TimeSpan.Zero, clock.PlayTime);
    }
}

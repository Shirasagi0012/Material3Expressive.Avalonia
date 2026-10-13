using Avalonia.Animation.Easings;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>A steady frame of a running transition allocates nothing.</summary>
public class FrameAllocationTest
{
    [Fact]
    public void TransitionFrame_Steady_AllocatesNothing()
    {
        var state = new MutableTransitionState<bool>(false);
        using var transition = new Transition<bool>(state);
        var value = transition.AnimateDouble(s => s ? 100 : 0,
            _ => new TweenSpec<double>(TimeSpan.FromHours(1), easing: new LinearEasing()));
        state.TargetState = true;
        var now = TimeSpan.Zero;
        for (var i = 0; i < 10; i++) transition.OnFrame(now += TimeSpan.FromMilliseconds(16));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 100; i++) transition.OnFrame(now += TimeSpan.FromMilliseconds(16));
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.True(value.Value > 0);
        Assert.Equal(0, allocated);
    }
}

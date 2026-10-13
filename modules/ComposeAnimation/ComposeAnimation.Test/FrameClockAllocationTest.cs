using Avalonia.Controls;
using Avalonia.Threading;
using ComposeAnimation.Runtime;
using ComposeAnimation.TestSupport;
using static ComposeAnimation.Test.Ui;

namespace ComposeAnimation.Test;

/// <summary>
/// Stage 5a allocation guard for the whole frame path (ManualFrameSource -> FrameClock hop ->
/// batch -> Animatable runs). Per running animation a steady frame allocates nothing; what is left
/// is a constant per frame: the manual source's own wait (TestSupport) plus the D4 dispatcher hop
/// (ContinueWith's continuation task and Avalonia's DispatcherOperation), which can only go with
/// an ordering change.
/// </summary>
public class FrameClockAllocationTest
{
    private const int Warm = 200, Frames = 200;
    // Source wait (~312 B) + hop (~356 B) measured at stage 5a, with headroom for runtime noise.
    private const double MaxBytesPerFrame = 1024;

    private static double SteadyBytesPerFrame(int animations)
    {
        var source = new ManualFrameSource();
        var window = new AnimationTestWindow(source) { Content = new Border() };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var spec = new SpringSpec<float>(dampingRatio: 1, stiffness: 0.5);
        var runs = new Animatable<float>[animations];
        for (var i = 0; i < animations; i++)
        {
            runs[i] = new Animatable<float>(new FloatConverter(), 0);
            _ = runs[i].AnimateToAsync(AnimationScope.Get((Border)window.Content!), 1_000_000f + i, spec);
        }
        var now = TimeSpan.Zero;
        for (var i = 0; i < Warm; i++) source.PumpTo(now += TimeSpan.FromMilliseconds(16));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Frames; i++) source.PumpTo(now += TimeSpan.FromMilliseconds(16));
        var bytes = (GC.GetAllocatedBytesForCurrentThread() - before) / (double)Frames;
        Assert.All(runs, r => Assert.True(r.IsRunning));
        window.Close();
        Dispatcher.UIThread.RunJobs();
        return bytes;
    }

    [Fact]
    public Task SteadyFrame_PerRunningAnimation_AllocatesNothing() => OnUi(() =>
    {
        SteadyBytesPerFrame(10);
        var few = SteadyBytesPerFrame(10);
        var many = SteadyBytesPerFrame(200);
        Assert.True(many - few < 8, $"190 extra animations cost {many - few:F1} B per frame (few={few:F1}, many={many:F1}).");
        Assert.True(many <= MaxBytesPerFrame, $"steady frame allocated {many:F1} B (limit {MaxBytesPerFrame}).");
    });
}

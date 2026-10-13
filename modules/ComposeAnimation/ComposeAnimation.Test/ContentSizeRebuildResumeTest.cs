using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>D17 for <see cref="AnimatedContentSize"/>: a rebuild resumes the in-flight size run.</summary>
public sealed class ContentSizeRebuildResumeTest
{
    private static (AnimatedContentSize Host, Border Leaf) Create(IFiniteAnimationSpec<Size> spec)
    {
        var leaf = new Border { Width = 50, Height = 40 };
        var host = new AnimatedContentSize
        {
            Content = leaf, AnimationSpec = spec,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        };
        return (host, leaf);
    }

    private static TweenSpec<Size> Linear(int ms) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());

    private static List<double> Run(bool move, IFiniteAnimationSpec<Size> spec, int frames, out AnimatedContentSize host, out int completions)
    {
        var (h, leaf) = Create(spec);
        host = h;
        var count = 0;
        h.AnimationCompleted += (_, _) => count++;
        var stage = new ReparentFixture(h);
        leaf.Height = 140;
        stage.Step(0);
        var samples = new List<double>();
        for (var i = 0; i < frames; i++)
        {
            if (move && i == 4) { stage.Move(); }
            stage.Step(16);
            samples.Add(h.Bounds.Height);
        }
        completions = count;
        stage.Dispose();
        return samples;
    }

    [Fact]
    public Task AnimatedContentSize_ReparentedMidFlight_ResumesAtTheDrainFrameAndReachesTheSameEnd() => OnUi(() =>
    {
        var moved = Run(move: true, Linear(160), 30, out _, out var completions);
        var still = Run(move: false, Linear(160), 30, out _, out _);
        var before = moved[3];
        Assert.InRange(before, 41, 139);
        // The resumed run continues the interrupted one on its timebase (Animatable's startTime
        // path): its first sample is the last pre-move timestamp, so the drain frame shows one
        // frame of the new run. A tween retarget restarts its duration (upstream semantics).
        Assert.Equal(before + 60.0 / 10, moved[4], 6);
        Assert.Equal(moved[4] + 6, moved[5], 6); // no freeze, no skipped frame
        Assert.Equal(still[^1], moved[^1], 6);
        Assert.Equal(140, moved[^1], 6);
        Assert.Equal(1, completions); // the rebuild-cancelled run did not complete
    });

    [Fact]
    public Task AnimatedContentSize_ReparentedMidSpring_KeepsVelocity() => OnUi(() =>
    {
        var spring = new SpringSpec<Size>(Spring.DampingRatioNoBouncy, Spring.StiffnessLow);
        var moved = Run(move: true, spring, 12, out _, out _);
        var before = moved[3] - moved[2];
        var after = moved[5] - moved[4];
        Assert.True(before > 1);
        Assert.InRange(after, before * 0.75, before * 1.1);
    });

    [Fact]
    public Task AnimatedContentSize_DetachedWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        var (host, leaf) = Create(Linear(160));
        using var stage = new ReparentFixture(host);
        leaf.Height = 140;
        stage.Step(0); stage.Step(16);
        stage.Detach();
        Assert.True(stage.Frames.PendingFrameCount > 0); // not released on detach
        Dispatcher.UIThread.RunJobs(); // no frame: the fallback post drains
        Assert.Equal(0, stage.Frames.PendingFrameCount);
    });

    [Fact]
    public Task AnimatedContentSize_DetachedWithoutReturn_IsReleasedAtTheDrainFrame() => OnUi(() =>
    {
        var (host, leaf) = Create(Linear(160));
        using var stage = new ReparentFixture(host);
        leaf.Height = 140;
        stage.Step(0); stage.Step(16);
        stage.Detach();
        stage.Frames.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, stage.Frames.PendingFrameCount);
    });

    [Fact]
    public Task AnimatedContentSize_ReentrantGetInScopeChanged_LeavesOneLiveRun() => OnUi(() =>
    {
        var (host, leaf) = Create(Linear(160));
        using var stage = new ReparentFixture(host);
        AnimationScope.AddScopeChangedHandler(host, (_, e) =>
        {
            if (e.NewScope is not null) _ = AnimationScope.Get(host); // re-entrant
        });
        leaf.Height = 140;
        stage.Step(0); stage.Step(16); stage.Step(16);
        stage.Move();
        stage.Step(16);
        Assert.Equal(1, stage.Frames.PendingFrameCount); // one live run
        for (var i = 0; i < 20; i++) stage.Step(16);
        Assert.Equal(140, host.Bounds.Height, 6);
    });

    [Fact]
    public Task AnimatedContentSize_ResetAnimation_StillResetsImmediately() => OnUi(() =>
    {
        var (host, leaf) = Create(Linear(160));
        using var stage = new ReparentFixture(host);
        leaf.Height = 140;
        stage.Step(0); stage.Step(16);
        host.ResetAnimation();
        stage.Layout();
        Assert.Equal(140, host.Bounds.Height);
        Assert.Equal(0, stage.Frames.PendingFrameCount);
    });

    [Fact]
    public Task AnimatedContentSize_HandlerReStalesItsHost_IsRebuiltNextFrameAndStillResumes() => OnUi(() =>
    {
        var (host, leaf) = Create(Linear(160));
        using var stage = new ReparentFixture(host);
        var changes = 0;
        AnimationScope.AddScopeChangedHandler(host, (_, e) =>
        {
            changes++;
            if (changes == 1) { stage.MoveBack(); } // D18: next frame
        });
        leaf.Height = 140;
        stage.Step(0); stage.Step(16); stage.Step(16);
        var before = host.Bounds.Height;
        stage.Move();
        var samples = new List<double>();
        for (var i = 0; i < 25; i++) { stage.Step(16); samples.Add(host.Bounds.Height); }
        Assert.Equal(2, changes);
        // Two resumes back to back (post drain, then the D18-deferred one) each re-anchor on the
        // interrupted run's last frame, so the deferred frame holds once; then it advances.
        Assert.True(samples[0] >= before);
        Assert.True(samples[1] > samples[0]);
        for (var i = 1; i < samples.Count; i++) Assert.True(samples[i] >= samples[i - 1]); // never drops back
        Assert.Equal(140, samples[^1], 6);
    });
}

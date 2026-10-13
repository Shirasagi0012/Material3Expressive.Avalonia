using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

/// <summary>D17 for <see cref="LookaheadSize"/> and <see cref="LookaheadPosition"/> inside one lookahead context.</summary>
public sealed class LookaheadRebuildResumeTest
{
    private static LookaheadSize Sized() => new()
    {
        Child = new LookaheadTextBlock { Text = "Text" },
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false, TargetSize = new Size(40, 40),
        AnimationSpec = new TweenSpec<Size>(TimeSpan.FromMilliseconds(160), easing: new LinearEasing()),
    };

    private static List<double> SizeRun(bool move, int frames)
    {
        var size = Sized();
        using var f = new ReparentFixture(size, lookahead: true);
        size.TargetSize = new Size(200, 140);
        f.Step(0);
        var samples = new List<double>();
        for (var i = 0; i < frames; i++)
        {
            if (move && i == 4) f.Move();
            f.Step(16);
            samples.Add(size.Bounds.Height);
        }
        return samples;
    }

    [Fact]
    public Task LookaheadSize_ReparentedMidFlight_ContinuesWithoutJump() => OnUi(() =>
    {
        var moved = SizeRun(move: true, 25);
        var still = SizeRun(move: false, 25);
        var before = moved[3];
        Assert.InRange(before, 41, 139);
        Assert.InRange(moved[4] - before, 0.01, 10.01); // at most one frame of motion, no snap to 140
        Assert.True(moved[5] > moved[4]);
        Assert.Equal(still[^1], moved[^1], 6);
        Assert.Equal(140, moved[^1], 6);
    });

    [Fact]
    public Task LookaheadSize_DetachedWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        var size = Sized();
        using var f = new ReparentFixture(size, lookahead: true);
        size.TargetSize = new Size(200, 140);
        f.Step(0); f.Step(16);
        f.Detach();
        Assert.True(f.Frames.PendingFrameCount > 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task LookaheadSize_ReentrantGetInScopeChanged_LeavesOneLiveRun() => OnUi(() =>
    {
        var size = Sized();
        using var f = new ReparentFixture(size, lookahead: true);
        AnimationScope.AddScopeChangedHandler(size, (_, e) => { if (e.NewScope is not null) _ = AnimationScope.Get(size); });
        size.TargetSize = new Size(200, 140);
        f.Step(0); f.Step(16); f.Step(16);
        f.Move();
        f.Step(16);
        Assert.Equal(1, f.Frames.PendingFrameCount);
        for (var i = 0; i < 20; i++) f.Step(16);
        Assert.Equal(140, size.Bounds.Height, 6);
    });

    private static LookaheadPosition Moving() => new()
    {
        Child = new Border { Width = 40, Height = 40 },
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false,
        AnimationSpec = new TweenSpec<Point>(TimeSpan.FromMilliseconds(160), easing: new LinearEasing()),
    };

    [Fact]
    public Task LookaheadPosition_ReparentedMidFlight_ContinuesWithoutJump() => OnUi(() =>
    {
        var spacer = new Border { Width = 40, Height = 0 };
        var moving = Moving();
        var host = new LookaheadStackPanel { Orientation = Orientation.Vertical, Children = { spacer, moving } };
        using var f = new ReparentFixture(host, lookahead: true);
        // Start a run, then move the moving node's container mid-flight.
        spacer.Height = 160; f.Layout();
        f.Step(16); f.Step(16); f.Step(16);
        var before = f.Position(moving).Y;
        Assert.InRange(before, 1, 159);
        f.Move();
        f.Step(16);
        var after = f.Position(moving).Y;
        Assert.InRange(after - before, 0, 16.01); // continues from where it was, no snap
        for (var i = 0; i < 20; i++) f.Step(16);
        Assert.Equal(160, f.Position(moving).Y, 6);
    });

    [Fact]
    public Task LookaheadPosition_DetachedWithoutReturn_IsReleasedAtTheDrain() => OnUi(() =>
    {
        var spacer = new Border { Width = 40, Height = 0 };
        var moving = Moving();
        var host = new LookaheadStackPanel { Orientation = Orientation.Vertical, Children = { spacer, moving } };
        using var f = new ReparentFixture(host, lookahead: true);
        spacer.Height = 160; f.Layout(); f.Step(16);
        f.Detach();
        Assert.True(f.Frames.PendingFrameCount > 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });
}

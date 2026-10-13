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
    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public LookaheadStackPanel Left { get; } = new() { Orientation = Orientation.Vertical };
        public LookaheadStackPanel Right { get; } = new() { Orientation = Orientation.Vertical };
        public LookaheadStackPanel Outer { get; }
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control content)
        {
            Left.Children.Add(content);
            Outer = new LookaheadStackPanel { Orientation = Orientation.Vertical, Children = { Left, Right } };
            Root = new LookaheadRoot { Width = 500, Height = 400, Child = Outer, UseLayoutRounding = false };
            Window = new AnimationTestWindow(Frames) { Width = 600, Height = 500, Content = Root };
            Window.Show(); Layout();
            AnimationScope.Get(Left); AnimationScope.Get(Right); AnimationScope.Get(content);
        }
        public void Move(Control content) { Left.Children.Remove(content); Right.Children.Add(content); }
        public Point Position(Control node) => node.TranslatePoint(default, Root)!.Value;
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        public void Step(int ms) { Layout(); Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() => Window.Close();
    }

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
        using var f = new Fixture(size);
        size.TargetSize = new Size(200, 140);
        f.Step(0);
        var samples = new List<double>();
        for (var i = 0; i < frames; i++)
        {
            if (move && i == 4) f.Move(size);
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
        using var f = new Fixture(size);
        size.TargetSize = new Size(200, 140);
        f.Step(0); f.Step(16);
        f.Left.Children.Remove(size);
        Assert.True(f.Frames.PendingFrameCount > 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task LookaheadSize_ReentrantGetInScopeChanged_LeavesOneLiveRun() => OnUi(() =>
    {
        var size = Sized();
        using var f = new Fixture(size);
        AnimationScope.AddScopeChangedHandler(size, (_, e) => { if (e.NewScope is not null) _ = AnimationScope.Get(size); });
        size.TargetSize = new Size(200, 140);
        f.Step(0); f.Step(16); f.Step(16);
        f.Move(size);
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
        using var f = new Fixture(host);
        // Start a run, then move the moving node's container mid-flight.
        spacer.Height = 160; f.Layout();
        f.Step(16); f.Step(16); f.Step(16);
        var before = f.Position(moving).Y;
        Assert.InRange(before, 1, 159);
        f.Left.Children.Remove(host); f.Right.Children.Add(host);
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
        using var f = new Fixture(host);
        spacer.Height = 160; f.Layout(); f.Step(16);
        f.Left.Children.Remove(host);
        Assert.True(f.Frames.PendingFrameCount > 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });
}

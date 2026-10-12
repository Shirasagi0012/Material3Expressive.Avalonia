using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadAutomaticDomainTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static AnimatedBounds Card(Control child, Size? size) => new()
    {
        Child = child, TargetSize = size, UseLayoutRounding = false,
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        BoundsTransform = new BoundsTransform
        {
            Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
        }
    };

    private sealed class Fixture : IDisposable
    {
        public Window Window { get; }
        public ManualFrameSource Frames { get; } = new();
        public Fixture(Control content, params AnimatedBounds[] cards)
        {

            Window = new AnimationTestWindow(Frames) { Content = content, Width = 500, Height = 400 };
            Window.Show(); Layout();
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() { Window.Close(); }
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public Task PositionChanges_OnlyRebuildTheirOwnAutomaticDomain(bool outerMoves, bool innerMoves) => OnUi(() =>
    {
        var inner = Card(new LookaheadBoundary { Child = new Border() }, new Size(60, 30));
        var innerBox = new LookaheadBox { Width = 200, Height = 100, Children = { inner }, UseLayoutRounding = false };
        var native = new Grid { Width = 200, Height = 100, Children = { innerBox } };
        var boundary = new LookaheadBoundary { Child = native, UseLayoutRounding = false };
        var outer = Card(boundary, new Size(200, 100));
        var outerBox = new LookaheadBox { Width = 400, Height = 300, Children = { outer }, UseLayoutRounding = false };
        using var f = new Fixture(outerBox, outer, inner);
        var beforeOuter = Lookahead.GetSnapshot(outer)!;
        var beforeInner = Lookahead.GetSnapshot(inner)!;
        Assert.Same(outerBox, beforeOuter.Entry);
        Assert.Same(innerBox, beforeInner.Entry);
        Assert.False(beforeOuter.Results.ContainsKey(inner));
        Assert.False(beforeInner.Results.ContainsKey(outer));
        if (outerMoves) outer.HorizontalAlignment = HorizontalAlignment.Right;
        if (innerMoves) inner.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout();
        var outerTarget = Lookahead.GetSnapshot(outer)!;
        var innerTarget = Lookahead.GetSnapshot(inner)!;
        Assert.Equal(outerMoves, !ReferenceEquals(beforeOuter, outerTarget));
        Assert.Equal(innerMoves, !ReferenceEquals(beforeInner, innerTarget));
        f.Step(16); f.Step(50);
        Assert.Equal(outerMoves, outer.IsRunning);
        Assert.Equal(innerMoves, inner.IsRunning);
        Assert.Same(outerTarget, Lookahead.GetSnapshot(outer));
        Assert.Same(innerTarget, Lookahead.GetSnapshot(inner));
        Assert.Equal(new Size(200, 100), native.Bounds.Size);
        f.Step(50);
        Assert.False(outer.IsRunning);
        Assert.False(inner.IsRunning);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task OuterTargetConstraintChange_UpdatesInnerOnceBeforeOuterSamples() => OnUi(() =>
    {
        var inner = Card(new LookaheadTextBlock { Text = "inner" }, new Size(60, 30));
        inner.HorizontalAlignment = HorizontalAlignment.Right;
        var innerBox = new LookaheadBox { Children = { inner }, UseLayoutRounding = false };
        var native = new Grid { Children = { innerBox } };
        var outer = Card(new LookaheadBoundary { Child = native }, new Size(200, 100));
        using var f = new Fixture(new Grid { Children = { outer } }, outer, inner);
        var oldInner = Lookahead.GetSnapshot(inner)!;
        outer.TargetSize = new Size(300, 160);
        f.Layout();
        var outerTarget = Lookahead.GetSnapshot(outer)!;
        var innerTarget = Lookahead.GetSnapshot(inner)!;
        Assert.NotSame(oldInner, innerTarget);
        Assert.Equal(new Size(300, 160), native.Bounds.Size);
        Assert.Equal(240, innerTarget.Results[inner].Bounds.X);
        f.Step(16); f.Step(50);
        Assert.Same(innerTarget, Lookahead.GetSnapshot(inner));
        Assert.Same(outerTarget, Lookahead.GetSnapshot(outer));
        Assert.Equal(new Size(300, 160), native.Bounds.Size);
        Assert.True(inner.IsRunning);
        Assert.True(outer.IsRunning);
    });

    [Fact]
    public Task InnerTargetDesiredSizeChange_UpdatesOuterDestinationWithoutFrameFeedback() => OnUi(() =>
    {
        var inner = Card(new LookaheadTextBlock { Text = "inner" }, new Size(60, 30));
        var innerBox = new LookaheadBox { Children = { inner }, UseLayoutRounding = false };
        var outer = Card(new LookaheadBoundary { Child = innerBox }, null);
        using var f = new Fixture(new Grid { Children = { outer } }, outer, inner);
        var beforeOuter = Lookahead.GetSnapshot(outer)!;
        inner.TargetSize = new Size(120, 80);
        f.Layout();
        var outerTarget = Lookahead.GetSnapshot(outer)!;
        var innerTarget = Lookahead.GetSnapshot(inner)!;
        Assert.NotSame(beforeOuter, outerTarget);
        Assert.Equal(new Size(120, 80), outerTarget.Results[outer].MeasuredSize);
        f.Step(16); f.Step(50);
        Assert.Same(outerTarget, Lookahead.GetSnapshot(outer));
        Assert.Same(innerTarget, Lookahead.GetSnapshot(inner));
        Assert.True(outer.IsRunning);
        Assert.True(inner.IsRunning);
        Assert.Equal(new Size(120, 80), outer.Bounds.Size);
    });
}

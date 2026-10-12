using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static AnimatedContentSize Animate(Control content) => new()
    {
        Content = content,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        AnimationSpec = new TweenSpec<Size>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
    };

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Clock { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control child, params AnimatedContentSize[] animations)
        {
            Root = new LookaheadRoot { Width = 300, Height = 350, Child = child };
            Window = new AnimationTestWindow(Clock) { Width = 500, Height = 500, Content = Root };
            Window.Show();
            Layout();
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Clock.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() { Window.Close(); }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task TargetPlacement_IsStableAcrossFramesAndRetargets(bool horizontal) => OnUi(() =>
    {
        var leaf = new Border { Width = 40, Height = 40 };
        var animated = Animate(leaf);
        var marker = new Border { Width = 20, Height = 20 };
        var stack = new LookaheadStackPanel
        {
            Orientation = horizontal ? Orientation.Horizontal : Orientation.Vertical,
            Spacing = 5,
            Children = { animated, marker }
        };
        using var f = new Fixture(stack, animated);
        double Position(Rect r) => horizontal ? r.X : r.Y;
        var initial = f.Root.Snapshot!;
        Assert.Equal(45, Position(initial.Results[marker].Bounds));
        Assert.Equal(0, f.Clock.PendingFrameCount);
        if (horizontal) leaf.Width = 140; else leaf.Height = 140;
        f.Layout();
        var target = f.Root.Snapshot!;
        Assert.True(target.Version > initial.Version);
        Assert.Equal(145, Position(target.Results[marker].Bounds));
        Assert.Equal(45, Position(marker.Bounds));
        f.Step(16);
        f.Step(50);
        Assert.Equal(95, Position(marker.Bounds), 5);
        Assert.Same(target, f.Root.Snapshot);
        Assert.Equal(45, Position(initial.Results[marker].Bounds)); // Old snapshots stay immutable.

        if (horizontal) leaf.Width = 60; else leaf.Height = 60;
        f.Layout();
        var redirected = f.Root.Snapshot!;
        Assert.True(redirected.Version > target.Version);
        Assert.Equal(65, Position(redirected.Results[marker].Bounds));
        Assert.Equal(95, Position(marker.Bounds), 5);
        f.Step(50);
        Assert.Equal(80, Position(marker.Bounds), 5);
        f.Step(50);
        Assert.Equal(65, Position(marker.Bounds), 5);
        Assert.Same(redirected, f.Root.Snapshot);
    });

    [Fact]
    public Task NestedAnimations_UseFinalChildSizeForParentTarget() => OnUi(() =>
    {
        var leaf = new Border { Width = 100, Height = 40 };
        var inner = Animate(leaf);
        var innerStack = new LookaheadStackPanel { Children = { inner, new Border { Width = 100, Height = 10 } } };
        var outer = Animate(innerStack);
        var marker = new Border { Width = 20, Height = 20 };
        using var f = new Fixture(new LookaheadStackPanel { Children = { outer, marker } }, inner, outer);
        leaf.Height = 140;
        f.Layout();
        var target = f.Root.Snapshot!;
        Assert.Equal(150, target.Results[outer].MeasuredSize.Height);
        Assert.Equal(150, target.Results[marker].Bounds.Y);
        f.Step(16);
        f.Step(50);
        Assert.Equal(90, inner.Bounds.Height, 5);
        Assert.Equal(100, outer.Bounds.Height, 5);
        Assert.Equal(100, marker.Bounds.Y, 5);
        Assert.Same(target, f.Root.Snapshot);
        f.Step(50);
        Assert.Equal(150, marker.Bounds.Y, 5);
        Assert.Same(target, f.Root.Snapshot);
    });

    [Fact]
    public Task TargetEvaluation_DoesNotMeasureOrArrangeRealControls() => OnUi(() =>
    {
        var leaf = new CountingLeaf { Width = 40, Height = 40 };
        var animated = Animate(leaf);
        using var f = new Fixture(new LookaheadStackPanel { Children = { animated } }, animated);
        var beforeBounds = leaf.Bounds;
        var beforeDesired = leaf.DesiredSize;
        var measures = leaf.Measures;
        var arrangements = leaf.Arrangements;
        leaf.Height = 140;
        f.Root.TargetLayoutUpdated += (_, _) =>
        {
            Assert.Equal(beforeBounds, leaf.Bounds);
            Assert.Equal(beforeDesired, leaf.DesiredSize);
            Assert.Equal(measures, leaf.Measures);
            Assert.Equal(arrangements, leaf.Arrangements);
            Assert.Equal(140, f.Root.Snapshot!.Results[leaf].Bounds.Height);
        };
        f.Layout();
        Assert.True(leaf.Measures > measures);
    });

    [Fact]
    public Task ConstraintsAndStructure_RebuildCompleteSnapshot() => OnUi(() =>
    {
        var leaf = new Border { Width = 400, Height = 40 };
        var animated = Animate(leaf);
        var marker = new Border { Width = 20, Height = 20, HorizontalAlignment = HorizontalAlignment.Right };
        var stack = new LookaheadStackPanel { Children = { animated, marker } };
        using var f = new Fixture(stack, animated);
        var original = f.Root.Snapshot!;
        Assert.Equal(300, original.Results[animated].MeasuredSize.Width);
        f.Root.Width = 200;
        f.Layout();
        var resized = f.Root.Snapshot!;
        Assert.Equal(200, resized.Results[animated].MeasuredSize.Width);
        Assert.Equal(180, resized.Results[marker].Bounds.X);
        stack.Children.Remove(animated);
        stack.Spacing = 8;
        f.Layout();
        var removed = f.Root.Snapshot!;
        Assert.False(removed.Results.ContainsKey(animated));
        Assert.Equal(0, removed.Results[marker].Bounds.Y);
        leaf.Height = 160;
        f.Layout();
        Assert.Same(removed, f.Root.Snapshot); // Removed subtree is no longer subscribed.
        marker.IsVisible = false;
        f.Layout();
        Assert.Equal(default, f.Root.Snapshot!.Results[marker].DesiredSize);
    });

    [Fact]
    public Task ContentReplacementAndReattach_RemoveOldRegistrations() => OnUi(() =>
    {
        var oldLeaf = new Border { Width = 40, Height = 40 };
        var animated = Animate(oldLeaf);
        using var f = new Fixture(new LookaheadStackPanel { Children = { animated } }, animated);
        var newLeaf = new Border { Width = 100, Height = 140 };
        animated.Content = newLeaf;
        f.Layout();
        var replaced = f.Root.Snapshot!;
        Assert.False(replaced.Results.ContainsKey(oldLeaf));
        Assert.True(replaced.Results.ContainsKey(newLeaf));
        f.Window.Content = null;
        f.Layout();
        Assert.Null(f.Root.Snapshot);
        Assert.Equal(0, f.Clock.PendingFrameCount);
        newLeaf.Height = 80;
        f.Window.Content = f.Root;
        f.Layout();
        Assert.Equal(80, f.Root.Snapshot!.Results[animated].MeasuredSize.Height);
        Assert.Equal(40, animated.Bounds.Height); // Reattachment resumes the retained sample.
        f.Step(16);
        f.Step(100);
        Assert.Equal(80, animated.Bounds.Height);
        Assert.Equal(0, f.Clock.PendingFrameCount);
    });

    [Fact]
    public Task TargetPlacement_MatchesActualMarginAlignmentAndLocalRounding() => OnUi(() =>
    {
        var leaf = new Border
        {
            Width = 30.2, Height = 20.2, Margin = new Thickness(0.4),
            UseLayoutRounding = true, HorizontalAlignment = HorizontalAlignment.Right
        };
        var stack = new LookaheadStackPanel
        {
            Margin = new Thickness(0.25), UseLayoutRounding = false, Children = { leaf }
        };
        using var f = new Fixture(stack);
        var target = f.Root.Snapshot!.Results[leaf].Bounds;
        var position = leaf.TranslatePoint(default, f.Root)!.Value;
        Assert.Equal(position.X, target.X, 6);
        Assert.Equal(position.Y, target.Y, 6);
        Assert.Equal(leaf.Bounds.Size, target.Size);
        leaf.Margin = new Thickness(8);
        leaf.HorizontalAlignment = HorizontalAlignment.Center;
        f.Layout();
        target = f.Root.Snapshot!.Results[leaf].Bounds;
        position = leaf.TranslatePoint(default, f.Root)!.Value;
        Assert.Equal(position.X, target.X, 6);
        Assert.Equal(position.Y, target.Y, 6);
    });

    [Fact]
    public Task UnsupportedAutoContentAndUnboundedRoot_AreExplicit() => OnUi(() =>
    {
        var root = new LookaheadRoot { Child = new TextBlock { Text = "Auto size" } };
        var error = Assert.Throws<NotSupportedException>(() => root.Measure(new Size(300, 300)));
        Assert.Contains("TextBlock", error.Message);
        var unbounded = new LookaheadRoot { Child = new Border { Width = 10, Height = 10 } };
        Assert.Throws<NotSupportedException>(() => unbounded.Measure(Size.Infinity));
    });

    private sealed class CountingLeaf : Control
    {
        public int Measures { get; private set; }
        public int Arrangements { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return availableSize; }
        protected override Size ArrangeOverride(Size finalSize) { Arrangements++; return finalSize; }
    }
}

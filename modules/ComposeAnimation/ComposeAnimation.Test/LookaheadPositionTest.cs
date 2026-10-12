using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadPositionTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static LookaheadPosition Moving(Control? child = null, int duration = 100) => new()
    {
        Child = child ?? new Border { Width = 40, Height = 40 },
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false,
        AnimationSpec = new TweenSpec<Point>(TimeSpan.FromMilliseconds(duration), easing: new LinearEasing())
    };

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control content, params LookaheadPosition[] nodes)
        {

            Root = new LookaheadRoot { Width = 350, Height = 350, Child = content };
            Window = new AnimationTestWindow(Frames) { Width = 500, Height = 500, Content = Root };
            Window.Show(); Layout();
        }
        public Point Position(Control control) => control.TranslatePoint(default, Root)!.Value;
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() { Window.Close(); }
    }

    [Theory]
    [InlineData(Orientation.Vertical)]
    [InlineData(Orientation.Horizontal)]
    public Task Reorder_AnimatesRealBounds_KeepsTargetsAndMeasurementsStable(Orientation orientation) => OnUi(() =>
    {
        var leaf = new CountingLeaf { Width = 40, Height = 40 };
        var a = Moving(leaf); var b = Moving(); var c = Moving();
        var stack = new LookaheadStackPanel { Orientation = orientation, Spacing = 10, Children = { a, b, c } };
        using var f = new Fixture(stack, a, b, c);
        double Along(Point p) => orientation == Orientation.Vertical ? p.Y : p.X;
        Assert.Equal(0, f.Frames.PendingFrameCount);
        stack.Children.Move(0, 2);
        f.Layout();
        var target = f.Root.Snapshot!;
        var measures = leaf.Measures;
        Assert.Equal(100, Along(target.Results[a].Bounds.TopLeft));
        Assert.Equal(0, Along(f.Position(a)));
        f.Step(16); f.Step(50);
        Assert.Equal(50, Along(f.Position(a)), 5);
        Assert.Equal(25, Along(f.Position(b)), 5);
        Assert.Null(a.RenderTransform);
        Assert.Equal(new Size(40, 40), a.Bounds.Size);
        Assert.Equal(measures, leaf.Measures);
        Assert.Same(target, f.Root.Snapshot);
        stack.Spacing = 30;
        f.Layout();
        var retarget = f.Root.Snapshot!;
        Assert.Equal(140, Along(retarget.Results[a].Bounds.TopLeft));
        Assert.Equal(50, Along(f.Position(a)), 5);
        f.Step(50);
        Assert.Equal(95, Along(f.Position(a)), 5);
        f.Step(50);
        foreach (var node in new[] { a, b, c }) Assert.Equal(retarget.Results[node].Bounds.TopLeft, f.Position(node));
        Assert.Same(retarget, f.Root.Snapshot);
    });

    [Fact]
    public Task NestedDifferentDurations_UseCurrentParentOrigin_AndDoNotDoubleMotion() => OnUi(() =>
    {
        var a = Moving(); var b = Moving();
        var inner = new LookaheadStackPanel { Spacing = 10, Children = { a, b } };
        var parent = Moving(inner, duration: 200);
        var outer = new LookaheadStackPanel { Children = { parent }, UseLayoutRounding = false };
        using var f = new Fixture(outer, parent, a, b);
        outer.Margin = new Thickness(100, 80, 0, 0);
        inner.Children.Move(0, 1);
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        Assert.Equal(new Point(100, 130), snapshot.Results[a].Bounds.TopLeft);
        f.Step(16); f.Step(50);
        Assert.Equal(new Point(25, 20), f.Position(parent));
        Assert.Equal(new Point(50, 65), f.Position(a));
        Assert.Equal(new Point(25, 45), a.Bounds.TopLeft); // root sample minus current parent
        f.Step(50);
        Assert.Equal(new Point(50, 40), f.Position(parent));
        Assert.Equal(new Point(100, 130), f.Position(a)); // child finished, parent still moving
        f.Step(50);
        Assert.Equal(new Point(100, 130), f.Position(a));
        f.Step(50);
        Assert.Equal(snapshot.Results[parent].Bounds.TopLeft, f.Position(parent));
        Assert.Equal(snapshot.Results[a].Bounds.TopLeft, f.Position(a));
        Assert.Equal(new Point(0, 50), a.Bounds.TopLeft);
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task NestedRetargetAndAxisChange_ContinueFromCurrentRootSamples() => OnUi(() =>
    {
        var a = Moving(); var b = Moving();
        var inner = new LookaheadStackPanel { Spacing = 10, Children = { a, b } };
        var parent = Moving(inner, 200);
        var outer = new LookaheadStackPanel { Children = { parent } };
        using var f = new Fixture(outer, parent, a, b);
        outer.Margin = new Thickness(100, 80, 0, 0);
        inner.Children.Move(0, 1);
        f.Layout(); f.Step(16); f.Step(50);
        var aBefore = f.Position(a);
        var parentBefore = f.Position(parent);
        outer.Margin = new Thickness(60, 20, 0, 0);
        inner.Orientation = Orientation.Horizontal;
        f.Layout();
        var target = f.Root.Snapshot!;
        Assert.Equal(aBefore, f.Position(a));
        Assert.Equal(parentBefore, f.Position(parent));
        f.Step(50);
        var aTarget = target.Results[a].Bounds.TopLeft;
        Assert.Equal(aBefore.X + (aTarget.X - aBefore.X) / 2, f.Position(a).X, 5);
        Assert.Equal(aBefore.Y + (aTarget.Y - aBefore.Y) / 2, f.Position(a).Y, 5);
        f.Step(150);
        foreach (var node in new[] { a, b, parent }) Assert.Equal(target.Results[node].Bounds.TopLeft, f.Position(node));
        Assert.Same(target, f.Root.Snapshot);
    });

    [Fact]
    public Task MovingUnanimatedAncestor_AlignmentAndRounding_ConvergeWithoutJump() => OnUi(() =>
    {
        var node = Moving();
        node.UseLayoutRounding = true;
        node.Margin = new Thickness(0.4);
        var stack = new LookaheadStackPanel { UseLayoutRounding = false, Margin = new Thickness(0.25), Children = { node } };
        using var f = new Fixture(stack, node);
        var old = f.Position(node);
        node.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout();
        Assert.Equal(old, f.Position(node));
        var target = f.Root.Snapshot!;
        f.Step(16); f.Step(50);
        Assert.InRange(f.Position(node).X, old.X + 1, target.Results[node].Bounds.X - 1);
        stack.Margin = new Thickness(30.25, 20.25, 0.25, 0.25);
        var before = f.Position(node);
        f.Layout();
        Assert.Equal(before, f.Position(node));
        f.Step(100);
        Assert.Equal(f.Root.Snapshot!.Results[node].Bounds.TopLeft, f.Position(node));
        // Moving the whole root is outside the scope's coordinate system, not a new target.
        var stable = f.Root.Snapshot;
        f.Root.Margin = new Thickness(10);
        f.Layout();
        Assert.Same(stable, f.Root.Snapshot);
    });

    [Fact]
    public Task InsertRemoveHideAndReattach_ResetPlacementWithoutExitRetention() => OnUi(() =>
    {
        var a = Moving(); var b = Moving();
        var stack = new LookaheadStackPanel { Spacing = 10, Children = { a, b } };
        using var f = new Fixture(stack, a, b);
        var added = Moving();
        stack.Children.Insert(0, added); f.Layout();
        Assert.Equal(f.Root.Snapshot!.Results[added].Bounds.TopLeft, f.Position(added));
        Assert.Equal(new Point(0, 0), f.Position(a));
        f.Step(16); f.Step(50);
        stack.Children.Remove(b); f.Layout();
        Assert.False(f.Root.Snapshot!.Results.ContainsKey(b));
        a.IsVisible = false; f.Layout();
        Assert.Equal(0, f.Frames.PendingFrameCount);
        a.IsVisible = true; f.Layout();
        Assert.Equal(f.Root.Snapshot!.Results[a].Bounds.TopLeft, f.Position(a));
        stack.Spacing = 80; f.Layout();
        Assert.True(f.Frames.PendingFrameCount > 0);
        f.Window.Content = null;
        Assert.Equal(0, f.Frames.PendingFrameCount);
        stack.Spacing = 20;
        f.Window.Content = f.Root; f.Layout();
        Assert.Equal(f.Root.Snapshot!.Results[a].Bounds.TopLeft, f.Position(a));
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    private sealed class CountingLeaf : Control
    {
        public int Measures { get; private set; }
        protected override Size MeasureOverride(Size availableSize) { Measures++; return availableSize; }
    }
}

using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadBoxTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static TweenSpec<T> Tween<T>(int ms) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());

    private static LookaheadBox Box(params Control[] children)
    {
        var box = new LookaheadBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        foreach (var child in children) box.Children.Add(child);
        return box;
    }

    private sealed class AvailableProbe : Control, ILookaheadLayoutNode
    {
        public Size LastAvailable { get; private set; }
        IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => [];

        Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
        {
            LastAvailable = available;
            return default;
        }

        void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        {
        }
    }

    private static Border Leaf(
        double width,
        double height,
        HorizontalAlignment horizontal = HorizontalAlignment.Left,
        VerticalAlignment vertical = VerticalAlignment.Top
    ) => new()
    {
        Width = width, Height = height, HorizontalAlignment = horizontal, VerticalAlignment = vertical,
        UseLayoutRounding = false
    };

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }

        public Fixture(Control child, params Control[] animations)
        {

            Root = new LookaheadRoot { Width = 400, Height = 300, Child = child, UseLayoutRounding = false };
            Window = new AnimationTestWindow(Frames) { Width = 500, Height = 400, Content = Root };
            Window.Show();
            Layout();
        }

        public Point Position(Control node) => node.TranslatePoint(default, Root)!.Value;

        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }

        public void Step(int ms)
        {
            Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms));
            Layout();
        }

        public void Dispose()
        {
            Window.Close();

        }
    }

    [Fact]
    public Task SharedSlot_UsesMaxDesiredSizeAndSameConstraints() => OnUi(() =>
    {
        var wide = Leaf(80, 20);
        var tall = Leaf(30, 60);
        var corner = Leaf(24, 16, HorizontalAlignment.Right, VerticalAlignment.Bottom);
        var first = new AvailableProbe();
        var second = new AvailableProbe();
        var box = Box(wide, tall, corner, first, second);
        using var f = new Fixture(box);
        var snapshot = f.Root.Snapshot!;
        Assert.Equal(new Size(80, 60), snapshot.Results[box].MeasuredSize);
        Assert.Equal(new Size(80, 60), snapshot.Results[box].Bounds.Size);
        Assert.Equal(first.LastAvailable, second.LastAvailable);
        Assert.Equal(new Point(0, 0), snapshot.Results[wide].Bounds.TopLeft);
        Assert.Equal(new Point(0, 0), snapshot.Results[tall].Bounds.TopLeft);
        Assert.Equal(new Point(56, 44), snapshot.Results[corner].Bounds.TopLeft);
        Assert.Equal(snapshot.Results[wide].Bounds, new Rect(f.Position(wide), wide.Bounds.Size));
        Assert.Equal(snapshot.Results[corner].Bounds, new Rect(f.Position(corner), corner.Bounds.Size));
        Assert.Equal(new Size(80, 60), box.Bounds.Size);
    });

    [Fact]
    public Task AddRemoveReorderHide_UpdateTargetsAndDrawingOrder() => OnUi(() =>
    {
        var back = Leaf(80, 80);
        var front = Leaf(40, 40, HorizontalAlignment.Right, VerticalAlignment.Bottom);
        var box = Box(back);
        using var f = new Fixture(box);
        Assert.Equal(new Size(80, 80), f.Root.Snapshot!.Results[box].Bounds.Size);

        box.Children.Add(front);
        f.Layout();
        var stacked = f.Root.Snapshot!;
        Assert.Equal(new Size(80, 80), stacked.Results[box].Bounds.Size);
        Assert.Equal(new Point(40, 40), stacked.Results[front].Bounds.TopLeft);
        Assert.Same(front, box.Children[^1]);

        var beforeBack = stacked.Results[back].Bounds;
        var beforeFront = stacked.Results[front].Bounds;
        box.Children.Move(1, 0);
        f.Layout();
        var reordered = f.Root.Snapshot!;
        Assert.NotSame(stacked, reordered);
        Assert.Same(front, box.Children[0]);
        Assert.Same(back, box.Children[^1]);
        Assert.Equal(beforeBack, reordered.Results[back].Bounds);
        Assert.Equal(beforeFront, reordered.Results[front].Bounds);

        back.IsVisible = false;
        f.Layout();
        var hidden = f.Root.Snapshot!;
        Assert.Equal(default, hidden.Results[back].DesiredSize);
        Assert.Equal(new Size(40, 40), hidden.Results[box].Bounds.Size);
        Assert.Equal(new Point(0, 0), hidden.Results[front].Bounds.TopLeft);

        back.IsVisible = true;
        f.Layout();
        Assert.Equal(new Size(80, 80), f.Root.Snapshot!.Results[box].Bounds.Size);
        Assert.Equal(new Point(40, 40), f.Root.Snapshot.Results[front].Bounds.TopLeft);

        box.Children.Remove(front);
        f.Layout();
        var removed = f.Root.Snapshot!;
        Assert.False(removed.Results.ContainsKey(front));
        Assert.Equal(new Size(80, 80), removed.Results[box].Bounds.Size);
        front.Width = 120;
        f.Layout();
        Assert.Same(removed, f.Root.Snapshot);
    });

    [Fact]
    public Task MarginAlignmentAndRounding_MatchActualPlacement() => OnUi(() =>
    {
        var child = new Border
        {
            Width = 30.2, Height = 20.2, Margin = new Thickness(0.4),
            UseLayoutRounding = true, HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        var box = new LookaheadBox { Margin = new Thickness(0.25), UseLayoutRounding = false, Children = { child } };
        using var f = new Fixture(box);
        var target = f.Root.Snapshot!.Results[child].Bounds;
        var position = f.Position(child);
        Assert.Equal(position.X, target.X, 6);
        Assert.Equal(position.Y, target.Y, 6);
        Assert.Equal(child.Bounds.Size, target.Size);

        child.Margin = new Thickness(8);
        child.HorizontalAlignment = HorizontalAlignment.Center;
        child.VerticalAlignment = VerticalAlignment.Center;
        f.Layout();
        target = f.Root.Snapshot!.Results[child].Bounds;
        position = f.Position(child);
        Assert.Equal(position.X, target.X, 6);
        Assert.Equal(position.Y, target.Y, 6);
        Assert.Equal(child.Bounds.Size, target.Size);
    });

    [Fact]
    public Task InvalidatedInputAndRemovedSubtree_DoNotReuseStaleSubscriptions() => OnUi(() =>
    {
        var child = Leaf(40, 30);
        var box = Box(child);
        using var f = new Fixture(box);
        var initial = f.Root.Snapshot!;
        Assert.True(box.IsMeasureValid);
        box.InvalidateMeasure();
        Assert.False(box.IsMeasureValid);
        child.Width = 180;
        child.Height = 50;
        f.Layout();
        var grown = f.Root.Snapshot!;
        Assert.NotSame(initial, grown);
        Assert.Equal(new Size(180, 50), grown.Results[box].MeasuredSize);
        Assert.Equal(new Size(180, 50), grown.Results[child].Bounds.Size);

        f.Root.Child = new Border { Width = 10, Height = 10 };
        f.Layout();
        var replaced = f.Root.Snapshot!;
        Assert.False(replaced.Results.ContainsKey(box));
        child.Width = 90;
        box.Children.Clear();
        f.Layout();
        Assert.Same(replaced, f.Root.Snapshot);
    });

    [Fact]
    public Task SizeAndPosition_KeepSnapshotStableDuringApproach() => OnUi(() =>
    {
        var size = new LookaheadSize
        {
            TargetSize = new Size(100, 80), Child = new Border { Width = 20, Height = 20 },
            AnimationSpec = Tween<Size>(200), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false
        };
        var overlay = new LookaheadPosition
        {
            Child = new Border { Width = 40, Height = 20 }, AnimationSpec = Tween<Point>(100),
            HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom,
            UseLayoutRounding = false
        };
        var box = Box(size, overlay);
        using var f = new Fixture(box, size, overlay);
        Assert.Equal(new Size(100, 80), f.Root.Snapshot!.Results[box].Bounds.Size);
        Assert.Equal(new Point(60, 60), f.Root.Snapshot.Results[overlay].Bounds.TopLeft);
        Assert.Equal(0, f.Frames.PendingFrameCount);

        size.TargetSize = new Size(200, 140);
        f.Layout();
        var target = f.Root.Snapshot!;
        Assert.Equal(new Size(200, 140), target.Results[box].Bounds.Size);
        Assert.Equal(new Point(160, 120), target.Results[overlay].Bounds.TopLeft);
        Assert.Equal(new Size(100, 80), size.Bounds.Size);
        Assert.Equal(new Point(60, 60), f.Position(overlay));

        f.Step(16);
        f.Step(50);
        Assert.Equal(125, size.Bounds.Width, 5);
        Assert.Equal(95, size.Bounds.Height, 5);
        Assert.Equal(110, f.Position(overlay).X, 5);
        Assert.Equal(90, f.Position(overlay).Y, 5);
        Assert.Same(target, f.Root.Snapshot);

        f.Step(50);
        Assert.Equal(new Point(160, 120), f.Position(overlay));
        Assert.Equal(150, size.Bounds.Width, 5);
        Assert.Same(target, f.Root.Snapshot);

        f.Step(100);
        Assert.Equal(target.Results[size].Bounds.Size, size.Bounds.Size);
        Assert.Equal(target.Results[overlay].Bounds.TopLeft, f.Position(overlay));
        Assert.Equal(target.Results[box].Bounds.Size, box.Bounds.Size);
        Assert.Same(target, f.Root.Snapshot);
        Assert.Null(overlay.RenderTransform);
    });
}
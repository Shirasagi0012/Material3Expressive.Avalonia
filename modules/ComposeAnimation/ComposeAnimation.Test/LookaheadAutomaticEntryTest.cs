using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Animation.Easings;
using Avalonia.Threading;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadAutomaticEntryTest
{

    private sealed class Probe : Control, ILookaheadConstrainedLayoutNode
    {
        public LookaheadConstraints Target { get; private set; }
        public LookaheadConstraints Actual { get; private set; }
        public IEnumerable<Control> LookaheadChildren => [];
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available) => new(20, 10);
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        {
            Target = constraints;
            return new Size(20, 10);
        }
        public Size MeasureApproach(LookaheadConstraints constraints)
        {
            Actual = constraints;
            return new Size(20, 10);
        }
        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds) { }
        protected override Size MeasureCore(Size availableSize)
            => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired) ? desired : base.MeasureCore(availableSize);
        protected override Size ArrangeOverride(Size finalSize) => finalSize;
        protected override void ArrangeCore(Rect finalRect)
        {
            if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
            else base.ArrangeCore(finalRect);
        }
    }

    private sealed class Fixture : IDisposable
    {
        public Window Window { get; }
        public Fixture(Control content, IFrameSource? clock = null)
        {
            Window = new AnimationTestWindow(clock) { Width = 360, Height = 240, Content = content };
            Window.Show();
            Layout();
        }
        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
        }
        public void Dispose() => Window.Close();
    }

    [Fact]
    public Task FullConstraintMeasure_AtAutomaticEntry_PreservesMinimaInBothPasses() => OnUi(() =>
    {
        var probe = new Probe();
        var box = new LookaheadBox
        {
            PropagateMinConstraints = true,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false,
            Children = { probe }
        };
        var parent = new Grid { Children = { box } };
        var constraints = new LookaheadConstraints(120, 180, 80, 120);

        Assert.Equal(constraints.MinSize, LookaheadMeasure.Measure(box, constraints));
        box.Arrange(new Rect(10, 20, constraints.MaxSize.Width, constraints.MaxSize.Height));

        var snapshot = Lookahead.GetSnapshot(box)!;
        Assert.Same(snapshot, Lookahead.GetSnapshot(probe));
        Assert.Equal(constraints, snapshot.Results[box].FullMeasureConstraints);
        Assert.Equal(constraints, probe.Target);
        Assert.Equal(constraints, probe.Actual);
        Assert.Same(parent, box.Parent);
    });

    [Fact]
    public Task RootlessCooperativeTree_UsesOneDomain_ForContainersSizePositionAndScroll() => OnUi(() =>
    {
        var leaf = new Probe();
        var size = new LookaheadSize { TargetSize = new Size(90, 50), Child = leaf, UseLayoutRounding = false };
        var position = new LookaheadPosition { Child = size, UseLayoutRounding = false };
        var scroll = new LookaheadScrollViewport
        {
            Width = 140,
            Height = 70,
            CanVerticallyScroll = true,
            Child = new LookaheadBox { Height = 180, Children = { position }, UseLayoutRounding = false },
            UseLayoutRounding = false
        };
        var stack = new LookaheadStackPanel
        {
            Width = 200,
            Height = 120,
            Children = { scroll },
            UseLayoutRounding = false
        };

        using var fixture = new Fixture(new Border { Child = stack });

        var snapshot = Lookahead.GetSnapshot(stack)!;
        Assert.Same(snapshot, Lookahead.GetSnapshot(scroll));
        Assert.Same(snapshot, Lookahead.GetSnapshot(position));
        Assert.Same(snapshot, Lookahead.GetSnapshot(size));
        Assert.Same(snapshot, Lookahead.GetSnapshot(leaf));
        Assert.Contains(stack, snapshot.Results.Keys);
        Assert.Contains(scroll, snapshot.Results.Keys);
        Assert.Contains(position, snapshot.Results.Keys);
        Assert.Contains(size, snapshot.Results.Keys);
        Assert.Equal(new Size(140, 70), scroll.Viewport);
        Assert.True(scroll.Extent.Height >= 180);
    });

    [Fact]
    public Task DetachThenJoinExplicitRoot_ReleasesOwnedDomainAndUsesAncestorDomain() => OnUi(() =>
    {
        var box = new LookaheadBox
        {
            Width = 120,
            Height = 80,
            Children = { new Probe() },
            UseLayoutRounding = false
        };
        using var fixture = new Fixture(box);
        var local = Lookahead.GetSnapshot(box);
        Assert.NotNull(local);

        fixture.Window.Content = null;
        fixture.Layout();
        Assert.Null(Lookahead.GetSnapshot(box));

        var root = new LookaheadRoot { Width = 240, Height = 160, Child = box, UseLayoutRounding = false };
        fixture.Window.Content = root;
        fixture.Layout();

        Assert.NotSame(local, Lookahead.GetSnapshot(box));
        Assert.Same(root.Snapshot, Lookahead.GetSnapshot(box));
        Assert.Contains(box, root.Snapshot!.Results.Keys);
    });

    [Fact]
    public Task RootlessStack_SizeAndPositionAnimateInsideTheStableAutomaticDomain() => OnUi(() =>
    {
        var clock = new ManualFrameSource();
        var spacer = new Border { Width = 1, Height = 20 };
        var size = new LookaheadSize
        {
            TargetSize = new Size(40, 30),
            AnimationSpec = new TweenSpec<Size>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()),
            Child = new Probe(),
            HorizontalAlignment = HorizontalAlignment.Left,
            UseLayoutRounding = false
        };
        var position = new LookaheadPosition
        {
            AnimationSpec = new TweenSpec<Point>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing()),
            Child = size,
            HorizontalAlignment = HorizontalAlignment.Left,
            UseLayoutRounding = false
        };


        var stack = new LookaheadStackPanel
        {
            Width = 200,
            Children = { spacer, position },
            UseLayoutRounding = false
        };
        using var fixture = new Fixture(stack, clock);
        var snapshot = Lookahead.GetSnapshot(stack);
        var startSize = size.Bounds.Size;
        var startY = position.Bounds.Y;

        spacer.Height = 80;
        size.TargetSize = new Size(120, 70);
        fixture.Layout();
        var target = Lookahead.GetSnapshot(stack)!;
        var targetSize = target.Results[size].Bounds.Size;
        var targetY = target.Results[position].Bounds.Y;
        Assert.NotEqual(startSize, targetSize);
        Assert.NotEqual(startY, targetY);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        fixture.Layout();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        fixture.Layout();

        Assert.Same(target, Lookahead.GetSnapshot(stack));
        Assert.InRange(size.Bounds.Width, startSize.Width + .01, targetSize.Width - .01);
        Assert.InRange(position.Bounds.Y, startY + .01, targetY - .01);
        Assert.Same(target, Lookahead.GetSnapshot(size));
        Assert.NotSame(snapshot, target);
    });
}

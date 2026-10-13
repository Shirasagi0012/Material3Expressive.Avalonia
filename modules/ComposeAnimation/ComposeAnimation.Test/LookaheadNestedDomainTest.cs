using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadNestedDomainTest
{

    private static LookaheadPosition Moving(Control child, int duration = 100) => new()
    {
        Child = child,
        AnimationSpec = new TweenSpec<Point>(TimeSpan.FromMilliseconds(duration), easing: new LinearEasing()),
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false
    };

    private static LookaheadBoundary Boundary(Control child) => new()
    {
        Child = child,
        HorizontalAlignment = HorizontalAlignment.Left,
        VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false
    };

    private sealed class CountingLeaf : Control, ILookaheadConstrainedLayoutNode
    {
        public int Measures { get; private set; }

        public IEnumerable<Control> LookaheadChildren => [];

        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size availableSize) => Natural(availableSize);

        public Size MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
            => Natural(constraints.MaxSize);

        public Size MeasureApproach(LookaheadConstraints constraints)
        {
            Measures++;
            return Natural(constraints.MaxSize);
        }

        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds) { }

        protected override Size MeasureOverride(Size availableSize)
        {
            Measures++;
            return Natural(availableSize);
        }

        protected override Size MeasureCore(Size availableSize)
            => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired)
                ? desired
                : base.MeasureCore(availableSize);

        protected override Size ArrangeOverride(Size finalSize) => finalSize;

        protected override void ArrangeCore(Rect finalRect)
        {
            if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
            else base.ArrangeCore(finalRect);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            LookaheadMeasure.Forget(this);
            base.OnDetachedFromVisualTree(e);
        }

        private static Size Natural(Size availableSize)
            => new(Math.Min(40, availableSize.Width), Math.Min(20, availableSize.Height));
    }

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public LookaheadRoot OuterRoot { get; }
        public Window Window { get; }

        public Fixture(Control content, params Control[] animated)
        {

            OuterRoot = new LookaheadRoot
            {
                Width = 400,
                Height = 300,
                Child = content,
                UseLayoutRounding = false
            };
            Window = new AnimationTestWindow(Frames) { Width = 500, Height = 400, Content = OuterRoot };
            Window.Show();
            Layout();
        }

        public void Layout()
        {
            Dispatcher.UIThread.RunJobs();
            Window.UpdateLayout();
            Dispatcher.UIThread.RunJobs();
        }

        public void Step(int milliseconds)
        {
            Frames.AdvanceBy(TimeSpan.FromMilliseconds(milliseconds));
            Layout();
        }

        public Point OuterPosition(Control control) => control.TranslatePoint(default, OuterRoot)!.Value;

        public void Dispose()
        {
            Window.Close();

        }
    }

    [Fact]
    public Task OuterAndInnerTargetsAndApproaches_AreIndependent() => OnUi(() =>
    {
        var innerMover = Moving(new Border { Width = 40, Height = 20 });
        var innerSibling = new Border { Width = 10, Height = 10 };
        var innerStack = new LookaheadStackPanel
        {
            Spacing = 20,
            Children = { innerMover, innerSibling },
            UseLayoutRounding = false
        };
        var innerRoot = new LookaheadRoot
        {
            Width = 120,
            Height = 100,
            Child = innerStack,
            UseLayoutRounding = false
        };
        var ordinaryContent = new Grid { Children = { innerRoot } };
        var outerMover = Moving(Boundary(ordinaryContent));
        var outerSibling = new Border { Width = 10, Height = 10 };
        var outerStack = new LookaheadStackPanel
        {
            Spacing = 20,
            Children = { outerMover, outerSibling },
            UseLayoutRounding = false
        };
        using var f = new Fixture(outerStack, outerMover, innerMover);

        var initialOuter = f.OuterRoot.Snapshot!;
        var initialInner = innerRoot.Snapshot!;
        Assert.False(initialOuter.Results.ContainsKey(innerMover));
        Assert.False(initialInner.Results.ContainsKey(outerMover));

        outerStack.Children.Move(0, 1);
        f.Layout();
        var outerTarget = f.OuterRoot.Snapshot!;
        Assert.NotSame(initialOuter, outerTarget);
        Assert.Same(initialInner, innerRoot.Snapshot);
        Assert.Equal(default, f.OuterPosition(outerMover));
        Assert.Equal(default, LookaheadCoordinates.ActualPosition(innerMover));

        f.Step(16);
        f.Step(50);
        Assert.InRange(f.OuterPosition(outerMover).Y, 0.01, outerTarget.Results[outerMover].Bounds.Y - 0.01);
        Assert.Equal(default, LookaheadCoordinates.ActualPosition(innerMover));
        Assert.Same(initialInner, innerRoot.Snapshot);
        f.Step(50);

        var stableOuter = f.OuterRoot.Snapshot!;
        innerStack.Children.Move(0, 1);
        f.Layout();
        var innerTarget = innerRoot.Snapshot!;
        Assert.NotSame(initialInner, innerTarget);
        Assert.Same(stableOuter, f.OuterRoot.Snapshot);
        Assert.Equal(default, LookaheadCoordinates.ActualPosition(innerMover));
        Assert.Equal(stableOuter.Results[outerMover].Bounds.TopLeft, f.OuterPosition(outerMover));

        f.Step(16);
        f.Step(50);
        var innerPosition = LookaheadCoordinates.ActualPosition(innerMover);
        Assert.InRange(innerPosition.Y, 0.01, innerTarget.Results[innerMover].Bounds.Y - 0.01);
        Assert.Equal(stableOuter.Results[outerMover].Bounds.TopLeft, f.OuterPosition(outerMover));
        Assert.Same(stableOuter, f.OuterRoot.Snapshot);
    });

    [Fact]
    public Task BoundaryOrdinaryContent_ReopensDomain_AndCoordinatesCannotCrossIt() => OnUi(() =>
    {
        var innerLeaf = new Border { Width = 30, Height = 20 };
        var innerRoot = new LookaheadRoot
        {
            Width = 100,
            Height = 80,
            Child = new LookaheadBox { Children = { innerLeaf } },
            UseLayoutRounding = false
        };
        var ordinaryGrid = new Grid { Children = { innerRoot } };
        var boundary = Boundary(ordinaryGrid);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });

        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        Assert.Null(LookaheadRoot.TargetFor(ordinaryGrid));
        Assert.NotNull(LookaheadRoot.TargetFor(innerLeaf));
        Assert.False(f.OuterRoot.Snapshot!.Results.ContainsKey(innerLeaf));
        Assert.False(innerRoot.Snapshot!.Results.ContainsKey(boundary));

        Assert.Throws<NotSupportedException>(() =>
            LookaheadCoordinates.TargetPosition(innerLeaf, f.OuterRoot));
        Assert.Throws<NotSupportedException>(() =>
            LookaheadCoordinates.ActualPosition(innerLeaf, f.OuterRoot));
        Assert.Throws<NotSupportedException>(() =>
            LookaheadCoordinates.TargetPosition(boundary, innerRoot));

        Assert.Equal(innerRoot.Snapshot.Results[innerLeaf].Bounds.TopLeft,
            LookaheadCoordinates.TargetPosition(innerLeaf));
        Assert.Equal(f.OuterRoot.Snapshot.Results[boundary].Bounds.TopLeft,
            LookaheadCoordinates.TargetPosition(boundary));
    });

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InnerApproachRemeasureFrames_DoNotRebuildOuterTarget(bool outerAnimating) => OnUi(() =>
    {
        var leaf = new CountingLeaf();
        var innerBounds = new AnimatedBounds
        {
            Child = leaf,
            TargetSize = new Size(60, 30),
            BoundsTransform = new BoundsTransform
            {
                Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
            },
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        var innerRoot = new LookaheadRoot
        {
            Width = 160,
            Height = 120,
            Child = new LookaheadBox { Children = { innerBounds } },
            UseLayoutRounding = false
        };
        var boundary = Boundary(innerRoot);
        AnimatedBounds? outerBounds = null;
        Control outerContent;
        if (outerAnimating)
        {
            outerBounds = new AnimatedBounds
            {
                Child = boundary,
                TargetSize = new Size(180, 140),
                BoundsTransform = new BoundsTransform
                {
                    Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(100), easing: new LinearEasing())
                },
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                UseLayoutRounding = false
            };
            outerContent = new LookaheadBox { Children = { outerBounds } };
        }
        else outerContent = new LookaheadBox { Children = { boundary } };
        var animated = outerBounds is null ? new Control[] { innerBounds } : [innerBounds, outerBounds];
        using var f = new Fixture(outerContent, animated);

        innerBounds.TargetSize = new Size(120, 80);
        if (outerBounds is not null) outerBounds.TargetSize = new Size(260, 200);
        f.Layout();
        var stableOuter = f.OuterRoot.Snapshot!;
        var outerVersion = stableOuter.Version;
        var innerTarget = innerRoot.Snapshot!;
        var measures = leaf.Measures;

        // The layout manager may revisit the outer entry before the inner approach measure.
        // Dirty native measurement alone is not a changed destination in the inner domain.
        innerRoot.InvalidateMeasure();
        f.OuterRoot.InvalidateMeasure();
        f.OuterRoot.Measure(new Size(400, 300));
        Assert.Same(stableOuter, f.OuterRoot.Snapshot);

        f.Step(16);
        f.Step(25);
        Assert.True(leaf.Measures > measures);
        Assert.Same(stableOuter, f.OuterRoot.Snapshot);
        Assert.Equal(outerVersion, f.OuterRoot.Snapshot!.Version);
        Assert.Same(innerTarget, innerRoot.Snapshot);

        measures = leaf.Measures;
        f.Step(25);
        Assert.True(leaf.Measures > measures);
        Assert.Same(stableOuter, f.OuterRoot.Snapshot);
        Assert.Equal(outerVersion, f.OuterRoot.Snapshot!.Version);
        Assert.Same(innerTarget, innerRoot.Snapshot);
    });

    [Fact]
    public Task NestedDomain_MoveRemoveAndReattach_RecoversBothMemberships() => OnUi(() =>
    {
        var innerLeaf = new Border { Width = 30, Height = 20 };
        var innerRoot = new LookaheadRoot
        {
            Width = 100,
            Height = 80,
            Child = new LookaheadBox { Children = { innerLeaf } },
            UseLayoutRounding = false
        };
        var boundary = Boundary(new Grid { Children = { innerRoot } });
        var first = new LookaheadBox { Children = { boundary } };
        var second = new LookaheadBox();
        var outer = new LookaheadBox { Children = { first, second } };
        using var f = new Fixture(outer);

        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        Assert.NotNull(LookaheadRoot.TargetFor(innerLeaf));
        var innerBeforeMove = innerRoot.Snapshot;

        f.OuterRoot.MoveContent(boundary, () =>
        {
            first.Children.Remove(boundary);
            second.Children.Add(boundary);
        });
        f.Layout();
        Assert.True(f.OuterRoot.Snapshot!.Results.ContainsKey(boundary));
        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        Assert.NotNull(LookaheadRoot.TargetFor(innerLeaf));
        Assert.NotNull(innerRoot.Snapshot);
        Assert.True(innerRoot.Snapshot!.Version >= innerBeforeMove!.Version);

        second.Children.Remove(boundary);
        f.Layout();
        Assert.Null(LookaheadRoot.TargetFor(boundary));
        Assert.Null(LookaheadRoot.TargetFor(innerLeaf));
        Assert.Null(innerRoot.Snapshot);

        first.Children.Add(boundary);
        f.Layout();
        Assert.True(f.OuterRoot.Snapshot!.Results.ContainsKey(boundary));
        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        Assert.NotNull(LookaheadRoot.TargetFor(innerLeaf));
        Assert.True(innerRoot.Snapshot!.Results.ContainsKey(innerLeaf));
        Assert.Equal(innerRoot.Snapshot.Results[innerLeaf].Bounds.TopLeft,
            LookaheadCoordinates.TargetPosition(innerLeaf));
    });
}

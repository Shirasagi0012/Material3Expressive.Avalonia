using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadBoundaryTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);

    private sealed class Probe : Control
    {
        public Size LastAvailable { get; private set; }
        public Size LastArrange { get; private set; }
        public int Measures { get; private set; }
        public int Arrangements { get; private set; }
        public Size Natural { get; set; } = new(80, 40);
        public bool ThrowOnMeasure { get; set; }
        public Exception? PlacementError { get; private set; }
        public Probe()
        {
            UseLayoutRounding = false;
            HorizontalAlignment = HorizontalAlignment.Left;
            VerticalAlignment = VerticalAlignment.Top;
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            Measures++;
            LastAvailable = availableSize;
            if (ThrowOnMeasure) throw new InvalidOperationException("native measure failed");
            return Natural;
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            Arrangements++;
            LastArrange = finalSize;
            try { LookaheadPlacement.RootPosition(this); }
            catch (Exception error) { PlacementError = error; }
            return finalSize;
        }
    }

    private sealed class ExistingView : UserControl
    {
        public ExistingView()
        {
            Content = new Grid
            {
                RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star)],
                Children =
                {
                    new TextBlock { Text = "标题" },
                    new TextBox { Text = "原有内容", [Grid.RowProperty] = 1 }
                }
            };
        }
    }

    private sealed class Fixture : IDisposable
    {
        public ManualAnimationFrameClock Clock { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control child, double width = 400, double height = 300, params Control?[] clocks)
        {
            Root = new LookaheadRoot { Width = width, Height = height, Child = child, UseLayoutRounding = false };
            Window = new AnimationTestWindow(Clock) { Width = width + 100, Height = height + 100, Content = Root };
            Window.Show();
            Layout();
        }
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        public void Step(int ms) { Clock.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public Point Position(Control node) => node.TranslatePoint(default, Root)!.Value;
        public void Dispose() { Window.Close(); }
    }

    private static BoundsTransform Tween(int ms = 200) => new()
    {
        Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing())
    };

    private static LookaheadBoundary Wrap(Control child, VisibilityAlignment alignment = VisibilityAlignment.TopStart,
        Thickness? padding = null) => new()
    {
        Child = child, ContentAlignment = alignment, Padding = padding ?? default, UseLayoutRounding = false
    };

    [Fact]
    public Task OrdinaryGridAndUserControl_ProduceContentSizedTarget() => OnUi(() =>
    {
        var probe = new Probe { Natural = new Size(120, 50) };
        var boundary = Wrap(probe);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });
        Assert.True(f.Root.Snapshot!.Results.ContainsKey(boundary));
        Assert.False(f.Root.Snapshot.Results.ContainsKey(probe));
        Assert.Equal(new Size(120, 50), f.Root.Snapshot.Results[boundary].MeasuredSize);
        Assert.Null(LookaheadRoot.TargetFor(probe));
        Assert.NotNull(LookaheadRoot.TargetFor(boundary));

        var view = new ExistingView();
        var hosted = Wrap(view);
        f.Root.Child = hosted;
        f.Layout();
        Assert.True(f.Root.Snapshot!.Results.ContainsKey(hosted));
        Assert.False(f.Root.Snapshot.Results.ContainsKey(view));
        Assert.True(hosted.DesiredSize.Width > 0);
        Assert.True(hosted.DesiredSize.Height > 0);
    });

    [Fact]
    public Task AnimationFrames_KeepDestinationMeasureAndSlot_WhileBoundsChange() => OnUi(() =>
    {
        var probe = new Probe { Natural = new Size(200, 80) };
        var boundary = Wrap(probe);
        var card = new AnimatedBounds
        {
            Child = boundary, TargetSize = new Size(120, 40), BoundsTransform = Tween(),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        using var f = new Fixture(new LookaheadBox { Children = { card } }, width: 700, height: 500, clocks: [card]);
        var targetConstraints = probe.LastAvailable;
        var snapshot = f.Root.Snapshot!;
        card.TargetSize = new Size(500, 300);
        f.Layout();
        snapshot = f.Root.Snapshot!;
        var destination = probe.LastAvailable;
        Assert.Equal(new Size(500, 300), destination);
        Assert.NotEqual(targetConstraints, destination);
        var measures = probe.Measures;
        f.Step(16); f.Step(100);
        Assert.Equal(destination, probe.LastAvailable);
        Assert.InRange(card.CurrentBounds!.Value.Width, 120.01, 499.99);
        Assert.InRange(card.CurrentBounds.Value.Height, 40.01, 299.99);
        Assert.Equal(card.CurrentBounds.Value.Size, boundary.Bounds.Size);
        Assert.Equal(new Size(500, 300), card.Bounds.Size);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Step(100);
        Assert.Equal(destination, probe.LastAvailable);
        Assert.Equal(new Size(500, 300), card.Bounds.Size);
        Assert.True(probe.Measures >= measures);
    });

    [Fact]
    public Task StackTarget_DoesNotUseMeasureMaxAsArrangeSlot() => OnUi(() =>
    {
        var probe = new Probe();
        var boundary = Wrap(probe);
        using var f = new Fixture(new LookaheadStackPanel { Children = { boundary, new Border { Width = 10, Height = 10 } } });
        Assert.True(double.IsPositiveInfinity(probe.LastAvailable.Height));
        Assert.Equal(40, probe.LastArrange.Height);
        var record = (LookaheadBoundary.TargetRecord)f.Root.Snapshot!.Results[boundary].LayoutData!;
        Assert.True(double.IsPositiveInfinity(record.ContentConstraints.Height));
        Assert.Equal(40, record.ContentSlot!.Value.Height);
        Assert.False(double.IsInfinity(record.ContentSlot.Value.Width));
        Assert.NotEqual(record.ContentConstraints, record.ContentSlot);
    });

    [Theory]
    [InlineData(VisibilityAlignment.TopStart, 0d, 0d, false)]
    [InlineData(VisibilityAlignment.Center, 0.5, 0.5, false)]
    [InlineData(VisibilityAlignment.BottomEnd, 1d, 1d, false)]
    [InlineData(VisibilityAlignment.TopStart, 1d, 0d, true)]
    public Task AlignmentAndPadding_OffsetDestinationSlot(VisibilityAlignment alignment, double x, double y, bool rtl)
        => OnUi(() =>
    {
        var probe = new Probe { Natural = new Size(80, 40) };
        var boundary = Wrap(probe, alignment, new Thickness(10));
        if (rtl) boundary.FlowDirection = FlowDirection.RightToLeft;
        var card = new AnimatedBounds
        {
            Child = boundary, TargetSize = new Size(80, 40), BoundsTransform = Tween(200),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        using var f = new Fixture(new LookaheadBox { Children = { card } }, clocks: [card]);
        card.TargetSize = new Size(200, 120);
        f.Layout();
        var slot = ((LookaheadBoundary.TargetRecord)f.Root.Snapshot!.Results[boundary].LayoutData!).ContentSlot!.Value;
        var measures = probe.Measures;
        var arrangements = probe.Arrangements;
        f.Step(16); f.Step(100);
        Assert.Equal(measures, probe.Measures);
        Assert.Equal(arrangements, probe.Arrangements);
        var padding = new Thickness(10);
        var current = boundary.Bounds.Size.Deflate(padding);
        var offset = new Point(
            padding.Left + (current.Width - slot.Width) * x,
            padding.Top + (current.Height - slot.Height) * y);
        var actual = probe.TranslatePoint(default, boundary)!.Value;
        Assert.Equal(offset.X, actual.X, 5);
        Assert.Equal(offset.Y, actual.Y, 5);
        Assert.True(double.IsNaN(probe.Width));
        Assert.True(boundary.ClipToBounds);
    });

    [Fact]
    public Task ZeroAndFractionalPadding_AllowNegativeOffsetWithoutPatchingChildSize() => OnUi(() =>
    {
        var probe = new Probe { Width = 160, Height = 90, Natural = new Size(160, 90) };
        var boundary = Wrap(probe, VisibilityAlignment.Center, new Thickness(1.5));
        boundary.UseLayoutRounding = true;
        var card = new AnimatedBounds
        {
            Child = boundary, TargetSize = new Size(20, 10), BoundsTransform = Tween(),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
        };
        using var f = new Fixture(new LookaheadBox { Children = { card } }, clocks: [card]);
        card.TargetSize = new Size(200, 140);
        f.Layout();
        f.Step(16); f.Step(100);
        Assert.Equal(160, probe.Width);
        Assert.Equal(90, probe.Height);
        var position = probe.TranslatePoint(default, boundary)!.Value;
        Assert.True(position.X < 0 || position.Y < 0);
        Assert.True(card.Bounds.Width >= 0);
        Assert.True(card.Bounds.Height >= 0);
    });

    [Fact]
    public Task ContentChange_RebuildsTarget_AnimationFramesDoNot() => OnUi(() =>
    {
        var probe = new Probe();
        var boundary = Wrap(probe);
        var card = new AnimatedBounds
        {
            Child = boundary, TargetSize = new Size(200, 120), BoundsTransform = Tween(),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        using var f = new Fixture(new LookaheadBox { Children = { card } }, clocks: [card]);
        var snapshot = f.Root.Snapshot!;
        var version = snapshot.Version;
        card.TargetSize = new Size(320, 180);
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version > version);
        snapshot = f.Root.Snapshot;
        f.Step(16); f.Step(50);
        Assert.Same(snapshot, f.Root.Snapshot);
        probe.Natural = new Size(140, 70);
        probe.InvalidateMeasure();
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version > snapshot.Version);
        Assert.Equal(new Size(140, 70), f.Root.Snapshot.Results[boundary].MeasuredSize);
    });

    [Fact]
    public Task LayoutManagerRemeasure_DoesNotDropTargetInvalidation() => OnUi(() =>
    {
        var probe = new Probe();
        var boundary = Wrap(probe);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });
        var snapshot = f.Root.Snapshot!;
        probe.Natural = new Size(150, 60);
        probe.InvalidateMeasure();
        probe.Measure(new Size(400, 300));
        Assert.False(boundary.IsMeasureValid);
        Assert.Same(snapshot, f.Root.Snapshot);
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version > snapshot.Version);
        Assert.Equal(new Size(150, 60), f.Root.Snapshot.Results[boundary].MeasuredSize);
    });

    [Fact]
    public Task ChildReplaceRemoveAndNativeThrow_DoNotPublishPartialTarget() => OnUi(() =>
    {
        var first = new Probe();
        var boundary = Wrap(first);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });
        var version = f.Root.Snapshot!.Version;
        var second = new Probe { Natural = new Size(40, 20) };
        boundary.Child = second;
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version > version);
        Assert.False(f.Root.Snapshot.Results.ContainsKey(first));
        Assert.False(f.Root.Snapshot.Results.ContainsKey(second));
        Assert.Equal(new Size(40, 20), f.Root.Snapshot.Results[boundary].MeasuredSize);

        boundary.Child = null;
        f.Layout();
        Assert.Equal(default, f.Root.Snapshot!.Results[boundary].MeasuredSize);

        var failing = new Probe { ThrowOnMeasure = true };
        var error = Assert.ThrowsAny<Exception>(() =>
        {
            boundary.Child = failing;
            f.Layout();
        });
        Assert.Contains("LookaheadBoundary", error.Message + error.InnerException?.Message);
        Assert.Contains("Probe", error.ToString());
        Assert.Null(f.Root.Snapshot);
        Assert.False(boundary.IsMeasureValid);
        failing.ThrowOnMeasure = false;
        f.Layout();
        Assert.NotNull(f.Root.Snapshot);
        Assert.Equal(new Size(80, 40), f.Root.Snapshot!.Results[boundary].MeasuredSize);
    });

    [Fact]
    public Task InnerContent_CannotUseOuterTargetCoordinates_BoundaryStillCan() => OnUi(() =>
    {
        var probe = new Probe();
        var boundary = Wrap(probe);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });
        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        var position = LookaheadCoordinates.TargetPosition(boundary);
        Assert.Equal(f.Root.Snapshot!.Results[boundary].Bounds.TopLeft, position);
        Assert.Null(LookaheadRoot.TargetFor(probe));
        var inner = Assert.Throws<NotSupportedException>(() => LookaheadCoordinates.TargetPosition(probe));
        Assert.Contains("LookaheadBoundary", inner.Message);
        Assert.NotNull(probe.PlacementError);
        Assert.Contains("placement", probe.PlacementError!.Message, StringComparison.OrdinalIgnoreCase);
        var innerBounds = new AnimatedBounds { Child = Wrap(new Probe { Natural = new Size(20, 10) }), TargetSize = new Size(20, 10) };
        boundary.Child = innerBounds;
        f.Layout();
        Assert.False(innerBounds.IsRunning);
        Assert.NotNull(LookaheadRoot.TargetFor(innerBounds));
        Assert.False(f.Root.Snapshot!.Results.ContainsKey(innerBounds));
    });

    [Fact]
    public Task NestedLookaheadRoot_UsesIndependentDomain() => OnUi(() =>
    {
        var innerLeaf = new Border { Width = 20, Height = 10 };
        var nested = new LookaheadRoot { Width = 80, Height = 40, Child = innerLeaf };
        var boundary = Wrap(nested);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });

        Assert.True(f.Root.Snapshot!.Results.ContainsKey(boundary));
        Assert.False(f.Root.Snapshot.Results.ContainsKey(nested));
        Assert.False(f.Root.Snapshot.Results.ContainsKey(innerLeaf));
        Assert.True(nested.Snapshot!.Results.ContainsKey(innerLeaf));
        Assert.Same(f.Root.Snapshot.Results[boundary], LookaheadRoot.TargetFor(boundary));
        Assert.Same(nested.Snapshot.Results[innerLeaf], LookaheadRoot.TargetFor(innerLeaf));
    });

    [Fact]
    public Task WithoutRoot_OrdinaryDecoratorLayout_LeaveDoesNotReuseOldTarget() => OnUi(() =>
    {
        var probe = new Probe();
        var boundary = Wrap(probe, VisibilityAlignment.Center, new Thickness(8));
        Assert.True(boundary.ClipToBounds);
        var window = new AnimationTestWindow() { Width = 300, Height = 200, Content = boundary };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Assert.Equal(new Size(80, 40), probe.DesiredSize);
        Assert.True(probe.LastAvailable.Width >= 80);
        Assert.Equal(new Point(8, 8), probe.TranslatePoint(default, boundary));
        Assert.Null(LookaheadRoot.TargetFor(boundary));
        window.Content = null;
        window.Close();

        var box = new LookaheadBox { Children = { boundary } };
        using var f = new Fixture(box);
        var first = f.Root.Snapshot!.Version;
        Assert.Equal(new Size(96, 56), f.Root.Snapshot.Results[boundary].MeasuredSize);
        var generation = f.Root.Snapshot.Version;
        box.Children.Remove(boundary);
        f.Root.Child = new Border { Width = 10, Height = 10 };
        f.Layout();
        Assert.Null(LookaheadRoot.TargetFor(boundary));
        var host = new LookaheadBox { Children = { boundary } };
        f.Root.Child = host;
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version >= first);
        Assert.NotEqual(generation, f.Root.Snapshot.Version);
        Assert.NotNull(LookaheadRoot.TargetFor(boundary));
        Assert.Equal(new Size(96, 56), f.Root.Snapshot.Results[boundary].MeasuredSize);
    });

    [Fact]
    public Task SharedBoundsEndpoints_KeepIndependentDestinationContent_AndFlightCaptureSize() => OnUi(() =>
    {
        var clock = new ManualAnimationFrameClock();
        var parent = new Transition<bool>(true, clock);
        var fromProbe = new Probe { Natural = new Size(80, 40) };
        var toProbe = new Probe { Natural = new Size(160, 80) };
        SharedBounds Endpoint(Probe probe, Size size) => new()
        {
            Key = "dialog", Content = Wrap(probe), ResizeMode = SharedBoundsResizeMode.RemeasureToBounds,
            UseLayoutRounding = false,
            BoundsTransform = new SharedBoundsTransform { Spec = new TweenSpec<Rect>(TimeSpan.FromMilliseconds(200), easing: new LinearEasing()) },
            FadeSpec = new TweenSpec<double>(TimeSpan.FromMilliseconds(200), easing: new LinearEasing()),
            Width = size.Width, Height = size.Height
        };
        var from = Endpoint(fromProbe, new Size(120, 80));
        var to = Endpoint(toProbe, new Size(240, 160));
        var fromRoot = new LookaheadRoot { Width = 120, Height = 80, Child = from, UseLayoutRounding = false };
        var toRoot = new LookaheadRoot { Width = 240, Height = 160, Child = to, UseLayoutRounding = false };
        var source = new AnimatedVisibility
        {
            Content = fromRoot, Source = VisibilitySource.From(parent, state => state),
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };
        var target = new AnimatedVisibility
        {
            Content = toRoot, Source = VisibilitySource.From(parent, state => !state),
            Enter = EnterTransition.None, Exit = ExitTransition.None
        };
        var stage = new Canvas();
        Canvas.SetLeft(source, 10); Canvas.SetTop(source, 20);
        Canvas.SetLeft(target, 250); Canvas.SetTop(target, 100);
        stage.Children.Add(source); stage.Children.Add(target);
        var host = new SharedTransitionHost
        {
            Width = 700, Height = 400, Transition = parent,
            PreparationTimeout = Timeout.InfiniteTimeSpan, Content = stage
        };
        var window = new AnimationTestWindow(clock) { Width = 700, Height = 400, Content = host };
        window.Show();
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        parent.OnFrame(TimeSpan.Zero);
        Assert.Null(LookaheadRoot.TargetFor(fromProbe));
        Assert.NotNull(LookaheadRoot.TargetFor((Control)from.Content!));
        parent.UpdateTarget(false);
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        var motion = Assert.Single(host.Coordinator!.Motions);
        parent.OnFrame(TimeSpan.FromMilliseconds(0));
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        var fromSnapshot = fromRoot.Snapshot;
        var toSnapshot = toRoot.Snapshot;
        parent.OnFrame(TimeSpan.FromMilliseconds(100));
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        var sample = motion.Bounds.Value;
        Assert.Equal(sample.Size, from.Capture!.Bounds.Size);
        Assert.Equal(sample.Size, to.Capture!.Bounds.Size);
        Assert.Equal(fromProbe.Natural, fromProbe.LastArrange);
        Assert.Equal(toProbe.Natural, toProbe.LastArrange);
        Assert.NotEqual(sample.Size, fromProbe.LastArrange);
        Assert.Same(fromSnapshot, fromRoot.Snapshot);
        Assert.Same(toSnapshot, toRoot.Snapshot);
        var item = Assert.Single(host.Overlay!.Items);
        Assert.Equal(Stretch.None, item.SourceBrush!.Stretch);
        Assert.Equal(Stretch.None, item.TargetBrush.Stretch);
        window.Close();
        parent.Dispose();
    });

    [Fact]
    public Task InvalidateTargetMeasure_RebuildsWithoutTreatingAlignmentAsTargetInput() => OnUi(() =>
    {
        var probe = new Probe();
        var boundary = Wrap(probe);
        using var f = new Fixture(new LookaheadBox { Children = { boundary } });
        var snapshot = f.Root.Snapshot!;
        boundary.ContentAlignment = VisibilityAlignment.Center;
        f.Layout();
        Assert.Same(snapshot, f.Root.Snapshot);
        boundary.ClipToBounds = false;
        f.Layout();
        Assert.Same(snapshot, f.Root.Snapshot);
        boundary.InvalidateTargetMeasure();
        f.Layout();
        Assert.True(f.Root.Snapshot!.Version > snapshot.Version);
    });
}

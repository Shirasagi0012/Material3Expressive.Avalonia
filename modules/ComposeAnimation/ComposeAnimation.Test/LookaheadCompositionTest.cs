using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test;

public class LookaheadCompositionTest
{
    private static Task OnUi(Action body) => HeadlessApp.Session.Dispatch(body, CancellationToken.None);
    private static TweenSpec<T> Tween<T>(int ms) => new(TimeSpan.FromMilliseconds(ms), easing: new LinearEasing());

    private sealed class Fixture : IDisposable
    {
        public ManualFrameSource Frames { get; } = new();
        public LookaheadRoot Root { get; }
        public Window Window { get; }
        public Fixture(Control child)
        {
            Root = new LookaheadRoot { Width = 500, Height = 400, Child = child, UseLayoutRounding = false };
            SetClock(child);
            Window = new AnimationTestWindow(Frames) { Width = 600, Height = 500, Content = Root };
            Window.Show(); Layout();
            void SetClock(Control node)
            {

                if (node is ILookaheadLayoutNode policy)
                    foreach (var nested in policy.LookaheadChildren) SetClock(nested);
            }
        }
        public Point Position(Control node) => node.TranslatePoint(default, Root)!.Value;
        public void Layout() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); }
        public void Step(int ms) { Frames.AdvanceBy(TimeSpan.FromMilliseconds(ms)); Layout(); }
        public void Dispose() { Window.Close(); }
    }

    private sealed class MeasuredText : LookaheadTextBlock
    {
        public Size LastConstraints { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            LastConstraints = availableSize;
            return base.MeasureOverride(availableSize);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SizeAndPosition_BothOrders_RemeasureTextAndRetargetWithoutDirtyingPrediction(bool positionInside) => OnUi(() =>
    {
        var text = new MeasuredText
        {
            Text = string.Join(" ", Enumerable.Repeat("Lookahead measures real text during motion.", 6)),
            TextWrapping = TextWrapping.Wrap, FontSize = 16, UseLayoutRounding = false
        };
        var size = new LookaheadSize
        {
            TargetSize = new Size(140, 180), AnimationSpec = Tween<Size>(200),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        var position = new LookaheadPosition { AnimationSpec = Tween<Point>(100), UseLayoutRounding = false };
        Control outer;
        if (positionInside) { size.Child = position; position.Child = text; outer = size; }
        else { position.Child = size; size.Child = text; outer = position; }
        var host = new OffsetPanel { Children = { outer } };
        using var f = new Fixture(host);
        Assert.Equal(new Size(140, 180), text.LastConstraints);
        var oldLines = text.TextLayout.TextLines.Count;
        host.Offset = new Point(100, 60);
        size.TargetSize = new Size(300, 100);
        f.Layout();
        var target = f.Root.Snapshot!;
        Assert.Equal(new Size(300, 100), target.Results[size].Bounds.Size);
        Assert.Equal(default, f.Position(position));
        f.Step(16); f.Step(50);
        Assert.Equal(new Size(180, 160), size.Bounds.Size);
        Assert.Equal(new Size(180, 160), text.LastConstraints);
        Assert.Equal(new Point(50, 30), f.Position(position));
        Assert.Same(target, f.Root.Snapshot);
        f.Step(50);
        Assert.Equal(new Point(100, 60), f.Position(position));
        Assert.Equal(new Size(220, 140), text.LastConstraints);
        f.Step(50);
        Assert.Equal(new Point(100, 60), f.Position(position));
        Assert.True(text.TextLayout.TextLines.Count < oldLines);
        Assert.Same(target, f.Root.Snapshot);

        var before = size.Bounds.Size;
        host.Offset = new Point(40, 20);
        size.TargetSize = new Size(180, 160);
        text.Text += " New content while moving.";
        f.Layout();
        var retarget = f.Root.Snapshot!;
        Assert.NotSame(target, retarget);
        Assert.Equal(before, size.Bounds.Size);
        Assert.Equal(new Point(100, 60), f.Position(position));
        f.Step(50);
        Assert.Equal(before.Width + (180 - before.Width) / 4, size.Bounds.Width, 5);
        // The completed position run starts with its own first frame after the idle gap.
        f.Step(200);
        Assert.Equal(retarget.Results[size].Bounds.Size, size.Bounds.Size);
        Assert.Equal(retarget.Results[position].Bounds.TopLeft, f.Position(position));
        Assert.Equal(new Size(180, 160), text.LastConstraints);
        Assert.Same(retarget, f.Root.Snapshot);
        Assert.Null(position.RenderTransform);
    });

    [Fact]
    public Task AnimatedContentSize_TemplateBridge_CompensatesAlignmentWhileParentContinuesResizing() => OnUi(() =>
    {
        var leaf = new Border { Width = 80, Height = 40 };
        var child = new LookaheadPosition { Child = leaf, AnimationSpec = Tween<Point>(100), UseLayoutRounding = false };
        var content = new AnimatedContentSize
        {
            Content = child, ContentAlignment = VisibilityAlignment.Center,
            AnimationSpec = Tween<Size>(200), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top, UseLayoutRounding = false, ClipToBounds = false
        };
        var parent = new LookaheadPosition { Child = content, AnimationSpec = Tween<Point>(200), UseLayoutRounding = false };
        var host = new OffsetPanel { Children = { parent } };
        using var f = new Fixture(host);
        host.Offset = new Point(100, 60);
        leaf.Width = 240; leaf.Height = 120;
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        f.Step(16); f.Step(50);
        Assert.Equal(new Size(120, 60), content.Bounds.Size);
        Assert.Equal(new Size(240, 120), leaf.Bounds.Size); // ACS retains upstream measurement semantics.
        Assert.Equal(new Point(25, 15), f.Position(parent));
        Assert.Equal(new Point(50, 30), f.Position(child));
        f.Step(50);
        Assert.Equal(new Point(100, 60), f.Position(child));
        f.Step(50);
        Assert.Equal(new Point(100, 60), f.Position(child));
        Assert.Equal(75, f.Position(parent).X, 5);
        Assert.Equal(45, f.Position(parent).Y, 5);
        f.Step(50);
        Assert.Equal(snapshot.Results[child].Bounds.TopLeft, f.Position(child));
        Assert.Equal(snapshot.Results[content].Bounds.Size, content.Bounds.Size);
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task CustomProtocol_ProjectedCollectionPropertiesExternalInputsAndUnsubscribe() => OnUi(() =>
    {
        var first = new LookaheadPosition { Child = new Border { Width = 30, Height = 20 }, AnimationSpec = Tween<Point>(100) };
        var host = new OffsetPanel { Children = { first } };
        using var f = new Fixture(host);
        var snapshot = f.Root.Snapshot!;
        var second = new Border { Width = 50, Height = 25 };
        host.Children.Add(second); f.Layout();
        Assert.NotSame(snapshot, f.Root.Snapshot);
        Assert.True(f.Root.Snapshot!.Results.ContainsKey(second));
        host.InvalidateMeasure();
        host.Offset = new Point(40, 30); // must notify even while already measure-invalid
        f.Layout();
        Assert.Equal(new Point(40, 30), f.Root.Snapshot.Results[first].Bounds.TopLeft);
        host.SetExternalOffset(new Point(70, 50)); f.Layout();
        var target = f.Root.Snapshot;
        Assert.Equal(new Point(70, 50), target!.Results[first].Bounds.TopLeft);
        f.Step(16); f.Step(100);
        Assert.Equal(new Point(70, 50), f.Position(first));
        Assert.Same(target, f.Root.Snapshot);
        second.IsVisible = false; f.Layout();
        Assert.Equal(default, f.Root.Snapshot!.Results[second].DesiredSize);
        host.Children.Remove(second); f.Layout();
        target = f.Root.Snapshot;
        second.Width = 90; f.Layout();
        Assert.Same(target, f.Root.Snapshot);
        f.Root.Child = new Border { Width = 10, Height = 10 }; f.Layout();
        target = f.Root.Snapshot;
        host.SetExternalOffset(default); host.Children.Clear(); f.Layout();
        Assert.Same(target, f.Root.Snapshot);
    });

    [Fact]
    public Task ResizingStackChild_SiblingPositionFinishesBeforeItsActualSlotStopsChanging() => OnUi(() =>
    {
        var size = new LookaheadSize
        {
            TargetSize = new Size(100, 40), Child = new LookaheadTextBlock { Text = "Resize" },
            AnimationSpec = Tween<Size>(200), HorizontalAlignment = HorizontalAlignment.Left,
            UseLayoutRounding = false
        };
        var sibling = new LookaheadPosition
        {
            Child = new Border { Width = 40, Height = 20 }, AnimationSpec = Tween<Point>(100),
            HorizontalAlignment = HorizontalAlignment.Left, UseLayoutRounding = false
        };
        var stack = new LookaheadStackPanel { Spacing = 10, Children = { size, sibling }, UseLayoutRounding = false };
        using var f = new Fixture(stack);
        Assert.Equal(new Point(0, 50), f.Position(sibling));
        size.TargetSize = new Size(200, 140); f.Layout();
        var target = f.Root.Snapshot!;
        Assert.Equal(new Point(0, 150), target.Results[sibling].Bounds.TopLeft);
        f.Step(16); f.Step(50);
        Assert.Equal(65, size.Bounds.Height, 5);
        Assert.Equal(100, f.Position(sibling).Y, 5);
        f.Step(50); f.Step(50);
        Assert.Equal(115, size.Bounds.Height, 5);
        Assert.Equal(150, f.Position(sibling).Y, 5);
        Assert.Same(target, f.Root.Snapshot);
        f.Step(50);
        Assert.Equal(target.Results[size].Bounds.Size, size.Bounds.Size);
        Assert.Equal(target.Results[sibling].Bounds.TopLeft, f.Position(sibling));
    });

    [Fact]
    public Task NaturalSize_UsesIndependentTargetAndTracksViewportConstraints() => OnUi(() =>
    {
        var text = new MeasuredText { Text = "Short", TextWrapping = TextWrapping.Wrap, FontSize = 16, UseLayoutRounding = false };
        var size = new LookaheadSize
        {
            Child = text, AnimationSpec = Tween<Size>(100),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            UseLayoutRounding = false
        };
        using var f = new Fixture(size);
        var initial = size.Bounds.Size;
        text.Text = string.Join(" ", Enumerable.Repeat("A much longer piece of text", 8));
        f.Layout();
        var target = f.Root.Snapshot!;
        var destination = target.Results[size].Bounds.Size;
        Assert.True(destination.Width > initial.Width);
        f.Step(16); f.Step(50);
        Assert.Equal((initial.Width + destination.Width) / 2, text.LastConstraints.Width, 5);
        Assert.Same(target, f.Root.Snapshot);
        f.Root.Width = 180; f.Layout();
        var retarget = f.Root.Snapshot!;
        Assert.True(size.Bounds.Width <= 180);
        f.Step(100);
        Assert.Equal(retarget.Results[size].Bounds.Size, size.Bounds.Size);
        Assert.Same(retarget, f.Root.Snapshot);
    });


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SizeApproach_FailedTargetDoesNotRestartEachLayout(bool cancellation) => OnUi(() =>
    {
        var error = cancellation ? (Exception)new OperationCanceledException("spec") : new InvalidOperationException("spec");
        var spec = new BrokenSpec(error);
        var size = new LookaheadSize
        {
            TargetSize = new Size(100, 80), AnimationSpec = spec,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
        };
        using var f = new Fixture(size);
        size.TargetSize = new Size(200, 160); f.Layout();
        Assert.Same(error, ((AnimationTestWindow)TopLevel.GetTopLevel(size)!).AnimationRoot.Diagnostics.LastError);
        Assert.Equal(new Size(200, 160), size.Bounds.Size);
        var calls = spec.Calls;
        var snapshot = f.Root.Snapshot;
        for (var i = 0; i < 3; i++) { size.InvalidateMeasure(); f.Layout(); }
        Assert.Equal(calls, spec.Calls);
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.Equal(0, f.Frames.PendingFrameCount);
        size.AnimationSpec = Tween<Size>(100); f.Layout();
        size.TargetSize = new Size(100, 80); f.Layout(); f.Step(16); f.Step(50);
        Assert.Equal(new Size(150, 120), size.Bounds.Size);
    });

    private sealed class BrokenSpec(Exception error) : IFiniteAnimationSpec<Size>
    {
        public int Calls { get; private set; }
        public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<Size> converter) { Calls++; throw error; }
    }

    [Fact]
    public Task SizeApproach_HideDetachAndOutsideScope_ReleaseFramesAndInitializeAtTarget() => OnUi(() =>
    {
        var size = new LookaheadSize
        {
            TargetSize = new Size(100, 80), Child = new LookaheadTextBlock { Text = "Text" },
            AnimationSpec = Tween<Size>(100), HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top
        };
        using var f = new Fixture(size);
        size.TargetSize = new Size(200, 160); f.Layout(); f.Step(16); f.Step(50);
        Assert.True(f.Frames.PendingFrameCount > 0);
        size.IsVisible = false; f.Layout();
        Assert.Equal(0, f.Frames.PendingFrameCount);
        size.IsVisible = true; f.Layout();
        Assert.Equal(new Size(200, 160), size.Bounds.Size);
        size.TargetSize = new Size(100, 80); f.Layout(); f.Step(16);
        f.Window.Content = null;
        Assert.Equal(0, f.Frames.PendingFrameCount);
        f.Root.Child = null;
        f.Window.Content = size; f.Layout();
        Assert.Equal(new Size(100, 80), size.Bounds.Size);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    // A deliberately different policy, declared entirely through the public extension protocol.
    // Test-only offset container; LookaheadBox is the stacking reference implementation.
    private sealed class OffsetPanel : Panel, ILookaheadLayoutNode, ILookaheadPlacementHost, ILookaheadTargetInvalidationSource
    {
        public static readonly StyledProperty<Point> OffsetProperty = AvaloniaProperty.Register<OffsetPanel, Point>(nameof(Offset));
        public Point Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }
        private Point? _external;
        public event EventHandler? TargetMeasureInvalidated;
        static OffsetPanel() => AffectsMeasure<OffsetPanel>(OffsetProperty);
        public void SetExternalOffset(Point offset) { _external = offset; InvalidateMeasure(); TargetMeasureInvalidated?.Invoke(this, EventArgs.Empty); }
        public IEnumerable<Control> LookaheadChildren => Children.Select(c => c);
        public INotifyCollectionChanged LookaheadChildrenChanged => Children;
        public IEnumerable<AvaloniaProperty> LookaheadProperties => [OffsetProperty];
        private Size MeasureChildren(Size available, Func<Control, Size, Size> measure)
        {
            foreach (var child in Children) measure(child, available);
            return available;
        }
        protected override Size MeasureOverride(Size availableSize) => MeasureChildren(availableSize, (child, constraints) =>
        {
            child.Measure(constraints); return child.DesiredSize;
        });
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available) => MeasureChildren(available, builder.Measure);
        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (var child in Children) LookaheadPlacement.ArrangeChild(this, child, new Rect(_external ?? Offset, child.DesiredSize));
            return finalSize;
        }
        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        {
            var offset = _external ?? Offset;
            foreach (var child in Children) builder.Place(child, new Rect(bounds.TopLeft + new Vector(offset.X, offset.Y), builder.Desired(child)));
        }
    }
}

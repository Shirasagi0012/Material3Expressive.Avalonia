using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;

namespace ComposeAnimation.Test;

public partial class AnimatedBoundsTest
{
    private static LookaheadScrollViewport Scroll(Control content, double height = 200) => new()
    {
        Width = 300, Height = height, Child = content,
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        UseLayoutRounding = false
    };

    [Fact]
    public Task MotionFrame_DefaultScrollFollowsImmediatelyWithoutAnimationOrTargetRebuild() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var policy = new RecordingTransform { Spec = Tween().Spec };
        card.BoundsTransform = policy;
        var content = new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false };
        var scroll = Scroll(content);
        using var f = new Fixture(scroll);
        var snapshot = f.Root.Snapshot!;
        var measures = text.MeasureCount;
        scroll.Offset = new Vector(0, 80); f.Layout();
        Equal(new Rect(0, -80, 120, 160), f.Actual(card));
        Equal(f.Actual(card), f.Actual(text));
        Assert.Equal(new Point(0, -80), LookaheadCoordinates.TargetPosition(card));
        Assert.Equal(default, LookaheadCoordinates.TargetPosition(card, includeMotionFrameOfReference: false));
        Assert.Equal(default, LookaheadCoordinates.ActualPosition(card, includeMotionFrameOfReference: false));
        Assert.Equal(measures, text.MeasureCount);
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.Empty(policy.Calls);
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task MotionFrame_IncludedScrollRetargetsFromCurrentRectangleWithoutRebuildingTarget() => OnUi(() =>
    {
        var card = Bounds(Text());
        card.AnimateMotionFrameOfReference = true;
        var policy = new RecordingTransform { Spec = Tween().Spec };
        card.BoundsTransform = policy;
        var scroll = Scroll(new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false });
        using var f = new Fixture(scroll);
        var snapshot = f.Root.Snapshot!;
        scroll.Offset = new Vector(0, 80); f.Layout();
        Equal(new Rect(0, 0, 120, 160), f.Actual(card));
        f.Step(16); f.Step(100);
        Equal(new Rect(0, -40, 120, 160), f.Actual(card));
        scroll.Offset = new Vector(0, 120); f.Layout();
        Equal(new Rect(0, -40, 120, 160), f.Actual(card));
        Assert.Equal(4, policy.Calls.Count);
        Assert.Equal(-40, policy.Calls[2].From.Y);
        Assert.Equal(-120, policy.Calls[2].To.Y);
        f.Step(100);
        Equal(new Rect(0, -80, 120, 160), f.Actual(card));
        f.Step(100);
        Equal(new Rect(0, -120, 120, 160), f.Actual(card));
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Fact]
    public Task MotionFrame_ExcludedScrollAndBoundsResizeComposeThroughoutFlight() => OnUi(() =>
    {
        var text = Text();
        var card = Bounds(text);
        var scroll = Scroll(new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false });
        using var f = new Fixture(scroll);
        card.TargetSize = new Size(240, 100); card.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout(); f.Step(16); f.Step(100);
        var snapshot = f.Root.Snapshot!;
        scroll.Offset = new Vector(0, 80); f.Layout();
        Equal(new Rect(30, -80, 180, 130), f.Actual(card));
        Assert.Equal(new Size(180, 130), text.LastConstraints);
        scroll.Offset = new Vector(0, 120); f.Step(100);
        Equal(new Rect(60, -120, 240, 100), f.Actual(card));
        Equal(f.Actual(card), f.Actual(text));
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task ReferenceScope_AncestorAnimationFollowsCurrentArrangeOriginWithoutStartingChildRun() => OnUi(() =>
    {
        var child = Bounds(Text()); child.TargetSize = new Size(60, 40);
        var policy = new RecordingTransform { Spec = Tween(400).Spec };
        child.BoundsTransform = policy;
        var scope = new LookaheadBox { Children = { child }, UseLayoutRounding = false };
        child.ReferenceScope = scope;
        var parent = Bounds(scope); parent.TargetSize = new Size(180, 160);
        using var f = new Fixture(new LookaheadBox { Children = { parent }, UseLayoutRounding = false });
        parent.HorizontalAlignment = HorizontalAlignment.Right; parent.VerticalAlignment = VerticalAlignment.Bottom;
        f.Layout(); f.Step(16); f.Step(100);
        Equal(new Rect(110, 70, 60, 40), f.Actual(child));
        Assert.Equal(default, LookaheadCoordinates.ActualPosition(child, scope));
        Assert.Empty(policy.Calls);
        Assert.False(child.IsRunning);
        f.Step(100);
        Equal(new Rect(220, 140, 60, 40), f.Actual(child));
        child.HorizontalAlignment = HorizontalAlignment.Right; f.Layout(); f.Step(16); f.Step(200);
        Assert.True(child.IsRunning);
        Assert.Equal(280, f.Actual(child).X, 5);
        f.Step(200);
        Assert.Equal(340, f.Actual(child).X, 5);
    });

    [Fact]
    public Task ReferenceScope_ScrollingOutsideSelectedScopeFollowsEvenWhenMotionIsIncluded() => OnUi(() =>
    {
        var card = Bounds(Text()); card.AnimateMotionFrameOfReference = true;
        var content = new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false };
        card.ReferenceScope = content;
        var scroll = Scroll(content);
        using var f = new Fixture(scroll);
        scroll.Offset = new Vector(0, 90); f.Layout();
        Equal(new Rect(0, -90, 120, 160), f.Actual(card));
        Assert.Equal(default, LookaheadCoordinates.TargetPosition(card, content));
        Assert.Equal(default, LookaheadCoordinates.ActualPosition(card, content));
        Assert.False(card.IsRunning);
    });

    [Fact]
    public Task NestedMotionFrames_AccumulateOnlyBelowSelectedReference() => OnUi(() =>
    {
        var card = Bounds(Text()); card.AnimateMotionFrameOfReference = true;
        var inner = Scroll(new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false }, 150);
        card.ReferenceScope = inner;
        var outer = Scroll(new LookaheadStackPanel
        {
            Children = { inner, new Border { Width = 10, Height = 500 } }, UseLayoutRounding = false
        });
        using var f = new Fixture(outer);
        outer.Offset = new Vector(0, 30); inner.Offset = new Vector(0, 80); f.Layout();
        Equal(new Rect(0, -30, 120, 160), f.Actual(card));
        f.Step(16); f.Step(100);
        Equal(new Rect(0, -70, 120, 160), f.Actual(card));
        Assert.Equal(new Point(0, -80), LookaheadCoordinates.TargetPosition(card, inner));
        Assert.Equal(new Point(0, -110), LookaheadCoordinates.TargetPosition(card));
        outer.Offset = new Vector(0, 60); f.Layout();
        Equal(new Rect(0, -100, 120, 160), f.Actual(card));
        f.Step(100);
        Equal(new Rect(0, -140, 120, 160), f.Actual(card));
        Assert.False(card.IsRunning);
    });

    [Fact]
    public Task CoordinateConfigurationChanges_ResetMotionWithoutRebuildingTarget() => OnUi(() =>
    {
        var card = Bounds(Text());
        var content = new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false };
        var scroll = Scroll(content);
        using var f = new Fixture(scroll);
        card.TargetSize = new Size(240, 100); f.Layout(); f.Step(16); f.Step(50);
        var snapshot = f.Root.Snapshot!;
        card.ReferenceScope = content; f.Layout();
        Assert.False(card.IsRunning);
        Equal(snapshot.Results[card].Bounds, f.Actual(card));
        card.AnimateMotionFrameOfReference = true; f.Layout();
        Assert.Same(snapshot, f.Root.Snapshot);
        card.ReferenceScope = card;
        Assert.Throws<NotSupportedException>(() => f.Layout());
        Assert.False(card.IsRunning);
        card.ReferenceScope = new LookaheadBox();
        Assert.Throws<NotSupportedException>(() => f.Layout());
        card.ReferenceScope = f.Root; f.Layout();
        Equal(snapshot.Results[card].Bounds, f.Actual(card));
    });

    [Fact]
    public Task ScrollViewport_ClampsOffsetAndUnsubscribesOnRemoval() => OnUi(() =>
    {
        var card = Bounds(Text());
        var content = new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false };
        var scroll = Scroll(content);
        var host = new LookaheadBox { Children = { scroll } };
        using var f = new Fixture(host);
        Assert.Equal(new Size(300, 600), scroll.Extent);
        Assert.Equal(new Size(300, 200), scroll.Viewport);
        Assert.True(scroll.ClipToBounds);
        scroll.Offset = new Vector(50, 1000); f.Layout();
        Assert.Equal(new Vector(0, 400), scroll.Offset);
        scroll.Offset = new Vector(0, -10); f.Layout();
        Assert.Equal(default, scroll.Offset);
        scroll.Offset = new Vector(0, 100); f.Layout();
        content.Height = 250; f.Layout();
        Assert.Equal(new Vector(0, 50), scroll.Offset);
        host.Children.Remove(scroll); f.Layout();
        var snapshot = f.Root.Snapshot!;
        scroll.Offset = new Vector(0, 20);
        Assert.True(f.Root.IsArrangeValid);
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task ScrollViewport_InitialOffsetIsClampedBeforeAnimationInitialization() => OnUi(() =>
    {
        var card = Bounds(Text()); card.AnimateMotionFrameOfReference = true;
        var scroll = Scroll(new LookaheadBox { Height = 600, Children = { card }, UseLayoutRounding = false });
        scroll.Offset = new Vector(0, 1000);
        using var f = new Fixture(scroll);
        Assert.Equal(new Vector(0, 400), scroll.Offset);
        Equal(new Rect(0, -400, 120, 160), f.Actual(card));
        Assert.False(card.IsRunning);
        Assert.Equal(0, f.Frames.PendingFrameCount);
    });

    [Theory]
    [InlineData(100, 200)]
    [InlineData(200, 100)]
    public Task MotionFrame_NestedRunsKeepCompensatingAfterEitherFinishes(int outerMs, int innerMs) => OnUi(() =>
    {
        var child = Bounds(Text()); child.TargetSize = new Size(60, 70); child.BoundsTransform = Tween(innerMs);
        child.HorizontalAlignment = HorizontalAlignment.Right; child.VerticalAlignment = VerticalAlignment.Bottom;
        var parent = Bounds(new LookaheadBox { Children = { child }, UseLayoutRounding = false });
        parent.TargetSize = new Size(180, 160); parent.BoundsTransform = Tween(outerMs);
        var scroll = Scroll(new LookaheadBox { Height = 600, Children = { parent }, UseLayoutRounding = false });
        using var f = new Fixture(scroll);
        var start = f.Actual(child);
        parent.TargetSize = new Size(280, 220); parent.HorizontalAlignment = HorizontalAlignment.Right;
        f.Layout();
        var snapshot = f.Root.Snapshot!;
        var target = snapshot.Results[child].Bounds;
        f.Step(16); f.Step(150);
        scroll.Offset = new Vector(0, 80); f.Layout();
        var fraction = Math.Min(1, 150.0 / innerMs);
        Equal(new Rect(start.X + (target.X - start.X) * fraction,
            start.Y + (target.Y - start.Y) * fraction - 80, start.Width, start.Height), f.Actual(child));
        f.Step(50);
        Equal(new Rect(target.TopLeft - new Vector(0, 80), target.Size), f.Actual(child));
        Assert.False(parent.IsRunning);
        Assert.False(child.IsRunning);
        Assert.Same(snapshot, f.Root.Snapshot);
    });

    [Fact]
    public Task ScrollViewport_HorizontalAndVerticalContributionsRemainIndependent() => OnUi(() =>
    {
        var card = Bounds(Text());
        var scroll = Scroll(new LookaheadBox { Width = 600, Height = 600, Children = { card }, UseLayoutRounding = false });
        scroll.CanHorizontallyScroll = true;
        using var f = new Fixture(scroll);
        var snapshot = f.Root.Snapshot!;
        scroll.Offset = new Vector(70, 80); f.Layout();
        Equal(new Rect(-70, -80, 120, 160), f.Actual(card));
        Assert.Same(snapshot, f.Root.Snapshot);
        scroll.CanVerticallyScroll = false; f.Layout();
        Assert.Equal(new Vector(70, 0), scroll.Offset);
        Equal(new Rect(-70, 0, 120, 160), f.Actual(card));
    });

    [Fact]
    public Task ScrollViewport_WheelBubblesToOuterViewportOnlyAtAnEdge() => OnUi(() =>
    {
        var card = Bounds(Text());
        var inner = Scroll(new LookaheadBox { Height = 600, Children = { card } }, 150);
        var outer = Scroll(new LookaheadStackPanel { Children = { inner, new Border { Width = 10, Height = 500 } } });
        using var f = new Fixture(outer);
        var pointer = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        PointerWheelEventArgs Wheel() => new(card, pointer, f.Window, default, 0,
            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.Other), KeyModifiers.None, new Vector(0, -1));
        var first = Wheel(); card.RaiseEvent(first); f.Layout();
        Assert.True(first.Handled);
        Assert.Equal(new Vector(0, 50), inner.Offset);
        Assert.Equal(default, outer.Offset);
        inner.Offset = new Vector(0, 450); f.Layout();
        var second = Wheel(); card.RaiseEvent(second); f.Layout();
        Assert.True(second.Handled);
        Assert.Equal(new Vector(0, 450), inner.Offset);
        Assert.Equal(new Vector(0, 50), outer.Offset);
    });

    private sealed class MotionHost : Decorator, ILookaheadLayoutNode, ILookaheadMotionFrameProvider
    {
        private Vector _offset;
        public event EventHandler? MotionFrameChanged;
        public Vector Offset
        {
            get => _offset;
            set { _offset = value; MotionFrameChanged?.Invoke(this, EventArgs.Empty); }
        }
        public Vector GetMotionFrameOffset(Control child) => _offset;
        public IEnumerable<Control> LookaheadChildren => Child is { } child ? [child] : [];
        public Size MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
            => Child is { } child ? builder.Measure(child, available) : default;
        public void ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        {
            if (Child is { } child) builder.Place(child, bounds);
        }
        protected override Size MeasureOverride(Size availableSize)
            => Child is { } child ? LookaheadMeasure.Measure(child, availableSize) : default;
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
            return finalSize;
        }
    }

    [Fact]
    public Task ThirdPartyMotionProvider_UsesPublicProtocolAndValidatesOffsets() => OnUi(() =>
    {
        var card = Bounds(Text());
        var host = new MotionHost { Child = card, UseLayoutRounding = false };
        using var f = new Fixture(host);
        var snapshot = f.Root.Snapshot!;
        host.Offset = new Vector(15, -30); f.Layout();
        Equal(new Rect(15, -30, 120, 160), f.Actual(card));
        Assert.Same(snapshot, f.Root.Snapshot);
        Assert.False(card.IsRunning);
        host.Offset = new Vector(double.NaN, 0);
        Assert.Throws<InvalidOperationException>(() => LookaheadCoordinates.TargetPosition(card));
        host.Offset = default; f.Layout();
        Equal(snapshot.Results[card].Bounds, f.Actual(card));
    });
}

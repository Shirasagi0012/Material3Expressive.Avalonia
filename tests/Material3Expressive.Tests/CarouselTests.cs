using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class CarouselTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomationPagesAndPercentage_RespectBoundsAndUserScrolling(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.MultiBrowse);
        carousel.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = DialogTests.Show(carousel);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(carousel)!;
            var scroll = Assert.IsAssignableFrom<IScrollProvider>(peer.GetProvider<IScrollProvider>());
            Assert.True(scroll.HorizontallyScrollable); Assert.False(scroll.VerticallyScrollable);
            Assert.Equal(0, scroll.HorizontalScrollPercent); Assert.InRange(scroll.HorizontalViewSize, 1, 99);
            scroll.Scroll(ScrollAmount.LargeIncrement, ScrollAmount.NoAmount); Advance();
            Assert.Equal(1, carousel.CurrentItem);
            scroll.Scroll(ScrollAmount.SmallDecrement, ScrollAmount.NoAmount); Advance();
            Assert.Equal(0, carousel.CurrentItem);
            scroll.Scroll(ScrollAmount.LargeDecrement, ScrollAmount.NoAmount); Advance();
            Assert.Equal(0, carousel.ScrollOffset);
            scroll.SetScrollPercent(100, ScrollPatternIdentifiers.NoScroll); Flush(window);
            Assert.Equal(carousel.MaximumScrollOffset, carousel.ScrollOffset); Assert.Equal(100, scroll.HorizontalScrollPercent);
            scroll.SetScrollPercent(50, ScrollPatternIdentifiers.NoScroll); Flush(window);
            Assert.Equal(carousel.MaximumScrollOffset / 2, carousel.ScrollOffset);
            Assert.Throws<ArgumentOutOfRangeException>(() => scroll.SetScrollPercent(double.NaN, -1));
            Assert.Throws<InvalidOperationException>(() => scroll.Scroll(ScrollAmount.NoAmount, ScrollAmount.LargeIncrement));
            carousel.UserScrollEnabled = false;
            Assert.Null(peer.GetProvider<IScrollProvider>()); Assert.False(scroll.HorizontallyScrollable);
            Assert.Throws<InvalidOperationException>(() => scroll.Scroll(ScrollAmount.SmallIncrement, ScrollAmount.NoAmount));
            carousel.ScrollToItem(0); Flush(window); Assert.Equal(0, carousel.CurrentItem);
            carousel.UserScrollEnabled = true; carousel.IsEnabled = false;
            Assert.Throws<ElementNotEnabledException>(() => scroll.SetScrollPercent(20, -1));
        }
        finally { window.Close(); }
        void Advance() { for (var i = 0; i < 90; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    }

    [AvaloniaFact]
    public void MouseLeavesContentClickable_AndNavigationPreservesTwoWayBinding()
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.MultiBrowse);
        var source = new HorizontalCarousel();
        carousel.Bind(HorizontalCarousel.ScrollOffsetProperty, new Avalonia.Data.Binding(nameof(HorizontalCarousel.ScrollOffset))
        { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        var button = new Button { Content = "Open", Width = 80, Height = 40 };
        ((CarouselItem)carousel.Items[0]!).Content = button;
        var clicks = 0; button.Click += (_, _) => clicks++;
        var window = DialogTests.Show(carousel);
        try
        {
            using (window.CaptureRenderedFrame()) { }
            var point = button.TranslatePoint(new Point(40, 20), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, clicks);
            window.MouseDown(point, MouseButton.Left); window.MouseMove(point + new Vector(70, 0));
            window.MouseUp(point + new Vector(70, 0), MouseButton.Left);
            Assert.Equal(0, carousel.ScrollOffset); Assert.False(carousel.IsScrollInProgress);
            var animation = carousel.AnimateScrollToItemAsync(2);
            for (var i = 0; i < 90; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.True(animation.IsCompletedSuccessfully);
            Assert.Equal(2, carousel.CurrentItem);
            Assert.Equal(carousel.ScrollOffset, source.ScrollOffset);
            source.ScrollOffset = 0; Flush(window);
            Assert.Equal(0, carousel.CurrentItem);
            carousel.ScrollToItem(3); Flush(window);
            Assert.Equal(carousel.ScrollOffset, source.ScrollOffset);
            source.ScrollOffset = 10; Flush(window);
            Assert.Equal(10, carousel.ScrollOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void TouchDrag_SlopReleaseDisableAndProgrammaticInterruptionPairInteractions(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        using var touch = new TouchDevice();
        var carousel = Create(CarouselVariant.MultiBrowse);
        carousel.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var interactions = new List<MaterialInteractionEvent>();
        carousel.InteractionSource.Interaction += interactions.Add;
        var window = DialogTests.Show(carousel);
        try
        {
            using (window.CaptureRenderedFrame()) { }
            carousel.ScrollToItem(2); Flush(window); var initial = carousel.ScrollOffset;
            Input(1000, RawPointerEventType.TouchBegin, 0);
            Input(1016, RawPointerEventType.TouchUpdate, 10);
            Assert.Equal(initial, carousel.ScrollOffset); Assert.Empty(interactions);
            Input(1032, RawPointerEventType.TouchUpdate, 70);
            Assert.Equal(initial + 52, carousel.ScrollOffset); Assert.True(carousel.IsScrollInProgress);
            Input(1048, RawPointerEventType.TouchEnd, 70);
            Assert.Same(Assert.IsType<MaterialInteractionEvent.DragStart>(interactions[0]),
                Assert.IsType<MaterialInteractionEvent.DragStop>(interactions[1]).Start);
            Advance(100); Assert.False(carousel.IsScrollInProgress);
            Input(3000, RawPointerEventType.TouchBegin, 0);
            Input(3016, RawPointerEventType.TouchUpdate, 70);
            carousel.UserScrollEnabled = false; Flush(window);
            Assert.Same(interactions[2], Assert.IsType<MaterialInteractionEvent.DragCancel>(interactions[3]).Start);
            Advance(100); var disabled = carousel.ScrollOffset;
            Input(3032, RawPointerEventType.TouchEnd, 70);
            Input(5000, RawPointerEventType.TouchBegin, 0); Input(5016, RawPointerEventType.TouchUpdate, 70);
            Input(5032, RawPointerEventType.TouchEnd, 70);
            Assert.Equal(disabled, carousel.ScrollOffset); Assert.Equal(4, interactions.Count);
            carousel.UserScrollEnabled = true;
            Input(6000, RawPointerEventType.TouchBegin, 0); Input(6016, RawPointerEventType.TouchUpdate, 70);
            carousel.ScrollToItem(1); Flush(window);
            Assert.Same(interactions[4], Assert.IsType<MaterialInteractionEvent.DragCancel>(interactions[5]).Start);
            Input(6032, RawPointerEventType.TouchUpdate, 120); Input(6048, RawPointerEventType.TouchEnd, 120);
            Assert.Equal(1, carousel.CurrentItem); Assert.False(carousel.IsScrollInProgress);
            Input(7000, RawPointerEventType.TouchBegin, 0); Input(7016, RawPointerEventType.TouchUpdate, 70);
            carousel.UserScrollEnabled = false;
            // A pending configuration-commit rebound must not revive after a newer navigation.
            carousel.ScrollToItem(4); Advance(100);
            Assert.Equal(4, carousel.CurrentItem); Assert.False(carousel.IsScrollInProgress);
            Assert.Same(interactions[6], Assert.IsType<MaterialInteractionEvent.DragCancel>(interactions[7]).Start);
        }
        finally { window.Close(); }
        void Input(ulong time, RawPointerEventType type, double distance)
        {
            var left = Math.Min(carousel.TranslatePoint(default, window)!.Value.X,
                carousel.TranslatePoint(new Point(carousel.Bounds.Width, 0), window)!.Value.X);
            var point = new Point(left + 200 + (rtl ? distance : -distance), carousel.TranslatePoint(new Point(0, 60), window)!.Value.Y);
            var root = (IInputRoot)window.PlatformImpl!.GetType().GetProperty("InputRoot")!.GetValue(window.PlatformImpl)!;
            window.PlatformImpl.Input!(new RawTouchEventArgs(touch, time, root, type, point, RawInputModifiers.None, 1));
            Flush(window);
        }
        void Advance(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    }

    [Fact]
    public void ScrollConsumption_RetainsFractionAndReportsOnlyUnconsumedBoundaryDelta()
    {
        var position = new CarouselScrollPosition();
        position.Rebase(0, 4);
        Assert.Equal(.2f, position.Consume(.2f, 100));
        Assert.Equal(0, position.Position);
        position.Consume(.2f, 100); position.Consume(.2f, 100);
        Assert.Equal(1, position.Position);
        Assert.Equal(.25, position.Value);
        Assert.Equal(-1, position.Consume(-20, 100));
        Assert.Equal(0, position.Position);
        position.Rebase(24, 4);
        Assert.Equal(4, position.Consume(20, 100));
        Assert.Equal(25, position.Value);
    }

    [AvaloniaTheory]
    [InlineData(CarouselFlingMode.SingleAdvance)]
    [InlineData(CarouselFlingMode.MultiBrowse)]
    [InlineData(CarouselFlingMode.NoSnap)]
    public void Fling_ConsumesVelocityAndCanBeInterrupted(CarouselFlingMode mode)
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.MultiBrowse); carousel.FlingMode = mode;
        var window = DialogTests.Show(carousel);
        try
        {
            carousel.ScrollToItem(2); Flush(window);
            carousel.ScrollOffset += 45; Flush(window);
            var initial = carousel.ScrollOffset;
            var task = carousel.FlingAsync(1400);
            Advance(150);
            Assert.True(task.IsCompletedSuccessfully); Assert.False(carousel.IsScrollInProgress);
            Assert.True(carousel.ScrollOffset > initial);
            if (mode != CarouselFlingMode.NoSnap) Assert.Equal(0, carousel.CurrentItemOffsetFraction, 2);
            using var cancellation = new CancellationTokenSource();
            carousel.ScrollToItem(3); Flush(window);
            carousel.ScrollOffset -= 45; Flush(window);
            carousel.SnapAnimationSpec = new TweenSpec<float>(TimeSpan.FromMilliseconds(500));
            var back = carousel.FlingAsync(-200, cancellation.Token);
            Advance(3); Assert.True(carousel.IsScrollInProgress); var progressed = carousel.ScrollOffset;
            cancellation.Cancel(); Flush(window);
            Assert.True(back.IsCompletedSuccessfully); Assert.False(carousel.IsScrollInProgress);
            Assert.Equal(progressed, carousel.ScrollOffset);
        }
        finally { window.Close(); }
        void Advance(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    }

    [Theory]
    [InlineData(CarouselVariant.MultiBrowse)]
    [InlineData(CarouselVariant.Uncontained)]
    [InlineData(CarouselVariant.CenteredHero)]
    public void Strategy_ShiftsBothEdgesAndRetainsAllKeylines(CarouselVariant variant)
    {
        var keylines = CarouselKeylines.Create(variant, 400, 180, 8, 10, 40, 56, 10);
        var strategy = new CarouselStrategy(keylines, 400, 8, 0, 0);
        Assert.True(strategy.IsValid);
        Assert.True(strategy.StartSteps[^1].IsFirstFocalAtStart);
        Assert.True(strategy.EndSteps[^1].IsLastFocalAtEnd(400));
        foreach (var offset in new[] { 0f, 10, 100, 400, strategy.MaxScrollOffset(10) })
        {
            var step = strategy.GetKeylines(offset, strategy.MaxScrollOffset(10));
            Assert.Equal(keylines.Count, step.Count);
            Assert.All(step, k => Assert.True(float.IsFinite(k.Size) && k.Size > 0));
        }
        Assert.Equal(0, strategy.SnapPosition(0, 10));
        Assert.True(strategy.SnapPosition(9, 10) > 0);
    }

    [AvaloniaTheory]
    [InlineData(CarouselVariant.MultiBrowse, false)]
    [InlineData(CarouselVariant.MultiBrowse, true)]
    [InlineData(CarouselVariant.Uncontained, false)]
    [InlineData(CarouselVariant.Uncontained, true)]
    [InlineData(CarouselVariant.CenteredHero, false)]
    [InlineData(CarouselVariant.CenteredHero, true)]
    public void Container_UsesFullSizeContentAndMasksAtFocalPositions(CarouselVariant variant, bool rtl)
    {
        var carousel = Create(variant); carousel.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var window = DialogTests.Show(carousel);
        try
        {
            var items = carousel.GetVisualDescendants().OfType<CarouselItem>().ToArray();
            Assert.Equal(8, items.Length);
            Assert.True(carousel.MaximumScrollOffset > 0);
            var width = items[0].Bounds.Width;
            carousel.ScrollToItem(3); Flush(window);
            Assert.Equal(3, carousel.CurrentItem);
            Assert.Equal(0, carousel.CurrentItemOffsetFraction, 5);
            Assert.All(items, item => Assert.Equal(width, item.Bounds.Width));
            Assert.True(items[3].DrawInfo.Size > items[4].DrawInfo.MinSize);
            carousel.ScrollToItem(100); Flush(window);
            Assert.Equal(7, carousel.CurrentItem);
            var origin = items[7].TranslatePoint(items[7].DrawInfo.MaskRect.Position, carousel)!.Value;
            Assert.InRange(origin.X, -0.01, carousel.Bounds.Width);
            Assert.InRange(origin.X + items[7].DrawInfo.Size, 0, carousel.Bounds.Width + .01);
            Assert.NotNull(items[7].GetVisualDescendants().OfType<CarouselMask>().Single(m => m.Name == "PART_Mask").Clip);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ItemsSource_EmptyAndReplacementRefreshExtentAndContent()
    {
        var carousel = Create(CarouselVariant.CenteredHero);
        carousel.Items.Clear();
        var window = DialogTests.Show(carousel);
        try
        {
            Assert.Equal(0, carousel.MaximumScrollOffset);
            carousel.ItemsSource = Enumerable.Range(0, 6).ToArray();
            carousel.ItemTemplate = new FuncDataTemplate<int>((i, _) => new Border { Background = Brushes.Coral, Tag = i });
            Flush(window); carousel.ScrollToItem(5); Flush(window);
            Assert.Equal(5, carousel.CurrentItem);
            carousel.ItemsSource = new[] { 42 }; Flush(window);
            Assert.Equal(0, carousel.MaximumScrollOffset);
            Assert.Equal(0, carousel.CurrentItem);
            Assert.Equal(42, carousel.GetVisualDescendants().OfType<Border>().Single(b => b.Tag is int).Tag);
            carousel.ItemsSource = Array.Empty<int>(); Flush(window);
            Assert.Equal(0, carousel.CurrentItem);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ProgrammaticAnimation_SnapCancellationDetachAndRemount()
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.MultiBrowse);
        var window = DialogTests.Show(carousel);
        try
        {
            var task = carousel.AnimateScrollToItemAsync(1, new TweenSpec<float>(TimeSpan.FromMilliseconds(160)));
            Advance(5); Assert.True(carousel.IsScrollInProgress); Assert.True(carousel.ScrollOffset > 0);
            carousel.ScrollToItem(3); Flush(window);
            Assert.True(task.IsCompletedSuccessfully); Assert.False(carousel.IsScrollInProgress); Assert.Equal(3, carousel.CurrentItem);
            var next = carousel.AnimateScrollToItemAsync(4); Advance(90);
            Assert.True(next.IsCompletedSuccessfully); Assert.Equal(4, carousel.CurrentItem);
            var detached = carousel.AnimateScrollToItemAsync(2); Advance(3);
            window.Content = null; Flush(window);
            Assert.True(detached.IsCompletedSuccessfully); Assert.False(carousel.IsScrollInProgress);
            window.Content = carousel; Flush(window); carousel.ScrollToItem(0); Flush(window);
            Assert.Equal(0, carousel.CurrentItem);
        }
        finally { window.Close(); }
        void Advance(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    }

    private static HorizontalCarousel Create(CarouselVariant variant)
    {
        var carousel = new HorizontalCarousel { Width = 400, Height = 120, Variant = variant, ItemWidth = 180, ItemSpacing = 8 };
        for (var i = 0; i < 8; i++) carousel.Items.Add(new CarouselItem { Background = Brushes.CornflowerBlue, CornerRadius = new(16) });
        return carousel;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

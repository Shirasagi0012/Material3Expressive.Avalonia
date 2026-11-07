using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class CarouselWheelTests
{
    [AvaloniaTheory]
    [InlineData(CarouselVariant.MultiBrowse, false)]
    [InlineData(CarouselVariant.MultiBrowse, true)]
    [InlineData(CarouselVariant.CenteredHero, false)]
    [InlineData(CarouselVariant.CenteredHero, true)]
    [InlineData(CarouselVariant.Uncontained, false)]
    [InlineData(CarouselVariant.Uncontained, true)]
    public void HorizontalWheel_AnimatesThenUsesVariantFlingWithoutDragInteractions(CarouselVariant variant, bool rtl)
    {
        using var host = new WheelHost(variant, rtl);
        var carousel = host.Carousel;
        var source = new HorizontalCarousel();
        carousel.Bind(HorizontalCarousel.ScrollOffsetProperty, new Avalonia.Data.Binding(nameof(HorizontalCarousel.ScrollOffset))
        { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        carousel.ScrollToItem(2); host.Flush();
        var initial = carousel.ScrollOffset;
        var events = new List<MaterialInteractionEvent>();
        carousel.InteractionSource.Interaction += events.Add;
        host.Wheel(-12);
        Assert.True(carousel.IsScrollInProgress);
        Assert.InRange(carousel.ScrollOffset - initial, 5.9, 6.1);
        host.Advance(150);
        Assert.False(carousel.IsScrollInProgress);
        Assert.True(carousel.ScrollOffset > initial);
        if (variant != CarouselVariant.Uncontained) Assert.InRange(Math.Abs(carousel.CurrentItemOffsetFraction), 0, .0001);
        Assert.Empty(events);
        Assert.Equal(carousel.ScrollOffset, source.ScrollOffset);
        source.ScrollOffset = initial; host.Flush();
        Assert.Equal(initial, carousel.ScrollOffset);
        Assert.Equal(0, host.Time.ActiveTimerCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Wheel_RejectsOrthogonalDisabledAndOutwardBoundaryInput(bool rtl)
    {
        using var host = new WheelHost(CarouselVariant.MultiBrowse, rtl);
        var carousel = host.Carousel;
        var unhandled = 0;
        host.Window.AddHandler(Avalonia.Input.InputElement.PointerWheelChangedEvent, (_, _) => unhandled++);
        host.Wheel(3); // outward at the start
        Assert.Equal(1, unhandled);
        host.Window.MouseWheel(new Point(160, 60), new Vector(0, -3), RawInputModifiers.None);
        Assert.Equal(2, unhandled);
        carousel.UserScrollEnabled = false; host.Wheel(-3);
        Assert.Equal(3, unhandled);
        Assert.Equal(0, carousel.ScrollOffset);
        carousel.UserScrollEnabled = true; carousel.ScrollToItem(7); host.Flush();
        var end = carousel.ScrollOffset; host.Wheel(-3);
        Assert.Equal(4, unhandled);
        Assert.Equal(end, carousel.ScrollOffset);
        Assert.False(carousel.IsScrollInProgress);
    }

    [AvaloniaFact]
    public void NavigationAndDetach_CancelWheelWithoutLateFlingAndReceiverAcceptsNextInput()
    {
        using var host = new WheelHost(CarouselVariant.MultiBrowse, false);
        var carousel = host.Carousel;
        host.Wheel(-.1);
        Assert.True(host.Time.ActiveTimerCount > 0);
        carousel.ScrollToItem(3); host.Advance(100);
        Assert.Equal(3, carousel.CurrentItem); Assert.False(carousel.IsScrollInProgress);
        Assert.Equal(0, host.Time.ActiveTimerCount);
        host.Wheel(-.1);
        carousel.ScrollToItem(1);
        // Queue before cancellation continuations complete, as with state-driven navigation.
        host.Wheel(-12); host.Advance(100);
        Assert.True(carousel.CurrentItem > 1); Assert.False(carousel.IsScrollInProgress);
        host.Wheel(-.1);
        host.Window.Content = null; host.Flush(); host.Advance(100);
        Assert.False(carousel.IsScrollInProgress); Assert.Equal(0, host.Time.ActiveTimerCount);
        host.Window.Content = carousel; host.Flush(); carousel.ScrollToItem(0); host.Flush();
        host.Wheel(-12); host.Advance(100);
        Assert.True(carousel.ScrollOffset > 0); Assert.False(carousel.IsScrollInProgress);
    }

    private sealed class WheelHost : IDisposable
    {
        private readonly ManualFrameSource _frames = new(Dispatcher.UIThread.VerifyAccess);
        internal ManualTimeProvider Time { get; } = new(Dispatcher.UIThread.VerifyAccess);
        internal AnimationTestWindow Window { get; }
        internal HorizontalCarousel Carousel { get; }
        private readonly bool _rtl;
        internal WheelHost(CarouselVariant variant, bool rtl)
        {
            _rtl = rtl;
            Window = new(_frames, Time) { Width = 400, Height = 180 };
            Window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
            Carousel = new() { Width = 320, Height = 120, ItemWidth = 160, ItemSpacing = 8, Variant = variant,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            for (var i = 0; i < 8; i++) Carousel.Items.Add(new CarouselItem { Background = Brushes.Coral });
            Window.Content = Carousel; Window.Show(); Flush();
            using (Window.CaptureRenderedFrame()) { }
            _frames.SendFrame(TimeSpan.Zero); Flush();
        }
        internal void Wheel(double x) => Window.MouseWheel(new Point(160, 60), new Vector(_rtl ? -x : x, 0), RawInputModifiers.None);
        internal void Flush() { Dispatcher.UIThread.RunJobs(); Window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
        internal void Advance(int count)
        {
            for (var i = 0; i < count; i++)
            {
                Time.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush();
                _frames.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush();
            }
        }
        public void Dispose() { Window.Close(); Flush(); _frames.Dispose(); }
    }
}

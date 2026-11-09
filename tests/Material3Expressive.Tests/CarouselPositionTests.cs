using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class CarouselPositionTests
{
    [Fact]
    public void PositionValidatesItsDomainAndRoundTripsAsApplicationData()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CarouselPosition(-1));
        foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, -.51f, .51f })
            Assert.Throws<ArgumentOutOfRangeException>(() => new CarouselPosition(0, invalid));
        var position = new CarouselPosition(7, -.25f);
        Assert.Equal(position, JsonSerializer.Deserialize<CarouselPosition>(JsonSerializer.Serialize(position)));
        Assert.Equal(new CarouselPosition(0), default);
    }

    [AvaloniaTheory]
    [InlineData(CarouselVariant.MultiBrowse, false)]
    [InlineData(CarouselVariant.MultiBrowse, true)]
    [InlineData(CarouselVariant.CenteredHero, false)]
    [InlineData(CarouselVariant.CenteredHero, true)]
    [InlineData(CarouselVariant.Uncontained, false)]
    [InlineData(CarouselVariant.Uncontained, true)]
    public void CapturedFractionRestoresInANewViewport(CarouselVariant variant, bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var original = Create(variant, rtl);
        original.RestorePosition(new(3, .25f));
        var window = DialogTests.Show(original);
        try
        {
            Assert.Equal(3, original.CurrentItem);
            var saved = original.CapturePosition();
            Assert.InRange(saved.OffsetFraction, .24f, .26f);
            var restored = Create(variant, rtl); restored.Width = 240; restored.ItemWidth = 180;
            restored.RestorePosition(saved);
            window.Content = restored; Flush(window);
            Assert.Equal(saved.Item, restored.CurrentItem);
            Assert.InRange(Math.Abs(restored.CurrentItemOffsetFraction - saved.OffsetFraction), 0, .01);
            Assert.False(restored.IsScrollInProgress);
            Assert.NotEqual(original.ScrollOffset, restored.ScrollOffset);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PendingPositionSurvivesAnEmptyCollectionAndCanBeReplaced()
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.MultiBrowse); carousel.Items.Clear();
        carousel.RestorePosition(new(4, -.25f));
        var window = DialogTests.Show(carousel);
        try
        {
            Assert.Equal(new CarouselPosition(4, -.25f), carousel.CapturePosition());
            carousel.RestorePosition(new(3, .25f));
            foreach (var item in Enumerable.Range(0, 8)) carousel.Items.Add(new CarouselItem { Background = Brushes.Coral });
            Flush(window);
            Assert.Equal(3, carousel.CurrentItem);
            Assert.InRange(carousel.CurrentItemOffsetFraction, .24, .26);
            carousel.RestorePosition(new(5));
            carousel.ScrollOffset = 0; Flush(window);
            Assert.Equal(0, carousel.CurrentItem);
            Assert.Equal(new CarouselPosition(0), carousel.CapturePosition());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void RestoringDuringAnimationCancelsOldMotionAndPreservesTheBinding()
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.MultiBrowse);
        var source = new HorizontalCarousel();
        carousel.Bind(HorizontalCarousel.ScrollOffsetProperty, new Avalonia.Data.Binding(nameof(HorizontalCarousel.ScrollOffset))
        { Source = source, Mode = Avalonia.Data.BindingMode.TwoWay });
        var window = DialogTests.Show(carousel);
        try
        {
            var animation = carousel.AnimateScrollToItemAsync(7);
            for (var i = 0; i < 5; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.True(carousel.IsScrollInProgress);
            carousel.RestorePosition(new(2, -.25f)); Flush(window);
            Assert.False(carousel.IsScrollInProgress); Assert.Equal(2, carousel.CurrentItem);
            Assert.InRange(carousel.CurrentItemOffsetFraction, -.26, -.24);
            for (var i = 0; i < 80; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
            Assert.True(animation.IsCompleted); Assert.Equal(2, carousel.CurrentItem);
            Assert.Equal(carousel.ScrollOffset, source.ScrollOffset);
            source.ScrollOffset = 0; Flush(window); Assert.Equal(0, carousel.CurrentItem);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void NewCollectionBoundsAndExplicitNavigationSupersedeSavedPosition()
    {
        using var clock = new MotionTestClockScope();
        var carousel = Create(CarouselVariant.Uncontained);
        carousel.RestorePosition(new(100, .25f));
        var window = DialogTests.Show(carousel);
        try
        {
            Assert.Equal(7, carousel.CurrentItem);
            Assert.InRange(carousel.ScrollOffset, 0, carousel.MaximumScrollOffset);
            carousel.RestorePosition(new(3, .25f));
            carousel.ScrollToItem(1); Flush(window);
            Assert.Equal(1, carousel.CurrentItem); Assert.Equal(0, carousel.CurrentItemOffsetFraction);
            carousel.RestorePosition(new(0, -.5f)); Flush(window);
            Assert.Equal(0, carousel.ScrollOffset);
            Assert.Equal(0, carousel.CurrentItemOffsetFraction);
        }
        finally { window.Close(); }
    }

    private static HorizontalCarousel Create(CarouselVariant variant, bool rtl = false)
    {
        var carousel = new HorizontalCarousel { Width = 320, Height = 120, ItemWidth = 160, ItemSpacing = 8,
            Variant = variant, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        foreach (var item in Enumerable.Range(0, 8)) carousel.Items.Add(new CarouselItem { Background = Brushes.Coral });
        return carousel;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Styles;
using SmoothScroll.Avalonia.Composition;
using SmoothScroll.Avalonia.Interaction;
using System.Runtime.CompilerServices;
using Xunit;

namespace Material3Expressive.Tests;

public class SearchScrollEdgeTests
{
    [AvaloniaTheory]
    [InlineData("search", false)]
    [InlineData("top", false)]
    [InlineData("bottom", false)]
    [InlineData("search", true)]
    [InlineData("top", true)]
    [InlineData("bottom", true)]
    public void UnmountedScope_IsNotRetainedByAnApplicationOwnedBehavior(string kind, bool attachThenDetach)
    {
        AvaloniaObject behavior = kind switch
        {
            "search" => new SearchBarScrollBehavior(),
            "top" => new TopAppBarScrollBehavior(),
            _ => new BottomAppBarScrollBehavior(),
        };
        var scope = CreateUnusedScope(behavior, attachThenDetach);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.False(scope.IsAlive);
        GC.KeepAlive(behavior);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateUnusedScope(AvaloniaObject behavior, bool attachThenDetach)
    {
        var scope = new AppBarScrollScope();
        switch (behavior)
        {
            case SearchBarScrollBehavior search: scope.SearchScrollBehavior = search; break;
            case TopAppBarScrollBehavior top: scope.TopScrollBehavior = top; break;
            case BottomAppBarScrollBehavior bottom: scope.BottomScrollBehavior = bottom; break;
        }
        if (attachThenDetach)
        {
            var window = new MotionTestWindow { Width = 200, Height = 200, Content = scope };
            window.Show(); Render(window);
            window.Content = null; Render(window); window.Close();
        }
        return new WeakReference(scope);
    }

    [AvaloniaFact]
    public void ReattachedScope_ObservesBehaviorConfigurationAgain()
    {
        var behavior = new SearchBarScrollBehavior();
        var scope = new AppBarScrollScope { SearchScrollBehavior = behavior };
        var window = new MotionTestWindow { Width = 200, Height = 200, Content = scope };
        try
        {
            window.Show(); Render(window);
            Assert.True(scope.IsMeasureValid);
            behavior.CanScroll = false;
            Assert.False(scope.IsMeasureValid);
            Render(window);
            window.Content = null; Render(window);
            behavior.CanScroll = true;
            window.Content = scope; Render(window);
            Assert.True(scope.IsMeasureValid);
            behavior.CanScroll = false;
            Assert.False(scope.IsMeasureValid);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(1, 2, true, false)]
    [InlineData(2, 1, true, false)]
    [InlineData(1.25, 2, true, false)]
    [InlineData(1, 2, true, true)]
    [InlineData(1, 2, false, false)]
    [InlineData(2, 1, false, false)]
    public void ActiveSnap_KeepsItsDipPositionWhenRenderDensityChanges(double initialDensity, double nextDensity, bool search, bool customSpec)
    {
        using var clock = new MotionTestClockScope();
        var window = new MotionTestWindow { Width = 200, Height = 200, Content = new Border() };
        window.Show(); Render(window);
        var compositor = ElementComposition.GetElementVisual((Control)window.Content!)!.Compositor;
        using var offsets = CompositionVector3Output.Create(compositor);
        using var appearance = CompositionVector3Output.Create(compositor);
        using var colors = CompositionColorOutput.Create(compositor);
        using var tracker = compositor.CreateInteractionTracker(null);
        var observed = (Top: double.NaN, Bottom: double.NaN);
        try
        {
            var config = new AppBarScrollConfiguration(TopAppBarScrollMode.EnterAlways, 64, 64, initialDensity,
                true, true, false, true, false, Colors.Transparent, Colors.Transparent,
                new AppBarSpring(1, 200, .01f), new AppBarSpring(1, 200, .01f), new AppBarSpring(1, 1600, .01f), 400, 1200,
                Search: search, TopAnimationScale: search ? customSpec ? 1 : initialDensity : null);
            tracker.ConfigureVerticalScroll(new DensityChangeFactory(config, nextDensity, customSpec, offsets.Writer, appearance.Writer, colors.Writer,
                value => tracker.DispatchToUI(() => observed = value)));
            Render(window);
            Assert.Equal(20, observed.Top);
            Assert.Equal(20, observed.Bottom);
        }
        finally { tracker.ConfigureVerticalScroll(null); window.Close(); }
    }

    private sealed record DensityChangeFactory(AppBarScrollConfiguration Config, double NextDensity, bool CustomSpec,
        CompositionVector3OutputWriter Offsets, CompositionVector3OutputWriter Appearance, CompositionColorOutputWriter Colors,
        Action<(double Top, double Bottom)> Publish) : IScrollMovementParticipantFactory
    {
        public IScrollMovementParticipant Create()
        {
            var value = (Top: double.NaN, Bottom: double.NaN);
            var participant = new AppBarScrollParticipant(Config, new(0, 0, 20, 20, 0, 0, 0, 400),
                Offsets, Appearance, Colors, state => value = (state.TopCollapsed, state.BottomHidden));
            participant.OnIdle(default);
            participant.Configure(Config with { Density = NextDensity, TopAnimationScale = Config.Search ? CustomSpec ? 1 : NextDensity : null });
            // Time zero must still represent the original DIP position, even though
            // drawing quantization now uses the new physical density.
            participant.OnFrame(0);
            Publish(value);
            return participant;
        }
    }

    [AvaloniaFact]
    public void ReverseNativeWheelAtExpandedBodyStart_StillDeliversRemainingScroll()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(reverse: true);
        try
        {
            Assert.Equal(0, scene.Viewer.Offset.Y);
            Assert.Equal(0, scene.Behavior.State.ScrollOffset);
            scene.Window.MouseWheel(scene.Viewer.TranslatePoint(new Point(20, 20), scene.Window)!.Value, new Vector(0, 1));
            Render(scene.Window);
            // Fixed SearchBar.kt reverse onPostScroll consumes availableY > 0 and
            // adds it to contentOffset even if scrollOffset is already zero.
            Assert.True(scene.Behavior.State.ContentOffset > 0,
                $"Reverse post-scroll was skipped at the body boundary; contentOffset={scene.Behavior.State.ContentOffset}.");
            Assert.Equal(0, scene.Behavior.State.ScrollOffset);
            Assert.Equal(0, scene.Viewer.Offset.Y);
        }
        finally { scene.Window.Close(); }
    }

    [AvaloniaFact]
    public void NativeWheelAtBodyEnd_ReclampsTheRetainedOffsetAfterLimitShrinks()
    {
        using var clock = new MotionTestClockScope();
        var scene = Show(reverse: false);
        try
        {
            scene.Bar.ContentPadding = new Thickness(4, 24); Render(scene.Window);
            scene.Behavior.State.ScrollOffset = scene.Behavior.State.ScrollOffsetLimit; Render(scene.Window);
            scene.Viewer.Offset = new Vector(0, 2000); Render(scene.Window);
            var original = scene.Behavior.State.ScrollOffset;
            scene.Bar.ContentPadding = new Thickness(4, 4); Render(scene.Window);
            Assert.Equal(original, scene.Behavior.State.ScrollOffset);
            Assert.True(original < scene.Behavior.State.ScrollOffsetLimit);
            scene.Window.MouseWheel(scene.Viewer.TranslatePoint(new Point(20, 20), scene.Window)!.Value, new Vector(0, -1));
            Render(scene.Window);
            // The retained out-of-range offset must pass through the next pre-scroll
            // setter even when the positive delta cannot move the body any farther.
            Assert.Equal(scene.Behavior.State.ScrollOffsetLimit, scene.Behavior.State.ScrollOffset);
        }
        finally { scene.Window.Close(); }
    }

    private static (MotionTestWindow Window, ScrollViewer Viewer, AppBarWithSearch Bar, SearchBarScrollBehavior Behavior) Show(bool reverse)
    {
        var behavior = new SearchBarScrollBehavior { ReverseLayout = reverse };
        var bar = new AppBarWithSearch { InputField = new Border { Height = 56 }, ScrollBehavior = behavior };
        var viewer = new ScrollViewer { IsScrollInertiaEnabled = false,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
            Content = new Border { Height = 1200, Background = Brushes.LightGray } };
        var screen = new Scaffold { TopBar = bar };
        var body = new Border { Child = viewer };
        body.Bind(Border.WidthProperty, new Binding("Bounds.Width") { Source = screen });
        body.Bind(Border.HeightProperty, new Binding("Bounds.Height") { Source = screen });
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty));
        screen.Content = body;
        var window = new MotionTestWindow { Width = 400, Height = 400, WindowDecorations = WindowDecorations.None };
        window.Styles.Add(new MaterialTheme()); window.Content = screen; window.Show(); Render(window);
        return (window, viewer, bar, behavior);
    }
    private static void Render(Window window)
    {
        for (var i = 0; i < 6; i++)
        {
            window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            using (window.CaptureRenderedFrame()) { }
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.TestSupport;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class AppBarWithSearchTests
{
    [AvaloniaTheory]
    [InlineData(600, false, 120, 360)]
    [InlineData(600, true, 120, 360)]
    [InlineData(1000, false, 320, 360)]
    [InlineData(280, false, 60, 160)]
    public void SlotsReserveEdgesAndCenterTheConstrainedSearch(int width, bool rtl, double expectedX, double expectedWidth)
    {
        var input = new Marker(360, 56);
        var nav = new Marker(48, 48); var action = new Marker(48, 48);
        var state = new SearchBarState();
        var bar = new AppBarWithSearch { State = state, InputField = input, NavigationIcon = nav, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        bar.Items.Add(action);
        var window = Show(bar, width);
        Assert.Equal(new Size(width, 64), bar.Bounds.Size);
        Assert.Equal(new Size(expectedWidth, 56), bar.CollapsedBar!.Bounds.Size);
        var p0 = bar.CollapsedBar.TranslatePoint(default, window)!.Value;
        var p1 = bar.CollapsedBar.TranslatePoint(new(expectedWidth, 0), window)!.Value;
        Assert.Equal(expectedX, Math.Min(p0.X, p1.X)); Assert.Equal(4, p0.Y);
        Assert.Same(bar.CollapsedBar, state.CollapsedBar);
        window.Close(); Assert.Null(state.CollapsedBar);
    }

    [AvaloniaFact]
    public void TemplateOnlyNavigationAndInputCreateRealSlotsAndConsumeInsets()
    {
        var input = new Marker(400, 72);
        var bar = new AppBarWithSearch { WindowInsets = new(12, 20, 24, 16),
            InputFieldTemplate = new FuncDataTemplate<object?>((_, _) => input),
            NavigationIconTemplate = new FuncDataTemplate<object?>((_, _) => new Marker(48, 48)) };
        var root = new Grid { Children = { bar } };
        MaterialWindowInsets.SetConsumePadding(root, new(4, 8, 6, 10));
        var window = Show(root);
        Assert.Equal(98, bar.Bounds.Height);
        Assert.Equal(16, input.TranslatePoint(default, window)!.Value.Y);
        Assert.Equal(new Thickness(12, 20, 24, 16), input.GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
        window.Close();
    }

    [AvaloniaFact]
    public void ScrollPolicyPreservesClampConsumptionReverseLayoutAndDisabledBranches()
    {
        var state = new SearchBarScrollState(-64, 0, 0);
        var behavior = new SearchBarScrollBehavior(state);
        Assert.Equal(-80, behavior.OnPreScroll(-80)); Assert.Equal(-64, state.ScrollOffset);
        Assert.Equal(0, behavior.OnPreScroll(-1));
        Assert.Equal(0, behavior.OnPostScroll(-20, 0)); Assert.Equal(-20, state.ContentOffset);
        behavior.ReverseLayout = true;
        Assert.Equal(0, behavior.OnPreScroll(20)); Assert.Equal(-44, state.ScrollOffset);
        Assert.Equal(0, behavior.OnPostScroll(-10, -20)); Assert.Equal(-20, state.ContentOffset);
        Assert.Equal(30, behavior.OnPostScroll(0, 30)); Assert.Equal(-14, state.ScrollOffset); Assert.Equal(10, state.ContentOffset);
        behavior.CanScroll = false;
        Assert.Equal(0, behavior.OnPreScroll(-20)); Assert.Equal(0, behavior.OnPostScroll(0, 20)); Assert.Equal(-14, state.ScrollOffset);
        state.ScrollOffsetLimit = -8; Assert.Equal(-14, state.ScrollOffset);
        state.ScrollOffset = state.ScrollOffset; Assert.Equal(-8, state.ScrollOffset);
    }

    [AvaloniaFact]
    public void ScrollChangesMeasuredHeightAndRetainsFullContentGeometry()
    {
        var behavior = new SearchBarScrollBehavior();
        var bar = new AppBarWithSearch { InputField = new Marker(360, 56), ScrollBehavior = behavior };
        var window = Show(bar);
        Assert.Equal(-64, behavior.State.ScrollOffsetLimit);
        behavior.OnPreScroll(-24); Flush(window);
        Assert.Equal(40, bar.Bounds.Height);
        Assert.Equal(-20, bar.CollapsedBar!.TranslatePoint(default, window)!.Value.Y);
        Assert.Equal(56, bar.CollapsedBar!.Bounds.Height);
        Assert.Equal(-64, behavior.State.ScrollOffsetLimit);
        behavior.OnPreScroll(-100); Flush(window); Assert.Equal(0, bar.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void ColorAndElevationScopesFollowBaseTransparencyAndLocalContentOverrides()
    {
        var nav = new Marker(48, 48); var action = new Marker(48, 48);
        var bar = new AppBarWithSearch { InputField = new Marker(360, 56), NavigationIcon = nav, NavigationIconContentColor = Colors.Red, ActionIconContentColor = Colors.Blue,
            TonalElevation = 3, ShadowElevation = 2 };
        bar.Items.Add(action);
        var window = Show(bar);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)nav.GetValue(TextElement.ForegroundProperty)!).Color);
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)action.GetValue(TextElement.ForegroundProperty)!).Color);
        Assert.Equal(0, bar.CollapsedBar!.ShadowElevation);
        bar.Background = Brushes.Transparent; Flush(window);
        Assert.Equal(2, bar.CollapsedBar.ShadowElevation); Assert.Equal(3, bar.CollapsedBar.TonalElevation);
        action.SetValue(TextElement.ForegroundProperty, Brushes.Green);
        bar.ActionIconContentColor = Colors.Yellow; Flush(window);
        Assert.Equal(Brushes.Green, action.GetValue(TextElement.ForegroundProperty));
        bar.Resources[Comp.SearchBar.AppBarSearchVerticalPadding] = 8d; Flush(window); Assert.Equal(72, bar.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void InitiallyOverlappedContainerPublishesItsColorWithoutWaitingForAnAnimation()
    {
        var behavior = new SearchBarScrollBehavior(new SearchBarScrollState(-64, 0, -64));
        var bar = new AppBarWithSearch
        {
            InputField = new Marker(360, 56), ScrollBehavior = behavior,
            Background = Brushes.Red, ScrolledContainerColor = Colors.Blue,
            SearchBarContainerColor = Colors.Green, ScrolledSearchBarContainerColor = Colors.Yellow
        };
        var root = new Grid { Children = { bar } };
        var window = Show(root);
        var surface = bar.GetVisualDescendants().OfType<MaterialSurface>().First();
        Assert.Equal(Colors.Blue, ((ISolidColorBrush)surface.Background!).Color);
        Assert.Equal(Colors.Yellow, ((ISolidColorBrush)bar.CollapsedBar!.Background!).Color);
        root.Children.Clear(); Flush(window);
        behavior.State.ContentOffset = 0;
        root.Children.Add(bar); Flush(window);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)surface.Background!).Color);
        Assert.Equal(Colors.Green, ((ISolidColorBrush)bar.CollapsedBar!.Background!).Color);
        window.Close();
    }

    [AvaloniaFact]
    public void FullScreenCollapseHidesOnlyTheSearchSurfaceUntilTheCollapsedThreshold()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState(SearchBarValue.Expanded);
        var bar = new AppBarWithSearch { State = state, InputField = new Marker(360, 56) };
        var popup = new ExpandedFullScreenSearchBar { State = state, InputField = new Marker(360, 56) };
        var window = Show(new Grid { Children = { bar, popup } }); clock.SendFrame(TimeSpan.Zero);
        Assert.Equal(1, bar.CollapsedBar!.Opacity);
        clock.Track(state.AnimateToCollapsedAsync()); Flush(window);
        Assert.Equal(0, bar.CollapsedBar.Opacity); Assert.Equal(64, bar.Bounds.Height);
        Frames(clock, window, 16, 1008);
        Assert.Equal(1, bar.CollapsedBar.Opacity);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DisablingScrollGatesNewInputWithoutCancellingAnExistingNestedSettle()
    {
        using var clock = new MotionTestClockScope();
        var behavior = new SearchBarScrollBehavior();
        var bar = new AppBarWithSearch { InputField = new Marker(360, 56), ScrollBehavior = behavior };
        var window = Show(bar);
        clock.SendFrame(TimeSpan.Zero);
        behavior.OnPreScroll(-24);
        var settling = behavior.OnPostFlingAsync(0);
        clock.Track(settling);
        Frames(clock, window, 16, 48);
        behavior.CanScroll = false;
        Assert.Equal(0, behavior.OnPreScroll(-64));
        Assert.False(settling.IsCanceled);
        Frames(clock, window, 64, 1024);
        Assert.True(settling.IsCompletedSuccessfully);
        Assert.Equal(0, behavior.State.ScrollOffset);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void DirectDragRetainsItsInitialEnabledValueWhileNestedInputReadsCanScrollLive()
    {
        using var clock = new MotionTestClockScope();
        var behavior = new SearchBarScrollBehavior();
        var bar = new AppBarWithSearch { InputField = new Marker(360, 56), ScrollBehavior = behavior };
        var window = Show(bar);
        clock.SendFrame(TimeSpan.Zero);
        var mouse = new HeadlessMouseInput(window);
        using (window.CaptureRenderedFrame()) { } // commit hit-test geometry before raw input
        mouse.Send(1001, "move", new Point(300, 32));
        mouse.Send(1001, "down", new Point(300, 32));
        mouse.Send(1017, "move", new Point(300, 12));
        Flush(window);
        Assert.Equal(-19.875f, behavior.State.ScrollOffset);
        behavior.CanScroll = false;
        Assert.Equal(0, behavior.OnPreScroll(-20));
        // This test isolates the initial enabled policy: a stationary pause removes fling velocity.
        // Immediate-release velocity and moving-content behavior are covered by pointer-velocity parity.
        mouse.Send(1080, "up", new Point(300, 12));
        Frames(clock, window, 16, 1008);
        Assert.Equal(0, behavior.State.ScrollOffset);
        using (window.CaptureRenderedFrame()) { }
        mouse.Send(2033, "down", new Point(300, 32));
        mouse.Send(2049, "move", new Point(300, 12));
        Flush(window);
        Assert.Equal(-19.875f, behavior.State.ScrollOffset);
        mouse.Send(2112, "up", new Point(300, 12));
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ReplacingTemplateReleasesMotionAndDefaultActionStylesAndReconnectsTheState()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState();
        var scrolling = new SearchBarScrollBehavior();
        var action = new Marker(48, 48);
        var originalStyles = action.Styles.Count;
        var bar = new AppBarWithSearch
        {
            State = state, InputField = new Marker(360, 56), ScrollBehavior = scrolling
        };
        bar.Items.Add(action);
        var window = Show(bar);
        var template = bar.Template;
        Assert.Equal(originalStyles + 1, action.Styles.Count);
        clock.SendFrame(TimeSpan.Zero);
        scrolling.OnPreScroll(-32);
        var settling = scrolling.OnPostFlingAsync(0);
        clock.Track(settling);
        Frames(clock, window, 16, 48);
        bar.Template = new FuncControlTemplate<AppBarWithSearch>((_, _) => new Border());
        Flush(window);
        Assert.True(settling.IsCanceled);
        Assert.Null(state.CollapsedBar);
        Assert.Equal(originalStyles, action.Styles.Count);
        bar.Template = template;
        Flush(window);
        Assert.Same(bar.CollapsedBar, state.CollapsedBar);
        Assert.Equal(originalStyles + 1, action.Styles.Count);
        clock.Track(scrolling.OnPostFlingAsync(0));
        Frames(clock, window, 64, 1024);
        window.Close();
        Assert.Equal(originalStyles, action.Styles.Count);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SettlingIsInterruptedByReplacementAndDetachAndThemeValuesStayIntact()
    {
        using var clock = new MotionTestClockScope();
        var behavior = new SearchBarScrollBehavior();
        var bar = new AppBarWithSearch { InputField = new Marker(360, 56), ScrollBehavior = behavior };
        var root = new Grid { Children = { bar } }; var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        behavior.OnPreScroll(-40);
        var settling = behavior.OnPostFlingAsync(0); clock.Track(settling);
        Frames(clock, window, 16, 48);
        Assert.InRange(behavior.State.ScrollOffset, -64, -40);
        var next = new SearchBarScrollBehavior(); bar.ScrollBehavior = next; Flush(window);
        Assert.True(settling.IsCanceled);
        next.OnPreScroll(-20); clock.Track(next.OnPostFlingAsync(0)); Frames(clock, window, 64, 96);
        root.Children.Clear(); Flush(window); Assert.Equal(0, clock.PendingFrameCount);
        root.Children.Add(bar); Flush(window); Assert.Equal(-64, next.State.ScrollOffsetLimit);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    private sealed class Marker(double width, double height) : Control
    { protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height)); }
    private static MotionTestWindow Show(Control root, double width = 600)
    {
        var window = new MotionTestWindow { Width = width, Height = 600 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = root; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    { for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); } }
}

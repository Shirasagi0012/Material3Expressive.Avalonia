using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class ExpandedDockedSearchBarTests
{
    [AvaloniaTheory]
    [InlineData(600, 40, 240)]
    [InlineData(600, 700, 400)]
    [InlineData(240, 700, 160)]
    public void ExpandedHeightConstrainsWholeSurfaceAndDeclarationTakesNoSpace(int windowHeight, int resultHeight, int expectedHeight)
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (bar, popup, root) = Create(state, resultHeight);
        var window = Show(root, height: windowHeight);
        Assert.True(popup.IsPresented);
        Assert.Equal(default, popup.DesiredSize);
        Assert.Equal(new Size(360, 56), bar.Bounds.Size);
        Assert.Equal(new Size(360, expectedHeight), popup.Presentation!.Bounds.Size);
        Assert.Equal(new Point(16, 24), popup.Presentation!.TranslatePoint(default, window));
        window.Height = 300; Flush(window);
        Assert.Equal(200, popup.Presentation.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void ZeroWidthAnchorIsARealConstraintAndIsDifferentFromAnAbsentAnchor()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (bar, popup, root) = Create(state);
        ((Border)bar.Content!).Width = 0;
        var window = Show(root);
        Assert.Equal(0, popup.Presentation!.Bounds.Width);
        root.Children.Remove(bar); Flush(window);
        Assert.Equal(360, popup.Presentation.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public async Task StateRetainsRawProgressAndUsesUpstreamThresholdAndTargetRule()
    {
        var state = new SearchBarState();
        await state.SnapToAsync(.02f);
        Assert.Equal(SearchBarValue.Collapsed, state.CurrentValue);
        await state.SnapToAsync(.02001f);
        Assert.Equal(SearchBarValue.Expanded, state.CurrentValue);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        await state.SnapToAsync(1.2f);
        Assert.Equal(1, state.Progress);
        Assert.Equal(1.2f, state.RawProgress);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        await state.SnapToAsync(1);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        Assert.Equal(0, state.ContentProgress);
        await state.SnapToAsync(-.3f);
        Assert.Equal(0, state.Progress);
        Assert.Equal(SearchBarValue.Collapsed, state.CurrentValue);
    }

    [AvaloniaFact]
    public void ReversePreservesProgressAndDetachCancelsOnlyThisStatesAnimations()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState();
        var (bar, popup, root) = Create(state);
        var window = Show(root);
        clock.SendFrame(TimeSpan.Zero);
        var expand = state.AnimateToExpandedAsync(); clock.Track(expand);
        Frames(clock, window, 16, 96);
        var before = state.Progress;
        Assert.InRange(before, .02f, .99f); Assert.True(popup.IsPresented);
        var collapse = state.AnimateToCollapsedAsync(); clock.Track(collapse);
        Assert.Equal(before, state.Progress);
        Frames(clock, window, 112, 960);
        Assert.True(expand.IsCanceled); Assert.True(collapse.IsCompletedSuccessfully);
        Assert.Equal(0, state.Progress); Assert.False(popup.IsPresented);
        var next = state.AnimateToExpandedAsync(); clock.Track(next);
        Frames(clock, window, 976, 1040);
        root.Children.Clear(); Flush(window);
        Assert.True(next.IsCanceled); Assert.False(popup.IsPresented);
        Assert.Equal(0, clock.PendingFrameCount);
        root.Children.Add(bar); root.Children.Add(popup); Flush(window);
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 1056, 2208);
        Assert.Equal(1, state.Progress); Assert.True(popup.IsPresented);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void EscapeAndOutsidePressCollapseAndRestoreFocusAfterExit()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState();
        var (bar, popup, root) = Create(state);
        var anchor = new Button { Content = "Open", Width = 360, Height = 56 };
        var input = new TextBox { Width = 360, Height = 56 };
        bar.Content = anchor; popup.InputField = input;
        var window = Show(root); anchor.Focus();
        clock.SendFrame(TimeSpan.Zero); clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 16, 1008);
        Assert.True(input.IsFocused);
        input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Escape });
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue); Assert.True(popup.IsPresented);
        Frames(clock, window, 1024, 2016);
        Assert.False(popup.IsPresented); Assert.True(anchor.IsFocused);
        Assert.InRange(state.Progress, float.Epsilon, .02f);
        Assert.False(state.IsAnimating); // Dismissal coroutine belonged to the removed popup.
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 2032, 3024);
        window.MouseDown(new Point(550, 550), MouseButton.Left); Flush(window);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        window.MouseUp(new Point(550, 550), MouseButton.Left);
        Frames(clock, window, 3040, 4032); Assert.False(popup.IsPresented);
        window.Close();
    }

    [AvaloniaFact]
    public async Task TemplatesAreLazyAndChangingStateReleasesOldPresentation()
    {
        var state = new SearchBarState();
        var (bar, popup, root) = Create(state);
        var builds = 0;
        popup.Content = "results";
        popup.ContentTemplate = new FuncDataTemplate<string>((_, _) => { builds++; return new Border { Height = 40 }; });
        var window = Show(root);
        Assert.Equal(0, builds);
        await state.SnapToAsync(1); Flush(window);
        Assert.True(popup.IsPresented); Assert.Equal(1, builds);
        Assert.Same(popup, ((Control)popup.InputField!).Parent);
        var other = new SearchBarState(); popup.State = other; Flush(window);
        Assert.False(popup.IsPresented);
        await state.SnapToAsync(.5f); Flush(window); Assert.False(popup.IsPresented);
        popup.State = state; Flush(window); Assert.True(popup.IsPresented);
        popup.Resources[Comp.SearchBar.MinimumResultsHeight] = 180d;
        popup.Resources[Comp.SearchBar.ContainerColor] = Colors.Red; Flush(window);
        Assert.Equal(180, popup.Presentation!.Bounds.Height);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)((MaterialSurface)popup.Presentation).Background!).Color);
        var count = builds;
        await state.SnapToAsync(0); Flush(window);
        Assert.False(((Control)popup.InputField!).IsAttachedToVisualTree());
        await state.SnapToAsync(1); Flush(window);
        Assert.Equal(count + 1, builds);
        window.Close();
    }

    [AvaloniaFact]
    public async Task ReplacingBothStatesOnAnAlreadyArrangedPairPreservesTheAnchorAndDisconnectsTheOldState()
    {
        var first = new SearchBarState(SearchBarValue.Expanded);
        var (bar, popup, root) = Create(first);
        var window = Show(root);
        var second = new SearchBarState(SearchBarValue.Expanded);
        bar.State = second; popup.State = second; Flush(window);
        Assert.True(popup.IsPresented);
        Assert.Equal(new Point(16, 24), popup.Presentation!.TranslatePoint(default, window));
        await first.SnapToAsync(0); Flush(window);
        Assert.True(popup.IsPresented);
        await second.SnapToAsync(0); Flush(window);
        Assert.False(popup.IsPresented);
        window.Close();
    }

    [AvaloniaFact]
    public async Task MovingTheSameAnchorDoesNotInvalidateAnOpenPopupButReopeningReadsItsCoordinates()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (bar, popup, root) = Create(state);
        var window = Show(root);
        bar.Margin = new(32, 48, 0, 0); Flush(window);
        Assert.Equal(new Point(16, 24), popup.Presentation!.TranslatePoint(default, window));
        await state.SnapToAsync(0); Flush(window);
        await state.SnapToAsync(1); Flush(window);
        Assert.Equal(new Point(32, 48), popup.Presentation!.TranslatePoint(default, window));
        window.Close();
    }

    [AvaloniaFact]
    public void HandledEscapeAndNestedPopupDoNotDismissTheSearchSurface()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (bar, popup, root) = Create(state);
        var input = new TextBox { Width = 360, Height = 56 };
        popup.InputField = input;
        var window = Show(root);
        input.AddHandler(InputElement.KeyDownEvent, (_, e) => { if (e.Key == Key.Escape) e.Handled = true; });
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Flush(window);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        var menu = new ContextMenu { Items = { new MenuItem { Header = "Result" } } };
        menu.Open(input); Flush(window);
        Assert.True(menu.IsOpen);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Flush(window);
        Assert.False(menu.IsOpen); Assert.True(popup.IsPresented);
        window.Close();
    }

    [AvaloniaFact]
    public void AutomationExposesInputOnceThroughTheOverlayAndRemovesItOnClose()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        var input = new TextBox { Width = 360, Height = 56 }; popup.InputField = input;
        var window = Show(root);
        var windowPeer = ControlAutomationPeer.CreatePeerForElement(window)!;
        Assert.Single(Walk(windowPeer).OfType<ControlAutomationPeer>(), peer => peer.Owner == input);
        _ = state.SnapToAsync(0); Flush(window);
        Assert.DoesNotContain(Walk(windowPeer).OfType<ControlAutomationPeer>(), peer => peer.Owner == input);
        window.Close();
        static IEnumerable<AutomationPeer> Walk(AutomationPeer peer)
        {
            yield return peer;
            foreach (var child in peer.GetChildren())
                foreach (var descendant in Walk(child)) yield return descendant;
        }
    }

    [AvaloniaFact]
    public void IndependentContentAnimationOutlivesMainProgressAndCallerCancellationStopsBoth()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState(SearchBarValue.Collapsed, new SnapSpec<float>(), new SnapSpec<float>(),
            new TweenSpec<float>(TimeSpan.FromSeconds(1), easing: new Avalonia.Animation.Easings.LinearEasing()));
        var (_, popup, root) = Create(state);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        using var cancellation = new CancellationTokenSource();
        var expand = state.AnimateToExpandedAsync(cancellation.Token); clock.Track(expand);
        Frames(clock, window, 16, 512);
        Assert.Equal(1, state.Progress); Assert.False(state.IsAnimating); Assert.True(popup.IsPresented);
        Assert.InRange(state.ContentProgress, .45f, .55f); Assert.False(expand.IsCompleted);
        var content = state.ContentProgress;
        _ = state.SnapToAsync(.5f); Flush(window);
        Assert.Equal(content, state.ContentProgress); // Snap changes only the main animatable.
        cancellation.Cancel(); Flush(window);
        Assert.True(expand.IsCanceled); Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(MouseButton.Right)]
    [InlineData(MouseButton.Middle)]
    public void AnyOutsideButtonPressDismissesAndDoesNotActivateTheUnderlyingControl(MouseButton button)
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        var behind = new Button { Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        root.Children.Insert(0, behind); var presses = 0;
        behind.AddHandler(InputElement.PointerPressedEvent, (_, _) => presses++, Avalonia.Interactivity.RoutingStrategies.Bubble, true);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        window.MouseDown(new Point(550, 550), button); Flush(window);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue); Assert.Equal(0, presses);
        window.MouseUp(new Point(550, 550), button);
        Frames(clock, window, 16, 1008); Assert.False(popup.IsPresented); window.Close();
    }

    [AvaloniaFact]
    public void RetemplatingAnOpenDeclarationReleasesTheOldPopupAndKeepsOneFocusScope()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        var input = new TextBox { Width = 360, Height = 56 }; popup.InputField = input;
        var window = Show(root); var previous = popup.Presentation;
        var template = popup.Template;
        popup.Template = null; Flush(window);
        Assert.False(popup.IsPresented); Assert.False(previous!.IsAttachedToVisualTree());
        popup.Template = template; Flush(window);
        Assert.True(popup.IsPresented); Assert.True(input.IsFocused);
        Assert.NotSame(previous, popup.Presentation);
        Assert.Single(window.GetVisualDescendants().OfType<SearchBarPopupPresenter>());
        window.Close();
    }

    private static (SearchBar Bar, ExpandedDockedSearchBar Popup, Grid Root) Create(SearchBarState state, double height = 40)
    {
        var bar = new SearchBar { State = state, Content = new Border { Width = 360, Height = 56 }, Margin = new(16, 24, 0, 0) };
        var popup = new ExpandedDockedSearchBar { State = state, InputField = new Border { Width = 360, Height = 56 }, Content = new Marker(height) };
        return (bar, popup, new Grid { Children = { bar, popup } });
    }
    private sealed class Marker(double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, Math.Min(height, availableSize.Height));
    }
    private static MotionTestWindow Show(Control content, double width = 600, double height = 600)
    {
        var window = new MotionTestWindow { Width = width, Height = height };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    { for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); } }
}

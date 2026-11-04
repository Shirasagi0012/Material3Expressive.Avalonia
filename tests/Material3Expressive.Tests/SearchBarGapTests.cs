using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class SearchBarGapTests
{
    [AvaloniaFact]
    public void GapFactoryDelaysContentAndReversesWithoutResettingProgress()
    {
        using var clock = new MotionTestClockScope();
        var state = SearchBarState.CreateWithGap();
        var (_, popup, root) = Create(state);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        var opening = state.AnimateToExpandedAsync(); clock.Track(opening);
        Frames(clock, window, 16, 48);
        Assert.True(state.Progress > 0);
        Assert.Equal(0, state.ContentProgress);
        Frames(clock, window, 64, 96);
        Assert.InRange(state.ContentProgress, .001f, .999f);
        var progress = state.Progress;
        var content = state.ContentProgress;
        var closing = state.AnimateToCollapsedAsync(); clock.Track(closing);
        Assert.Equal(progress, state.Progress);
        Assert.Equal(content, state.ContentProgress);
        Frames(clock, window, 112, 1008);
        Assert.True(opening.IsCanceled);
        Assert.True(closing.IsCompletedSuccessfully);
        Assert.Equal(0, state.Progress);
        Assert.Equal(0, state.ContentProgress);
        Assert.False(popup.IsPresented);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void GapFactoryResolvesItsOwnLiveThemeMotionAndPreservesSeededContent()
    {
        var state = SearchBarState.CreateWithGap(SearchBarValue.Expanded);
        Assert.Equal(1, state.ContentProgress);
        var (bar, _, root) = Create(state);
        var window = Show(root);
        var expand = new Material3Expressive.Foundation.Motion.SpringSpec(.8, 321);
        var collapse = new Material3Expressive.Foundation.Motion.SpringSpec(.9, 654);
        root.Resources[Comp.SearchBar.GapExpandAnimation] = expand;
        root.Resources[Comp.SearchBar.GapCollapseAnimation] = collapse;
        Flush(window);
        Assert.Equal(321, Assert.IsType<SpringSpec<float>>(state.AnimationSpec(true)).Stiffness);
        Assert.Equal(654, Assert.IsType<SpringSpec<float>>(state.AnimationSpec(false)).Stiffness);
        Assert.NotEqual(expand, bar.ExpandAnimation);
        root.Resources[Comp.SearchBar.GapExpandAnimation] = new Material3Expressive.Foundation.Motion.SpringSpec(.8, 432);
        Flush(window);
        Assert.Equal(432, Assert.IsType<SpringSpec<float>>(state.AnimationSpec(true)).Stiffness);
        window.Close();
    }

    [AvaloniaFact]
    public void ScrimFeedbackUsesLiveAmbientForegroundOutsideTheSearchSurfaceColorScope()
    {
        var (_, popup, root) = Create(new SearchBarState(SearchBarValue.Expanded));
        root.SetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty, Brushes.Blue);
        var window = Show(root);
        var scrim = window.GetVisualDescendants().OfType<SearchBarScrimButton>().Single();
        Assert.Equal(Brushes.Blue, scrim.Foreground);
        Assert.NotEqual(scrim.Foreground, popup.Foreground);
        root.SetValue(Avalonia.Controls.Documents.TextElement.ForegroundProperty, Brushes.Red); Flush(window);
        Assert.Equal(Brushes.Red, scrim.Foreground);
        window.Close();
    }
    [AvaloniaFact]
    public void InputAndNestedButtonConsumeTheirClicksWithoutDismissingTheAncestorScrim()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        var input = new TextBox { Width = 360, Height = 56 };
        var result = new Button { Content = "Action", Width = 120, Height = 40 };
        var clicked = false;
        result.Click += (_, _) => clicked = true;
        popup.InputField = input; popup.Content = result;
        var window = Show(root);
        foreach (var child in new Control[] { input, result })
        {
            var point = child.TranslatePoint(new Point(20, 20), window)!.Value;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        }
        Assert.True(clicked);
        window.Close();
    }
    [AvaloniaTheory]
    [InlineData(true, 16)]
    [InlineData(false, 224)]
    public void RtlScrimUsesRelativeOffsetWhileNoScrimKeepsPhysicalAnchor(bool scrim, double left)
    {
        var (_, popup, root) = Create(new SearchBarState(SearchBarValue.Expanded));
        root.FlowDirection = FlowDirection.RightToLeft;
        if (!scrim) popup.DropdownScrimColor = Colors.Transparent;
        var window = Show(root);
        var surface = popup.Presentation!;
        var start = surface.TranslatePoint(default, window)!.Value.X;
        var end = surface.TranslatePoint(new(surface.Bounds.Width, 0), window)!.Value.X;
        Assert.Equal(left, Math.Min(start, end));
        window.Close();
    }
    [AvaloniaTheory]
    [InlineData(600, 40, 240, 40)]
    [InlineData(600, 700, 300, 242)]
    [InlineData(240, 700, 120, 62)]
    public void SeparateSurfacesUseGapAndHalfWindowMaximum(int windowHeight, int contentHeight, int expectedHeight, int expectedResults)
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state, contentHeight);
        var window = Show(root, windowHeight);
        Assert.True(popup.IsPresented);
        Assert.Equal(default, popup.DesiredSize);
        Assert.Equal(new Size(360, expectedHeight), popup.Presentation!.Bounds.Size);
        Assert.Equal(new Size(360, expectedResults), popup.Dropdown!.Bounds.Size);
        Assert.Equal(new Point(16, 82), popup.Dropdown.TranslatePoint(default, window));
        Assert.Equal(new Point(16, 82), ((Control)popup.Content!).TranslatePoint(default, window));
        Assert.Equal(new CornerRadius(12), ((MaterialSurface)popup.Dropdown).CornerRadius);
        Assert.Equal(Colors.Transparent, ((ISolidColorBrush)((MaterialSurface)popup.Presentation).Background!).Color);
        Assert.DoesNotContain(popup.Presentation.GetVisualDescendants(), control => control.Name == "PART_Divider");
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void ScrimDismissesOnReleaseWhileNoScrimDismissesOnPress(bool scrim, bool zeroAlphaColor)
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        if (!scrim) popup.DropdownScrimColor = Colors.Transparent;
        if (zeroAlphaColor) popup.DropdownScrimColor = Color.FromArgb(0, 255, 0, 0);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        window.MouseDown(new Point(550, 550), MouseButton.Left); Flush(window);
        Assert.Equal(scrim ? SearchBarValue.Expanded : SearchBarValue.Collapsed, state.TargetValue);
        window.MouseUp(new Point(550, 550), MouseButton.Left); Flush(window);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        Frames(clock, window, 16, 1008);
        Assert.False(popup.IsPresented);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(MouseButton.Right)]
    [InlineData(MouseButton.Middle)]
    public void ScrimOnlyAcceptsPrimaryButtonClicks(MouseButton button)
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, _, root) = Create(state);
        var window = Show(root);
        window.MouseDown(new Point(550, 550), button); window.MouseUp(new Point(550, 550), button); Flush(window);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        window.Close();
    }

    [AvaloniaFact]
    public void ContentFadeContinuesAfterMainProgressHasFinishedAndDoesNotFadeTheBackground()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState(SearchBarValue.Collapsed, new SnapSpec<float>(), new SnapSpec<float>(),
            new TweenSpec<float>(TimeSpan.FromSeconds(1), easing: new Avalonia.Animation.Easings.LinearEasing()));
        var (_, popup, root) = Create(state);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 16, 512);
        Assert.Equal(1, state.Progress); Assert.False(state.IsAnimating);
        var visibility = popup.Presentation!.GetVisualDescendants().OfType<AnimatedVisibility>().Single();
        Assert.InRange(visibility.Opacity, .45, .55);
        Assert.Equal(1, popup.Dropdown!.Opacity);
        Frames(clock, window, 528, 1104); Assert.Equal(1, visibility.Opacity);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public async Task SnappingMainProgressDoesNotInventContentProgressOrAnExpandedTarget()
    {
        var state = new SearchBarState();
        var (_, popup, root) = Create(state);
        var window = Show(root);
        await state.SnapToAsync(.1f); Flush(window);
        Assert.True(popup.IsPresented);
        var visibility = popup.Presentation!.GetVisualDescendants().OfType<AnimatedVisibility>().Single();
        Assert.False(visibility.Visible);
        await state.SnapToAsync(.5f); Flush(window); Assert.False(visibility.Visible);
        await state.SnapToAsync(1); Flush(window); Assert.True(visibility.Visible);
        Assert.Equal(0, visibility.Opacity);
        window.Close();
    }

    [AvaloniaFact]
    public void TokensUpdateLiveAndLocalValuesKeepTheirPriority()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        var window = Show(root);
        popup.Resources[Comp.SearchBar.DropdownGapSize] = 10d;
        popup.Resources[Comp.SearchBar.DropdownContainerShape] = new CornerRadius(4);
        popup.Resources[Comp.SearchBar.DropdownScrimColor] = Colors.Transparent;
        Flush(window);
        Assert.Equal(10, popup.DropdownGapSize);
        Assert.Equal(new CornerRadius(4), ((MaterialSurface)popup.Dropdown!).CornerRadius);
        Assert.Equal(new Point(16, 90), popup.Dropdown.TranslatePoint(default, window));
        popup.DropdownGapSize = 6;
        popup.Resources[Comp.SearchBar.DropdownGapSize] = 12d; Flush(window);
        Assert.Equal(6, popup.DropdownGapSize);
        Assert.Equal(new Point(16, 86), popup.Dropdown.TranslatePoint(default, window));
        window.Close();
    }

    [AvaloniaFact]
    public void ReversalStateReplacementAndDetachReleaseSlideAndContentAnimations()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState();
        var (bar, popup, root) = Create(state);
        var builds = 0;
        popup.Content = "results";
        popup.ContentTemplate = new FuncDataTemplate<string>((_, _) => { builds++; return new Marker(40); });
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        Assert.Equal(0, builds);
        var first = state.AnimateToExpandedAsync(); clock.Track(first); Frames(clock, window, 16, 160);
        Assert.Equal(1, builds); Assert.True(popup.IsPresented);
        var before = state.Progress;
        clock.Track(state.AnimateToCollapsedAsync()); Assert.Equal(before, state.Progress);
        Frames(clock, window, 176, 224);
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 240, 1200);
        Assert.True(first.IsCanceled); Assert.Equal(1, state.Progress);
        var replacement = new SearchBarState(SearchBarValue.Expanded);
        bar.State = replacement; popup.State = replacement; Flush(window);
        Assert.True(popup.IsPresented); Assert.Equal(2, builds);
        var collapse = replacement.AnimateToCollapsedAsync(); clock.Track(collapse); Frames(clock, window, 1216, 1248);
        root.Children.Clear(); Flush(window);
        Assert.True(collapse.IsCanceled); Assert.False(popup.IsPresented);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    }

    private static (SearchBar Bar, ExpandedDockedSearchBarWithGap Popup, Grid Root) Create(SearchBarState state, double height = 40)
    {
        var bar = new SearchBar { State = state, Content = new Marker(56), Margin = new(16, 24, 0, 0) };
        var popup = new ExpandedDockedSearchBarWithGap { State = state, InputField = new Marker(56), Content = new Marker(height) };
        return (bar, popup, new Grid { Children = { bar, popup } });
    }
    private sealed class Marker(double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(360, availableSize.Width), Math.Min(height, availableSize.Height));
    }
    private static MotionTestWindow Show(Control root, double height = 600)
    {
        var window = new MotionTestWindow { Width = 600, Height = height };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = root; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    { for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); } }
}

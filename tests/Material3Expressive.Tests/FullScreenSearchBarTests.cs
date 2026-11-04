using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class FullScreenSearchBarTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExpandedSurfaceFillsClientAndInputAndResultsRespectPhysicalInsets(bool rtl)
    {
        var (_, popup, root) = Create(new SearchBarState(SearchBarValue.Expanded));
        root.FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        popup.WindowInsets = new(12, 20, 24, 16);
        var window = Show(root);
        Assert.Equal(default, popup.DesiredSize);
        Assert.Equal(new Size(600, 600), popup.Presentation!.Bounds.Size);
        var surfaceStart = popup.Presentation.TranslatePoint(default, window)!.Value;
        var surfaceEnd = popup.Presentation.TranslatePoint(new(600, 600), window)!.Value;
        Assert.Equal(new Point(0, 0), new Point(Math.Min(surfaceStart.X, surfaceEnd.X), Math.Min(surfaceStart.Y, surfaceEnd.Y)));
        var input = (Control)popup.InputField!;
        var results = (Control)popup.Content!;
        Assert.Equal(new Size(564, 56), input.Bounds.Size);
        Assert.Equal(new Size(564, 40), results.Bounds.Size);
        Assert.Equal(28, input.TranslatePoint(default, window)!.Value.Y);
        Assert.Equal(93, results.TranslatePoint(default, window)!.Value.Y);
        var left = Math.Min(input.TranslatePoint(default, window)!.Value.X, input.TranslatePoint(new(input.Bounds.Width, 0), window)!.Value.X);
        Assert.Equal(12, left);
        window.Close();
    }

    [AvaloniaFact]
    public async Task IntermediateProgressUsesSeparateSurfaceAndInputInterpolationAndCustomShapeSwitch()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state);
        var window = Show(root);
        await state.SnapToAsync(.5f); Flush(window);
        var surface = (MaterialSurface)popup.Presentation!;
        Assert.Equal(new Size(480, 328), surface.Bounds.Size);
        Assert.Equal(new Point(8, 12), surface.TranslatePoint(default, window));
        var input = (Control)popup.InputField!;
        Assert.Equal(new Size(420, 56), input.Bounds.Size);
        Assert.Equal(new Point(12, 16), input.TranslatePoint(default, window));
        Assert.Equal(new CornerRadius(14), surface.CornerRadius);
        popup.CornerRadius = new(12, 8, 4, 2); Flush(window);
        Assert.Equal(default, surface.CornerRadius);
        await state.SnapToAsync(.25f); Flush(window);
        Assert.Equal(new CornerRadius(12, 8, 4, 2), surface.CornerRadius);
        window.Close();
    }

    [AvaloniaFact]
    public void AncestorInsetConsumptionIsExcludedAndRemainingInsetsReachNestedContent()
    {
        var (_, popup, root) = Create(new SearchBarState(SearchBarValue.Expanded));
        popup.WindowInsets = new(12, 20, 24, 16);
        MaterialWindowInsets.SetConsumePadding(root, new(4, 8, 6, 10));
        var window = Show(root);
        var input = (Control)popup.InputField!;
        Assert.Equal(new Point(8, 20), input.TranslatePoint(default, window));
        Assert.Equal(new Thickness(12, 20, 24, 16), ((Control)popup.Content!).GetValue(MaterialWindowInsets.ConsumedPaddingProperty));
        MaterialWindowInsets.SetConsumePadding(root, new(20, 30, 30, 30)); Flush(window);
        Assert.Equal(new Point(0, 8), input.TranslatePoint(default, window));
        window.Close();
    }

    [AvaloniaFact]
    public async Task ZeroOrMissingAnchorUsesDefaultInputHeightAndWindowResizeRecomputesLayout()
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var (bar, popup, root) = Create(state);
        bar.Content = new Border { Width = 0, Height = 0 };
        var window = Show(root);
        Assert.Equal(56, ((Control)popup.InputField!).Bounds.Height);
        root.Children.Remove(bar); Flush(window);
        Assert.Equal(56, ((Control)popup.InputField!).Bounds.Height);
        window.Width = 240; window.Height = 240; Flush(window);
        Assert.Equal(new Size(240, 240), popup.Presentation!.Bounds.Size);
        Assert.Equal(240, ((Control)popup.InputField!).Bounds.Width);
        await state.SnapToAsync(0); Flush(window);
        Assert.False(popup.IsPresented);
        window.Close();
    }

    [AvaloniaFact]
    public void EscapeRestoresFocusWhileClicksWithinTheClientDoNotDismiss()
    {
        using var clock = new MotionTestClockScope();
        var state = new SearchBarState();
        var (bar, popup, root) = Create(state);
        var anchor = new Button { Width = 360, Height = 56 };
        var input = new TextBox { HorizontalAlignment = HorizontalAlignment.Stretch };
        bar.Content = anchor; popup.InputField = input;
        var window = Show(root); anchor.Focus(); clock.SendFrame(TimeSpan.Zero);
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 16, 1008);
        Assert.True(input.IsFocused);
        window.MouseDown(new(550, 550), MouseButton.Left); window.MouseUp(new(550, 550), MouseButton.Left); Flush(window);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        window.KeyPress(Key.Escape, 0, PhysicalKey.Escape, null); Flush(window);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        Frames(clock, window, 1024, 2016);
        Assert.False(popup.IsPresented); Assert.True(anchor.IsFocused);
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 2032, 2080);
        root.Children.Clear(); Flush(window);
        Assert.False(popup.IsPresented); Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    }

    [AvaloniaFact]
    public async Task ContainedSurfaceFadesInPlaceAndInputRetainsHorizontalPaddingWithoutDivider()
    {
        var state = SearchBarState.CreateContained(SearchBarValue.Expanded);
        var (_, popup, root) = Create(state, contained: true);
        var window = Show(root);
        var input = (Control)popup.InputField!;
        var results = (Control)popup.Content!;
        Assert.Equal(new Point(8, 4), input.TranslatePoint(default, window));
        Assert.Equal(new Size(584, 56), input.Bounds.Size);
        Assert.Equal(new Point(0, 68), results.TranslatePoint(default, window));
        Assert.Empty(popup.PresentationLayout!.GetVisualDescendants().OfType<Separator>());
        await state.SnapToAsync(.5f); Flush(window);
        var surface = (MaterialSurface)popup.Presentation!;
        Assert.Equal(new Size(600, 600), surface.Bounds.Size);
        Assert.Equal(new Point(0, 0), surface.TranslatePoint(default, window));
        Assert.Equal(.5, surface.Opacity);
        Assert.Equal(1, results.GetVisualAncestors().OfType<Border>().First().Opacity);
        Assert.Equal(new Size(472, 56), input.Bounds.Size);
        Assert.Equal(new Point(12, 14), input.TranslatePoint(default, window));
        Assert.Equal(new Point(0, 78), results.TranslatePoint(default, window));
        window.Close();
    }

    [AvaloniaFact]
    public void ContainedStateUsesFastSpatialAndDelayedIndependentContentFade()
    {
        using var clock = new MotionTestClockScope();
        var state = SearchBarState.CreateContained();
        var (_, popup, root) = Create(state, contained: true);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        clock.Track(state.AnimateToExpandedAsync());
        Frames(clock, window, 16, 48);
        Assert.True(state.Progress > 0);
        Assert.Equal(0, state.ContentProgress);
        Frames(clock, window, 64, 96);
        Assert.InRange(state.ContentProgress, .01f, .99f);
        var contentLayer = ((Control)popup.Content!).GetVisualAncestors().OfType<Border>().First();
        Assert.Equal((double)state.ContentProgress, contentLayer.Opacity);
        Frames(clock, window, 112, 1008);
        Assert.Equal(1, state.ContentProgress);
        Assert.Equal(1, state.Progress);
        clock.Track(state.AnimateToCollapsedAsync());
        Frames(clock, window, 1024, 1056);
        Assert.InRange(state.ContentProgress, .01f, .99f);
        clock.Track(state.AnimateToExpandedAsync()); Frames(clock, window, 1072, 2016);
        Assert.Equal(1, state.Progress); Assert.Equal(1, state.ContentProgress);
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }

    private static (SearchBar Bar, ExpandedFullScreenSearchBar Popup, Grid Root) Create(SearchBarState state, bool contained = false)
    {
        var bar = new SearchBar { State = state, Content = new Marker(56), Margin = new(16, 24, 0, 0) };
        ExpandedFullScreenSearchBar popup = contained ? new ExpandedFullScreenContainedSearchBar() : new ExpandedFullScreenSearchBar();
        popup.State = state; popup.InputField = new Marker(56); popup.Content = new Marker(40);
        return (bar, popup, new Grid { Children = { bar, popup } });
    }
    private sealed class Marker(double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(360, availableSize.Width), Math.Min(height, availableSize.Height));
    }
    private static MotionTestWindow Show(Control root)
    {
        var window = new MotionTestWindow { Width = 600, Height = 600 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = root; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window, int start, int end)
    { for (var ms = start; ms <= end; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); } }
}

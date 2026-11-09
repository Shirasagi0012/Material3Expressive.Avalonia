using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class SearchDismissalPolicyTests
{
    [AvaloniaTheory]
    [InlineData(2)]
    [InlineData(3)]
    public void EscapePolicyChangesWhilePresentedAndRetainsNativeFocus(int variant)
    {
        using var clock = new MotionTestClockScope();
        var (state, popup, root, input) = Create(variant);
        var full = Assert.IsAssignableFrom<ExpandedFullScreenSearchBar>(popup);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        Assert.True(full.DismissOnEscape);
        Assert.True(full.DismissOnBack);
        Assert.True(popup.DismissOnPointerOutside);
        full.DismissOnEscape = false;
        PressEscape(window); Flush(window);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        Assert.True(input.IsFocused);
        Assert.True(popup.IsPresented);
        full.ClearValue(ExpandedFullScreenSearchBar.DismissOnEscapeProperty);
        PressEscape(window); Flush(window);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        Frames(clock, window);
        Assert.False(popup.IsPresented);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(2)]
    [InlineData(3)]
    public void PlatformBackAndEscapeHaveIndependentPolicies(int variant)
    {
        using var clock = new MotionTestClockScope();
        var (state, popup, root, _) = Create(variant);
        var full = Assert.IsAssignableFrom<ExpandedFullScreenSearchBar>(popup);
        full.DismissOnBack = false;
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent)); Flush(window);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        full.DismissOnEscape = false;
        full.DismissOnBack = true;
        window.RaiseEvent(new RoutedEventArgs(TopLevel.BackRequestedEvent)); Flush(window);
        Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
        Frames(clock, window); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    public void OutsidePolicyDoesNotDisableTheGapScrimOrPassInputThrough(int variant, bool scrim)
    {
        using var clock = new MotionTestClockScope();
        var (state, popup, root, _) = Create(variant);
        if (popup is ExpandedDockedSearchBarWithGap gap) gap.DropdownScrimColor = scrim ? Colors.Gray : Colors.Transparent;
        popup.DismissOnPointerOutside = false;
        var behind = new Button { Width = 100, Height = 50, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom };
        var clicks = 0; behind.Click += (_, _) => clicks++; root.Children.Insert(0, behind);
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        window.MouseDown(new(550, 550), MouseButton.Left); Flush(window);
        Assert.Equal(SearchBarValue.Expanded, state.TargetValue);
        window.MouseUp(new(550, 550), MouseButton.Left); Flush(window);
        Assert.Equal(scrim ? SearchBarValue.Collapsed : SearchBarValue.Expanded, state.TargetValue);
        Assert.Equal(0, clicks);
        if (!scrim)
        {
            popup.DismissOnPointerOutside = true;
            window.MouseDown(new(550, 550), MouseButton.Right); Flush(window);
            Assert.Equal(SearchBarValue.Collapsed, state.TargetValue);
            window.MouseUp(new(550, 550), MouseButton.Right);
        }
        Frames(clock, window); window.Close();
    }

    [AvaloniaFact]
    public void PolicySurvivesTemplateReplacementStateReplacementAndReparenting()
    {
        using var clock = new MotionTestClockScope();
        var (oldState, popup, root, _) = Create(2);
        var full = Assert.IsAssignableFrom<ExpandedFullScreenSearchBar>(popup);
        full.DismissOnEscape = false;
        var window = Show(root); clock.SendFrame(TimeSpan.Zero);
        var template = popup.Template;
        popup.Template = null; Flush(window);
        popup.Template = template; Flush(window);
        PressEscape(window); Assert.Equal(SearchBarValue.Expanded, oldState.TargetValue);
        var nextState = new SearchBarState(SearchBarValue.Expanded);
        popup.State = nextState; Flush(window);
        root.Children.Remove(popup); Flush(window);
        root.Children.Add(popup); Flush(window);
        PressEscape(window); Assert.Equal(SearchBarValue.Expanded, nextState.TargetValue);
        full.DismissOnEscape = true;
        PressEscape(window); Assert.Equal(SearchBarValue.Collapsed, nextState.TargetValue);
        Assert.Equal(SearchBarValue.Expanded, oldState.TargetValue);
        Frames(clock, window); window.Close();
    }

    private static (SearchBarState State, ExpandedDockedSearchBar Popup, Grid Root, Button Input) Create(int variant)
    {
        var state = new SearchBarState(SearchBarValue.Expanded);
        var bar = new SearchBar { State = state, Content = new Border { Width = 360, Height = 56 }, Margin = new(16, 24, 0, 0) };
        ExpandedDockedSearchBar popup = variant switch { 1 => new ExpandedDockedSearchBarWithGap(), 2 => new ExpandedFullScreenSearchBar(), 3 => new ExpandedFullScreenContainedSearchBar(), _ => new() };
        var input = new Button { Width = 360, Height = 56 };
        popup.State = state; popup.InputField = input; popup.Content = new Border { Height = 40 };
        return (state, popup, new Grid { Children = { bar, popup } }, input);
    }
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 600, Height = 600 };
        window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content; window.Show(); Flush(window); return window;
    }
    private static void PressEscape(Window window)
    {
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static void Frames(MotionTestClockScope clock, Window window)
    { for (var ms = 16; ms <= 1024; ms += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(ms)); Flush(window); } }
}

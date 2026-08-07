// Upstream behavior source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Slider.kt:
// RangeSliderImpl and rangeSliderOnKeyEvents give each endpoint independent semantics, focus,
// dynamic limits, and keyboard operation.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialRangeSliderTests
{
    [AvaloniaFact]
    public void AutomationTree_HasStableGroupAndTwoSliderChildren()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);

        var parent = ControlAutomationPeer.CreatePeerForElement(slider);
        var firstRead = parent.GetChildren();
        var secondRead = parent.GetChildren();

        Assert.Equal(AutomationControlType.Group, parent.GetAutomationControlType());
        Assert.Null(parent.GetProvider<IRangeValueProvider>());
        Assert.Equal(2, firstRead.Count);
        Assert.Same(firstRead[0], secondRead[0]);
        Assert.Same(firstRead[1], secondRead[1]);
        Assert.Same(parent, firstRead[0].GetParent());
        Assert.Same(parent, firstRead[1].GetParent());
        Assert.Equal(AutomationControlType.Slider, firstRead[0].GetAutomationControlType());
        Assert.Equal(AutomationControlType.Slider, firstRead[1].GetAutomationControlType());
        Assert.Equal("Start range", firstRead[0].GetName());
        Assert.Equal("End range", firstRead[1].GetName());
        Assert.Equal("StartRange", firstRead[0].GetAutomationId());
        Assert.Equal("EndRange", firstRead[1].GetAutomationId());
        Assert.Empty(firstRead[0].GetChildren());
        Assert.Empty(firstRead[1].GetChildren());
    }

    [AvaloniaFact]
    public void ThumbProviders_ExposeDynamicRangesAndSetValuesIndependently()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);
        var children = ControlAutomationPeer.CreatePeerForElement(slider).GetChildren();
        var start = Assert.IsAssignableFrom<IRangeValueProvider>(
            children[0].GetProvider<IRangeValueProvider>());
        var end = Assert.IsAssignableFrom<IRangeValueProvider>(
            children[1].GetProvider<IRangeValueProvider>());

        Assert.Equal(0, start.Minimum);
        Assert.Equal(8, start.Maximum);
        Assert.Equal(2, start.Value);
        Assert.Equal(2, end.Minimum);
        Assert.Equal(10, end.Maximum);
        Assert.Equal(8, end.Value);
        Assert.Equal(1, start.SmallChange);
        Assert.Equal(4, end.LargeChange);
        Assert.False(start.IsReadOnly);
        Assert.False(end.IsReadOnly);

        start.SetValue(3);
        Assert.Equal(3, slider.StartValue);
        Assert.Equal(3, end.Minimum);

        end.SetValue(7);
        Assert.Equal(7, slider.EndValue);
        Assert.Equal(7, start.Maximum);
    }

    [AvaloniaFact]
    public void ThumbProviders_RejectDisabledAndOutOfRangeWrites()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);
        var children = ControlAutomationPeer.CreatePeerForElement(slider).GetChildren();
        var start = Assert.IsAssignableFrom<IRangeValueProvider>(
            children[0].GetProvider<IRangeValueProvider>());
        var end = Assert.IsAssignableFrom<IRangeValueProvider>(
            children[1].GetProvider<IRangeValueProvider>());

        Assert.Throws<ArgumentOutOfRangeException>(() => start.SetValue(9));
        Assert.Throws<ArgumentOutOfRangeException>(() => end.SetValue(1));
        Assert.Throws<ArgumentOutOfRangeException>(() => start.SetValue(double.NaN));

        slider.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(() => start.SetValue(3));
        Assert.Throws<ElementNotEnabledException>(() => end.SetValue(7));
    }

    [AvaloniaFact]
    public void EndpointChanges_RaiseValueAndCrossPeerLimitNotifications()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);
        var children = ControlAutomationPeer.CreatePeerForElement(slider).GetChildren();
        var startChanges = new List<AutomationPropertyChangedEventArgs>();
        var endChanges = new List<AutomationPropertyChangedEventArgs>();
        children[0].PropertyChanged += (_, e) => startChanges.Add(e);
        children[1].PropertyChanged += (_, e) => endChanges.Add(e);

        slider.StartValue = 3;

        Assert.Contains(startChanges, change =>
            change.Property == RangeValuePatternIdentifiers.ValueProperty &&
            Equals(change.OldValue, 2d) && Equals(change.NewValue, 3d));
        Assert.Contains(endChanges, change =>
            change.Property == RangeValuePatternIdentifiers.MinimumProperty &&
            Equals(change.OldValue, 2d) && Equals(change.NewValue, 3d));

        startChanges.Clear();
        endChanges.Clear();
        slider.EndValue = 7;

        Assert.Contains(endChanges, change =>
            change.Property == RangeValuePatternIdentifiers.ValueProperty &&
            Equals(change.OldValue, 8d) && Equals(change.NewValue, 7d));
        Assert.Contains(startChanges, change =>
            change.Property == RangeValuePatternIdentifiers.MaximumProperty &&
            Equals(change.OldValue, 8d) && Equals(change.NewValue, 7d));
    }

    [AvaloniaFact]
    public void VirtualPeerFocus_SelectsTheMatchingPhysicalEndpoint()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);
        var children = ControlAutomationPeer.CreatePeerForElement(slider).GetChildren();

        children[1].SetFocus();
        Assert.True(slider.IsFocused);
        Assert.False(slider.IsStartEndpointActive);
        Assert.False(children[0].HasKeyboardFocus());
        Assert.True(children[1].HasKeyboardFocus());

        children[0].SetFocus();
        Assert.True(slider.IsStartEndpointActive);
        Assert.True(children[0].HasKeyboardFocus());
        Assert.False(children[1].HasKeyboardFocus());

        slider.IsEnabled = false;
        Assert.Throws<ElementNotEnabledException>(children[1].SetFocus);
    }

    [AvaloniaFact]
    public void RangeAndBoundsChanges_RaiseApplicableAutomationNotifications()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);
        var children = ControlAutomationPeer.CreatePeerForElement(slider).GetChildren();
        var startChanges = new List<AutomationPropertyChangedEventArgs>();
        var endChanges = new List<AutomationPropertyChangedEventArgs>();
        children[0].PropertyChanged += (_, e) => startChanges.Add(e);
        children[1].PropertyChanged += (_, e) => endChanges.Add(e);

        slider.Minimum = 1;
        slider.Maximum = 9;

        Assert.Contains(startChanges, change =>
            change.Property == RangeValuePatternIdentifiers.MinimumProperty &&
            Equals(change.OldValue, 0d) && Equals(change.NewValue, 1d));
        Assert.Contains(endChanges, change =>
            change.Property == RangeValuePatternIdentifiers.MaximumProperty &&
            Equals(change.OldValue, 10d) && Equals(change.NewValue, 9d));
        Assert.Contains(startChanges, change =>
            change.Property == AutomationElementIdentifiers.BoundingRectangleProperty);
        Assert.Contains(endChanges, change =>
            change.Property == AutomationElementIdentifiers.BoundingRectangleProperty);
    }

    [AvaloniaFact]
    public void ThumbPeers_ReportDistinctLiveBounds()
    {
        var slider = CreateSlider();
        ThemeTestHelper.ShowWindow(slider);
        var children = ControlAutomationPeer.CreatePeerForElement(slider).GetChildren();

        var start = children[0].GetBoundingRectangle();
        var end = children[1].GetBoundingRectangle();

        Assert.Equal(SliderDefaultsTokens.Default.HandleWidth, start.Width, 3);
        Assert.Equal(SliderDefaultsTokens.Default.HandleHeight, start.Height, 3);
        Assert.Equal(start.Size, end.Size);
        Assert.True(start.X < end.X);

        slider.StartValue = 4;
        Assert.True(children[0].GetBoundingRectangle().X > start.X);

        slider.FlowDirection = FlowDirection.RightToLeft;
        Assert.True(children[0].GetBoundingRectangle().X > children[1].GetBoundingRectangle().X);
    }

    [AvaloniaFact]
    public void TabAndShiftTab_TraverseEndpointsWithoutTrappingFocus()
    {
        var slider = CreateSlider();
        var before = new Button { Content = "Before" };
        var after = new Button { Content = "After" };
        ThemeTestHelper.ShowWindow(new StackPanel { Children = { before, slider, after } });
        Assert.True(slider.Focus(NavigationMethod.Tab));

        var toEnd = RaiseKey(slider, Key.Tab);
        Assert.True(toEnd.Handled);
        Assert.False(slider.IsStartEndpointActive);
        Assert.True(slider.IsFocused);

        var leaveForward = RaiseKey(slider, Key.Tab);
        Assert.True(leaveForward.Handled);
        Assert.True(after.IsFocused);

        Assert.True(slider.Focus(NavigationMethod.Tab, KeyModifiers.Shift));
        Assert.False(slider.IsStartEndpointActive);
        var toStart = RaiseKey(slider, Key.Tab, KeyModifiers.Shift);
        Assert.True(toStart.Handled);
        Assert.True(slider.IsStartEndpointActive);
        Assert.True(slider.IsFocused);

        var leaveBackward = RaiseKey(slider, Key.Tab, KeyModifiers.Shift);
        Assert.True(leaveBackward.Handled);
        Assert.True(before.IsFocused);
    }

    [AvaloniaFact]
    public void BothEndpoints_AreKeyboardOperableWithVisibleActiveFocusBounds()
    {
        var slider = CreateSlider();
        var window = ThemeTestHelper.ShowWindow(slider);
        Assert.True(slider.Focus(NavigationMethod.Tab));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        using var startFrame = window.CaptureRenderedFrame();

        Assert.True(slider.IsStartEndpointActive);
        Assert.True(slider.IsEndpointFocusIndicatorActive);
        Assert.Equal(slider.GetThumbBounds(true).Inflate(4), slider.EndpointFocusRingArea);
        RaiseKey(slider, Key.Right);
        Assert.Equal(3, slider.StartValue);

        RaiseKey(slider, Key.Tab);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(2);
        using var endFrame = window.CaptureRenderedFrame();

        Assert.False(slider.IsStartEndpointActive);
        Assert.Equal(slider.GetThumbBounds(false).Inflate(4), slider.EndpointFocusRingArea);
        RaiseKey(slider, Key.PageDown);
        Assert.Equal(4, slider.EndValue);
        RaiseKey(slider, Key.End);
        Assert.Equal(10, slider.EndValue);
    }

    [AvaloniaFact]
    public void RtlPointerAndArrowInput_FollowTheReversedTrack()
    {
        var slider = CreateSlider();
        slider.FlowDirection = FlowDirection.RightToLeft;
        var window = ThemeTestHelper.ShowWindow(slider);

        slider.Focus(NavigationMethod.Tab);
        RaiseKey(slider, Key.Right);
        Assert.Equal(1, slider.StartValue);

        // Avalonia mirrors the visual transform for RTL, so the local right edge is the physical
        // left edge while pointer coordinates received by the control stay local.
        var nearLeft = slider.TranslatePoint(
            new Point(slider.Bounds.Width - 20, slider.Bounds.Height / 2),
            window)!.Value;
        window.MouseDown(nearLeft, MouseButton.Left);
        Assert.False(slider.IsStartEndpointActive);
        Assert.InRange(slider.EndValue, 8, 10);
    }

    [AvaloniaFact]
    public void CachedPeers_SurviveOwnerDetachAndReconnect()
    {
        var slider = CreateSlider();
        var firstWindow = ThemeTestHelper.ShowWindow(slider);
        var parent = ControlAutomationPeer.CreatePeerForElement(slider);
        var children = parent.GetChildren();

        firstWindow.Content = null;
        firstWindow.Close();
        var secondWindow = ThemeTestHelper.ShowWindow(slider);
        var reconnectedParent = ControlAutomationPeer.CreatePeerForElement(slider);
        var reconnectedChildren = reconnectedParent.GetChildren();

        Assert.Same(parent, reconnectedParent);
        Assert.Same(children[0], reconnectedChildren[0]);
        Assert.Same(children[1], reconnectedChildren[1]);
        Assert.True(reconnectedChildren[0].GetBoundingRectangle().Width > 0);
        Assert.True(reconnectedChildren[1].GetBoundingRectangle().Width > 0);
        secondWindow.Close();
    }

    private static MaterialRangeSlider CreateSlider() =>
        new()
        {
            Width = 200,
            Minimum = 0,
            Maximum = 10,
            StartValue = 2,
            EndValue = 8,
            SmallChange = 1,
            LargeChange = 4
        };

    private static KeyEventArgs RaiseKey(
        InputElement target,
        Key key,
        KeyModifiers modifiers = KeyModifiers.None)
    {
        var args = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key,
            KeyModifiers = modifiers
        };
        target.RaiseEvent(args);
        return args;
    }
}

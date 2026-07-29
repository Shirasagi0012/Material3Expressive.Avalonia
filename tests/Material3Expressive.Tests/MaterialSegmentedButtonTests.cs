// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt
// and tokens/OutlinedSegmentedButtonTokens.kt (VERSION v0_162). The alpha22 executable reference
// contains the same static rows and semantics; these tests cover the Avalonia row API and desktop
// input/automation behavior that a screenshot cannot observe.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialSegmentedButtonTests
{
    [AvaloniaFact]
    public void DefaultsFollowTheAlpha25TokenContract()
    {
        var button = new MaterialSegmentedButton { Content = "Day" };
        ThemeTestHelper.ShowWindow(button);

        Assert.Equal(40, button.MinHeight);
        Assert.Equal(58, button.MinWidth);
        Assert.Equal(18, button.IconSize);
        Assert.Equal(8, button.IconSpacing);
        Assert.Equal(new Thickness(12, 8), button.ContentPadding);
        Assert.Equal(TypographyRole.LabelLarge, TypographyScheme.GetTextStyle(button));
        Assert.False(button.HasVisibleIcon);
    }

    [AvaloniaFact]
    public void RowUsesEqualWidthsOneDpOverlapAndPositionShapes()
    {
        var first = new MaterialSegmentedButton { Content = "Day" };
        var middle = new MaterialSegmentedButton { Content = "Month", IsChecked = true };
        var last = new MaterialSegmentedButton { Content = "Week" };
        var row = new MaterialSegmentedButtonRow
        {
            Width = 300,
            Children = { first, middle, last }
        };

        ThemeTestHelper.ShowWindow(row);

        Assert.Equal(300, row.Bounds.Width);
        // Compose Row distributes the two overlap pixels before weight division: 302 / 3 gives
        // 101, 101, 100 physical pixels at 1x, with the remainder assigned from logical start.
        Assert.Equal(101, first.Bounds.Width, precision: 10);
        Assert.Equal(101, middle.Bounds.Width, precision: 10);
        Assert.Equal(100, last.Bounds.Width, precision: 10);
        Assert.Equal(-1, middle.Bounds.X - first.Bounds.Right, precision: 10);
        Assert.Equal(-1, last.Bounds.X - middle.Bounds.Right, precision: 10);
        Assert.Equal("First", GetPosition(first).ToString());
        Assert.Equal(new CornerRadius(first.Bounds.Width / 2, 0, 0, first.Bounds.Width / 2), GetCorners(first));
        Assert.Equal(new CornerRadius(0), GetCorners(middle));
        Assert.Equal(new CornerRadius(0, last.Bounds.Width / 2, last.Bounds.Width / 2, 0), GetCorners(last));
        Assert.Equal(5, middle.ZIndex);
    }

    [AvaloniaFact]
    public void SingleModeCoordinatesCheckedStateAndDoesNotToggleTheSelectedItemOff()
    {
        var first = new MaterialSegmentedButton { Content = "Day", IsChecked = true };
        var second = new MaterialSegmentedButton { Content = "Month" };
        var row = new MaterialSegmentedButtonRow
        {
            Children = { first, second }
        };
        var window = ThemeTestHelper.ShowWindow(row);

        second.IsChecked = true;
        Assert.False(first.IsChecked);
        Assert.True(second.IsChecked);

        var point = second.TranslatePoint(new Point(second.Bounds.Width / 2, second.Bounds.Height / 2), window)
                    ?? throw new InvalidOperationException("button was not mounted");
        window.MouseMove(point);
        window.MouseDown(point, MouseButton.Left);
        window.MouseUp(point, MouseButton.Left);

        Assert.True(second.IsChecked);
    }

    [AvaloniaFact]
    public void MultipleModeKeepsIndependentCheckedState()
    {
        var first = new MaterialSegmentedButton { Content = "Day", IsChecked = true };
        var second = new MaterialSegmentedButton { Content = "Month", IsChecked = true };
        var row = new MaterialSegmentedButtonRow
        {
            SelectionMode = SegmentedButtonSelectionMode.Multiple,
            Children = { first, second }
        };
        ThemeTestHelper.ShowWindow(row);

        Assert.True(first.IsChecked);
        Assert.True(second.IsChecked);
        second.IsChecked = false;
        Assert.True(first.IsChecked);
    }

    [AvaloniaFact]
    public void SingleAndMultipleRowsExposeMatchingAutomationProviders()
    {
        var singleItem = new MaterialSegmentedButton { Content = "Day", IsChecked = true };
        var single = new MaterialSegmentedButtonRow { Children = { singleItem } };
        ThemeTestHelper.ShowWindow(single);

        var singlePeer = ControlAutomationPeer.CreatePeerForElement(single);
        var singleItemPeer = ControlAutomationPeer.CreatePeerForElement(singleItem);
        Assert.Equal(AutomationControlType.List, singlePeer.GetAutomationControlType());
        Assert.False(singlePeer.GetProvider<ISelectionProvider>()!.CanSelectMultiple);
        Assert.IsAssignableFrom<ISelectionItemProvider>(
            singleItemPeer.GetProvider<ISelectionItemProvider>());
        Assert.Null(singleItemPeer.GetProvider<IToggleProvider>());

        var multipleItem = new MaterialSegmentedButton { Content = "Day", IsChecked = true };
        var multiple = new MaterialSegmentedButtonRow
        {
            SelectionMode = SegmentedButtonSelectionMode.Multiple,
            Children = { multipleItem }
        };
        ThemeTestHelper.ShowWindow(multiple);

        var multiplePeer = ControlAutomationPeer.CreatePeerForElement(multiple);
        var multipleItemPeer = ControlAutomationPeer.CreatePeerForElement(multipleItem);
        Assert.Null(multiplePeer.GetProvider<ISelectionProvider>());
        Assert.IsAssignableFrom<IToggleProvider>(
            multipleItemPeer.GetProvider<IToggleProvider>());
        Assert.Null(multipleItemPeer.GetProvider<ISelectionItemProvider>());
    }

    [AvaloniaFact]
    public void ExistingCheckedChildrenAreNormalizedWhenAddedToASingleRow()
    {
        var first = new MaterialSegmentedButton { Content = "Day", IsChecked = true };
        var second = new MaterialSegmentedButton { Content = "Month", IsChecked = true };
        var row = new MaterialSegmentedButtonRow { Children = { first, second } };
        ThemeTestHelper.ShowWindow(row);

        Assert.True(first.IsChecked);
        Assert.False(second.IsChecked);
    }

    [AvaloniaFact]
    public void ArrowKeysMoveFocusAndSelectionInSingleMode()
    {
        var first = new MaterialSegmentedButton { Content = "Day", IsChecked = true };
        var second = new MaterialSegmentedButton { Content = "Month" };
        var third = new MaterialSegmentedButton { Content = "Week" };
        var row = new MaterialSegmentedButtonRow { Children = { first, second, third } };
        var window = ThemeTestHelper.ShowWindow(row);
        first.Focus();

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);

        Assert.False(first.IsChecked);
        Assert.True(second.IsChecked);
        Assert.True(second.IsFocused);
    }

    [AvaloniaFact]
    public void RightArrowUsesLogicalDirectionAndSkipsDisabledItemsInRtl()
    {
        var first = new MaterialSegmentedButton { Content = "Day" };
        var disabled = new MaterialSegmentedButton { Content = "Month", IsEnabled = false };
        var last = new MaterialSegmentedButton { Content = "Week", IsChecked = true };
        var row = new MaterialSegmentedButtonRow
        {
            FlowDirection = Avalonia.Media.FlowDirection.RightToLeft,
            Children = { first, disabled, last }
        };
        var window = ThemeTestHelper.ShowWindow(row);
        last.Focus();

        window.KeyPress(Key.Right, RawInputModifiers.None, PhysicalKey.ArrowRight, null);

        Assert.True(first.IsChecked);
        Assert.False(last.IsChecked);
        Assert.True(first.IsFocused);
    }

    [AvaloniaFact]
    public void HiddenChildrenDoNotConsumeSegmentPositions()
    {
        var first = new MaterialSegmentedButton { Content = "Day" };
        var hidden = new MaterialSegmentedButton { Content = "Month", IsVisible = false };
        var last = new MaterialSegmentedButton { Content = "Week" };
        var row = new MaterialSegmentedButtonRow
        {
            Width = 200,
            Children = { first, hidden, last }
        };
        ThemeTestHelper.ShowWindow(row);

        Assert.Equal("First", GetPosition(first).ToString());
        Assert.Equal("None", GetPosition(hidden).ToString());
        Assert.Equal("Last", GetPosition(last).ToString());
        Assert.Equal(101, first.Bounds.Width, precision: 10);
        Assert.Equal(100, last.Bounds.Width, precision: 10);
    }

    [AvaloniaFact]
    public void CallerShapeOverrideIsNotReplacedByRowLayout()
    {
        var custom = RoundedCornerShape.Uniform(4);
        var button = new MaterialSegmentedButton
        {
            Content = "Day",
            ContainerShapeOverride = custom
        };
        var row = new MaterialSegmentedButtonRow
        {
            Width = 200,
            Children = { button, new MaterialSegmentedButton { Content = "Month" } }
        };
        ThemeTestHelper.ShowWindow(row);

        Assert.Same(custom, button.ContainerShapeOverride);
        Assert.Equal(new CornerRadius(4), GetCorners(button));
    }

    [AvaloniaFact]
    public void CheckedDefaultIconReservesTheSameSlotAsAComposeIcon()
    {
        var button = new MaterialSegmentedButton { Content = "Month", IsChecked = true };
        ThemeTestHelper.ShowWindow(button);

        Assert.True(button.HasVisibleIcon);
        Assert.True(button.IsDefaultIconVisible);
    }

    [AvaloniaFact]
    public void DefaultIconAndLabelOffsetUseControlledMaterialMotion()
    {
        using var clock = new MotionTestClockScope();
        var button = new MaterialSegmentedButton { Content = "Month" };
        ThemeTestHelper.ShowWindow(button);
        clock.SettleAll();

        Assert.Equal(-13, button.ContentOffset, precision: 10);
        Assert.Equal(0, button.IconScale, precision: 10);
        Assert.Equal(0, button.IconOpacity, precision: 10);

        button.IsChecked = true;
        clock.AdvanceBy(TimeSpan.FromMilliseconds(32));

        Assert.InRange(button.ContentOffset, -13, 0);
        Assert.InRange(button.IconScale, 0, 1);
        Assert.InRange(button.IconOpacity, 0, 1);

        clock.SettleAll();

        Assert.Equal(0, button.ContentOffset, precision: 10);
        Assert.Equal(1, button.IconScale, precision: 10);
        Assert.Equal(1, button.IconOpacity, precision: 10);

        button.IsChecked = false;

        // Upstream's default icon has ExitTransition.None while the label offset retargets with
        // FastSpatial.
        Assert.Equal(0, button.IconScale, precision: 10);
        Assert.Equal(0, button.IconOpacity, precision: 10);
        Assert.True(button.ContentOffset > -13);

        clock.SettleAll();
        Assert.Equal(-13, button.ContentOffset, precision: 10);
    }

    private static RoundedCornerShape GetShape(MaterialSegmentedButton button) =>
        button.ContainerShapeOverride
        ?? (RoundedCornerShape)typeof(MaterialSegmentedButton)
            .GetField("_segmentShape", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(button)!;

    private static CornerRadius GetCorners(MaterialSegmentedButton button) =>
        GetShape(button).ToCornerRadius(button.Bounds.Size);

    private static object GetPosition(MaterialSegmentedButton button) =>
        typeof(MaterialSegmentedButton)
            .GetProperty("SegmentPosition", System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.NonPublic)!
            .GetValue(button)!;
}

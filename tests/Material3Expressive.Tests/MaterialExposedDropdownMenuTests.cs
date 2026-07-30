using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialExposedDropdownMenuTests
{
    [AvaloniaFact]
    public void SelectionAndExpansionUseTheNativeBindableState()
    {
        var menu = CreateMenu();
        ThemeTestHelper.ShowWindow(menu);

        menu.SelectedIndex = 1;

        Assert.Equal("Green", menu.SelectedItem);
        Assert.Equal("Green", menu.SelectionBoxItem);
        Assert.False(menu.IsDropDownOpen);

        menu.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        Assert.True(menu.IsDropDownOpen);
        Assert.True(PopupOf(menu).IsOpen);
    }

    [AvaloniaFact]
    public void KeyboardOpensNavigatesAndClosesTheMenu()
    {
        var menu = CreateMenu();
        ThemeTestHelper.ShowWindow(menu);
        menu.Focus();

        RaiseKey(menu, Key.Enter);
        Dispatcher.UIThread.RunJobs();

        Assert.True(menu.IsDropDownOpen);

        RaiseKey(menu, Key.Down);
        var focusedItem = Assert.IsType<MaterialExposedDropdownMenuItem>(
            TopLevel.GetTopLevel(menu)!.FocusManager.GetFocusedElement());
        Assert.Equal("Red", focusedItem.Content);
        RaiseKey(focusedItem, Key.Enter);
        Assert.Equal(0, menu.SelectedIndex);

        menu.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();
        RaiseKey(menu, Key.Escape);
        Dispatcher.UIThread.RunJobs();

        Assert.False(menu.IsDropDownOpen);
        Assert.Same(menu, TopLevel.GetTopLevel(menu)!.FocusManager.GetFocusedElement());
    }

    [AvaloniaFact]
    public void EditableAnchorKeepsTypingFocusAndEnterExpands()
    {
        var menu = CreateMenu();
        menu.IsEditable = true;
        var window = ThemeTestHelper.ShowWindow(menu);
        menu.Focus();
        var editor = EditorOf(menu);

        Assert.Same(editor, window.FocusManager.GetFocusedElement());

        RaiseKey(editor, Key.Enter);
        Dispatcher.UIThread.RunJobs();

        Assert.True(menu.IsDropDownOpen);
        Assert.Same(editor, window.FocusManager.GetFocusedElement());
    }

    [AvaloniaFact]
    public void PopupMatchesTheAnchorAndReservesTheAndroidXWindowMargin()
    {
        var menu = CreateMenu();
        menu.Width = 240;
        var window = new Window
        {
            Width = 360,
            Height = 240,
            Content = menu
        };
        window.Show();

        Assert.Equal(240, menu.PopupWidth);
        Assert.Equal(240, menu.PopupMinWidth);
        Assert.Equal(240, menu.PopupMaxWidth);
        Assert.Equal(144, menu.EffectiveMenuMaxHeight);

        menu.MatchAnchorWidth = false;

        Assert.True(double.IsNaN(menu.PopupWidth));
        Assert.Equal(112, menu.PopupMinWidth);
        Assert.Equal(280, menu.PopupMaxWidth);

        menu.MatchAnchorWidth = true;
        Assert.Equal(240, menu.PopupWidth);

        window.Height = 300;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(204, menu.EffectiveMenuMaxHeight);
    }

    [AvaloniaFact]
    public void PlacementFlipsAboveAnAnchorAtTheBottomEdge()
    {
        var menu = CreateMenu();
        menu.Width = 220;
        menu.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom;
        var window = new Window
        {
            Width = 320,
            Height = 280,
            Content = menu
        };
        window.Show();

        menu.IsDropDownOpen = true;
        Dispatcher.UIThread.RunJobs();

        var popup = PopupOf(menu);
        var child = Assert.IsAssignableFrom<Control>(popup.Child);
        var surface = child.GetVisualDescendants().OfType<MaterialSurface>().Single();
        var anchor = menu.GetTemplateDescendants().Single(control => control.Name == "PART_Anchor");
        // Measure the surface's layout slot through the popup gutter. PointToScreen on the
        // surface itself includes the in-flight entrance ScaleTransform and makes placement
        // assertions depend on unrelated motion-clock state.
        var popupOrigin = child.PointToScreen(surface.Bounds.Position);
        var anchorOrigin = anchor.PointToScreen(default);

        Assert.Equal(PlacementMode.BottomEdgeAlignedLeft, popup.Placement);
        // PopupRoot reserves the shadow gutter and the headless positioner may round its
        // compensating offset by up to two DIPs while keeping the surface left-aligned.
        Assert.InRange(Math.Abs(popupOrigin.X - anchorOrigin.X), 0, 2);
        Assert.True(popupOrigin.Y < anchorOrigin.Y);
    }

    [AvaloniaFact]
    public void AutomationRetainsComboBoxPatternsAndAddsFieldMetadata()
    {
        var menu = CreateMenu();
        menu.Label = "Color";
        menu.SupportingText = "Choose a swatch";
        ThemeTestHelper.ShowWindow(menu);

        var peer = ControlAutomationPeer.CreatePeerForElement(menu);
        var expand = Assert.IsAssignableFrom<IExpandCollapseProvider>(
            peer.GetProvider<IExpandCollapseProvider>());
        var selection = Assert.IsAssignableFrom<ISelectionProvider>(
            peer.GetProvider<ISelectionProvider>());
        AutomationPropertyChangedEventArgs? statusChange = null;
        peer.PropertyChanged += (_, args) =>
        {
            if (args.Property == AutomationElementIdentifiers.ItemStatusProperty)
                statusChange = args;
        };

        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        Assert.Equal(nameof(MaterialExposedDropdownMenu), peer.GetClassName());
        Assert.Equal("Color", peer.GetName());
        Assert.Equal("Choose a swatch", peer.GetHelpText());
        Assert.True(expand.ShowsMenu);
        Assert.False(selection.CanSelectMultiple);

        expand.Expand();
        Assert.True(menu.IsDropDownOpen);
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);

        menu.IsError = true;
        Assert.Equal("Invalid", peer.GetItemStatus());
        Assert.NotNull(statusChange);
        Assert.Null(statusChange.OldValue);
        Assert.Equal("Invalid", statusChange.NewValue);

        expand.Collapse();
        Assert.False(menu.IsDropDownOpen);
    }

    [AvaloniaFact]
    public void OpenAndArrowMotionShareOneDeterministicFraction()
    {
        using var clock = new MotionTestClockScope();
        var menu = CreateMenu();
        ThemeTestHelper.ShowWindow(menu);
        clock.SettleAll();

        Assert.Equal(0, menu.MenuOpenFraction);
        Assert.Equal(0, menu.ArrowAngle);
        Assert.Equal(0.8, menu.MenuScale);
        Assert.Equal(0, menu.MenuOpacity);

        menu.IsDropDownOpen = true;
        Assert.Equal(180, menu.ArrowAngle);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));

        Assert.InRange(menu.MenuOpenFraction, 0.000001, 0.999999);
        Assert.InRange(menu.MenuOpacity, 0.000001, 0.999999);
        Assert.Equal(0.8 + menu.MenuOpenFraction * 0.2, menu.MenuScale, precision: 6);

        clock.SettleAll();
        Assert.Equal(1, menu.MenuOpenFraction);
        Assert.Equal(180, menu.ArrowAngle);
        Assert.Equal(1, menu.MenuOpacity);

        menu.IsDropDownOpen = false;
        Assert.Equal(0, menu.ArrowAngle);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(50));
        Assert.InRange(menu.MenuOpenFraction, 0.000001, 0.999999);
        Assert.InRange(menu.MenuOpacity, 0.000001, 0.999999);

        clock.SettleAll();
        Assert.Equal(0, menu.MenuOpenFraction);
    }

    private static MaterialExposedDropdownMenu CreateMenu() =>
        new()
        {
            Label = "Color",
            ItemsSource = new[] { "Red", "Green", "Blue" }
        };

    private static Popup PopupOf(MaterialExposedDropdownMenu menu) =>
        menu.GetTemplateDescendants().OfType<Popup>().Single();

    private static TextBox EditorOf(MaterialExposedDropdownMenu menu) =>
        menu.GetTemplateDescendants().OfType<TextBox>().Single(
            control => control.Name == "PART_EditableTextBox");

    private static void RaiseKey(InputElement target, Key key)
    {
        target.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = key
        });
    }
}

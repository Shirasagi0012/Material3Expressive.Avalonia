using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class DropdownMenuTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Separator_FillsIntrinsicMenuWidth_AndTracksContentChanges(bool grouped)
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Width = 140, Height = 16 };
        var item = new DropdownMenuItem { Content = content };
        DropdownMenuPopup menu = grouped ? new DropdownMenuPopup() : new DropdownMenu();
        ItemsControl items = grouped ? new DropdownMenuGroup() : menu;
        items.Items.Add(item);
        if (grouped) menu.Items.Add(items);
        var (window, _) = Show(menu); menu.IsOpen = true;
        Flush(window); Advance(clock, window, 100);
        var width = menu.Presentation!.Bounds.Width;
        var separator = new Separator(); items.Items.Add(separator); Flush(window);
        Assert.Equal(width, menu.Presentation.Bounds.Width);
        Assert.Equal(width, separator.Bounds.Width);
        content.Width += 40; Flush(window);
        Assert.Equal(width + 40, menu.Presentation.Bounds.Width);
        Assert.Equal(width + 40, separator.Bounds.Width);
        content.Width -= 40; Flush(window);
        Assert.Equal(width, menu.Presentation.Bounds.Width);
        menu.Width = 240; Flush(window);
        Assert.Equal(240, separator.Bounds.Width);
        menu.Width = 90; Flush(window);
        Assert.Equal(90, menu.Presentation.Bounds.Width);
        Assert.Equal(90, item.Bounds.Width);
        Assert.Equal(90, separator.Bounds.Width);
        window.Close();
    }

    [Theory]
    [InlineData(false, 105)]
    [InlineData(true, 35)]
    public void Position_UsesLogicalOffsetAndPhysicalIntersectionPivot(bool rtl, int expectedX)
    {
        var provider = new DropdownMenuPositionProvider(offset: new(5, 3));
        Assert.Equal(new PixelPoint(expectedX, 143), provider.CalculatePosition(new(100, 100, 40, 40), new(400, 400), rtl, new(100, 60)));
        Assert.Equal(0, provider.TransformOrigin.Point.Y);
        Assert.InRange(provider.TransformOrigin.Point.X, 0, 1);
    }

    [Theory]
    [InlineData(8, 8)]
    [InlineData(0, 0)]
    public void Position_DesktopBaselineAndComposablePopupHaveDistinctMargins(int margin, int expectedX)
    {
        var provider = new DropdownMenuPositionProvider(horizontalMargin: margin);
        Assert.Equal(new PixelPoint(expectedX, 60), provider.CalculatePosition(new(0, 40, 20, 20), new(300, 300), false, new(100, 80)));
    }

    [Fact]
    public void Position_TriesAboveThenCentersOversizedContent_AndPreservesEmptyCustomCandidates()
    {
        var provider = new DropdownMenuPositionProvider();
        Assert.Equal(new PixelPoint(180, 160), provider.CalculatePosition(new(240, 240, 40, 20), new(300, 300), false, new(100, 80)));
        Assert.Equal(new PixelPoint(-50, -50), provider.CalculatePosition(new(100, 100, 20, 20), new(300, 300), false, new(400, 400)));
        provider = new(MenuAnchorPosition.Custom(_ => [], _ => []), new(50, 50));
        Assert.Equal(default, provider.CalculatePosition(default, new(300, 300), false, new(100, 100)));
    }

    [AvaloniaFact]
    public void InitiallyOpenPopup_AttachesItsInputShieldBeforeTheFirstWindowLayout()
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { IsOpen = true, Items = { new DropdownMenuItem { Content = "Action" } } };
        var (window, _) = Show(menu);
        Flush(window); Advance(clock, window, 100);
        Assert.True(menu.IsPresented);
        Assert.Same(menu.Popup.Child, window.FocusManager!.GetFocusedElement());
        var requests = 0;
        menu.DismissRequested += (_, _) => requests++;
        window.MouseDown(new(450, 250), MouseButton.Left);
        window.MouseUp(new(450, 250), MouseButton.Left);
        Assert.Equal(1, requests);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.ActiveChildCount);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void Popup_UsesRealItemsAndIntrinsicSurface_AndRetainsExitUntilBothChannelsFinish()
    {
        using var clock = new MotionTestClockScope();
        var item = new DropdownMenuItem { Content = new Border { Width = 40, Height = 16 } };
        var menu = new DropdownMenu { Items = { item } };
        var (window, anchor) = Show(menu);
        anchor.Focus(); menu.IsOpen = true; Flush(window); Advance(clock, window, 1);
        Assert.True(menu.IsPresented);
        Assert.Equal(default, menu.DesiredSize);
        Assert.Equal(new Size(112, 48), item.Bounds.Size);
        Assert.Equal(new Size(112, 64), menu.Presentation!.Bounds.Size);
        Assert.Equal(.8f, menu.AppearanceScale);
        var layer = menu.Presentation;
        Assert.True(layer.ClipToBounds);
        Advance(clock, window, 100);
        Assert.Equal(1, menu.AppearanceScale); Assert.Equal(1, menu.AppearanceAlpha);
        Assert.False(layer.ClipToBounds);
        menu.IsOpen = false;
        Assert.True(menu.IsPresented);
        Advance(clock, window, 100);
        Assert.False(menu.IsPresented);
        Assert.Same(anchor, window.FocusManager!.GetFocusedElement());
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Assert.Equal(0, window.MotionRoot.ActiveChildCount);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void CloseAndReopen_ReversesTheExistingTransition_ThenDetachReleasesEverything()
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { Items = { new DropdownMenuItem { Content = "Action" } } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        var presentation = menu.Popup.Child;
        menu.IsOpen = false; Advance(clock, window, 4);
        var scale = menu.AppearanceScale;
        Assert.InRange(scale, .8f, 1f);
        menu.IsOpen = true; Assert.Same(presentation, menu.Popup.Child);
        Assert.Equal(scale, menu.AppearanceScale);
        Advance(clock, window, 100); Assert.Equal(1, menu.AppearanceScale);
        ((Grid)window.Content!).Children.Remove(menu); Flush(window);
        Assert.False(menu.IsPresented);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Assert.Equal(0, window.MotionRoot.ActiveChildCount);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaTheory]
    [InlineData(MouseButton.Left)]
    [InlineData(MouseButton.Right)]
    public void DismissRequest_IsControlled_AndOutsidePressIsConsumed(MouseButton button)
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { Items = { new DropdownMenuItem { Content = "Action" } } };
        var (window, _) = Show(menu); var requests = 0; var backgroundPresses = 0;
        menu.DismissRequested += (_, _) => requests++;
        ((Grid)window.Content!).PointerPressed += (_, _) => backgroundPresses++;
        menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        window.MouseDown(new(450, 250), button); window.MouseUp(new(450, 250), button);
        Assert.Equal(1, requests); Assert.Equal(0, backgroundPresses); Assert.True(menu.IsOpen);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(2, requests); Assert.True(menu.IsOpen);
        menu.DismissOnBack = false; window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(2, requests);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_SkipsDisabledItems_AndEnterActivatesOnReleaseWithoutAutomaticDismissal()
    {
        using var clock = new MotionTestClockScope();
        var first = new DropdownMenuItem { Name = "First", Content = "First" };
        var disabled = new DropdownMenuItem { Content = "Disabled", IsEnabled = false };
        var last = new DropdownMenuItem { Name = "Last", Content = "Last" };
        var menu = new DropdownMenu { Items = { first, disabled, last } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null); Assert.True(first.IsFocused);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal("Last", (window.FocusManager!.GetFocusedElement() as Control)?.Name);
        var clicks = 0; last.Click += (_, _) => clicks++;
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(0, clicks);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, clicks); Assert.True(menu.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void CheckableItem_IsControlled_AndSupportingSlotsHaveTheUpstreamLayout()
    {
        using var clock = new MotionTestClockScope();
        var item = new CheckableDropdownMenuItem
        {
            Content = new Border { Width = 40, Height = 16 },
            SupportingText = new Border { Width = 70, Height = 20 },
            LeadingIcon = new Border { Width = 20, Height = 20 },
            TrailingContent = new Border { Width = 20, Height = 20 }
        };
        var group = new DropdownMenuGroup { Items = { item } };
        var menu = new DropdownMenuPopup { Items = { group } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        Assert.Equal(new Size(158, 64), item.Bounds.Size);
        Assert.Equal(new Size(158, 68), group.Bounds.Size);
        bool? requested = null; item.CheckedChangeRequested += (_, e) => requested = e.Value;
        item.Focus(); window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.True(requested); Assert.False(item.IsChecked);
        item.IsChecked = true; window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.False(requested); Assert.True(item.IsChecked);
        Assert.True(menu.IsOpen);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SelectedIcon_ExpandsInPhysicalPixels_ReversesAndReleasesWithTemplate(bool useTemplate)
    {
        using var clock = new MotionTestClockScope();
        var icon = new Border { Width = 20, Height = 20 };
        var item = new SelectableDropdownMenuItem { Content = new Border { Width = 48, Height = 16 },
            SupportingText = new Border { Width = 32, Height = 12 },
            TrailingContent = new Border { Width = 16, Height = 16 }, SelectedLeadingIcon = icon };
        if (useTemplate)
        {
            item.SelectedLeadingIcon = null;
            item.SelectedLeadingIconTemplate = new FuncDataTemplate<object>((_, _) => icon);
        }
        var menu = new DropdownMenuPopup { Items = { new DropdownMenuGroup { Items = { item } } } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        var slot = item.GetVisualDescendants().OfType<DropdownMenuLeadingIcon>().Single();
        Assert.Equal(0, slot.Bounds.Width);
        Assert.DoesNotContain(icon, item.GetVisualDescendants());
        Assert.Equal(120, menu.Presentation!.Bounds.Width);
        item.IsSelected = true; Advance(clock, window, 1);
        Assert.Equal(0, slot.Bounds.Width);
        Assert.Equal(136, menu.Presentation.Bounds.Width); // Intrinsic content, before expansion.
        Assert.Contains(icon, item.GetVisualDescendants());
        Advance(clock, window, 3); var width = slot.Bounds.Width;
        Assert.InRange(width, 0, 21);
        item.IsSelected = false; Advance(clock, window, 100); Assert.Equal(0, slot.Bounds.Width);
        Assert.Equal(120, menu.Presentation.Bounds.Width);
        Assert.DoesNotContain(icon, item.GetVisualDescendants());
        item.IsSelected = true; Advance(clock, window, 100); Assert.Equal(20, slot.Bounds.Width);
        Assert.Equal(1, slot.Opacity);
        item.Template = null; Flush(window);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Assert.Equal(0, window.MotionRoot.ActiveChildCount); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void Group_StartsWithItsShape_AndOnlyBecomesInactiveAfterHover()
    {
        using var clock = new MotionTestClockScope();
        var group = new DropdownMenuGroup { Items = { new CheckableDropdownMenuItem { Content = "Choice" } } };
        var menu = new DropdownMenuPopup { Items = { group } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        var surface = group.GetVisualDescendants().OfType<MaterialSurface>().First();
        Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
        window.MouseMove(group.TranslatePoint(new Point(30, 20), window)!.Value); Advance(clock, window, 100);
        window.MouseMove(new(450, 250)); Advance(clock, window, 100);
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);
        window.MouseMove(group.TranslatePoint(new Point(30, 20), window)!.Value); Advance(clock, window, 100);
        Assert.Equal(new CornerRadius(16), surface.EffectiveCornerRadius);
        window.Close(); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void ScrollOffset_IsPublic_AndNarrowMenuDoesNotForce112Dip()
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { Width = 90, MaxHeight = 160 };
        for (var i = 0; i < 15; i++) menu.Items.Add(new DropdownMenuItem { Content = $"Item {i}" });
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        Assert.Equal(90, menu.Presentation!.Bounds.Width);
        Assert.Equal(160, menu.Presentation.Bounds.Height);
        menu.ScrollOffset = new(0, 100); Flush(window);
        var scroll = menu.Presentation.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.Equal(100, scroll.Offset.Y);
        scroll.Offset = new(0, 200); Flush(window); Assert.Equal(200, menu.ScrollOffset.Y);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Popup_PhysicalPositionAndPivotStayAtTheAnchorInBothDirections(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Items = { new DropdownMenuItem { Content = new Border { Width = 40, Height = 16 } } } };
        var (window, anchor) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1);
        var provider = new DropdownMenuPositionProvider(horizontalMargin: 0);
        var anchorRect = new PixelRect(40, 60, 80, 32);
        var point = provider.CalculatePosition(anchorRect, new(520, 300), rtl, new(112, 64));
        var pivot = provider.TransformOrigin.Point;
        var layer = menu.Presentation!;
        var first = layer.TranslatePoint(default, window)!.Value;
        var last = layer.TranslatePoint(new Point(112, 0), window)!.Value;
        // A scale of .8 must leave the intersection pivot physically fixed.
        Assert.Equal(point.X + pivot.X * 112 * .2, Math.Min(first.X, last.X), 4);
        Assert.Equal(92, first.Y, 4);
        Advance(clock, window, 100);
        first = layer.TranslatePoint(default, window)!.Value;
        last = layer.TranslatePoint(new Point(112, 0), window)!.Value;
        Assert.Equal(point.X, Math.Min(first.X, last.X), 4);
        anchor.Margin = new(200, 60, 0, 0); Flush(window);
        first = layer.TranslatePoint(default, window)!.Value;
        last = layer.TranslatePoint(new Point(112, 0), window)!.Value;
        Assert.Equal(rtl ? 168 : 200, Math.Min(first.X, last.X), 4);
        window.Close();
    }

    [AvaloniaFact]
    public void CascadingPopup_OnlyTopPopupDismisses_AndRestoresParentFocus()
    {
        using var clock = new MotionTestClockScope();
        var parentItem = new DropdownMenuItem { Content = "More" };
        var childItem = new DropdownMenuItem { Content = "Child" };
        var child = new DropdownMenu { PlacementTarget = parentItem, AnchorPosition = MenuAnchorPosition.End, Items = { childItem } };
        var menu = new DropdownMenu { Items = { parentItem, child } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        parentItem.Focus(); child.IsOpen = true; Flush(window); Advance(clock, window, 100);
        var parentDismiss = 0; var childDismiss = 0;
        menu.DismissRequested += (_, _) => parentDismiss++;
        child.DismissRequested += (_, _) => { childDismiss++; child.IsOpen = false; };
        childItem.Focus(); window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Equal(1, childDismiss); Assert.Equal(0, parentDismiss);
        Advance(clock, window, 100);
        Assert.True(menu.IsPresented); Assert.False(child.IsPresented); Assert.True(parentItem.IsFocused);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.ActiveChildCount);
    }

    [AvaloniaFact]
    public void NonFocusablePopup_PassesOutsidePress_AndDoesNotTakeKeyboardFocus()
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenuPopup { IsPopupFocusable = false, Items = { new Border { Width = 100, Height = 40 } } };
        var (window, anchor) = Show(menu); anchor.Focus(); menu.IsOpen = true; Flush(window); Advance(clock, window, 1);
        Assert.True(anchor.IsFocused);
        var requests = 0; var presses = 0;
        menu.DismissRequested += (_, _) => requests++;
        ((Grid)window.Content!).PointerPressed += (_, _) => presses++;
        window.MouseDown(new(450, 250), MouseButton.Left); window.MouseUp(new(450, 250), MouseButton.Left);
        Assert.Equal(1, requests); Assert.Equal(1, presses);
        window.Close();
    }

    [AvaloniaFact]
    public void ClosingDuringEntry_PreservesTheOuterStateThroughDisposalAndRecomposition()
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { Items = { new DropdownMenuItem { Content = "Action" } } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 3);
        Assert.True(menu.IsPresented); Assert.NotEqual(1, menu.AppearanceScale);
        menu.IsOpen = false; Flush(window); Advance(clock, window, 1); Assert.False(menu.IsPresented);
        // Fixed upstream removes the composition first; disposing its old Transition
        // completes currentState=true and schedules a fresh exit on the next composition.
        Advance(clock, window, 1); Assert.True(menu.IsPresented); Assert.Equal(1, menu.AppearanceScale);
        menu.IsOpen = true; Flush(window); Advance(clock, window, 1);
        Advance(clock, window, 100); Assert.Equal(1, menu.AppearanceScale);
        window.Close();
    }

    [AvaloniaFact]
    public void ReplacingTemplate_ReusesCallerOwnedIcons_AndChangingThemeUpdatesColors()
    {
        using var clock = new MotionTestClockScope();
        var icon = new Border { Width = 20, Height = 20 };
        var item = new CheckableDropdownMenuItem { Content = "Choice", LeadingIcon = icon, IsChecked = true };
        var group = new DropdownMenuGroup { Items = { item } };
        var menu = new DropdownMenuPopup { Items = { group } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        item.IsChecked = false; Flush(window);
        Assert.Equal(((ISolidColorBrush)group.Background!).Color, ((ISolidColorBrush)item.Background!).Color);
        item.IsChecked = true; Flush(window);
        var standard = ((ISolidColorBrush)item.Background!).Color;
        MenuAppearance.SetColorStyle(menu, MenuColorStyle.Vibrant); Flush(window);
        Assert.NotEqual(standard, ((ISolidColorBrush)item.Background!).Color);
        var template = item.Template;
        item.Template = null; Flush(window); item.Template = template; Flush(window);
        Assert.Contains(icon, item.GetVisualDescendants());
        var theme = window.ActualThemeVariant;
        window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
        Assert.NotEqual(theme, window.ActualThemeVariant);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.ActiveChildCount); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void Automation_ReportsPresentedRolesAndRequestsControlledSelection()
    {
        using var clock = new MotionTestClockScope();
        var check = new CheckableDropdownMenuItem { Content = "Check" };
        var radio = new SelectableDropdownMenuItem { Content = "Radio" };
        var menu = new DropdownMenuPopup { Items = { new DropdownMenuGroup { Items = { check, radio } } } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1);
        var presenter = Assert.IsType<DropdownMenuPresenter>(menu.Popup.Child);
        Assert.Equal(AutomationControlType.Menu, ControlAutomationPeer.CreatePeerForElement(presenter)!.GetAutomationControlType());
        var checkPeer = ControlAutomationPeer.CreatePeerForElement(check)!;
        var radioPeer = ControlAutomationPeer.CreatePeerForElement(radio)!;
        Assert.Equal(AutomationControlType.CheckBox, checkPeer.GetAutomationControlType());
        Assert.Equal(AutomationControlType.RadioButton, radioPeer.GetAutomationControlType());
        var changes = 0; var clicks = 0;
        check.CheckedChangeRequested += (_, e) => { Assert.True(e.Value); changes++; };
        radio.Click += (_, _) => clicks++;
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(checkPeer);
        var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(radioPeer);
        toggle.Toggle(); selection.Select(); Flush(window);
        Assert.Equal(1, changes); Assert.Equal(1, clicks);
        Assert.False(check.IsChecked); Assert.False(radio.IsSelected);
        check.IsChecked = true; radio.IsSelected = true;
        Assert.Equal(ToggleState.On, toggle.ToggleState); Assert.True(selection.IsSelected);
        window.Close();
    }

    [AvaloniaFact]
    public void MiddleGroupShape_RemainsBoundToTheSystemShapeToken()
    {
        using var clock = new MotionTestClockScope();
        var group = new DropdownMenuGroup { Shapes = MenuDefaults.GroupShape(1, 3), Items = { new DropdownMenuItem { Content = "Middle" } } };
        var menu = new DropdownMenuPopup { Items = { group } };
        var (window, _) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1);
        Assert.Equal(new CornerRadius(8), group.CornerRadius);
        window.Resources[Material3Expressive.Tokens.MaterialTokens.Sys.Shape.Small] = new CornerRadius(10);
        Flush(window); Assert.Equal(new CornerRadius(10), group.CornerRadius);
        window.Close();
    }

    [AvaloniaFact]
    public void OutsideInputConsumption_BlocksHoverAndWheel_AndCanBeChangedWhileOpen()
    {
        using var clock = new MotionTestClockScope();
        var menu = new DropdownMenu { Items = { new DropdownMenuItem { Content = "Action" } } };
        var (window, anchor) = Show(menu); menu.IsOpen = true; Flush(window); Advance(clock, window, 1); Advance(clock, window, 100);
        var wheels = 0; ((Grid)window.Content!).PointerWheelChanged += (_, _) => wheels++;
        window.MouseMove(new(60, 75)); Assert.False(anchor.IsPointerOver);
        window.MouseWheel(new(60, 75), new(0, -1)); Assert.Equal(0, wheels);
        menu.ConsumePointerInputOutside = false; Flush(window);
        window.MouseMove(new(61, 75)); Assert.True(anchor.IsPointerOver);
        window.MouseWheel(new(61, 75), new(0, -1)); Assert.Equal(1, wheels);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Assert.Equal(0, window.MotionRoot.ActiveChildCount);
    }

    private static (MotionTestWindow Window, Button Anchor) Show(DropdownMenuPopup menu)
    {
        var anchor = new Button { Content = "Open", Width = 80, Height = 32, Margin = new(40, 60, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        menu.PlacementTarget = anchor;
        var window = DialogTests.Show(new Grid { Background = Brushes.Transparent, Children = { anchor, menu } });
        return (window, anchor);
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    { for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
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
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Styles;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MenuBarThemeTests
{
    // Root cause of the missing chevron/icons and hover after a popup reopens: the default item
    // theme came from a descendant-style Theme binding that dropped to null and back on every
    // popup close/open, and a template that survives that loses the theme's /template/ styles.
    [AvaloniaFact]
    public void DefaultItemThemes_AreAssignedOnce_AcrossPopupCloseAndReopen()
    {
        using var clock = new MotionTestClockScope();
        var leaf = new MenuItem { Header = "Leaf" };
        var parent = new MenuItem { Header = "Parent", Items = { leaf } };
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Other" }, parent } };
        var (menu, file, _, recent, recentChild, _) = Bar();
        var target = new Border { Width = 40, Height = 20 };
        var window = Show(new StackPanel { Children = { menu, target } });
        var cleared = 0;
        foreach (var item in new[] { parent, leaf, recent, recentChild })
            item.PropertyChanged += (_, e) => { if (e.Property == StyledElement.ThemeProperty && e.NewValue is null) cleared++; };
        flyout.Popup.ShouldUseOverlayLayer = true;
        for (var i = 0; i < 3; i++)
        {
            flyout.ShowAt(target); Settle(clock, window);
            parent.IsSubMenuOpen = true; Settle(clock, window);
            AssertChevronDrawn(parent);
            flyout.Hide(); Settle(clock, window);
            file.IsSubMenuOpen = true; Settle(clock, window);
            recent.IsSubMenuOpen = true; Settle(clock, window);
            AssertChevronDrawn(recent);
            file.IsSubMenuOpen = false; Settle(clock, window);
        }
        Assert.Equal(0, cleared);
        var vertical = window.FindResource("MaterialVerticalMenuItemTheme");
        Assert.All(new[] { parent, leaf, recent, recentChild }, item => Assert.Same(vertical, item.Theme));
        window.Close();
    }

    [AvaloniaFact]
    public void Hover_ShowsTheStateLayer_InsideMenuBarSubmenus()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, recent, recentChild, _) = Bar();
        var window = Show(menu);
        Click(window, file);
        Assert.True(file.IsSubMenuOpen);
        foreach (var item in new[] { open, recent })
        {
            window.MouseMove(Center(item, window)); Settle(clock, window);
            AssertHovered(item);
        }
        recent.IsSubMenuOpen = true; Settle(clock, window);
        window.MouseMove(Center(recentChild, window)); Settle(clock, window);
        AssertHovered(recentChild);
        window.Close();
    }

    // Avalonia focuses a submenu parent when its submenu opens. Natively that focus showed the 0.10
    // focus layer, because each native popup had its own keyboard-default input mode (headless
    // popups are overlay-hosted, so that part is only observable on a real windowing platform).
    [AvaloniaFact]
    public void SubmenuParent_HoversImmediately_AndKeepsTheHoverLayerWhenItsSubmenuOpens()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, recent, _, _) = Bar();
        var window = Show(menu);
        Click(window, file);
        window.MouseMove(Center(recent, window)); Settle(clock, window);
        AssertHovered(recent);
        Assert.Equal(0.08, MaterialInteraction.GetOrCreate(recent).StateLayerOpacity, 3);
        recent.IsSubMenuOpen = true; recent.Focus(); Settle(clock, window); // what the open delay does
        Assert.True(recent.IsSubMenuOpen);
        AssertHovered(recent);
        Assert.Equal(0.08, MaterialInteraction.GetOrCreate(recent).StateLayerOpacity, 3);
        window.Close();
    }

    [AvaloniaFact]
    public void FlyoutSubmenuParent_KeepsItsOwnHoverLayerWhenItsSubmenuOpens()
    {
        using var clock = new MotionTestClockScope();
        var name = new MenuItem { Header = "Name" };
        var sortBy = new MenuItem { Header = "Sort by", Items = { name, new MenuItem { Header = "Date" } } };
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Copy" }, sortBy } };
        var target = new Border { Width = 40, Height = 20 };
        var window = Show(target);
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.ShowAt(target); Settle(clock, window);
        window.MouseMove(Center(sortBy, window)); Settle(clock, window);
        AssertHovered(sortBy);
        sortBy.IsSubMenuOpen = true; sortBy.Focus(); Settle(clock, window); // what the open delay does
        Assert.True(sortBy.IsSubMenuOpen);
        AssertHovered(sortBy);
        Assert.Equal(0.08, MaterialInteraction.GetOrCreate(sortBy).StateLayerOpacity, 3);
        flyout.Hide(); window.Close();
    }

    // A press in a submenu item bubbles through its template popup to the submenu parent. The
    // parents used to treat it as their own press and ripple from the clicked point.
    [AvaloniaFact]
    public void PressInANestedFlyoutSubmenu_RipplesOnlyTheClickedItem()
    {
        using var clock = new MotionTestClockScope();
        var leaf = new MenuItem { Header = "Leaf", StaysOpenOnClick = true };
        var more = new MenuItem { Header = "More", Items = { leaf } };
        var sortBy = new MenuItem { Header = "Sort by", Items = { new MenuItem { Header = "Name" }, more } };
        var flyout = new MenuFlyout { Items = { new MenuItem { Header = "Copy" }, sortBy } };
        var target = new Border { Width = 40, Height = 20 };
        var window = Show(target);
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.ShowAt(target); Settle(clock, window);
        sortBy.IsSubMenuOpen = true; Settle(clock, window);
        more.IsSubMenuOpen = true; Settle(clock, window);
        AssertPressRipplesOnly(window, clock, leaf, sortBy, more);
        flyout.Hide(); window.Close();
    }

    [AvaloniaFact]
    public void PressInANestedMenuBarSubmenu_RipplesOnlyTheClickedItem()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, recent, recentChild, _) = Bar();
        recentChild.StaysOpenOnClick = true;
        var window = Show(menu);
        file.IsSubMenuOpen = true; Settle(clock, window);
        recent.IsSubMenuOpen = true; Settle(clock, window);
        AssertPressRipplesOnly(window, clock, recentChild, file, recent);
        window.Close();
    }

    static void AssertPressRipplesOnly(Window window, MotionTestClockScope clock, MenuItem clicked, params MenuItem[] ancestors)
    {
        var point = Center(clicked, window);
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Flush(window);
        clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window);
        Assert.True(MaterialInteraction.GetOrCreate(clicked).RippleCount > 0, "the clicked item should ripple");
        foreach (var ancestor in ancestors)
        {
            var controller = MaterialInteraction.GetOrCreate(ancestor);
            Assert.Equal(0, controller.RippleCount);
            Assert.False(controller.Interaction.HasFlag(InteractionState.Pressed), $"{ancestor.Header} pressed");
        }
        window.MouseUp(point, MouseButton.Left); Flush(window);
    }

    [AvaloniaFact]
    public void OpenSubmenuParent_ShowsOneHoverOpacityHighlight_WhileThePointerIsInTheSubmenu()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, recent, recentChild, _) = Bar();
        var window = Show(menu);
        Click(window, file);
        window.MouseMove(Center(recent, window)); Settle(clock, window);
        recent.IsSubMenuOpen = true; recent.Focus(); Settle(clock, window);
        window.MouseMove(Center(recentChild, window)); Settle(clock, window);
        Assert.True(recent.IsSubMenuOpen);
        AssertHovered(recentChild);
        Assert.Equal(0, MaterialInteraction.GetOrCreate(recent).StateLayerOpacity, 3);
        var container = Color(window, MaterialTokens.Comp.Menu.StandardContainer);
        Assert.Equal(Highlight(window, container, Color(window, MaterialTokens.Comp.Menu.Text), hover: true), Brush(Surface(recent).Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Hover_ShowsTheStateLayer_InsideAMenuFlyout()
    {
        using var clock = new MotionTestClockScope();
        var leaf = new MenuItem { Header = "Leaf" };
        var parent = new MenuItem { Header = "Parent", Items = { leaf } };
        var other = new MenuItem { Header = "Other" };
        var flyout = new MenuFlyout { Items = { other, parent } };
        var target = new Border { Width = 40, Height = 20 };
        var window = Show(target);
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.ShowAt(target); Settle(clock, window);
        foreach (var item in new[] { other, parent })
        {
            window.MouseMove(Center(item, window)); Settle(clock, window);
            AssertHovered(item);
        }
        flyout.Hide(); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void SubmenuChevron_IsDrawn_InBarSubmenusAndFlyouts(bool rtl)
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, recent, _, _) = Bar();
        var window = Show(menu);
        if (rtl) window.FlowDirection = FlowDirection.RightToLeft;
        file.IsSubMenuOpen = true; Settle(clock, window);
        AssertChevronDrawn(recent);

        var parent = new MenuItem { Header = "Parent", Items = { new MenuItem { Header = "Leaf" } } };
        var flyout = new MenuFlyout { Items = { parent } };
        var target = new Border { Width = 40, Height = 20 };
        ((DockPanel)window.Content!).Children.Add(target);
        flyout.Popup.ShouldUseOverlayLayer = true;
        file.IsSubMenuOpen = false;
        flyout.ShowAt(target); Settle(clock, window);
        AssertChevronDrawn(parent);
        flyout.Hide(); window.Close();
    }

    [AvaloniaFact]
    public void Reopened_SubmenuItems_StillHoverAndDrawTheirChevron()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, recent, recentChild, _) = Bar();
        var window = Show(menu);
        Click(window, file); Settle(clock, window);
        Press(window, Avalonia.Input.Key.Escape, PhysicalKey.Escape); Settle(clock, window);
        Assert.False(file.IsSubMenuOpen);
        Click(window, file); Settle(clock, window);
        Assert.True(file.IsSubMenuOpen);
        AssertChevronDrawn(recent);
        window.MouseMove(Center(open, window)); Settle(clock, window);
        AssertHovered(open);
        // A nested submenu reopened inside a reopened menu.
        recent.IsSubMenuOpen = true; Settle(clock, window);
        recent.IsSubMenuOpen = false; Settle(clock, window);
        recent.IsSubMenuOpen = true; Settle(clock, window);
        window.MouseMove(Center(recentChild, window)); Settle(clock, window);
        AssertHovered(recentChild);
        Assert.Same(window.FindResource("MaterialVerticalMenuItemTheme"), recentChild.Theme);
        window.Close();
    }

    [AvaloniaFact]
    public void Reopened_FlyoutItems_StillHoverAndDrawTheirChevron()
    {
        using var clock = new MotionTestClockScope();
        var leaf = new MenuItem { Header = "Leaf" };
        var parent = new MenuItem { Header = "Parent", Items = { leaf } };
        var other = new MenuItem { Header = "Other" };
        var flyout = new MenuFlyout { Items = { other, parent } };
        var target = new Border { Width = 40, Height = 20 };
        var window = Show(target);
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.ShowAt(target); Settle(clock, window);
        flyout.Hide(); Settle(clock, window);
        flyout.ShowAt(target); Settle(clock, window);
        AssertChevronDrawn(parent);
        window.MouseMove(Center(other, window)); Settle(clock, window);
        AssertHovered(other);
        flyout.Hide(); window.Close();
    }

    static void AssertHovered(MenuItem item)
    {
        var controller = MaterialInteraction.GetOrCreate(item);
        Assert.True(controller.Interaction.HasFlag(InteractionState.Hovered), $"{item.Header} not hovered: {controller.Interaction}");
        Assert.True(controller.StateLayerOpacity > 0, $"{item.Header}: no state layer drawn");
        // The item's own surface must be the one drawing it (not, e.g., its submenu's container).
        Assert.Same(Surface(item), controller.Presenter);
    }

    static void AssertChevronDrawn(MenuItem item)
    {
        var chevron = Part<Viewbox>(item, "PART_Chevron");
        Assert.True(chevron.IsEffectivelyVisible);
        Assert.True(chevron.Bounds.Width > 0 && chevron.Bounds.Height > 0, $"chevron bounds {chevron.Bounds}");
        var path = chevron.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
        Assert.True(path.Bounds.Width > 0, $"path bounds {path.Bounds}");
        Assert.True(path.Fill is ISolidColorBrush { Color.A: > 0 }, $"fill {path.Fill}");
        // Inside the item's surface, not clipped away.
        var surface = Surface(item);
        var origin = chevron.TranslatePoint(default, surface)!.Value;
        Assert.InRange(origin.X, 0, surface.Bounds.Width - chevron.Bounds.Width);
    }

    [AvaloniaFact]
    public void Theme_AppliesToMenuAndTopLevelItems_AsATransparentHorizontalBar()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, _, _, edit) = Bar();
        var window = Show(menu);
        Assert.Same(window.FindResource("MaterialMenuBarItemTheme"), file.Theme);
        Assert.Same(window.FindResource("MaterialMenuBarItemTheme"), edit.Theme);
        Assert.Null(file.ItemContainerTheme);
        Assert.Equal(Colors.Transparent, Brush(menu.Background));
        Assert.Equal(Colors.Transparent, Brush(file.Background));
        Assert.Equal(Colors.Transparent, Brush(Surface(file).Background));
        Assert.Equal(0, Surface(file).ShadowElevation);
        Assert.True(MaterialInteraction.GetIsInteractive(file));
        // Horizontal row, items side by side on one line.
        Assert.Equal(file.Bounds.Y, edit.Bounds.Y);
        Assert.True(edit.Bounds.X >= file.Bounds.Right);
        Assert.Equal(48, menu.Bounds.Height);
        window.Close();
    }

    [AvaloniaFact]
    public void TopLevelItem_IsLabelLargeOnSurface_With40ContainerIn48TouchTarget()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, _, _, _) = Bar();
        var window = Show(menu);
        var style = (TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.LabelLarge.Style);
        var text = Header(file);
        Assert.Equal(style.FontSize, text.FontSize);
        Assert.Equal(style.FontWeight, text.FontWeight);
        Assert.Equal(Color(window, MaterialTokens.Comp.Menu.Text), Brush(text.Foreground));
        Assert.Equal(new Thickness(12, 0), file.Padding);
        Assert.Equal(new Thickness(0), file.Margin);
        Assert.Equal(TokenScan.Resolve(window, MaterialTokens.Comp.Menu.ItemShape), Surface(file).CornerRadius);
        Assert.Equal(40, Surface(file).Bounds.Height);
        Assert.True(file.Bounds.Height >= 48);
        Assert.True(file.Bounds.Width >= 48);
        // A short header still gets the 48 touch width.
        var tiny = new MenuItem { Header = "A" };
        menu.Items.Add(tiny); Flush(window);
        Assert.Equal(48, tiny.Bounds.Width);
        // No gesture text or chevron on the bar, even for an item with a submenu.
        Assert.False(Part<StackPanel>(file, "PART_Trailing").IsVisible);
        window.Close();
    }

    [AvaloniaFact]
    public void TopLevelItem_IconSlotWorks()
    {
        using var clock = new MotionTestClockScope();
        var icon = new Border { Width = 18, Height = 18 };
        var item = new MenuItem { Header = "Home", Icon = icon };
        var window = Show(new Menu { Items = { item } });
        Assert.True(Part<Grid>(item, "PART_LeadingIcon").IsVisible);
        Assert.True(icon.IsAttachedToVisualTree());
        Assert.True(icon.TranslatePoint(default, item)!.Value.X < Header(item).TranslatePoint(default, item)!.Value.X);
        window.Close();
    }

    [AvaloniaFact]
    public void Open_And_KeyboardSelection_HaveAPersistentHighlight()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, _, _, edit) = Bar();
        var window = Show(menu);
        var highlight = Highlight(window, Colors.Transparent, Color(window, MaterialTokens.Comp.Menu.Text));
        Assert.Equal(Colors.Transparent, Brush(file.Background));

        file.IsSubMenuOpen = true; Settle(clock, window);
        Assert.Contains(":open", file.Classes);
        Assert.Equal(highlight, Brush(file.Background));
        Assert.Equal(highlight, Brush(Surface(file).Background));
        // Moving into the submenu keeps the open item highlighted.
        open.Focus(NavigationMethod.Directional); Flush(window);
        Assert.False(file.IsFocused);
        Assert.Equal(highlight, Brush(file.Background));

        file.IsSubMenuOpen = false; Flush(window);
        // Keyboard selection on the bar without focus (e.g. while the menu is not open) also shows.
        menu.Close(); Flush(window);
        edit.IsSelected = true; Flush(window);
        Assert.False(edit.IsFocused);
        Assert.Equal(highlight, Brush(edit.Background));
        edit.IsSelected = false; Flush(window);
        Assert.Equal(Colors.Transparent, Brush(edit.Background));
        window.Close();
    }

    [AvaloniaFact]
    public void Submenu_UsesTheVerticalMenuContainer_AndNestedItemsUseTheVerticalItemTheme()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, recent, recentChild, edit) = Bar();
        var window = Show(menu);
        var vertical = window.FindResource("MaterialVerticalMenuItemTheme");
        file.IsSubMenuOpen = true; Flush(window);
        var popup = Part<Popup>(file, "PART_Popup");
        Assert.True(popup.IsOpen);
        Assert.Equal(PlacementMode.BottomEdgeAlignedLeft, popup.Placement);
        Assert.True(popup.IsLightDismissEnabled);
        Assert.Same(menu, popup.OverlayInputPassThroughElement);
        var container = Assert.IsType<MaterialSurface>(popup.Child);
        Assert.Equal(Color(window, MaterialTokens.Comp.Menu.StandardContainer), Brush(container.Background));
        Assert.Equal(TokenScan.Resolve(window, MaterialTokens.Comp.Menu.VerticalShape), container.CornerRadius);
        Assert.Equal(TokenScan.Resolve(window, MaterialTokens.Comp.Menu.VerticalPadding), container.Padding);

        Assert.Same(vertical, open.Theme);
        Assert.Same(vertical, recent.Theme);
        Assert.True(open.Bounds.Width >= 112);
        // Gesture text and the submenu chevron are the vertical item's.
        Assert.True(Part<StackPanel>(open, "PART_Trailing").IsVisible);
        Assert.True(Part<Viewbox>(recent, "PART_Chevron").IsVisible);
        Assert.Equal(new Thickness(12, 2), file.Items.OfType<Separator>().Single().Margin);

        // Deeper submenu: still the vertical theme, opening to the side.
        recent.IsSubMenuOpen = true; Flush(window);
        Assert.Same(vertical, recentChild.Theme);
        Assert.True(recentChild.IsAttachedToVisualTree());
        Assert.Equal(PlacementMode.RightEdgeAlignedTop, Part<Popup>(recent, "PART_Popup").Placement);
        Assert.True(Left(recentChild, window) >= Right(recent, window) - 1);
        file.IsSubMenuOpen = false; Flush(window);

        // Checkable item: the vertical theme's check and selected colors.
        edit.IsSubMenuOpen = true; Flush(window);
        var wrap = edit.Items.OfType<MenuItem>().Single(i => i.ToggleType == MenuItemToggleType.CheckBox);
        Assert.Same(vertical, wrap.Theme);
        Assert.True(Part<Viewbox>(wrap, "PART_Check").IsVisible);
        Assert.Equal(Color(window, MaterialTokens.Comp.Menu.StandardSelectedContainer), Brush(wrap.Background));
        window.Close();
    }

    [AvaloniaFact]
    public void ExplicitItemContainerThemes_StillWin()
    {
        using var clock = new MotionTestClockScope();
        var custom = new ControlTheme(typeof(MenuItem)) { BasedOn = null };
        var child = new MenuItem { Header = "Child" };
        var top = new MenuItem { Header = "Top", ItemContainerTheme = custom, Items = { child } };
        var menu = new Menu { Items = { top } };
        var window = Show(menu);
        top.IsSubMenuOpen = true; Flush(window);
        Assert.Same(custom, child.Theme);
        window.Close();
    }

    [AvaloniaFact]
    public void VerticalItem_SelectedWithoutFocus_IsHighlighted_InTheBarAndInAFlyout()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, recent, recentChild, _) = Bar();
        var window = Show(menu);
        file.IsSubMenuOpen = true; Flush(window);
        recent.IsSubMenuOpen = true; Flush(window);
        recentChild.Focus(NavigationMethod.Directional); Flush(window);
        Assert.True(recent.IsSelected);
        Assert.False(recent.IsFocused);
        var container = Color(window, MaterialTokens.Comp.Menu.StandardContainer);
        // One highlight at the hover opacity, matching the parent's hover before the pointer left it.
        var highlight = Highlight(window, container, Color(window, MaterialTokens.Comp.Menu.Text), hover: true);
        Assert.Equal(highlight, Brush(recent.Background));
        Settle(clock, window);
        Assert.Equal(highlight, Brush(Surface(recent).Background)); // animated by the item chrome
        Assert.Equal(container, Brush(recentChild.Background)); // focused: the state layer shows instead
        window.Close();

        // The flyout path gets the same state.
        var leaf = new MenuItem { Header = "Leaf" };
        var parent = new MenuItem { Header = "Parent", Items = { leaf } };
        var flyout = new MenuFlyout { Items = { parent, new MenuItem { Header = "Other" } } };
        var target = new Border { Width = 40, Height = 20 };
        window = Show(target);
        flyout.Popup.ShouldUseOverlayLayer = true;
        flyout.ShowAt(target); Flush(window);
        parent.IsSubMenuOpen = true; Flush(window);
        leaf.Focus(NavigationMethod.Directional); Flush(window);
        Assert.True(parent.IsSelected);
        Assert.Equal(highlight, Brush(parent.Background));
        flyout.Hide(); window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_ArrowsMoveAcrossTheBarAndIntoSubmenus_EscapeCloses()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, _, _, edit) = Bar();
        var window = Show(menu);
        file.Focus(NavigationMethod.Tab); Flush(window);
        Press(window, Avalonia.Input.Key.Right, PhysicalKey.ArrowRight);
        Assert.True(edit.IsFocused);
        Assert.True(edit.IsSelected);
        Press(window, Avalonia.Input.Key.Left, PhysicalKey.ArrowLeft);
        Assert.True(file.IsFocused);
        Press(window, Avalonia.Input.Key.Down, PhysicalKey.ArrowDown);
        Assert.True(file.IsSubMenuOpen);
        Assert.True(open.IsFocused);
        // Right on a leaf moves to the next top-level menu, opened.
        Press(window, Avalonia.Input.Key.Right, PhysicalKey.ArrowRight);
        Assert.False(file.IsSubMenuOpen);
        Assert.True(edit.IsSubMenuOpen);
        Press(window, Avalonia.Input.Key.Escape, PhysicalKey.Escape);
        Assert.False(edit.IsSubMenuOpen);
        Assert.True(edit.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_RightOpensANestedSubmenu_LeftClosesIt()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, recent, recentChild, _) = Bar();
        var window = Show(menu);
        file.Focus(NavigationMethod.Tab);
        Press(window, Avalonia.Input.Key.Down, PhysicalKey.ArrowDown);
        Press(window, Avalonia.Input.Key.Down, PhysicalKey.ArrowDown);
        Assert.True(recent.IsFocused);
        Press(window, Avalonia.Input.Key.Right, PhysicalKey.ArrowRight);
        Assert.True(recent.IsSubMenuOpen);
        Assert.True(recentChild.IsFocused);
        Press(window, Avalonia.Input.Key.Left, PhysicalKey.ArrowLeft);
        Assert.False(recent.IsSubMenuOpen);
        Assert.True(recent.IsFocused);
        Assert.True(file.IsSubMenuOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void AccessKeys_RenderThroughAccessText_AndAltLetterOpensTheMenu()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, _, _, _) = Bar();
        var window = Show(menu);
        var text = Assert.IsType<AccessText>(Header(file));
        Assert.Equal("_File", text.Text); // AccessText draws "File" with the F underlined
        Assert.Equal("F", text.AccessKey, ignoreCase: true);
        window.KeyPress(Avalonia.Input.Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        window.KeyPress(Avalonia.Input.Key.F, RawInputModifiers.Alt, PhysicalKey.F, "f");
        Flush(window);
        window.KeyRelease(Avalonia.Input.Key.F, RawInputModifiers.Alt, PhysicalKey.F, "f");
        window.KeyRelease(Avalonia.Input.Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Flush(window);
        Assert.True(file.IsSubMenuOpen);
        Assert.IsType<AccessText>(Header(open));
        window.Close();
    }

    [AvaloniaFact]
    public void AltTap_ActivatesTheBar_WithTheFirstItemHighlighted()
    {
        // Avalonia 12.1.2's AccessKeyHandler opens the main menu on an Alt tap; F10 is not wired
        // natively (IMainMenu mentions it, nothing handles it), and the theme does not add it.
        using var clock = new MotionTestClockScope();
        var (menu, file, _, _, _, edit) = Bar();
        var window = Show(menu);
        window.KeyPress(Avalonia.Input.Key.LeftAlt, RawInputModifiers.Alt, PhysicalKey.AltLeft, null);
        window.KeyRelease(Avalonia.Input.Key.LeftAlt, RawInputModifiers.None, PhysicalKey.AltLeft, null);
        Settle(clock, window);
        Assert.True(menu.IsOpen);
        Assert.True(file.IsSelected);
        Assert.True(file.IsFocused);
        Assert.True(Header(file) is AccessText { ShowAccessKey: true });
        Press(window, Avalonia.Input.Key.Right, PhysicalKey.ArrowRight);
        Assert.True(edit.IsFocused);
        Press(window, Avalonia.Input.Key.Escape, PhysicalKey.Escape);
        Assert.False(menu.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Rtl_ItemsFlowRightToLeft_AndSubmenusMirror()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, recent, recentChild, edit) = Bar();
        var window = Show(menu);
        window.FlowDirection = FlowDirection.RightToLeft; Flush(window);
        Assert.True(Left(file, window) > Left(edit, window));
        Assert.Equal(window.Bounds.Width, Right(file, window), 0.5);

        file.IsSubMenuOpen = true; Flush(window);
        var container = (Control)Part<Popup>(file, "PART_Popup").Child!;
        // Bottom edge aligned to the item's start (right) edge, growing toward the left.
        Assert.Equal(Right(file, window), Right(container, window), 0.5);
        Assert.True(container.TranslatePoint(default, window)!.Value.Y >= file.TranslatePoint(default, window)!.Value.Y + file.Bounds.Height - 0.5);
        recent.IsSubMenuOpen = true; Flush(window);
        var nested = (Control)Part<Popup>(recent, "PART_Popup").Child!;
        Assert.True(Right(nested, window) <= Left(recent, window) + 0.5);
        window.Close();
    }

    [AvaloniaFact]
    public void Disabled_TopLevelAndNestedItems_UseTheDisabledColors_AndDoNotOpen()
    {
        using var clock = new MotionTestClockScope();
        var view = new MenuItem { Header = "_View", IsEnabled = false, Items = { new MenuItem { Header = "Zoom" } } };
        var cut = new MenuItem { Header = "Cu_t", IsEnabled = false };
        var edit = new MenuItem { Header = "_Edit", Items = { cut } };
        var menu = new Menu { Items = { edit, view } };
        var window = Show(menu);
        var disabled = Color(window, MaterialTokens.Comp.Menu.DisabledText);
        Assert.Equal(disabled, Brush(Header(view).Foreground));
        Click(window, view);
        Assert.False(view.IsSubMenuOpen);
        edit.IsSubMenuOpen = true; Flush(window);
        Assert.Equal(disabled, Brush(Header(cut).Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void PointerClick_OpensAndSwitchesMenus()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, _, _, _, edit) = Bar();
        var window = Show(menu);
        Click(window, file);
        Assert.True(file.IsSubMenuOpen);
        Assert.True(menu.IsOpen);
        window.MouseMove(Center(edit, window)); Flush(window);
        Assert.False(file.IsSubMenuOpen);
        Assert.True(edit.IsSubMenuOpen);
        Click(window, edit);
        Assert.False(edit.IsSubMenuOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void DarkMode_FollowsTheColorScheme()
    {
        using var clock = new MotionTestClockScope();
        var (menu, file, open, _, _, _) = Bar();
        var window = Show(menu);
        window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
        Assert.True(TokenResolver.TryResolve(window, MaterialTokens.Sys.Color.OnSurface, ThemeVariant.Dark, out var onSurface));
        Assert.True(TokenResolver.TryResolve(window, MaterialTokens.Sys.Color.SurfaceContainerLow, ThemeVariant.Dark, out var container));
        Assert.Equal((Color)onSurface!, Brush(Header(file).Foreground));
        file.IsSubMenuOpen = true; Flush(window);
        Assert.Equal((Color)container!, Brush(((MaterialSurface)Part<Popup>(file, "PART_Popup").Child!).Background));
        Assert.Equal((Color)onSurface!, Brush(Header(open).Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void ContextMenu_IsNotThemedByTheBar()
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Copy" };
        var target = new Border { Width = 40, Height = 20, ContextMenu = new ContextMenu { Items = { item } } };
        var window = Show(new StackPanel { Children = { new Menu { Items = { new MenuItem { Header = "File" } } }, target } });
        Assert.NotSame(window.FindResource("MaterialMenuBarItemTheme"), item.Theme);
        Assert.NotSame(window.FindResource("MaterialVerticalMenuItemTheme"), item.Theme);
        window.Close();
    }

    static (Menu, MenuItem, MenuItem, MenuItem, MenuItem, MenuItem) Bar()
    {
        var open = new MenuItem { Header = "_Open", InputGesture = new KeyGesture(Key.O, KeyModifiers.Control) };
        var recentChild = new MenuItem { Header = "Report.docx" };
        var recent = new MenuItem { Header = "Open _recent", Items = { recentChild } };
        var file = new MenuItem { Header = "_File", Items = { open, recent, new Separator(), new MenuItem { Header = "E_xit" } } };
        var edit = new MenuItem { Header = "_Edit", Items = { new MenuItem { Header = "_Undo" }, new MenuItem { Header = "Word wrap", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true } } };
        var menu = new Menu { Items = { file, edit, new MenuItem { Header = "_View", IsEnabled = false } } };
        return (menu, file, open, recent, recentChild, edit);
    }

    static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
    }

    static Color Color(IResourceHost host, MaterialTokenKey key) => (Color)TokenScan.Resolve(host, key);
    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;
    static Color Highlight(IResourceHost host, Color container, Color content, bool hover = false) =>
        Foundation.Rendering.ColorComposite.Over(content, (double)TokenScan.Resolve(host, hover
            ? MaterialTokens.Sys.State.HoverStateLayerOpacity : MaterialTokens.Sys.State.FocusStateLayerOpacity), container);
    static MaterialSurface Surface(MenuItem item) => Part<MaterialSurface>(item, "PART_ItemSurface");
    static T Part<T>(Control owner, string name) where T : Control =>
        owner.GetVisualDescendants().OfType<T>().First(c => c.Name == name && c.TemplatedParent == owner);
    static TextBlock Header(MenuItem item) => Part<ContentPresenter>(item, "PART_HeaderPresenter").GetVisualDescendants().OfType<TextBlock>().First();
    // Window-space edges; under RTL a control's origin maps to its right edge.
    static double Left(Control c, Window window) => Math.Min(c.TranslatePoint(default, window)!.Value.X, c.TranslatePoint(new Point(c.Bounds.Width, 0), window)!.Value.X);
    static double Right(Control c, Window window) => Math.Max(c.TranslatePoint(default, window)!.Value.X, c.TranslatePoint(new Point(c.Bounds.Width, 0), window)!.Value.X);
    static void Settle(MotionTestClockScope clock, Window window)
    {
        Flush(window); clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(2)); Flush(window);
    }
    static Point Center(Control control, Window window) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
    static void Press(Window window, Key key, PhysicalKey physical)
    {
        window.KeyPress(key, RawInputModifiers.None, physical, null);
        window.KeyRelease(key, RawInputModifiers.None, physical, null);
        Flush(window);
    }
    static void Click(Window window, MenuItem item)
    {
        var point = Center(item, window);
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
        Flush(window);
    }

    static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 640, Height = 480 };
        window.Template = new FuncControlTemplate<Window>((owner, scope) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter", [!ContentPresenter.ContentProperty] = owner[!ContentControl.ContentProperty] };
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            scope.Register(presenter.Name, presenter); scope.Register(layers.Name, layers);
            return layers;
        });
        window.Styles.Add(new MaterialTheme());
        window.Content = new DockPanel { Children = { content, new Border() } };
        DockPanel.SetDock(content, Dock.Top);
        window.Show(); Flush(window);
        return window;
    }
}

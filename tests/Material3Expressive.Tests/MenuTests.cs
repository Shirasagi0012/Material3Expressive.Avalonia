using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class MenuTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeInteractionSourcePairsInputAndCancelsOnReplacementAndClose(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add;
        var item = new MenuItem { Header = "Choice", ToggleType = MenuItemToggleType.CheckBox, StaysOpenOnClick = true };
        MenuAppearance.SetInteractionSource(item, source);
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout, baseline);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            var point = item.TranslatePoint(new Rect(item.Bounds.Size).Center, window)!.Value;
            var clicks = 0; item.Click += (_, _) => clicks++;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
            var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            window.MouseUp(point, MouseButton.Left);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
            Assert.True(item.IsChecked); Assert.Equal(1, clicks); Assert.True(flyout.IsOpen);
            events.Clear(); window.MouseDown(point, MouseButton.Left);
            press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            var replacement = new MaterialInteractionSource();
            MenuAppearance.SetInteractionSource(item, replacement);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
            window.MouseUp(point, MouseButton.Left);
            events.Clear(); replacement.Interaction += events.Add;
            window.MouseDown(point, MouseButton.Left);
            press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            flyout.Hide(); Flush(window);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
            window.MouseUp(point, MouseButton.Left);
            flyout.ShowAt((Control)window.Content!); Flush(window);
            MenuAppearance.SetInteractionSource(item, null);
            clicks = 0; Click(window, item); Assert.Equal(1, clicks);
        }
        finally { flyout.Hide(); window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeInteractionSourceDoesNotReplayAcrossReplacementTemplateAndRemount(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource();
        var item = new MenuItem { Header = "Choice", ToggleType = MenuItemToggleType.CheckBox };
        MenuAppearance.SetInteractionSource(item, source);
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout, baseline);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            var controller = MaterialInteraction.GetOrCreate(item);
            var press = new MaterialInteractionEvent.Press(new Point(24, 24));
            source.Emit(press); Advance(); Assert.Equal(1, controller.RippleCount);
            Assert.False(item.IsChecked);
            var replacement = new MaterialInteractionSource(); replacement.Emit(press);
            MenuAppearance.SetInteractionSource(item, replacement); Advance(); Assert.Equal(0, controller.RippleCount);
            source.Emit(press); Advance(); Assert.Equal(0, controller.RippleCount);
            replacement.Emit(press); Advance(); Assert.Equal(1, controller.RippleCount);
            var template = item.Template;
            item.Template = new FuncControlTemplate<MenuItem>((_, _) => new Border()); Flush(window);
            item.Template = template; Flush(window); Advance(); Assert.Equal(0, controller.RippleCount);
            replacement.Emit(press); Advance(); Assert.Equal(1, controller.RippleCount);
            flyout.Hide(); Flush(window); replacement.Emit(press);
            flyout.ShowAt((Control)window.Content!); Flush(window); Advance(); Assert.Equal(0, controller.RippleCount);
            replacement.Emit(press); Advance(); Assert.Equal(1, controller.RippleCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { flyout.Hide(); window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
        void Advance() { for (var i = 0; i < 3; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    }

    [AvaloniaTheory]
    [InlineData(MenuItemToggleType.CheckBox)]
    [InlineData(MenuItemToggleType.Radio)]
    public void NativeSelectionIcon_AnimatesReversesAndReleasesWithTemplate(MenuItemToggleType toggle)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = new Border { Width = 72, Height = 16 }, ToggleType = toggle, StaysOpenOnClick = true };
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            var slot = item.GetVisualDescendants().OfType<DropdownMenuLeadingIcon>().Single();
            var presenter = Assert.IsType<DropdownMenuSlotPresenter>(slot.Child);
            Assert.Equal(0, slot.Bounds.Width); Assert.Null(presenter.Child);
            slot.Opacity = .37;
            Assert.Equal(0, slot.Opacity); // Animation must not replace the caller's base value.
            var label = item.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().Single(p => p.Name == "PART_HeaderPresenter");
            var start = label.TranslatePoint(default, item)!.Value.X;
            var clicks = 0; item.Click += (_, _) => clicks++;
            Click(window, item); Flush(window);
            Assert.True(item.IsChecked); Assert.Equal(1, clicks); Assert.True(flyout.IsOpen);
            Advance(64);
            Assert.InRange(slot.Bounds.Width, .01, 19.99); Assert.InRange(slot.Opacity, .01, .99);
            Assert.True(label.TranslatePoint(default, item)!.Value.X > start);
            Assert.NotNull(presenter.Child);
            item.IsChecked = false; Advance(32);
            Assert.NotNull(presenter.Child); // Retain the check through its exit.
            item.IsChecked = true; Advance(1000);
            Assert.Equal(20, slot.Bounds.Width); Assert.Equal(1, slot.Opacity);
            var check = Assert.IsType<Viewbox>(presenter.Child);
            Assert.Equal(20, check.Bounds.Width);
            item.IsChecked = false; Advance(1000);
            Assert.Equal(0, slot.Bounds.Width); Assert.Null(presenter.Child);
            Assert.Equal(start, label.TranslatePoint(default, item)!.Value.X);
            item.IsChecked = true; Advance(32);
            var template = item.Template;
            item.Template = new FuncControlTemplate<MenuItem>((_, _) => new Border()); Flush(window);
            Assert.Equal(.37, slot.Opacity);
            item.Template = template; Flush(window);
            var replacement = item.GetVisualDescendants().OfType<DropdownMenuLeadingIcon>().Single();
            Assert.NotSame(slot, replacement); Assert.Equal(20, replacement.Bounds.Width);
            flyout.Hide(); window.Close(); Advance(16);
            Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
            Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveTaskCount);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { flyout.Hide(); window.Close(); }
        void Advance(int ms)
        {
            for (var t = 0; t < ms; t += 16) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeSelectionIcon_RegularIconAndToggleModeRemoveAnimatedBranch(bool exiting)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Choice", ToggleType = MenuItemToggleType.CheckBox, IsChecked = exiting };
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            var slot = item.GetVisualDescendants().OfType<DropdownMenuLeadingIcon>().Single();
            var presenter = Assert.IsType<DropdownMenuSlotPresenter>(slot.Child);
            item.IsChecked = !exiting; Advance(64);
            var regular = new Border { Width = 12, Height = 12 };
            item.Icon = regular; Flush(window);
            Assert.Equal(1, slot.Opacity); Assert.Equal(20, slot.Bounds.Width);
            if (exiting) Assert.Same(regular, presenter.Child);
            else Assert.IsType<Viewbox>(presenter.Child);
            Advance(800); Assert.Equal(1, slot.Opacity);
            item.ToggleType = MenuItemToggleType.None; Flush(window);
            Assert.Same(regular, presenter.Child);
            item.Icon = null; Flush(window); Assert.False(slot.IsVisible);
            item.IsChecked = false; item.ToggleType = MenuItemToggleType.CheckBox; Flush(window);
            Assert.True(slot.IsVisible); Assert.Equal(0, slot.Bounds.Width);
            item.IsChecked = true; Advance(800); Assert.Equal(20, slot.Bounds.Width);
            item.Resources[MaterialTokens.Comp.Menu.LeadingIconSize] = 28d; Flush(window); Advance(800);
            Assert.Equal(28, slot.Bounds.Width);
            flyout.Hide(); flyout.ShowAt((Control)window.Content!); Flush(window);
            Assert.Equal(28, slot.Bounds.Width); Assert.Equal(1, slot.Opacity);
            item.IsChecked = false; Advance(800);
            Assert.Equal(0, slot.Bounds.Width); Assert.Equal(0, slot.Opacity);
            var chrome = item.GetVisualDescendants().OfType<MenuItemChrome>().Single();
            Assert.Equal(Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color,
                Assert.IsAssignableFrom<ISolidColorBrush>(chrome.PaintedBrush).Color);
            item.IsChecked = true; Advance(800);
            Assert.Equal(28, slot.Bounds.Width); Assert.Equal(1, slot.Opacity);
        }
        finally { flyout.Hide(); window.Close(); }
        void Advance(int ms)
        {
            for (var t = 0; t < ms; t += 16) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
        }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeTrailingContent_CentersSmallContentAndPreservesGestureAndSubmenu(bool templateOnly)
    {
        using var clock = new MotionTestClockScope();
        var marker = new Border { Width = 8, Height = 12 };
        var item = new MenuItem { Header = "Title", InputGesture = new KeyGesture(Key.C, KeyModifiers.Control),
            Items = { new MenuItem { Header = "Child" } } };
        if (templateOnly) MenuAppearance.SetTrailingContentTemplate(item, new FuncDataTemplate<object?>((_, _) => marker));
        else MenuAppearance.SetTrailingContent(item, marker);
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            var trailing = item.GetVisualDescendants().OfType<NativeMenuRow>().Single(p => p.Name == "PART_Trailing");
            var slot = Assert.IsType<DropdownMenuSlotPresenter>(trailing.Children[0]);
            Assert.Same(marker, slot.Child); Assert.Equal(20, slot.Bounds.Width);
            Assert.Equal(6, marker.Bounds.X); Assert.Equal(new Size(8, 12), marker.Bounds.Size);
            Assert.All(trailing.Children, child => Assert.True(child.IsVisible));
            item.Resources[MaterialTokens.Comp.Menu.TrailingIconSize] = 24d; Flush(window);
            Assert.Equal(24, slot.Bounds.Width); Assert.Equal(8, marker.Bounds.X);
            MenuAppearance.SetTrailingContent(item, null); MenuAppearance.SetTrailingContentTemplate(item, null); Flush(window);
            Assert.False(slot.IsVisible); Assert.True(trailing.IsVisible);
            item.InputGesture = null; Flush(window); Assert.False(trailing.Children[1].IsVisible);
            Assert.True(trailing.IsVisible); Assert.True(trailing.Children[2].IsVisible);
            item.Items.Clear(); Flush(window); Assert.False(trailing.IsVisible);
            MenuAppearance.SetTrailingContent(item, marker); Flush(window); Assert.True(trailing.IsVisible);
            flyout.Hide(); flyout.ShowAt((Control)window.Content!); Flush(window);
            Assert.Same(marker, slot.Child);
            var clicks = 0; item.Click += (_, _) => clicks++;
            Click(window, item); Assert.Equal(1, clicks); Assert.False(flyout.IsOpen);
        }
        finally { flyout.Hide(); window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeSupportingText_PreservesBodyTypographyTemplateOnlyAndNativeActivation(bool templateOnly)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Title", ToggleType = MenuItemToggleType.CheckBox, StaysOpenOnClick = true };
        if (templateOnly) MenuAppearance.SetSupportingTextTemplate(item, new FuncDataTemplate<object?>((_, _) => new TextBlock { Text = "Description" }));
        else MenuAppearance.SetSupportingText(item, "Description");
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            var supporting = item.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>()
                .Single(p => p.Name == "PART_SupportingPresenter");
            var text = Assert.IsType<TextBlock>(supporting.Child);
            Assert.Equal("Description", text.Text); Assert.Equal(14, text.FontSize);
            Assert.Equal(new Thickness(4, 2), item.Margin);
            var clicks = 0; item.Click += (_, _) => clicks++;
            Click(window, item); Assert.Equal(1, clicks); Assert.True(item.IsChecked); Assert.True(flyout.IsOpen);
            MenuAppearance.SetSupportingText(item, null); MenuAppearance.SetSupportingTextTemplate(item, null); Flush(window);
            Assert.False(supporting.IsVisible); Assert.Equal(new Thickness(4, 0), item.Margin);
            MenuAppearance.SetSupportingText(item, "Restored"); Flush(window);
            Assert.True(supporting.IsVisible); Assert.Equal("Restored", Assert.IsType<TextBlock>(supporting.Child).Text);
            flyout.Hide(); flyout.ShowAt((Control)window.Content!); Flush(window);
            Assert.Equal("Restored", MenuAppearance.GetSupportingText(item));
        }
        finally { flyout.Hide(); window.Close(); }
    }

    [AvaloniaFact]
    public void NativeSupportingText_MarginKeepsTokenAndCallerStylePriority()
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Title" };
        var flyout = new MenuFlyout { Items = { item } }; var window = Show(flyout);
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            item.Resources[MaterialTokens.Comp.Menu.SupportingItemMargin] = new Thickness(8, 3);
            MenuAppearance.SetSupportingText(item, new Border { Width = 80, Height = 12 }); Flush(window);
            Assert.Equal(new Thickness(8, 3), item.Margin);
            item.Styles.Add(new Style(s => s.OfType<MenuItem>()) { Setters = { new Setter(Control.MarginProperty, new Thickness(1)) } });
            Flush(window); Assert.Equal(new Thickness(1), item.Margin);
            MenuAppearance.SetSupportingText(item, null); Flush(window); Assert.Equal(new Thickness(1), item.Margin);
            item.Styles.Clear(); Flush(window); Assert.Equal(new Thickness(4, 0), item.Margin);
        }
        finally { flyout.Hide(); window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, false, false)]
    [InlineData(true, false, false)]
    public void Separator_DoesNotExpandFlyoutOrSubmenuToAvailableWidth(bool baseline, bool legacySeparator, bool overlay)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = new Border { Width = 40, Height = 16 },
            Items = { new MenuItem { Header = new Border { Width = 40, Height = 16 } } } };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout, baseline);
        flyout.Popup.ShouldUseOverlayLayer = overlay;
        flyout.ShowAt((Control)window.Content!); Flush(window);
        var width = flyout.Popup.Child!.Bounds.Width;
        Assert.InRange(width, 112, 280);
        Control separator = legacySeparator ? new MenuItem { Header = "-" } : new Separator();
        flyout.Items.Add(separator); Flush(window);
        Assert.Equal(width, flyout.Popup.Child.Bounds.Width);
        Assert.True(separator.Bounds.Width > 0);
        item.IsSubMenuOpen = true; Flush(window);
        var child = (MenuItem)item.Items[0]!;
        var childWidth = child.Bounds.Width;
        item.Items.Add(legacySeparator ? new MenuItem { Header = "-" } : new Separator()); Flush(window);
        Assert.Equal(childWidth, child.Bounds.Width);
        item.IsSubMenuOpen = false;
        flyout.Popup.Child.Width = 240; Flush(window);
        var line = separator as Separator ?? separator.GetVisualDescendants().OfType<Separator>().Single();
        Assert.Equal(216, line.Bounds.Width); // 12 DIP padding on each side.
        flyout.Hide(); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false, MenuColorStyle.Standard)]
    [InlineData(true, MenuColorStyle.Standard)]
    [InlineData(false, MenuColorStyle.Vibrant)]
    public void NativeFlyout_UsesMatchingItemAndSubmenuThemes(bool baseline, MenuColorStyle colors)
    {
        using var clock = new MotionTestClockScope();
        var child = new MenuItem { Header = new Border { Width = 32, Height = 16 } };
        var icon = new Border { Width = 20, Height = 20, Background = Brushes.Red };
        var item = new MenuItem { Header = "More", Icon = icon, Items = { child } };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout, baseline);
        MenuAppearance.SetColorStyle(flyout, colors);
        flyout.ShowAt((Control)window.Content!); Flush(window);
        var presenter = Assert.IsType<MenuFlyoutPresenter>(flyout.Popup.Child);
        Assert.False(flyout.Popup.WindowManagerAddShadowHint);
        Assert.Equal(0, presenter.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_MenuSurface").ShadowElevation);
        Assert.Equal(colors, MenuAppearance.GetColorStyle(presenter));
        Assert.Equal(baseline ? new CornerRadius(4) : new CornerRadius(16), presenter.CornerRadius);
        Assert.Equal(baseline ? new Thickness(12, 0) : new Thickness(12), item.Padding);
        Assert.Equal(new Size(baseline ? 24 : 20, baseline ? 24 : 20),
            item.GetVisualDescendants().OfType<Viewbox>().Single(p => p.Name == "PART_Chevron").Bounds.Size);
        var chevron = item.GetVisualDescendants().OfType<Viewbox>().Single(p => p.Name == "PART_Chevron");
        var arrow = chevron.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
        var ink = arrow.Data!.Bounds;
        var top = arrow.TranslatePoint(ink.TopLeft, chevron)!.Value;
        var bottom = arrow.TranslatePoint(ink.BottomRight, chevron)!.Value;
        Assert.Equal(baseline ? 12 : 10, bottom.Y - top.Y, 5);
        Assert.True(top.Y > 0); // Retain the icon viewport's top/bottom whitespace.
        Assert.NotNull(Surface(item));
        Assert.Contains(icon, item.GetVisualDescendants());
        Assert.Equal(new Size(20, 20), icon.Bounds.Size);
        item.IsSubMenuOpen = true; Flush(window);
        var submenuPopup = item.GetVisualDescendants().OfType<Popup>().Single();
        Assert.False(submenuPopup.WindowManagerAddShadowHint);
        Assert.Equal(0, Assert.IsType<MaterialSurface>(submenuPopup.Child).ShadowElevation);
        Assert.NotNull(Surface(child));
        Assert.Equal(item.Padding, child.Padding);
        Assert.Equal(colors, MenuAppearance.GetColorStyle(child));
        child.ToggleType = MenuItemToggleType.Radio;
        child.IsChecked = true; Flush(window);
        var check = child.GetVisualDescendants().OfType<Viewbox>().Single(p => p.Name == "PART_Check");
        Assert.True(check.IsVisible);
        Assert.Equal(new Size(baseline ? 24 : 20, baseline ? 24 : 20), check.Bounds.Size);
        var checkPath = check.GetVisualDescendants().OfType<Avalonia.Controls.Shapes.Path>().Single();
        var checkInk = checkPath.Data!.Bounds;
        var checkStart = checkPath.TranslatePoint(checkInk.TopLeft, check)!.Value;
        var checkEnd = checkPath.TranslatePoint(checkInk.BottomRight, check)!.Value;
        Assert.Equal(17.59 * (baseline ? 1 : 20d / 24), checkEnd.X - checkStart.X, 5);
        Assert.True(checkStart.X > 0 && checkStart.Y > 0);
        child.IsChecked = false; Flush(window);
        if (baseline) Assert.False(check.IsVisible);
        else Assert.False(check.IsAttachedToVisualTree()); // Static icon replacement removes the check content.
        child.ToggleType = MenuItemToggleType.CheckBox;
        child.IsChecked = true; Flush(window);
        Assert.True(child.GetVisualDescendants().OfType<Viewbox>().Single(p => p.Name == "PART_Check").IsVisible);
        flyout.Hide(); window.Close();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void NativeFlyout_SubmenusAllowPlatformHostsAndOpenWithOverlayFallback(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var leaf = new MenuItem { Header = "Name" };
        var child = new MenuItem { Header = "Sort", Items = { leaf } };
        var parent = new MenuItem { Header = "More", Items = { child } };
        var flyout = new MenuFlyout { Items = { parent } };
        var window = Show(flyout, baseline);
        flyout.Popup.ShouldUseOverlayLayer = false;
        try
        {
            flyout.ShowAt((Control)window.Content!); Flush(window);
            // Avalonia 12.1.2 HeadlessWindowImpl.CreatePopup returns null: assert the
            // native-host policy separately from exercising its overlay fallback.
            var popup = parent.GetVisualDescendants().OfType<Popup>().Single();
            Assert.False(popup.ShouldUseOverlayLayer);
            parent.IsSubMenuOpen = true; Flush(window);
            Assert.Same(window, TopLevel.GetTopLevel(child));
            Assert.False(child.GetVisualDescendants().OfType<Popup>().Single().ShouldUseOverlayLayer);
            child.IsSubMenuOpen = true; Flush(window);
            Assert.Same(window, TopLevel.GetTopLevel(leaf));
            Assert.True(leaf.Bounds.Width > 0);
            child.IsSubMenuOpen = false;
            parent.IsSubMenuOpen = false;
            parent.IsSubMenuOpen = true; Flush(window);
            Assert.NotNull(TopLevel.GetTopLevel(child));
        }
        finally
        {
            flyout.Hide(); window.Close();
        }
    }

    [AvaloniaFact]
    public void NativeClick_ChecksBeforeCallback_AndDismisses()
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Check", ToggleType = MenuItemToggleType.CheckBox };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout);
        var clicks = 0; var observed = false;
        item.Click += (_, _) => { clicks++; observed = item.IsChecked; };
        flyout.ShowAt((Control)window.Content!); Flush(window);
        Click(window, item);
        Assert.Equal(1, clicks); Assert.True(observed); Assert.True(item.IsChecked);
        Assert.False(flyout.IsOpen);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_RadioGroup_DisabledItem_AndStayOpenRemainNative()
    {
        using var clock = new MotionTestClockScope();
        var first = new MenuItem { Header = "One", ToggleType = MenuItemToggleType.Radio, GroupName = "test", IsChecked = true, StaysOpenOnClick = true };
        var disabled = new MenuItem { Header = "Disabled", IsEnabled = false };
        var second = new MenuItem { Header = "Two", ToggleType = MenuItemToggleType.Radio, GroupName = "test", StaysOpenOnClick = true };
        var source = new MaterialInteractionSource(); var interactions = new List<MaterialInteractionEvent>();
        source.Interaction += interactions.Add;
        MenuAppearance.SetInteractionSource(second, source);
        var flyout = new MenuFlyout { Items = { first, disabled, second } };
        var window = Show(flyout); flyout.ShowAt((Control)window.Content!); Flush(window);
        var clicks = 0;
        second.Click += (_, _) => clicks++;
        first.Focus(NavigationMethod.Directional);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.True(second.IsFocused);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
        var press = Assert.Single(interactions.OfType<MaterialInteractionEvent.Press>());
        Assert.True(MaterialInteraction.GetOrCreate(second).Interaction.HasFlag(InteractionState.Pressed));
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Same(press, Assert.Single(interactions.OfType<MaterialInteractionEvent.Release>()).Start);
        Assert.False(MaterialInteraction.GetOrCreate(second).Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, clicks);
        Assert.True(second.IsChecked); Assert.False(first.IsChecked); Assert.True(flyout.IsOpen);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.False(flyout.IsOpen); window.Close();
    }

    [AvaloniaFact]
    public void ExplicitContainerTheme_IsPreserved()
    {
        using var clock = new MotionTestClockScope();
        var custom = new ControlTheme(typeof(MenuItem)) { Setters = { new Setter(MenuItem.PaddingProperty, new Thickness(7)),
            new Setter(MenuItem.TemplateProperty, new FuncControlTemplate<MenuItem>((_, _) => new Border { Width = 80, Height = 32 })) } };
        var item = new MenuItem { Header = "Custom" };
        var flyout = new MenuFlyout { Items = { item }, ItemContainerTheme = custom };
        var window = Show(flyout); flyout.ShowAt((Control)window.Content!); Flush(window);
        Assert.Equal(new Thickness(7), item.Padding);
        Assert.Empty(item.GetVisualDescendants().OfType<MaterialSurface>());
        flyout.Hide(); window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedContainers_UseSelectedPresenterTheme(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var flyout = new MenuFlyout { ItemsSource = new[] { "First", "Second" } };
        var window = Show(flyout, baseline);
        flyout.ShowAt((Control)window.Content!); Flush(window);
        var presenter = (MenuFlyoutPresenter)flyout.Popup.Child!;
        var item = Assert.IsType<MenuItem>(presenter.ContainerFromIndex(1));
        Assert.Equal("Second", item.Header);
        Assert.Equal(baseline ? new Thickness(12, 0) : new Thickness(12), item.Padding);
        Assert.NotNull(Surface(item));
        Assert.False(presenter.ClipToBounds);
        flyout.Hide(); window.Close();
    }

    [AvaloniaFact]
    public void CommandCanExecute_ControlsNativeActivation()
    {
        using var clock = new MotionTestClockScope();
        var command = new MenuCommand();
        var item = new MenuItem { Header = "Execute", Command = command, CommandParameter = "parameter" };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout); flyout.ShowAt((Control)window.Content!); Flush(window);
        Assert.False(item.IsEffectivelyEnabled);
        Click(window, item);
        Assert.Equal(0, command.Executions);
        command.Enable(); Flush(window);
        Assert.True(item.IsEffectivelyEnabled);
        Click(window, item);
        Assert.Equal(1, command.Executions);
        Assert.Equal("parameter", command.Parameter);
        Assert.False(flyout.IsOpen);
        window.Close();
    }

    private sealed class MenuCommand : System.Windows.Input.ICommand
    {
        private bool _enabled;
        public int Executions { get; private set; }
        public object? Parameter { get; private set; }
        public bool CanExecute(object? parameter) => _enabled;
        public void Execute(object? parameter) { Executions++; Parameter = parameter; }
        public event EventHandler? CanExecuteChanged;
        public void Enable() { _enabled = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
    }

    [AvaloniaFact]
    public void DynamicColorStyleAndTokens_UpdateOnReusedPresenter()
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Selected", ToggleType = MenuItemToggleType.CheckBox, IsChecked = true };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout); flyout.ShowAt((Control)window.Content!); Flush(window);
        var presenter = flyout.Popup.Child;
        MenuAppearance.SetColorStyle(flyout, MenuColorStyle.Vibrant);
        window.Resources[MaterialTokens.Comp.Menu.VibrantSelectedContainer] = Colors.Red;
        clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromSeconds(1));
        Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(Surface(item).Background).Color);
        flyout.Hide(); MenuAppearance.SetColorStyle(flyout, MenuColorStyle.Standard);
        flyout.ShowAt((Control)window.Content!); Flush(window);
        Assert.Same(presenter, flyout.Popup.Child);
        Assert.Equal(MenuColorStyle.Standard, MenuAppearance.GetColorStyle(item));
        flyout.Hide(); window.Close();
    }

    [AvaloniaFact]
    public void SelectionMorphAndTemplateReplacement_ReleaseAnimationWork()
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "Selected" };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout); flyout.ShowAt((Control)window.Content!); Flush(window);
        Assert.Equal(new CornerRadius(4), Surface(item).EffectiveCornerRadius);
        item.IsChecked = true;
        clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(48));
        Assert.InRange(Surface(item).EffectiveCornerRadius.TopLeft, 4.01, 11.99);
        clock.AdvanceBy(TimeSpan.FromSeconds(2));
        Assert.Equal(new CornerRadius(12), Surface(item).EffectiveCornerRadius);
        var template = item.Template;
        item.Template = new FuncControlTemplate<MenuItem>((_, _) => new Border()); Flush(window);
        item.Template = template; Flush(window);
        Assert.Equal(new CornerRadius(12), Surface(item).EffectiveCornerRadius);
        flyout.Hide(); window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveTaskCount);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreferredMinimumWidth_RespectsNarrowPopup(bool baseline)
    {
        using var clock = new MotionTestClockScope();
        var item = new MenuItem { Header = "A long label" };
        var flyout = new MenuFlyout { Items = { item } };
        var window = Show(flyout, baseline); flyout.ShowAt((Control)window.Content!); Flush(window);
        ((MenuFlyoutPresenter)flyout.Popup.Child!).MaxWidth = 80; Flush(window);
        Assert.InRange(item.Bounds.Width, 1, 80);
        Assert.InRange(Surface(item).Bounds.Width, 1, 80);
        flyout.Hide(); window.Close();
    }

    private static MaterialSurface Surface(MenuItem item) => item.GetVisualDescendants().OfType<MaterialSurface>().First(s => s.Name == "PART_ItemSurface");
    private static void Flush(Window window) { Avalonia.Threading.Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Avalonia.Threading.Dispatcher.UIThread.RunJobs(); }
    private static void Click(Window window, MenuItem item)
    {
        var point = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), window)!.Value;
        window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left);
    }
    private static MotionTestWindow Show(MenuFlyout flyout, bool baseline = false)
    {
        var window = new MotionTestWindow { Width = 480, Height = 400 };
        window.Template = new FuncControlTemplate<Window>((owner, scope) =>
        {
            var presenter = new Avalonia.Controls.Presenters.ContentPresenter { Name = "PART_ContentPresenter", Content = owner.Content };
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            scope.Register(presenter.Name, presenter); scope.Register(layers.Name, layers);
            return layers;
        });
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Border { Width = 100, Height = 32, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Margin = new Thickness(20), Background = Brushes.Gray };
        window.Show(); Flush(window);
        flyout.Popup.ShouldUseOverlayLayer = true;
        if (baseline) flyout.FlyoutPresenterTheme = (ControlTheme)window.FindResource("MaterialBaselineMenuTheme")!;
        return window;
    }
}


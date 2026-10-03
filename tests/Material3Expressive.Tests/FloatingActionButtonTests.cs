using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class FloatingActionButtonTests
{
    [AvaloniaFact]
    public void Menu_UsesNaturalItemWidthsAndCollapsedButtonAllocation()
    {
        using var clock = new MotionTestClockScope();
        var menu = new FloatingActionButtonMenu { Expanded = true, Button = new FloatingActionButton() };
        for (var i = 0; i < 3; i++) menu.Items.Add(new FloatingActionButtonMenuItem
        { Icon = new Border { Width = 24, Height = 24 }, Content = new Border { Width = 48 + i * 8, Height = 16 } });
        var window = Show(menu);
        var states = string.Join(";", menu.Items.OfType<FloatingActionButtonMenuItem>().Select(i => $"alpha={i.RevealAlpha} width={i.RevealWidth}"));
        var tree = states + "\n" + string.Join("\n", menu.GetVisualDescendants().OfType<Control>().Select(c => $"{c.GetType().Name} {c.Name} desired={c.DesiredSize} bounds={c.Bounds} opacity={c.Opacity}"));
        Assert.True(menu.Bounds.Size == new Size(176, 256), tree);
        menu.Expanded = false; Frames(clock, window, 70);
        Assert.Equal(new Size(88, 72), menu.Bounds.Size);
        window.Close();
    }
    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Default, 56, 56, 16)]
    [InlineData(FloatingActionButtonSize.Small, 48, 40, 12)]
    [InlineData(FloatingActionButtonSize.Medium, 80, 80, 20)]
    [InlineData(FloatingActionButtonSize.Large, 96, 96, 28)]
    public void IconDefaults_PreserveContainerAndInteractiveAllocation(FloatingActionButtonSize size, double outer, double container, double corner)
    {
        using var clock = new MotionTestClockScope();
        var fab = new FloatingActionButton { Size = size, Content = new Border { Width = 24, Height = 24 } };
        var window = Show(fab);
        Assert.Equal(new Size(outer, outer), fab.Bounds.Size);
        Assert.Equal(new Size(container, container), Surface(fab).Bounds.Size);
        Assert.Equal(new CornerRadius(corner), Surface(fab).CornerRadius);
        Assert.Equal(6, Surface(fab).TonalElevation);
        window.Close();
    }
    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Default, 120, 56)]
    [InlineData(FloatingActionButtonSize.Small, 112, 56)]
    [InlineData(FloatingActionButtonSize.Medium, 136, 80)]
    [InlineData(FloatingActionButtonSize.Large, 156, 96)]
    public void Extended_AutomaticSizingAndCollapse(FloatingActionButtonSize size, double expandedWidth, double height)
    {
        using var clock = new MotionTestClockScope();
        var fab = new ExtendedFloatingActionButton { Size = size, Content = new Border { Width = 48, Height = 16 }, Icon = new Border { Width = size == FloatingActionButtonSize.Large ? 36 : 24, Height = 24 } };
        var window = Show(fab);
        Assert.Equal(new Size(expandedWidth, height), fab.Bounds.Size);
        fab.Expanded = false; Frames(clock, window, 70);
        Assert.Equal(new Size(height, height), fab.Bounds.Size);
        fab.Expanded = true; Frames(clock, window, 70);
        Assert.Equal(new Size(expandedWidth, height), fab.Bounds.Size);
        window.Close();
    }
    [AvaloniaFact]
    public void Toggle_RequestsStateAndAutomationWithoutMutatingControlledValue()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleFloatingActionButton(); var window = Show(toggle); var requested = new List<bool>();
        toggle.CheckedChangeRequested += (_, e) => requested.Add(e.IsChecked);
        var peer = Assert.IsAssignableFrom<IToggleProvider>(ControlAutomationPeer.CreatePeerForElement(toggle));
        peer.Toggle(); Assert.False(toggle.IsChecked); Assert.Equal(new[] { true }, requested);
        toggle.IsChecked = true; Frames(clock, window, 70);
        Assert.Equal(1, toggle.CheckedProgress); Assert.Equal(ToggleState.On, peer.ToggleState);
        peer.Toggle(); Assert.True(toggle.IsChecked); Assert.Equal(new[] { true, false }, requested);
        toggle.IsEnabled = false; Assert.ThrowsAny<Exception>(() => peer.Toggle()); window.Close();
    }
    [AvaloniaFact]
    public void Visibility_HidesLayoutThenShowsAgain_AndDetachesDuringAnimation()
    {
        using var clock = new MotionTestClockScope();
        var fab = new FloatingActionButton(); var window = Show(fab);
        fab.AnimatedVisible = false; Frames(clock, window, 70);
        Assert.Equal(default, fab.Bounds.Size);
        fab.AnimatedVisible = true; Frames(clock, window, 70);
        Assert.Equal(new Size(56, 56), fab.Bounds.Size);
        fab.AnimatedVisible = false; Frames(clock, window, 4);
        window.Content = null; Frames(clock, window, 2);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Close();
    }
    [AvaloniaFact]
    public void Menu_RevealsBottomFirst_AndKeyboardMovesFromButtonToFirstItem()
    {
        using var clock = new MotionTestClockScope();
        var button = new FloatingActionButton();
        var first = new FloatingActionButtonMenuItem { Content = new Border { Width = 32, Height = 16 } };
        var last = new FloatingActionButtonMenuItem { Content = new Border { Width = 48, Height = 16 } };
        var menu = new FloatingActionButtonMenu { Button = button, Items = { first, last } };
        var window = Show(menu); Assert.False(first.IsHitTestVisible); Assert.False(last.IsHitTestVisible);
        menu.Expanded = true; Frames(clock, window, 70);
        Assert.True(first.IsHitTestVisible); Assert.True(last.IsHitTestVisible);
        button.Focus(); window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.True(first.IsFocused);
        menu.Expanded = false; Frames(clock, window, 70);
        Assert.False(first.IsHitTestVisible); Assert.False(last.IsHitTestVisible);
        window.Close();
    }
    [AvaloniaFact]
    public void ConstrainedMenu_ScrollsItemsIntoViewWhileButtonStaysFixed()
    {
        using var clock = new MotionTestClockScope();
        var button = new FloatingActionButton();
        var menu = new FloatingActionButtonMenu { Expanded = true, Button = button, Height = 180 };
        for (var i = 0; i < 5; i++) menu.Items.Add(new FloatingActionButtonMenuItem { Content = new Border { Width = 48, Height = 16 } });
        var window = Show(menu);
        var viewport = menu.GetVisualDescendants().OfType<FabMenuViewport>().Single();
        Assert.Equal(376, viewport.Extent.Height);
        Assert.Equal(180, viewport.Viewport.Height);
        var buttonPosition = button.TranslatePoint(default, window);
        var last = menu.Items.OfType<FloatingActionButtonMenuItem>().Last();
        last.BringIntoView(); window.UpdateLayout();
        Assert.True(viewport.Offset.Y > 0);
        Assert.Equal(buttonPosition, button.TranslatePoint(default, window));
        menu.Expanded = false; Frames(clock, window, 70);
        Assert.False(viewport.ScrollingEnabled);
        window.Close();
    }
    static MaterialSurface Surface(Control control) => control.GetVisualDescendants().OfType<MaterialSurface>().Single();
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DetachDuringToggleOrMenuAnimation_CancelsAllFramesAndReattaches(bool menuAnimation)
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleFloatingActionButton();
        var menu = new FloatingActionButtonMenu { Button = toggle };
        menu.Items.Add(new FloatingActionButtonMenuItem { Content = new Border { Width = 48, Height = 16 } });
        var window = Show(menu);
        toggle.IsChecked = true;
        if (menuAnimation) menu.Expanded = true;
        Frames(clock, window, 4);
        var root = window.Content;
        window.Content = null;
        Frames(clock, window, 2);
        Assert.Equal(0, clock.PendingFrameCount);
        window.Content = root;
        window.UpdateLayout();
        Frames(clock, window, 2);
        Assert.Equal(1, toggle.CheckedProgress);
        Assert.Equal(menuAnimation, menu.Items.OfType<FloatingActionButtonMenuItem>().Single().IsHitTestVisible);
        window.Close();
    }
    [AvaloniaFact]
    public void Toggle_IconBindingsUpdateSymbolSizeAndForeground()
    {
        using var clock = new MotionTestClockScope();
        var symbol = new Material3Expressive.Foundation.Rendering.MaterialSymbol
        {
            Symbol = MaterialSymbols.MaterialSymbolCodepoint.Add
        };
        var toggle = new ToggleFloatingActionButton { Content = symbol };
        symbol.Bind(Control.WidthProperty, new Avalonia.Data.Binding(nameof(toggle.IconSize)) { Source = toggle });
        symbol.Bind(Control.HeightProperty, new Avalonia.Data.Binding(nameof(toggle.IconSize)) { Source = toggle });
        symbol.Bind(Avalonia.Controls.Documents.TextElement.ForegroundProperty,
            new Avalonia.Data.Binding(nameof(toggle.IconColor))
            {
                Source = toggle, Converter = new Avalonia.Markup.Xaml.Converters.ColorToBrushConverter()
            });
        var window = Show(toggle);
        Assert.Equal(24, symbol.Width);
        Assert.Equal(toggle.IconColor, Assert.IsAssignableFrom<ISolidColorBrush>(symbol.Foreground).Color);
        toggle.IsChecked = true;
        Frames(clock, window, 70);
        Assert.Equal(20, symbol.Width);
        Assert.Equal(20, symbol.Height);
        Assert.Equal(toggle.IconColor, Assert.IsAssignableFrom<ISolidColorBrush>(symbol.Foreground).Color);
        window.Close();
    }
    [AvaloniaFact]
    public void Toggle_DefaultDimensionsConsumeComponentTokensAndPreserveExplicitFunctions()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleFloatingActionButton { Size = FloatingActionButtonSize.Medium };
        var window = Show(toggle);
        window.Resources[Material3Expressive.Tokens.MaterialTokens.Comp.FabMedium.ContainerHeight] = 88d;
        window.Resources[Material3Expressive.Tokens.MaterialTokens.Comp.FabMedium.IconSize] = 32d;
        window.UpdateLayout();
        Assert.Equal(new Size(88, 88), toggle.Bounds.Size);
        Assert.Equal(32, toggle.IconSize);
        toggle.ContainerSize = _ => 72;
        window.UpdateLayout();
        Assert.Equal(new Size(72, 72), toggle.Bounds.Size);
        window.Close();
    }
    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Toggle_OversizedContainerUsesCoercedPlacement(FlowDirection direction)
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleFloatingActionButton
        {
            IsChecked = true, FlowDirection = direction,
            ContainerSize = p => 96 + 3 * p
        };
        var window = Show(toggle);
        var container = Surface(toggle);
        Assert.Equal(new Size(96, 96), toggle.Bounds.Size);
        Assert.Equal(new Size(99, 99), container.Bounds.Size);
        // (96 - 99) / 2 truncates to -1. RTL logical coordinates compensate
        // the renderer's mirror to retain that absolute placement.
        Assert.Equal(new Point(direction == FlowDirection.RightToLeft ? -2 : -1, -1), container.Bounds.Position);
        window.Close();
    }
    [AvaloniaTheory]
    [InlineData(FlowDirection.LeftToRight)]
    [InlineData(FlowDirection.RightToLeft)]
    public void Toggle_ContentCenterUsesAbsoluteIntegerDivision(FlowDirection direction)
    {
        using var clock = new MotionTestClockScope();
        var glyph = new Border { Width = 24, Height = 24 };
        var toggle = new ToggleFloatingActionButton
        {
            IsChecked = true, FlowDirection = direction, Content = glyph,
            ContainerSize = p => 96 + (53 - 96) * p
        };
        var window = Show(toggle);
        var container = Surface(toggle);
        var transform = glyph.TransformToVisual(container)!.Value;
        var bounds = new Rect(glyph.Bounds.Size).TransformToAABB(transform);
        // TransformToVisual observes logical coordinates before the renderer's RTL
        // mirror. The RTL raster cases verify the resulting absolute 14px position.
        Assert.Equal(new Rect(direction == FlowDirection.RightToLeft ? 15 : 14, 14, 24, 24), bounds);
        window.Close();
    }
    [AvaloniaFact]
    public void Foreground_TracksPrimaryContainerRoleDuringThemeChanges()
    {
        var fab = new FloatingActionButton(); var window = Show(fab);
        var bg = Material3Expressive.Tokens.MaterialTokens.Sys.Color.PrimaryContainer;
        var fg = Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnPrimaryContainer;
        window.Resources[bg] = Color.Parse("#EADDFF"); window.Resources[fg] = Color.Parse("#21005D");
        Assert.Equal(Color.Parse("#21005D"), ((ISolidColorBrush)fab.Foreground!).Color);
        window.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
        window.Resources[bg] = Color.Parse("#4F378B"); window.Resources[fg] = Color.Parse("#EADDFF");
        window.UpdateLayout();
        Assert.Equal(Color.Parse("#4F378B"), ((ISolidColorBrush)fab.Background!).Color);
        Assert.Equal(Color.Parse("#EADDFF"), ((ISolidColorBrush)fab.Foreground!).Color);
        window.Close();
    }
    static void Frames(MotionTestClockScope clock, Window window, int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }
    static MotionTestWindow Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 360, Height = 400 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Animation;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Styles;
using Material3Expressive.Tokens;
using MaterialSymbols;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ExpanderThemeTests
{
    [AvaloniaFact]
    public void ImplicitTheme_IsAListItemHeaderWithoutContainer()
    {
        var expander = new Expander { Header = "Wi-Fi", Content = new Border { Height = 40 } };
        var (window, _) = Show(expander);
        Assert.NotNull(window.FindResource(typeof(Expander)));
        Assert.Equal(Colors.Transparent, Brush(expander.Background));
        Assert.Equal(default, expander.BorderThickness);
        Assert.Equal(new Thickness(16, 0), expander.Padding);
        Assert.False(expander.IsTabStop);
        var header = Header(expander);
        Assert.Same(window.FindResource("MaterialExpanderHeaderTheme"), header.Theme);
        Assert.True(MaterialInteraction.GetIsInteractive(header));
        Assert.Null(header.FocusAdorner);
        Assert.Equal(56, header.Bounds.Height);
        Assert.Equal(expander.Bounds.Width, header.Bounds.Width);
        Assert.Equal(56, expander.Bounds.Height);
        // The native peer stays: ExpanderAutomationPeer exposes expand/collapse.
        Assert.IsType<ExpanderAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(expander));
        window.Close();
    }

    [AvaloniaFact]
    public void Header_HasBodyLargeOnSurfaceLabelAndTrailingChevron()
    {
        var expander = new Expander { Header = "Wi-Fi", Content = new Border { Height = 40 } };
        var (window, _) = Show(expander);
        var header = Header(expander);
        var text = HeaderText(expander);
        var style = (TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.BodyLarge.Style);
        Assert.Equal(style.FontSize, text.FontSize);
        Assert.Equal(style.LineHeight, text.LineHeight);
        Assert.Equal(Color(window, MaterialTokens.Sys.Color.OnSurface), Brush(text.Foreground));
        Assert.Equal(Color(window, MaterialTokens.Sys.Color.OnSurface), Brush(Surface(expander).StateLayerBrush));
        Assert.Equal(16, text.TranslatePoint(default, header)!.Value.X);

        var chevron = Chevron(expander);
        Assert.Equal(MaterialSymbolCodepoint.KeyboardArrowDown, chevron.Symbol);
        Assert.Equal(new Size(24, 24), chevron.Bounds.Size);
        Assert.Equal(Color(window, MaterialTokens.Sys.Color.OnSurfaceVariant), Brush(chevron.Foreground));
        var left = chevron.TranslatePoint(default, header)!.Value;
        Assert.Equal(header.Bounds.Width - 16 - 24, left.X, 3);
        Assert.Equal(16, left.Y, 3);
        Assert.Equal(0, ChevronRotation.GetAngle(chevron));
        window.Close();
    }

    [AvaloniaFact]
    public void ControlHeaderAndTemplate_GetTheHeaderTypography_AndAppStylesWin()
    {
        var control = new TextBlock { Text = "Control" };
        var styled = new TextBlock { Text = "Styled" };
        MaterialText.SetStyle(styled, new TextStyle { FontSize = 30 });
        var templated = new Expander { Header = "Data", HeaderTemplate = new FuncDataTemplate<string>((s, _) => new TextBlock { Text = s }) };
        var content = new TextBlock { Text = "Content" };
        MaterialText.SetStyle(content, new TextStyle { FontSize = 11 });
        var (window, _) = Show(new Expander { Header = control, Content = content, IsExpanded = true }, new Expander { Header = styled }, templated);
        var size = ((TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.BodyLarge.Style)).FontSize;
        Assert.Equal(size, control.FontSize);
        Assert.Equal(30, styled.FontSize);
        Assert.Equal(size, HeaderText(templated).FontSize);
        Assert.Equal("Data", HeaderText(templated).Text);
        Assert.Equal(11, content.FontSize);
        window.Close();
    }

    [AvaloniaFact]
    public void Chevron_RotatesWithIsExpanded_AndContentAnimatesToItsFinalSize()
    {
        var content = new Border { Height = 80 };
        var expander = new Expander { Header = "Wi-Fi", Content = content };
        var (window, clock) = Show(expander);
        expander.IsExpanded = true; Draw(window);
        Assert.True(Header(expander).IsChecked);
        Advance(window, clock, 3);
        // Mid-flight: the chevron and the clipped size are between their destinations, the content
        // is laid out at full size and fading in.
        Assert.InRange(ChevronRotation.GetAngle(Chevron(expander)), 1, 179);
        Assert.InRange(expander.Bounds.Height, 57, 56 + 79);
        Assert.Equal(80, content.Bounds.Height);
        Settle(window, clock);
        Assert.Equal(180, ChevronRotation.GetAngle(Chevron(expander)), 3);
        Assert.Equal(180, Assert.IsType<RotateTransform>(Chevron(expander).RenderTransform).Angle, 3);
        Assert.Equal(56 + 80, expander.Bounds.Height);
        Assert.Equal(new Rect(0, 56, expander.Bounds.Width, 80), Region(expander).Bounds);
        Assert.Equal(16, content.TranslatePoint(default, expander)!.Value.X);
        Assert.Equal(expander.Bounds.Width - 32, content.Bounds.Width);
        Assert.True(Host(expander).IsIdle);

        expander.IsExpanded = false; Draw(window);
        Advance(window, clock, 3);
        Assert.InRange(expander.Bounds.Height, 57, 56 + 79);
        Assert.True(content.IsAttachedToVisualTree());
        Settle(window, clock);
        Assert.Equal(0, ChevronRotation.GetAngle(Chevron(expander)), 3);
        Assert.Equal(56, expander.Bounds.Height);
        Assert.False(content.IsAttachedToVisualTree());
        window.Close();
    }

    [AvaloniaFact]
    public void ExplicitContentTransition_StillReachesTheFinalState()
    {
        // The theme animates through AnimatedVisibility; Avalonia still runs an explicitly set
        // ContentTransition on PART_ContentPresenter, which must not break expansion or collapse.
        var content = new Border { Height = 80 };
        var expander = new Expander { Header = "Wi-Fi", Content = content, ContentTransition = new CrossFade(TimeSpan.FromMilliseconds(100)) };
        var (window, clock) = Show(expander);
        expander.IsExpanded = true; Draw(window); Settle(window, clock);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(56 + 80, expander.Bounds.Height);
        expander.IsExpanded = false; Draw(window); Settle(window, clock);
        Assert.Equal(56, expander.Bounds.Height);
        expander.IsExpanded = true; Draw(window); Settle(window, clock);
        Assert.Equal(56 + 80, expander.Bounds.Height);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    public void NonStretchExpander_SizesToMaxOfHeaderAndContent_WithoutJumping(HorizontalAlignment alignment)
    {
        // Narrow content: the Expander keeps the header's width in every state and frame, and the
        // content is stretched to that arranged width.
        var narrow = new Border { Height = 40 };
        var expander = new Expander { Header = "Wi-Fi settings", Content = narrow, HorizontalAlignment = alignment };
        // Wide content: the expanded Expander is as wide as the content, and the header follows.
        var wideContent = new Border { Width = 400, Height = 40 };
        var wide = new Expander { Header = "Wi-Fi", Content = wideContent, HorizontalAlignment = alignment };
        var panel = new WrapPanel { Width = 600, Children = { new StackPanel { Width = 600, Children = { expander, wide } } } };
        var (window, clock) = Show(panel);
        var collapsed = expander.Bounds.Width;
        Assert.InRange(collapsed, 100, 300);
        Assert.Equal(collapsed, expander.DesiredSize.Width - expander.Margin.Left - expander.Margin.Right, 3);
        expander.IsExpanded = true; wide.IsExpanded = true; Draw(window);
        for (var i = 0; i < 4; i++)
        {
            Advance(window, clock, 2);
            Assert.Equal(collapsed, expander.Bounds.Width, 3);
        }
        Settle(window, clock);
        Assert.Equal(collapsed, expander.Bounds.Width, 3);
        Assert.Equal(collapsed - 32, narrow.Bounds.Width, 3);
        Assert.Equal(400 + 32, wide.Bounds.Width, 3);
        Assert.Equal(400 + 32, Header(wide).Bounds.Width, 3);
        expander.IsExpanded = false; Draw(window);
        Advance(window, clock, 3);
        Assert.Equal(collapsed, expander.Bounds.Width, 3);
        Settle(window, clock);
        Assert.Equal(collapsed, expander.Bounds.Width, 3);
        window.Close();
    }

    [AvaloniaFact]
    public void StretchExpander_FillsItsSlot_WithNarrowContentStretched()
    {
        var narrow = new Border { Height = 40 };
        var expander = new Expander { Header = "Wi-Fi", Content = narrow, IsExpanded = true };
        var (window, clock) = Show(new StackPanel { Width = 500, Children = { expander } });
        Settle(window, clock);
        Assert.Equal(500, expander.Bounds.Width);
        Assert.Equal(500 - 32, narrow.Bounds.Width, 3);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(ExpandDirection.Left)]
    [InlineData(ExpandDirection.Right)]
    public void SideDirections_StretchOnTheHeightAxisOnly(ExpandDirection direction)
    {
        // Top-aligned in a tall slot: max(header, content) tall, content stretched to that height.
        var shortContent = new Border { Width = 100 };
        var top = new Expander { Header = "Side", Content = shortContent, ExpandDirection = direction, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        var tallContent = new Border { Width = 100, Height = 120 };
        var tall = new Expander { Header = "Side", Content = tallContent, ExpandDirection = direction, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        // Stretch in a 300 DIP slot: fills it.
        var fillContent = new Border { Width = 100 };
        var fill = new Expander { Header = "Side", Content = fillContent, ExpandDirection = direction, HorizontalAlignment = HorizontalAlignment.Left };
        var (window, clock) = Show(new Grid { Height = 300, Children = { top } }, new Grid { Height = 300, Children = { tall } }, new Grid { Height = 300, Children = { fill } });
        Assert.Equal(56, top.Bounds.Height);
        top.IsExpanded = tall.IsExpanded = fill.IsExpanded = true; Draw(window);
        Advance(window, clock, 3);
        Assert.Equal(56, top.Bounds.Height);
        Settle(window, clock);
        Assert.Equal(56, top.Bounds.Height);
        Assert.Equal(56, shortContent.Bounds.Height, 3);
        Assert.Equal(120, tall.Bounds.Height);
        Assert.Equal(120, Header(tall).Bounds.Height);
        Assert.Equal(300, fill.Bounds.Height);
        Assert.Equal(300, fillContent.Bounds.Height, 3);
        Assert.Equal(100, shortContent.Bounds.Width);
        window.Close();
    }

    [AvaloniaFact]
    public void CollapsedContent_TakesNoSpace_AndIsSkippedByTab()
    {
        var inner = new Button { Content = "Inner" };
        var expander = new Expander { Header = "Wi-Fi", Content = inner };
        var after = new Button { Content = "After" };
        var (window, clock) = Show(expander, after);
        Assert.Equal(56, expander.Bounds.Height);
        Assert.False(inner.IsAttachedToVisualTree());
        Header(expander).Focus(NavigationMethod.Tab); Draw(window);
        Tab(window);
        Assert.True(after.IsFocused);

        expander.IsExpanded = true; Draw(window); Settle(window, clock);
        Header(expander).Focus(NavigationMethod.Tab); Draw(window);
        Tab(window);
        Assert.True(inner.IsFocused);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_SpaceAndEnterToggle()
    {
        var expander = new Expander { Header = "Wi-Fi", Content = new Border { Height = 40 } };
        var (window, clock) = Show(expander);
        var header = Header(expander);
        header.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Contains(":focus-visible", header.Classes);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Draw(window);
        Assert.True(expander.IsExpanded);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Draw(window);
        Assert.False(expander.IsExpanded);
        Settle(window, clock);
        window.Close();
    }

    [AvaloniaFact]
    public void PointerClick_TogglesThroughTheHeader()
    {
        var expander = new Expander { Header = "Wi-Fi", Content = new Border { Height = 40 } };
        var (window, _) = Show(expander);
        var center = Header(expander).TranslatePoint(new Point(100, 28), window)!.Value;
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
        Draw(window);
        Assert.True(expander.IsExpanded);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(ExpandDirection.Down, 0, 180)]
    [InlineData(ExpandDirection.Up, 180, 0)]
    [InlineData(ExpandDirection.Left, 90, -90)]
    [InlineData(ExpandDirection.Right, -90, 90)]
    public void Directions_PlaceTheHeaderAndRotateTheChevron(ExpandDirection direction, double collapsed, double expanded)
    {
        var horizontal = direction is ExpandDirection.Left or ExpandDirection.Right;
        var content = new Border { Width = 120, Height = 80 };
        var expander = new Expander { Header = "Side", Content = content, ExpandDirection = direction };
        if (horizontal) expander.HorizontalAlignment = HorizontalAlignment.Left;
        var (window, clock) = Show(expander);
        Assert.Equal(collapsed, ChevronRotation.GetAngle(Chevron(expander)), 3);
        var collapsedSize = expander.Bounds.Size;
        expander.IsExpanded = true; Draw(window); Settle(window, clock);
        Assert.Equal(expanded, ChevronRotation.GetAngle(Chevron(expander)), 3);
        var header = Header(expander).Bounds;
        var host = Region(expander).Bounds;
        switch (direction)
        {
            case ExpandDirection.Down: Assert.Equal(0, header.Y); Assert.Equal(header.Bottom, host.Y); break;
            case ExpandDirection.Up: Assert.Equal(0, host.Y); Assert.Equal(host.Bottom, header.Y); break;
            case ExpandDirection.Left: Assert.Equal(0, host.X); Assert.Equal(host.Right, header.X); break;
            case ExpandDirection.Right: Assert.Equal(0, header.X); Assert.Equal(header.Right, host.X); break;
        }
        if (horizontal)
        {
            // Width grows by the content (plus its 16 + 16 padding); the header fills the height.
            Assert.Equal(collapsedSize.Width + 120 + 32, expander.Bounds.Width, 3);
            Assert.Equal(expander.Bounds.Height, header.Height);
        }
        else
        {
            Assert.Equal(collapsedSize.Height + 80, expander.Bounds.Height, 3);
        }
        expander.IsExpanded = false; Draw(window); Settle(window, clock);
        Assert.Equal(collapsedSize, expander.Bounds.Size);
        Assert.Equal(collapsed, ChevronRotation.GetAngle(Chevron(expander)), 3);
        window.Close();
    }

    [AvaloniaFact]
    public void Rtl_PutsTheChevronOnTheTrailingSide_AndMirrorsLeftRight()
    {
        var rtl = new Expander { Header = "Wi-Fi", Content = new Border { Height = 40 }, FlowDirection = FlowDirection.RightToLeft };
        var side = new Expander
        {
            Header = "Side", Content = new Border { Width = 120, Height = 40 }, ExpandDirection = ExpandDirection.Left,
            FlowDirection = FlowDirection.RightToLeft, HorizontalAlignment = HorizontalAlignment.Right, IsExpanded = true
        };
        var (window, clock) = Show(rtl, side);
        Settle(window, clock);
        var header = Header(rtl);
        double X(Visual v, Visual relativeTo) => Math.Min(v.TranslatePoint(default, relativeTo)!.Value.X, v.TranslatePoint(new Point(v.Bounds.Width, 0), relativeTo)!.Value.X);
        var origin = X(header, window);
        Assert.Equal(16, X(Chevron(rtl), window) - origin, 3);
        Assert.Equal(header.Bounds.Width - 16 - HeaderText(rtl).Bounds.Width, X(HeaderText(rtl), window) - origin, 3);
        // ExpandDirection.Left follows FlowDirection, as DockPanel does in Fluent: in RTL the header
        // sits on the physical left and the content opens to its right.
        Assert.True(X(Header(side), window) < X(Host(side), window));
        window.Close();
    }

    [AvaloniaFact]
    public void Disabled_UsesListDisabledColors_AndDoesNotToggle()
    {
        var expander = new Expander { Header = "Wi-Fi", Content = new Border { Height = 40 }, IsEnabled = false };
        var (window, _) = Show(expander);
        var header = Header(expander);
        Assert.False(header.IsEffectivelyEnabled);
        var onSurface = Color(window, MaterialTokens.Sys.Color.OnSurface);
        var opacity = (double)TokenScan.Resolve(window, MaterialTokens.Sys.State.DisabledContentOpacity);
        var disabled = Avalonia.Media.Color.FromArgb((byte)Math.Round(opacity * 255, MidpointRounding.AwayFromZero), onSurface.R, onSurface.G, onSurface.B);
        Assert.Equal(disabled, Brush(HeaderText(expander).Foreground));
        Assert.Equal(disabled, Brush(Chevron(expander).Foreground));
        var center = header.TranslatePoint(new Point(100, 28), window)!.Value;
        window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
        header.Focus(); window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Draw(window);
        Assert.False(expander.IsExpanded);
        window.Close();
    }

    [AvaloniaFact]
    public void DarkMode_FollowsTheColorScheme()
    {
        var expander = new Expander { Header = "Wi-Fi" };
        var (window, _) = Show(expander);
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        Assert.True(TokenResolver.TryResolve(window, MaterialTokens.Sys.Color.OnSurface, ThemeVariant.Dark, out var onSurface));
        Assert.True(TokenResolver.TryResolve(window, MaterialTokens.Sys.Color.OnSurfaceVariant, ThemeVariant.Dark, out var onSurfaceVariant));
        Assert.Equal((Color)onSurface!, Brush(HeaderText(expander).Foreground));
        Assert.Equal((Color)onSurfaceVariant!, Brush(Chevron(expander).Foreground));
        Assert.NotEqual(Color(window, MaterialTokens.Sys.Color.OnSurface), (Color)onSurface!);
        window.Close();
    }

    static void Tab(Window window)
    {
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        Draw(window);
    }

    static MaterialSurface Surface(Expander expander) =>
        Header(expander).GetTemplateDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");

    static ToggleButton Header(Expander expander) =>
        expander.GetTemplateDescendants().OfType<ToggleButton>().Single(t => t.Name == "ExpanderHeader");

    static AnimatedVisibility Host(Expander expander) =>
        expander.GetTemplateDescendants().OfType<AnimatedVisibility>().Single(t => t.Name == "PART_ContentHost");

    static Control Region(Expander expander) =>
        expander.GetTemplateDescendants().OfType<Control>().Single(t => t.Name == "PART_ContentStretch");

    static MaterialSymbol Chevron(Expander expander) =>
        Header(expander).GetTemplateDescendants().OfType<MaterialSymbol>().Single(s => s.Name == "PART_Chevron");

    static TextBlock HeaderText(Expander expander) =>
        Header(expander).GetTemplateDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_ContentPresenter")
            .GetVisualDescendants().OfType<TextBlock>().First();

    static Color Color(IResourceHost host, MaterialTokenKey key) => (Color)TokenScan.Resolve(host, key);

    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    static (AnimationTestWindow, ManualAnimationFrameClock) Show(params Control[] controls) => Show(Orientation.Vertical, controls);

    static (AnimationTestWindow, ManualAnimationFrameClock) Show(Orientation orientation, params Control[] controls)
    {
        var clock = new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock) { Width = 640, Height = 480 };
        window.Styles.Add(new MaterialTheme());
        var panel = new StackPanel { Orientation = orientation, Margin = new Thickness(16) };
        foreach (var control in controls) panel.Children.Add(control);
        window.Content = panel;
        window.Show();
        Draw(window); return (window, clock);
    }

    static void Advance(Window window, ManualAnimationFrameClock clock, int frames)
    {
        for (var i = 0; i < frames; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Draw(window);
        }
    }

    static void Settle(Window window, ManualAnimationFrameClock clock) => Advance(window, clock, 120);

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}

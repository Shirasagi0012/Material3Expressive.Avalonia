using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
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
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Styles;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class GroupBoxThemeTests
{
    [AvaloniaFact]
    public void ImplicitTheme_IsAnOutlinedCardSurface()
    {
        var group = new GroupBox { Header = "Network", Content = new TextBlock { Text = "Body" } };
        var card = new OutlinedCard { Content = new TextBlock { Text = "Body" } };
        var window = Show(group, card);
        Assert.NotNull(window.FindResource(typeof(GroupBox)));
        var surface = Container(group);
        Assert.Equal(Brush(card.Background), Brush(group.Background));
        Assert.Equal(Brush(card.BorderBrush), Brush(group.BorderBrush));
        Assert.Equal(card.BorderThickness, group.BorderThickness);
        Assert.Equal(card.CornerRadius, group.CornerRadius);
        Assert.Equal(Brush(card.Foreground), Brush(group.Foreground));
        Assert.Equal(Color(window, MaterialTokens.Comp.OutlinedCard.ContainerColor), Brush(surface.Background));
        Assert.Equal(Color(window, MaterialTokens.Comp.OutlinedCard.OutlineColor), Brush(surface.BorderBrush));
        Assert.Equal(new Thickness((double)TokenScan.Resolve(window, MaterialTokens.Comp.OutlinedCard.OutlineWidth)), surface.BorderThickness);
        Assert.Equal((CornerRadius)TokenScan.Resolve(window, MaterialTokens.Comp.OutlinedCard.ContainerShape), surface.CornerRadius);
        Assert.Equal(Color(window, MaterialTokens.Sys.Color.OnSurface), Brush(group.Foreground));
        Assert.Equal(Brush(group.Foreground), Brush(((TextBlock)group.Content!).Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void IsNotInteractive_AndContentFocusStaysNative()
    {
        var inner = new Button { Content = "Inside" };
        var group = new GroupBox { Header = "Group", Content = inner };
        var before = new Button { Content = "Before" };
        var window = Show(before, group);
        Assert.False(group.Focusable);
        Assert.False(MaterialInteraction.GetIsInteractive(group));
        Assert.Null(Container(group).StateLayerBrush);
        before.Focus(NavigationMethod.Tab); Draw(window);
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        Draw(window);
        Assert.True(inner.IsFocused);
        // Avalonia has no GroupBox peer: it stays the native NoneAutomationPeer, no invented name.
        var peer = ControlAutomationPeer.CreatePeerForElement(group);
        Assert.IsType<NoneAutomationPeer>(peer);
        window.Close();
    }

    [AvaloniaFact]
    public void Header_IsTitleSmallInOnSurfaceVariantAtTheTop()
    {
        var group = new GroupBox { Header = "Network", Content = new Border { Height = 40 } };
        var window = Show(group);
        var header = HeaderText(group);
        var style = (TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.TitleSmall.Style);
        Assert.Equal(style.FontSize, header.FontSize);
        Assert.Equal(style.FontWeight, header.FontWeight);
        Assert.Equal(Color(window, MaterialTokens.Sys.Color.OnSurfaceVariant), Brush(header.Foreground));
        var headerTop = header.TranslatePoint(default, group)!.Value;
        var content = (Control)group.Content!;
        var contentTop = content.TranslatePoint(default, group)!.Value;
        Assert.Equal(16, headerTop.X, 1);
        Assert.Equal(16, Presenter(group, "PART_HeaderPresenter").TranslatePoint(default, group)!.Value.Y, 1);
        // 8 DIP between the header row and the content.
        var headerBottom = Presenter(group, "PART_HeaderPresenter").TranslatePoint(new Point(0, Presenter(group, "PART_HeaderPresenter").Bounds.Height), group)!.Value.Y;
        Assert.Equal(headerBottom + 8, contentTop.Y, 1);
        Assert.Equal(16, group.Bounds.Height - (contentTop.Y + content.Bounds.Height), 1);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderTypography_DoesNotLeakIntoContent_AndAppStylesWin()
    {
        var body = new TextBlock { Text = "Body" };
        var custom = new TextBlock { Text = "Own style" };
        var bodyLarge = new TextStyle { FontSize = 22, FontWeight = FontWeight.Light };
        MaterialText.SetStyle(custom, bodyLarge);
        var group = new GroupBox { Header = "Title", Content = body };
        var own = new GroupBox { Header = custom, Content = new Border() };
        var window = Show(group, own);
        var titleSmall = (TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.TitleSmall.Style);
        var bodyStyle = (TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.BodyLarge.Style);
        Assert.Equal(bodyStyle.FontSize, body.FontSize);
        Assert.NotEqual(titleSmall.FontWeight, body.FontWeight);
        Assert.Equal(22, custom.FontSize);
        Assert.Equal(FontWeight.Light, custom.FontWeight);
        own.Header = null; Draw(window);
        Assert.Same(bodyLarge, MaterialText.GetStyle(custom));
        window.Close();
    }

    [AvaloniaFact]
    public void NullOrEmptyHeader_ReservesNoSpace()
    {
        var none = new GroupBox { Content = new Border { Height = 40, Width = 100 } };
        var empty = new GroupBox { Header = "", Content = new Border { Height = 40, Width = 100 } };
        var titled = new GroupBox { Header = "Title", Content = new Border { Height = 40, Width = 100 } };
        var window = Show(none, empty, titled);
        foreach (var g in new[] { none, empty })
        {
            Assert.False(Presenter(g, "PART_HeaderPresenter").IsVisible);
            Assert.Equal(16 + 40 + 16, g.Bounds.Height, 1);
            Assert.Equal(16, ((Control)g.Content!).TranslatePoint(default, g)!.Value.Y, 1);
        }
        Assert.True(titled.Bounds.Height > none.Bounds.Height + 8);
        titled.Header = null; Draw(window);
        Assert.Equal(none.Bounds.Height, titled.Bounds.Height, 1);
        window.Close();
    }

    [AvaloniaFact]
    public void HeaderTemplate_IsUsed()
    {
        var group = new GroupBox
        {
            Header = "Templated",
            HeaderTemplate = new FuncDataTemplate<string>((value, _) => new TextBlock { Text = value.ToUpperInvariant(), Name = "Custom" }),
            Content = new Border { Height = 20 },
        };
        var window = Show(group);
        var text = HeaderText(group);
        Assert.Equal("Custom", text.Name);
        Assert.Equal("TEMPLATED", text.Text);
        var titleSmall = (TextStyle)TokenScan.Resolve(window, MaterialTokens.Sys.TypeScale.TitleSmall.Style);
        Assert.Equal(titleSmall.FontSize, text.FontSize);
        Assert.Equal(titleSmall.FontWeight, text.FontWeight);
        Assert.Equal(Color(window, MaterialTokens.Sys.Color.OnSurfaceVariant), Brush(text.Foreground));
        window.Close();
    }

    [AvaloniaFact]
    public void PaddingAlignmentBorderAndCorner_AreOverridable()
    {
        var content = new Border { Width = 40, Height = 20 };
        var group = new GroupBox
        {
            Header = "Custom", Content = content, Width = 300, Padding = new Thickness(4, 6, 8, 10),
            HorizontalContentAlignment = HorizontalAlignment.Right, BorderThickness = new Thickness(3), CornerRadius = new CornerRadius(2),
        };
        var window = Show(group);
        Assert.Equal(new Thickness(3), Container(group).BorderThickness);
        Assert.Equal(new CornerRadius(2), Container(group).CornerRadius);
        var header = Presenter(group, "PART_HeaderPresenter").TranslatePoint(default, group)!.Value;
        Assert.Equal(new Point(4, 6), header);
        var contentPos = content.TranslatePoint(default, group)!.Value;
        Assert.Equal(300 - 8 - 40, contentPos.X, 1);
        Assert.Equal(10, group.Bounds.Height - (contentPos.Y + content.Bounds.Height), 1);
        window.Close();
    }

    [AvaloniaFact]
    public void Rtl_PutsTheHeaderOnTheStartSide()
    {
        var ltr = new GroupBox { Header = "Title", Content = new Border { Height = 20 }, Width = 300 };
        var rtl = new GroupBox { Header = "Title", Content = new Border { Height = 20 }, Width = 300, FlowDirection = FlowDirection.RightToLeft };
        var window = Show(ltr, rtl);
        double Left(Visual v, Visual owner) => v.TranslatePoint(default, window)!.Value.X
            - Math.Min(owner.TranslatePoint(default, window)!.Value.X, owner.TranslatePoint(new Point(owner.Bounds.Width, 0), window)!.Value.X);
        double Right(Visual v, Visual owner) => Math.Max(v.TranslatePoint(default, window)!.Value.X, v.TranslatePoint(new Point(v.Bounds.Width, 0), window)!.Value.X)
            - Math.Min(owner.TranslatePoint(default, window)!.Value.X, owner.TranslatePoint(new Point(owner.Bounds.Width, 0), window)!.Value.X);
        var ltrText = HeaderText(ltr); var rtlText = HeaderText(rtl);
        Assert.Equal(16, Math.Min(Left(ltrText, ltr), Right(ltrText, ltr)), 1);
        Assert.Equal(300 - 16, Math.Max(Left(rtlText, rtl), Right(rtlText, rtl)), 1);
        window.Close();
    }

    [AvaloniaFact]
    public void DarkMode_AndDisabled_FollowTheOutlinedCardContainer()
    {
        var group = new GroupBox { Header = "Title", Content = new TextBlock { Text = "Body" } };
        var card = new OutlinedCard { Content = new TextBlock { Text = "Body" } };
        var window = Show(group, card);
        var light = Brush(group.Background);
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        Assert.NotEqual(light, Brush(group.Background));
        Assert.Equal(Brush(card.Background), Brush(group.Background));
        Assert.Equal(Brush(card.BorderBrush), Brush(group.BorderBrush));
        Assert.Equal(Brush(card.Foreground), Brush(group.Foreground));
        Assert.True(TokenResolver.TryResolve(window, MaterialTokens.Sys.Color.OnSurfaceVariant, ThemeVariant.Dark, out var dark));
        Assert.Equal((Color)dark!, Brush(HeaderText(group).Foreground));
        // Like a non-clickable Card, disabling keeps the container colors; the content disables natively.
        group.IsEnabled = false; card.IsEnabled = false; Draw(window);
        Assert.Equal(Brush(card.Background), Brush(group.Background));
        Assert.Equal(Brush(card.BorderBrush), Brush(group.BorderBrush));
        Assert.False(((Control)group.Content!).IsEffectivelyEnabled);
        window.Close();
    }

    static ContentPresenter Presenter(GroupBox group, string name) =>
        group.GetTemplateDescendants().OfType<ContentPresenter>().Single(p => p.Name == name);

    static TextBlock HeaderText(GroupBox group) =>
        Presenter(group, "PART_HeaderPresenter").GetVisualDescendants().OfType<TextBlock>().First();

    static MaterialSurface Container(GroupBox group) =>
        group.GetTemplateDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");

    static Color Color(IResourceHost host, MaterialTokenKey key) => (Color)TokenScan.Resolve(host, key);

    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    static AnimationTestWindow Show(params Control[] controls)
    {
        var window = new AnimationTestWindow(new ManualAnimationFrameClock(Dispatcher.UIThread.VerifyAccess)) { Width = 640, Height = 600 };
        window.Styles.Add(new MaterialTheme());
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16 };
        foreach (var control in controls) panel.Children.Add(control);
        window.Content = panel;
        window.Show();
        Draw(window); return window;
    }

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}

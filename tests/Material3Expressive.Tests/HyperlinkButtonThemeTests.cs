using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Documents;
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
using Material3Expressive.Styles;
using MaterialSymbols;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class HyperlinkButtonThemeTests
{
    [AvaloniaFact]
    public void ImplicitTheme_IsTheTextButtonWithAnUnderlinedLabel()
    {
        var link = new HyperlinkButton { Content = "Learn more" };
        var reference = TextButton("Learn more");
        var window = Show(link, reference);
        Assert.Same(window.FindResource("MaterialTextButtonTheme"), ((ControlTheme)window.FindResource(typeof(HyperlinkButton))!).BasedOn);
        Assert.True(MaterialInteraction.GetIsInteractive(link));
        AssertLooksLike(reference, link);
        Assert.True(IsUnderlined(Label(link)));
        Assert.False(IsUnderlined(Label(reference)));
        window.Close();
    }

    [AvaloniaFact]
    public void Underline_ReachesTheLabelTextLayout()
    {
        // The headless test font draws no decoration pixels (a plain underlined TextBlock draws
        // none either), so check the shaped runs the renderer receives instead.
        var link = new HyperlinkButton { Content = "Underlined link" };
        var reference = TextButton("Underlined link");
        var window = Show(link, reference);
        static bool RunsUnderlined(TextBlock label) => label.TextLayout.TextLines.SelectMany(line => line.TextRuns)
            .OfType<Avalonia.Media.TextFormatting.ShapedTextRun>()
            .All(run => run.Properties.TextDecorations?.Any(d => d.Location == TextDecorationLocation.Underline) == true);
        Assert.True(RunsUnderlined(Label(link)));
        Assert.False(RunsUnderlined(Label(reference)));
        // Typography is the Text button's label style, unchanged by the decoration.
        Assert.Equal(Label(reference).FontSize, Label(link).FontSize);
        Assert.Equal(Label(reference).FontWeight, Label(link).FontWeight);
        Assert.Equal(Label(reference).Bounds.Size, Label(link).Bounds.Size);
        window.Close();
    }

    [AvaloniaFact]
    public void CustomLabelContent_IsUnderlinedButTheIconIsNot()
    {
        var text = new TextBlock { Text = "Custom" };
        var icon = new MaterialSymbol { Symbol = MaterialSymbolCodepoint.OpenInNew };
        var link = new HyperlinkButton { Content = text };
        MaterialButton.SetIcon(link, icon);
        var reference = TextButton("Custom");
        MaterialButton.SetIcon(reference, new MaterialSymbol { Symbol = MaterialSymbolCodepoint.OpenInNew });
        reference.Content = new TextBlock { Text = "Custom" };
        var window = Show(link, reference);
        Assert.True(IsUnderlined(text));
        Assert.False(IsUnderlined(icon));
        Assert.True(icon.IsEffectivelyVisible);
        Assert.Equal(reference.Bounds.Size, link.Bounds.Size);
        Assert.Equal(((Control)MaterialButton.GetIcon(reference)!).Bounds, icon.Bounds);
        Assert.Equal(Brush(reference.Foreground), Brush(icon.GetValue(TextElement.ForegroundProperty)));
        window.Close();
    }

    [AvaloniaFact]
    public void Visited_TogglesThePseudoClassAndKeepsTheLook()
    {
        var link = new HyperlinkButton { Content = "Visited" };
        var reference = TextButton("Visited");
        var window = Show(link, reference);
        link.IsVisited = true; Draw(window);
        Assert.Contains(":visited", link.Classes);
        AssertLooksLike(reference, link);
        Assert.True(IsUnderlined(Label(link)));
        link.IsVisited = false; Draw(window);
        Assert.DoesNotContain(":visited", link.Classes);
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledDarkAndRtl_FollowTheTextButton()
    {
        var link = new HyperlinkButton { Content = "Link", FlowDirection = FlowDirection.RightToLeft };
        var reference = TextButton("Link");
        reference.FlowDirection = FlowDirection.RightToLeft;
        MaterialButton.SetIcon(link, new Border { Width = 18, Height = 18 });
        MaterialButton.SetIcon(reference, new Border { Width = 18, Height = 18 });
        var window = Show(link, reference);
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        AssertLooksLike(reference, link);
        var icon = (Control)MaterialButton.GetIcon(link)!;
        Assert.Equal(((Control)MaterialButton.GetIcon(reference)!).TranslatePoint(new Point(4, 4), reference), icon.TranslatePoint(new Point(4, 4), link));
        link.IsEnabled = false; reference.IsEnabled = false; Draw(window);
        AssertLooksLike(reference, link);
        Assert.True(IsUnderlined(Label(link)));
        window.Close();
    }

    [AvaloniaFact]
    public void FocusVisibleAndPress_UseTheTextButtonStateLayer()
    {
        var link = new HyperlinkButton { Content = "Link" };
        var reference = TextButton("Link");
        var window = Show(link, reference);
        link.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Contains(":focus-visible", link.Classes);
        var focused = Brush(Container(link).StateLayerBrush);
        reference.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Equal(Brush(Container(reference).StateLayerBrush), focused);
        window.Close();
    }

    [AvaloniaFact]
    public void Keyboard_EnterAndSpaceClickAndAutomationStaysNative()
    {
        // NavigateUri stays null so nothing is launched.
        var link = new HyperlinkButton { Content = "Link" };
        var window = Show(link);
        var clicks = 0;
        link.Click += (_, _) => clicks++;
        link.Focus(NavigationMethod.Tab); Draw(window);
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.Equal(1, clicks);
        Assert.True(MaterialInteraction.GetOrCreate(link).Interaction.HasFlag(InteractionState.Pressed));
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        Assert.False(MaterialInteraction.GetOrCreate(link).Interaction.HasFlag(InteractionState.Pressed));
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Assert.Equal(1, clicks);
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Assert.Equal(2, clicks);
        Assert.False(link.IsVisited);
        Assert.IsType<ButtonAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(link));
        Assert.Equal("Link", ControlAutomationPeer.CreatePeerForElement(link).GetName());
        window.Close();
    }

    static Button TextButton(string content) => new() { Content = content, Theme = null, Tag = "MaterialTextButtonTheme" };

    static TextBlock Label(Button button) => button.GetTemplateDescendants().OfType<ContentPresenter>()
        .Single(p => p.Name == "PART_ContentPresenter").GetVisualDescendants().OfType<TextBlock>().First();

    static bool IsUnderlined(AvaloniaObject element) => element.GetValue(TextBlock.TextDecorationsProperty)?.Any(d => d.Location == TextDecorationLocation.Underline) == true;

    static MaterialSurface Container(Button button) =>
        button.GetTemplateDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_Container");

    static void AssertLooksLike(Button expected, Button actual)
    {
        Assert.Equal(expected.Bounds.Size, actual.Bounds.Size);
        Assert.Equal(expected.CornerRadius, actual.CornerRadius);
        Assert.Equal(expected.Padding, actual.Padding);
        Assert.Equal(expected.BorderThickness, actual.BorderThickness);
        Assert.Equal(Brush(expected.Background), Brush(actual.Background));
        Assert.Equal(Brush(expected.Foreground), Brush(actual.Foreground));
        Assert.Equal(Container(expected).Bounds, Container(actual).Bounds);
        Assert.Equal(Brush(Container(expected).StateLayerBrush), Brush(Container(actual).StateLayerBrush));
        Assert.Null(MaterialButton.GetElevation(actual));
    }

    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;
    static AnimationTestWindow Show(Control control, Control? sibling = null)
    {
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock) { Width = 480, Height = 320 };
        window.Styles.Add(new MaterialTheme());
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16, Children = { control } };
        if (sibling is not null) panel.Children.Add(sibling);
        window.Content = panel;
        window.Show();
        foreach (var button in panel.Children.OfType<Button>().Where(b => b.Tag is string))
            button.Theme = (ControlTheme)window.FindResource((string)button.Tag!)!;
        Draw(window); return window;
    }

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}

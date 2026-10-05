using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Typography;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarConfigurationTests
{
    [AvaloniaFact]
    public void CenterAlignment_ThemeTokensStylesAndLocalValues_PreserveFamilyStyleKey()
    {
        var bar = new CenterAlignedTopAppBar();
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            Assert.Equal(typeof(TopAppBar), bar.StyleKey);
            Assert.Equal(Avalonia.Layout.HorizontalAlignment.Center, bar.TitleHorizontalAlignment);
            bar.Resources[Comp.AppBar.CenterTitleAlignment] = Avalonia.Layout.HorizontalAlignment.Right;
            Flush(window); Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, bar.TitleHorizontalAlignment);
            var style = new Style { Setters = { new Setter(TopAppBar.TitleHorizontalAlignmentProperty, Avalonia.Layout.HorizontalAlignment.Left) } };
            bar.Styles.Add(style); bar.ApplyStyling(); Flush(window); Assert.Equal(Avalonia.Layout.HorizontalAlignment.Left, bar.TitleHorizontalAlignment);
            bar.TitleHorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center; Flush(window);
            Assert.Equal(Avalonia.Layout.HorizontalAlignment.Center, bar.TitleHorizontalAlignment);
            bar.ClearValue(TopAppBar.TitleHorizontalAlignmentProperty); Flush(window); Assert.Equal(Avalonia.Layout.HorizontalAlignment.Left, bar.TitleHorizontalAlignment);
            bar.Styles.Remove(style); Flush(window); Assert.Equal(Avalonia.Layout.HorizontalAlignment.Right, bar.TitleHorizontalAlignment);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("medium")] [InlineData("large")] [InlineData("largeFlexible")]
    public void Typography_ThemeSelectsVariantAndKeepsResourcesLive(string variant)
    {
        TopAppBar bar = variant switch { "medium" => new MediumTopAppBar(), "large" => new LargeTopAppBar(), _ => new LargeFlexibleTopAppBar() };
        var key = variant switch { "medium" => Comp.AppBar.MediumTitleStyle, "large" => Comp.AppBar.LargeTitleStyle, _ => Comp.AppBar.LargeFlexibleTitleStyle };
        bar.Title = "title";
        var expected = new TextStyle { FontSize = 30 };
        bar.Resources[key] = expected;
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            var title = bar.GetVisualDescendants().OfType<AppBarTitle>().Last();
            Assert.Equal(expected, title.TitleStyle); Assert.Equal(30, title.GetVisualDescendants().OfType<TextBlock>().Single().FontSize);
            expected = new TextStyle { FontSize = 33 }; bar.Resources[key] = expected; Flush(window);
            Assert.Equal(expected, title.TitleStyle); Assert.Equal(33, title.GetVisualDescendants().OfType<TextBlock>().Single().FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("medium", 112, 136)]
    [InlineData("large", 120, 152)]
    [InlineData("twoRows", 112, 136)]
    public void FlexibleHeight_FollowsContentTokens_AndRestoresAfterLocalValues(string variant, double plain, double subtitle)
    {
        TopAppBar bar = variant switch { "large" => new LargeFlexibleTopAppBar(), "twoRows" => new TwoRowsTopAppBar(), _ => new MediumFlexibleTopAppBar() };
        var plainKey = variant == "large" ? Comp.AppBar.LargeFlexibleExpandedHeight : Comp.AppBar.MediumFlexibleExpandedHeight;
        var subtitleKey = variant == "large" ? Comp.AppBar.LargeFlexibleWithSubtitleExpandedHeight : Comp.AppBar.MediumFlexibleWithSubtitleExpandedHeight;
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        void Check(double expected) { Flush(window); Assert.Equal(expected, bar.ExpandedHeight); Assert.Equal(expected, bar.Bounds.Height); }
        try
        {
            Check(plain);
            bar.Resources[plainKey] = 148d; bar.Resources[subtitleKey] = 180d; Check(148);
            bar.Subtitle = "account"; Check(180);
            bar.Subtitle = null; Check(148);
            bar.SubtitleTemplate = new FuncDataTemplate<object>((_, _) => new Border { Height = 12 }); Check(180);
            bar.ExpandedHeight = 200; Check(200);
            bar.SubtitleTemplate = null; Check(200);
            bar.ClearValue(TopAppBar.ExpandedHeightProperty); Check(148);
            var source = new Border { Tag = 196d };
            using (bar.Bind(TopAppBar.ExpandedHeightProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                Check(196); bar.Subtitle = "account"; Check(196);
                source.Tag = 204d; Check(204);
            }
            Check(180);
            var style = new Style { Setters = { new Setter(TopAppBar.ExpandedHeightProperty, 208d) } };
            bar.Styles.Add(style); bar.ApplyStyling(); Check(208);
            bar.ExpandedHeight = 212; Check(212);
            bar.ClearValue(TopAppBar.ExpandedHeightProperty); Check(208);
            bar.Styles.Remove(style); Check(180);
            bar.Resources.Remove(subtitleKey); Check(subtitle);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)]
    public void UnspecifiedHeight_UsesCurrentThemeSelection_WithoutReplacingInput(double input)
    {
        var state = new TopAppBarState();
        var bar = new MediumFlexibleTopAppBar { ExpandedHeight = input, ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.ExitUntilCollapsed) { State = state } };
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            Assert.Equal(input, bar.ExpandedHeight); Assert.Equal(112, bar.Bounds.Height);
            bar.Subtitle = "subtitle"; Flush(window); Assert.Equal(136, bar.Bounds.Height);
            bar.Resources[Comp.AppBar.MediumFlexibleWithSubtitleExpandedHeight] = 176d; Flush(window); Assert.Equal(176, bar.Bounds.Height);
            state.HeightOffset = -24; Flush(window); Assert.Equal(152, bar.Bounds.Height); Assert.Equal(input, bar.ExpandedHeight);
            bar.ClearValue(TopAppBar.ExpandedHeightProperty); Flush(window); Assert.Equal(176, bar.ExpandedHeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, 80)] [InlineData(true, 64)]
    public void BottomConfiguration_TokensLocalValuesAndScroll_ShareTheSameInputs(bool flexible, double height)
    {
        var state = new BottomAppBarState();
        BottomAppBar bar = flexible ? new FlexibleBottomAppBar() : new BottomAppBar();
        bar.ScrollBehavior = new BottomAppBarScrollBehavior() { State = state };
        var heightKey = flexible ? Comp.AppBar.FlexibleBottomExpandedHeight : Comp.AppBar.BottomExpandedHeight;
        var paddingKey = flexible ? Comp.AppBar.FlexibleBottomContentPadding : Comp.AppBar.BottomContentPadding;
        var elevationKey = flexible ? Comp.AppBar.FlexibleBottomTonalElevation : Comp.AppBar.BottomTonalElevation;
        var arrangementKey = flexible ? Comp.AppBar.FlexibleBottomArrangement : Comp.AppBar.BottomArrangement;
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            Assert.Equal(height, bar.ExpandedHeight); Assert.Equal(height, bar.Bounds.Height);
            Assert.Equal(flexible ? new Thickness(16, 0, 16, 0) : new Thickness(4, 4, 4, 0), bar.ContentPadding);
            Assert.Equal(flexible ? AppBarArrangement.SpaceBetween : AppBarArrangement.Start, bar.HorizontalArrangement);
            var style = new Style { Setters = { new Setter(BottomAppBar.ContentPaddingProperty, new Thickness(3)),
                new Setter(BottomAppBar.HorizontalArrangementProperty, AppBarArrangement.End) } };
            bar.Styles.Add(style); bar.ApplyStyling(); Flush(window);
            Assert.Equal(new Thickness(3), bar.ContentPadding); Assert.Equal(AppBarArrangement.End, bar.HorizontalArrangement);
            bar.Styles.Remove(style); Flush(window);
            bar.Resources[heightKey] = 100d; bar.Resources[paddingKey] = new Thickness(8, 2, 12, 4);
            bar.Resources[elevationKey] = 6d; bar.Resources[arrangementKey] = AppBarArrangement.Center;
            Flush(window); Assert.Equal(100, bar.ExpandedHeight); Assert.Equal(6, bar.TonalElevation);
            Assert.Equal(AppBarArrangement.Center, bar.HorizontalArrangement); Assert.Equal(new Thickness(8, 2, 12, 4), bar.ContentPadding);
            bar.ExpandedHeight = 120; bar.ContentPadding = new Thickness(1); Flush(window);
            state.HeightOffset = -20; Flush(window); Assert.Equal(100, bar.Bounds.Height); Assert.Equal(120, bar.ExpandedHeight);
            bar.ClearValue(BottomAppBar.ExpandedHeightProperty); bar.ClearValue(BottomAppBar.ContentPaddingProperty); Flush(window);
            Assert.Equal(100, bar.ExpandedHeight); Assert.Equal(80, bar.Bounds.Height); Assert.Equal(new Thickness(8, 2, 12, 4), bar.ContentPadding);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(double.NaN)] [InlineData(double.PositiveInfinity)] [InlineData(double.NegativeInfinity)] [InlineData(0d)] [InlineData(-1d)]
    public void FlexibleBottomInvalidHeight_UsesThemeDefault_AndPreservesInput(double input)
    {
        var bar = new FlexibleBottomAppBar { ExpandedHeight = input };
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            Assert.Equal(64, bar.Bounds.Height); bar.Resources[Comp.AppBar.FlexibleBottomExpandedHeight] = 96d; Flush(window);
            Assert.Equal(96, bar.Bounds.Height); Assert.Equal(input, bar.ExpandedHeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ThemeResourceAndMountChanges_UpdateHeightColorTypographyAndAnimation()
    {
        var bar = new MediumFlexibleTopAppBar();
        var first = new Grid { Children = { bar } }; var second = new Grid();
        first.Resources[Comp.AppBar.MediumFlexibleExpandedHeight] = 144d;
        second.Resources[Comp.AppBar.MediumFlexibleExpandedHeight] = 168d;
        second.Resources[Sys.Color.OnSurface] = Colors.Orange;
        var typography = new TextStyle { FontSize = 31 };
        second.Resources[Comp.AppBar.MediumFlexibleTitleStyle] = typography;
        var spring = new SpringSpec(1, 100);
        second.Resources[Sys.Motion.Spring.EffectsDefault] = spring;
        using var clock = new MotionTestClockScope(); var window = Show(new Grid { Children = { first, second } });
        try
        {
            Assert.Equal(144, bar.ExpandedHeight);
            first.Children.Remove(bar); second.Children.Add(bar); Flush(window);
            Assert.Equal(168, bar.ExpandedHeight); Assert.Equal(Colors.Orange, bar.TitleContentColor);
            var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
            Assert.Equal(spring, chrome.ContainerColorAnimation);
            Assert.Equal(typography, bar.GetVisualDescendants().OfType<AppBarTitle>().Last().TitleStyle);
            var before = bar.ScrolledContainerColor;
            window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window); Assert.NotEqual(before, bar.ScrolledContainerColor);
            second.Resources.ThemeDictionaries[ThemeVariant.Light] = new ResourceDictionary { [Comp.AppBar.MediumFlexibleExpandedHeight] = 168d };
            second.Resources.ThemeDictionaries[ThemeVariant.Dark] = new ResourceDictionary { [Comp.AppBar.MediumFlexibleExpandedHeight] = 184d };
            second.Resources.Remove(Comp.AppBar.MediumFlexibleExpandedHeight);
            Flush(window); Assert.Equal(184, bar.ExpandedHeight);
            window.RequestedThemeVariant = ThemeVariant.Light; Flush(window); Assert.Equal(168, bar.ExpandedHeight);
            var theme = new ControlTheme(typeof(TopAppBar)) { BasedOn = (ControlTheme)bar.FindResource(typeof(TopAppBar))!,
                Setters = { new Setter(TopAppBar.ExpandedHeightProperty, 200d) } };
            bar.Theme = theme; Flush(window); Assert.Equal(200, bar.ExpandedHeight);
            bar.ClearValue(StyledElement.ThemeProperty); Flush(window); Assert.Equal(168, bar.ExpandedHeight);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Colors_LocalBindingClearAndAnimation_KeepConfigurationSeparateFromPaint()
    {
        var state = new TopAppBarState();
        var bar = new TopAppBar { ScrollBehavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.Pinned) { State = state } };
        bar.Resources[Comp.AppBar.ContainerColor] = Colors.Red; bar.Resources[Comp.AppBar.ScrolledContainerColor] = Colors.Blue;
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
            Assert.Equal(Colors.Red, chrome.PaintedContainerColor);
            bar.Background = Brushes.Green; Flush(window);
            Assert.Equal(Colors.Green, chrome.GetCompositionColors().Base);
            bar.Resources[Comp.AppBar.ContainerColor] = Colors.Yellow; Flush(window);
            Assert.Equal(Colors.Green, chrome.GetCompositionColors().Base);
            bar.ClearValue(TopAppBar.BackgroundProperty); Flush(window); Assert.Equal(Colors.Yellow, chrome.GetCompositionColors().Base);
            bar.Resources[Sys.Color.OnSurface] = Colors.Orange; Flush(window); Assert.Equal(Colors.Orange, bar.TitleContentColor);
            bar.TitleContentColor = Colors.Purple; bar.Resources[Sys.Color.OnSurface] = Colors.Cyan; Flush(window);
            Assert.Equal(Colors.Purple, bar.TitleContentColor); Assert.Equal(Colors.Cyan, bar.NavigationIconContentColor);
            bar.ClearValue(TopAppBar.TitleContentColorProperty); Flush(window); Assert.Equal(Colors.Cyan, bar.TitleContentColor);
            var source = new Border { Tag = Colors.White };
            using (bar.Bind(TopAppBar.ScrolledContainerColorProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                state.ContentOffset = -80; Flush(window); clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                Assert.Equal(Colors.White, bar.ScrolledContainerColor);
                Assert.Equal(Colors.Yellow, ((ISolidColorBrush)bar.Background!).Color); Assert.Equal(64, bar.ExpandedHeight);
                source.Tag = Colors.Black; Flush(window);
                for (var i = 0; i < 60; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
                Assert.Equal(Colors.Black, chrome.PaintedContainerColor); Assert.Equal(Colors.Yellow, ((ISolidColorBrush)bar.Background!).Color);
            }
            Flush(window); Assert.Equal(Colors.Blue, bar.ScrolledContainerColor);
            for (var i = 0; i < 60; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(Colors.Blue, chrome.PaintedContainerColor); Assert.Null(MotionTestClockScope.Error(bar));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NullBackground_IsTransparent_AndNonSolidBackgroundFailsExplicitly()
    {
        var bar = new MediumTopAppBar { Background = null };
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            var chrome = bar.GetVisualDescendants().OfType<AppBarTopChrome>().Single();
            Assert.Equal(default, chrome.PaintedContainerColor);
            bar.Background = new LinearGradientBrush();
            Assert.Throws<ArgumentException>(() => Flush(window));
            bar.ClearValue(TopAppBar.BackgroundProperty); Flush(window);
            Assert.Equal(((ISolidColorBrush)bar.Background!).Color, chrome.PaintedContainerColor);
        }
        finally { window.Close(); }
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 300, RequestedThemeVariant = ThemeVariant.Light };
        window.Styles.Add(new MaterialTheme()); window.Content = new Grid { Children = { control } }; window.Show(); Flush(window); return window;
    }
}

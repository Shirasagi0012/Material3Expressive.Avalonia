using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class ContainerConfigurationTests
{
    [AvaloniaFact]
    public void Scaffold_DefaultArguments_FollowTokensAndEffectivePropertyPriority()
    {
        TokenScan.AssertComplete(new ScaffoldTokenSource(), typeof(Comp.Scaffold));
        var scaffold = new Scaffold(); var window = Show(scaffold);
        try
        {
            Assert.Equal(FabPosition.End, scaffold.FloatingActionButtonPosition);
            scaffold.Resources[Comp.Scaffold.FloatingActionButtonPosition] = FabPosition.Center;
            scaffold.Resources[Comp.Scaffold.ContentWindowInsets] = new Thickness(3, 5, 7, 9);
            Flush(window);
            Assert.Equal(FabPosition.Center, scaffold.FloatingActionButtonPosition);
            Assert.Equal(new Thickness(3, 5, 7, 9), scaffold.ContentPadding);
            var style = new Style { Setters = { new Setter(Scaffold.FloatingActionButtonPositionProperty, FabPosition.Start) } };
            scaffold.Styles.Add(style); scaffold.ApplyStyling(); Flush(window);
            Assert.Equal(FabPosition.Start, scaffold.FloatingActionButtonPosition);
            var source = new Border { Tag = FabPosition.EndOverlay };
            using (scaffold.Bind(Scaffold.FloatingActionButtonPositionProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                Assert.Equal(FabPosition.EndOverlay, scaffold.FloatingActionButtonPosition);
                source.Tag = FabPosition.End;
                Assert.Equal(FabPosition.End, scaffold.FloatingActionButtonPosition);
            }
            Assert.Equal(FabPosition.Start, scaffold.FloatingActionButtonPosition);
            scaffold.Styles.Remove(style); Flush(window);
            Assert.Equal(FabPosition.Center, scaffold.FloatingActionButtonPosition);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Divider_ThemeThickness_RemainsBelowUserStyleAndRestoresAfterClear()
    {
        var divider = new Separator(); var window = Show(divider);
        try
        {
            divider.Resources[Comp.Divider.Thickness] = 3d;
            var style = new Style { Setters = { new Setter(MaterialDivider.ThicknessProperty, 5d) } };
            divider.Styles.Add(style); divider.ApplyStyling(); Flush(window);
            Assert.Equal(5, divider.GetVisualDescendants().OfType<DividerPresenter>().Single().Thickness);
            MaterialDivider.SetThickness(divider, 2); Flush(window);
            Assert.Equal(2, MaterialDivider.GetThickness(divider));
            divider.ClearValue(MaterialDivider.ThicknessProperty); Flush(window);
            Assert.Equal(5, MaterialDivider.GetThickness(divider));
            divider.Styles.Remove(style); Flush(window);
            Assert.Equal(3, MaterialDivider.GetThickness(divider));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ScrollBar_OrientationDefaults_DoNotOverrideOrdinaryUserStyles()
    {
        var bar = new ScrollBar { Maximum = 75, ViewportSize = 25 };
        var window = Show(bar);
        try
        {
            var menu = new MenuFlyout();
            var style = new Style { Setters = { new Setter(Control.WidthProperty, 11d),
                new Setter(Control.HeightProperty, 120d), new Setter(Control.ContextFlyoutProperty, menu) } };
            bar.Styles.Add(style); bar.ApplyStyling(); Flush(window);
            foreach (var orientation in new[] { Orientation.Horizontal, Orientation.Vertical })
            {
                bar.Orientation = orientation; Flush(window);
                Assert.Equal(11, bar.Width); Assert.Equal(120, bar.Height); Assert.Same(menu, bar.ContextFlyout);
            }
            bar.Width = 15; bar.ClearValue(Control.WidthProperty); Flush(window); Assert.Equal(11, bar.Width);
            bar.Resources[Comp.ScrollBar.Thickness] = 13d;
            bar.Styles.Remove(style); Flush(window); Assert.Equal(13, bar.Width);
            var template = new FuncControlTemplate<ScrollBar>((_, _) => new Border());
            var templateStyle = new Style { Setters = { new Setter(TemplatedControl.TemplateProperty, template) } };
            bar.Styles.Add(templateStyle); bar.ApplyStyling(); Flush(window);
            bar.Orientation = Orientation.Horizontal; Flush(window); Assert.Same(template, bar.Template);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Card_ElevationIsAnEntireValue_WhilePassivePaintKeepsItsRememberedHeight()
    {
        var card = new ElevatedCard { Content = new Border { Width = 40, Height = 20 } };
        var window = Show(card);
        try
        {
            var surface = card.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(1, card.Elevation.Default); Assert.Equal(1, surface.ShadowElevation);
            card.Resources[Comp.ElevatedCard.ContainerElevation] = 7d; Flush(window);
            Assert.Equal(7, card.Elevation.Default); Assert.Equal(1, surface.ShadowElevation);
            var styled = new CardElevation { Default = 3, Hovered = 8 };
            var style = new Style { Setters = { new Setter(Card.ElevationProperty, styled) } };
            card.Styles.Add(style); card.ApplyStyling(); Flush(window); Assert.Same(styled, card.Elevation);
            card.Elevation = new CardElevation { Default = 4 };
            Assert.Equal(0, card.Elevation.Pressed); Assert.Equal(0, card.Elevation.Hovered);
            Assert.Equal(1, surface.ShadowElevation);
            card.ClearValue(Card.ElevationProperty); Assert.Same(styled, card.Elevation);
            card.Styles.Remove(style); Flush(window); Assert.Equal(7, card.Elevation.Default);
            var source = new Border { Tag = new CardElevation { Default = 9, Hovered = 10 } };
            using (card.Bind(Card.ElevationProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                Assert.Equal(9, card.Elevation.Default);
                source.Tag = new CardElevation { Default = 11 }; Assert.Equal(11, card.Elevation.Default);
            }
            Assert.Equal(7, card.Elevation.Default);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OutlinedCard_DefaultElevationRelationship_IsOnlyInTheThemeValue()
    {
        var card = new OutlinedCard(); var window = Show(card);
        try
        {
            card.Resources[Comp.OutlinedCard.ContainerElevation] = 4d;
            card.Resources[Comp.OutlinedCard.HoverContainerElevation] = 99d; Flush(window);
            Assert.Equal(4, card.Elevation.Default); Assert.Equal(4, card.Elevation.Pressed);
            Assert.Equal(4, card.Elevation.Focused); Assert.Equal(4, card.Elevation.Hovered);
            card.Elevation = new CardElevation { Default = 6 }; Flush(window);
            Assert.Equal(0, card.Elevation.Pressed); Assert.Equal(0, card.Elevation.Hovered);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Card_ContentKeepsAmbientBrush_AndStateDefaultsRemainBelowUserStyles()
    {
        var card = new Card { ContainerColor = Colors.Red }; var window = Show(card);
        try
        {
            var ambient = new LinearGradientBrush();
            TextElement.SetForeground((Grid)window.Content!, ambient); Flush(window);
            Assert.Same(ambient, card.ContentColor); Assert.Same(ambient, card.Foreground);
            card.IsClickable = true; card.IsEnabled = false; Flush(window);
            Assert.Same(ambient, card.DisabledContentColor); Assert.Same(ambient, card.Foreground);
            var template = new FuncControlTemplate<Card>((_, _) => new Border());
            var style = new Style { Setters = { new Setter(TemplatedControl.ForegroundProperty, Brushes.Blue),
                new Setter(InputElement.FocusableProperty, false), new Setter(TemplatedControl.TemplateProperty, template) } };
            card.Styles.Add(style); card.ApplyStyling(); Flush(window);
            card.IsEnabled = true; card.IsClickable = false; card.IsClickable = true; Flush(window);
            Assert.Same(Brushes.Blue, card.Foreground); Assert.False(card.Focusable); Assert.Same(template, card.Template);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Surface_ThemeInputs_UpdateWithoutChangingAmbientNullShadowContract()
    {
        var surface = new MaterialSurface(); var window = Show(surface);
        try
        {
            Assert.Null(surface.ShadowColor);
            surface.Resources[Sys.Color.Shadow] = Colors.Red;
            surface.Resources[Sys.Color.Surface] = Colors.Green;
            surface.Resources[Sys.Color.SurfaceTint] = Colors.Blue;
            var spring = new SpringSpec(1, 321);
            surface.Resources[Sys.Motion.Spring.EffectsDefault] = spring; Flush(window);
            Assert.Equal(Colors.Red, surface.DefaultShadowColor);
            Assert.Equal(Colors.Green, surface.TonalSurfaceColor); Assert.Equal(Colors.Blue, surface.TonalSurfaceTintColor);
            Assert.Equal(spring, surface.ShapeAnimation); Assert.Null(surface.ShadowColor);
            surface.ShadowColor = Colors.Yellow; surface.Resources[Sys.Color.Shadow] = Colors.Black; Flush(window);
            Assert.Equal(Colors.Yellow, surface.ShadowColor);
            surface.ShadowColor = null; Assert.Null(surface.ShadowColor); Assert.Equal(Colors.Black, surface.DefaultShadowColor);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SharedStateLayer_ConsumesTheSurfaceEffectiveConfiguration()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = new Border { Width = 24, Height = 16 } }; var window = Show(button);
        try
        {
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var controller = MaterialInteraction.GetOrCreate(button);
            button.Resources[Sys.State.HoverStateLayerOpacity] = .23;
            Flush(window); Assert.Equal(.23, surface.HoverStateLayerOpacity);
            controller.Set(InteractionState.Hovered, true);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            Assert.Equal(.23, controller.StateLayerOpacity, 6);
            surface.HoverStateLayerOpacity = .41;
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            Assert.Equal(.41, controller.StateLayerOpacity, 6);
            button.Resources[Sys.State.HoverStateLayerOpacity] = .31; Flush(window);
            Assert.Equal(.41, surface.HoverStateLayerOpacity);
            surface.ClearValue(MaterialSurface.HoverStateLayerOpacityProperty);
            clock.SendFrame(clock.Now); clock.AdvanceBy(TimeSpan.FromMilliseconds(100));
            Assert.Equal(.31, controller.StateLayerOpacity, 6);
        }
        finally { window.Close(); }
    }

    private static MotionTestWindow Show(Control control)
    {
        var window = new MotionTestWindow { Width = 320, Height = 240 };
        window.Styles.Add(new MaterialTheme());
        window.Content = new Grid { Children = { control } };
        window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

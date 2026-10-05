using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Styles;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonConfigurationTests
{
    [AvaloniaTheory]
    [InlineData("button")] [InlineData("icon")] [InlineData("toggle")] [InlineData("split")]
    public void ConditionalDefaults_PreserveUserStylesAndRestoreCurrentTokens(string family)
    {
        using var clock = new MotionTestClockScope();
        Button button = family switch { "icon" => new IconButton(), "toggle" => new ToggleButton(), _ => new Button() };
        if (family != "icon") button.Content = new Border { Width = 24, Height = 16 };
        var theme = family switch
        {
            "icon" => "MaterialOutlinedIconButtonTheme", "toggle" => "MaterialOutlinedToggleButtonTheme",
            "split" => "MaterialOutlinedSplitButtonLeadingTheme", _ => "MaterialOutlinedButtonTheme",
        };
        var window = Show(button, theme);
        try
        {
            var style = new Style
            {
                Setters =
                {
                    new Setter(Button.CornerRadiusProperty, new CornerRadius(17)),
                    new Setter(Button.PaddingProperty, new Thickness(9)),
                    new Setter(Button.BackgroundProperty, Brushes.Blue),
                    new Setter(Button.ForegroundProperty, Brushes.Red),
                    new Setter(Button.BorderBrushProperty, Brushes.Green),
                    new Setter(Button.BorderThicknessProperty, new Thickness(7)),
                },
            };
            button.Styles.Add(style); button.ApplyStyling(); Flush(window);
            void Check()
            {
                Assert.Equal(new CornerRadius(17), button.CornerRadius);
                Assert.Equal(new Thickness(9), button.Padding);
                Assert.Same(Brushes.Blue, button.Background);
                Assert.Same(Brushes.Red, button.Foreground);
                Assert.Same(Brushes.Green, button.BorderBrush);
                Assert.Equal(new Thickness(7), button.BorderThickness);
            }
            Check(); button.Classes.Add("large"); button.IsEnabled = false;
            if (button is ToggleButton toggle) toggle.IsChecked = true;
            Flush(window); Check();
            button.Background = Brushes.Orange; Flush(window); Assert.Same(Brushes.Orange, button.Background);
            button.ClearValue(Button.BackgroundProperty); Flush(window); Check();
            var source = new Border { Background = Brushes.Purple };
            using (button.Bind(Button.BackgroundProperty, new Binding(nameof(Border.Background)) { Source = source }))
            {
                button.IsEnabled = true; Flush(window); Assert.Same(Brushes.Purple, button.Background);
                source.Background = Brushes.Yellow; Flush(window); Assert.Same(Brushes.Yellow, button.Background);
            }
            Flush(window); Check(); button.Styles.Remove(style); Flush(window);
            Assert.NotEqual(new CornerRadius(17), button.CornerRadius);
            Assert.NotEqual(new Thickness(9), button.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void IconExtraSmallDimension_IsADefaultRatherThanAForcedConstraint()
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton(); button.Classes.Add("xsmall");
        var window = Show(button);
        try
        {
            Assert.Equal(32, button.Width);
            var style = new Style { Setters = { new Setter(Button.WidthProperty, 70d), new Setter(Button.HeightProperty, 60d) } };
            button.Styles.Add(style); button.ApplyStyling(); Flush(window);
            Assert.Equal(70, button.Width); Assert.Equal(60, button.Height);
            button.Classes.Clear(); Flush(window); Assert.Equal(70, button.Width);
            button.Styles.Remove(style); Flush(window); Assert.True(double.IsNaN(button.Width));
            button.Classes.Add("xsmall"); Flush(window); Assert.Equal(32, button.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SharedLayoutDefaults_AreLiveTokenInputs_AndDoNotWriteOwnerMinima()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = new Border { Width = 10, Height = 8 } };
        var window = Show(button);
        try
        {
            var layout = button.GetVisualDescendants().OfType<ButtonLayout>().Single();
            Assert.Equal(58, layout.ContainerMinWidth); Assert.Equal(48, layout.MinimumInteractiveSize);
            button.Resources[Comp.Button.MinWidth] = 100d; button.Resources[Comp.Button.MinimumInteractiveSize] = 64d;
            Flush(window); Assert.Equal(100, layout.ContainerMinWidth); Assert.Equal(64, layout.MinimumInteractiveSize);
            Assert.Equal(100, button.Bounds.Width); Assert.Equal(64, button.Bounds.Height);
            Assert.Equal(0, button.MinWidth); Assert.Equal(0, button.MinHeight);
            button.Width = 42; Flush(window); Assert.Equal(42, button.Width);
            button.ClearValue(Button.WidthProperty); Flush(window); Assert.Equal(100, button.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Elevation_IsACompleteLiveValue_AndExplicitReplacementWins()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = new Border { Width = 10, Height = 8 } };
        var window = Show(button);
        try
        {
            button.Resources[Sys.Elevation.Level1] = 5d; Flush(window);
            Assert.Equal(5, MaterialButton.GetElevation(button)!.Hovered);
            var configured = new ButtonElevation { Default = 2, Pressed = 3, Focused = 4, Hovered = 9, Disabled = 1 };
            MaterialButton.SetElevation(button, configured);
            button.Resources[Sys.Elevation.Level1] = 6d; button.IsEnabled = false; Flush(window);
            Assert.Same(configured, MaterialButton.GetElevation(button));
            button.ClearValue(MaterialButton.ElevationProperty); Flush(window);
            Assert.Equal(6, MaterialButton.GetElevation(button)!.Hovered);
            Assert.Equal(0, MaterialButton.GetElevation(button)!.Disabled);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ToggleBorderAnimation_ConsumesTemplateConfiguration()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleButton { Content = new Border { Width = 24, Height = 16 } };
        var window = Show(toggle, "MaterialOutlinedToggleButtonTheme");
        try
        {
            var part = toggle.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var width = new SpringSpec(.7, 777); var ink = new SpringSpec(1, 444);
            toggle.Resources[Comp.ToggleButton.BorderWidthAnimation] = width;
            toggle.Resources[Comp.ToggleButton.BorderColorAnimation] = ink; Flush(window);
            Assert.Equal(width, ButtonSurfaceState.GetBorderWidthAnimation(part));
            Assert.Equal(ink, ButtonSurfaceState.GetBorderColorAnimation(part));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BorderMotion_RetriesTheCurrentTargetAfterMissingConfigurationIsRestored()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ToggleButton { Content = new Border { Width = 24, Height = 16 } };
        var missing = new MaterialTokenKey<SpringSpec>("test.missing.border-animation");
        toggle.Resources[Comp.ToggleButton.BorderWidthAnimation] = new TokenRef(missing);
        toggle.Resources[Comp.ToggleButton.BorderColorAnimation] = new TokenRef(missing);
        var window = Show(toggle, "MaterialOutlinedToggleButtonTheme");
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
        try
        {
            var part = toggle.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var outline = part.GetVisualChildren().OfType<SurfaceOutline>().Single();
            var initial = outline.BorderThickness;
            toggle.IsChecked = true; Frames(4);
            Assert.Equal(initial, outline.BorderThickness);
            Assert.Null(MotionTestClockScope.Error(toggle));
            toggle.Resources.Remove(Comp.ToggleButton.BorderWidthAnimation);
            toggle.Resources.Remove(Comp.ToggleButton.BorderColorAnimation); Frames(100);
            Assert.Equal(0, outline.BorderThickness.Left);
            toggle.Resources[Comp.ToggleButton.BorderWidthAnimation] = new TokenRef(missing);
            toggle.IsChecked = false; Frames(4);
            Assert.Equal(0, outline.BorderThickness.Left);
            toggle.Resources.Remove(Comp.ToggleButton.BorderWidthAnimation); Frames(100);
            Assert.Equal(1, outline.BorderThickness.Left);
            Assert.Null(MotionTestClockScope.Error(toggle));
        }
        finally { window.Close(); }
    }

    private static Window Show(Button button, string? theme = null)
    {
        var window = new MotionTestWindow { Width = 600, Height = 300 };
        window.Styles.Add(new MaterialTheme());
        if (theme is not null) button.Theme = (ControlTheme)window.FindResource(theme)!;
        window.Content = button;
        window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class IconButtonAmbientTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void OrdinaryColors_FollowOuterEnvironment_DisabledAlphaAndUserOverrides(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton();
        var root = new Border { Child = button };
        var window = Show(root);
        try
        {
            button.Theme = (ControlTheme)window.FindResource(outlined ? "MaterialOutlinedIconButtonTheme" : "MaterialStandardIconButtonTheme")!;
            var ambient = Color.Parse("#40245A80");
            var ambientBrush = new SolidColorBrush(ambient);
            TextElement.SetForeground(root, ambientBrush); Flush(window);
            Assert.Equal(ambient, ColorOf(button.Foreground));
            if (outlined) Assert.Equal(ambient, ColorOf(button.BorderBrush));
            ambientBrush.Color = Colors.Blue; Flush(window);
            Assert.Equal(Colors.Blue, ColorOf(button.Foreground));
            if (outlined) Assert.Equal(Colors.Blue, ColorOf(button.BorderBrush));
            ambientBrush.Color = ambient; Flush(window);
            // Ordinary helpers do not consume the former vibrant color slots.
            button.Resources[outlined ? Comp.OutlinedIconButton.ContentColor : Comp.StandardIconButton.ContentColor] = Colors.Red;
            button.Resources[outlined ? Comp.OutlinedIconButton.ContainerColor : Comp.StandardIconButton.ContainerColor] = Colors.Red;
            Flush(window); Assert.Equal(ambient, ColorOf(button.Foreground)); Assert.Equal(Colors.Transparent, ColorOf(button.Background));
            root.IsEnabled = false; Flush(window);
            Assert.Equal(Color.Parse("#61245A80"), ColorOf(button.Foreground)); // replace, not multiply alpha
            if (outlined) Assert.Equal(Color.Parse("#61245A80"), ColorOf(button.BorderBrush));
            TextElement.SetForeground(root, Brushes.Green); Flush(window);
            Assert.Equal(Color.FromArgb(97, 0, 128, 0), ColorOf(button.Foreground));
            root.IsEnabled = true;
            var style = new Style { Setters = { new Setter(IconButton.ForegroundProperty, Brushes.Magenta) } };
            button.Styles.Add(style); button.ApplyStyling(); Flush(window);
            Assert.Equal(Colors.Magenta, ColorOf(button.Foreground));
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            var point = button.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Flush(window);
            Assert.Equal(outlined ? Colors.Magenta : Colors.Green, ColorOf(surface.StateLayerBrush));
            if (outlined) Assert.Equal(Colors.Green, ColorOf(button.BorderBrush));
            window.MouseUp(point, MouseButton.Left);
            button.Styles.Remove(style); Flush(window); Assert.Equal(Colors.Green, ColorOf(button.Foreground));
            // Reparenting must replace the ambient provider and must not retain a stale subscription.
            root.Child = null;
            var other = new Border { Child = button };
            TextElement.SetForeground(other, Brushes.Blue); window.Content = other; Flush(window);
            Assert.Equal(Colors.Blue, ColorOf(button.Foreground));
            TextElement.SetForeground(root, Brushes.Red); Flush(window); Assert.Equal(Colors.Blue, ColorOf(button.Foreground));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void VibrantThemes_RetainLiveComponentColorSlots(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton();
        var window = Show(button);
        try
        {
            button.Theme = (ControlTheme)window.FindResource(outlined ? "MaterialOutlinedVibrantIconButtonTheme" : "MaterialVibrantIconButtonTheme")!;
            window.Foreground = Brushes.Blue; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.OnSurfaceVariant), ColorOf(button.Foreground));
            button.Resources[outlined ? Comp.OutlinedIconButton.ContentColor : Comp.StandardIconButton.ContentColor] = Colors.Magenta;
            button.Resources[outlined ? Comp.OutlinedIconButton.DisabledContentColor : Comp.StandardIconButton.DisabledContentColor] = Colors.Red;
            Flush(window); Assert.Equal(Colors.Magenta, ColorOf(button.Foreground));
            Assert.Equal(Colors.Magenta, ColorOf(button.GetVisualDescendants().OfType<MaterialSurface>().Single().StateLayerBrush));
            button.IsEnabled = false; Flush(window); Assert.Equal(Colors.Red, ColorOf(button.Foreground));
            button.IsEnabled = true; window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
            if (outlined) Assert.Equal(ButtonThemeFixture.SchemeColor(true, s => s.OutlineVariant), ColorOf(button.BorderBrush));
            button.Theme = (ControlTheme)window.FindResource(outlined ? "MaterialOutlinedIconButtonTheme" : "MaterialStandardIconButtonTheme")!;
            Flush(window); Assert.Equal(Colors.Blue, ColorOf(button.Foreground));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void OutlinedToggle_UsesSixSlotColors_IndependentOfStandardToggleOrder()
    {
        using var clock = new MotionTestClockScope();
        var standard = new IconToggleButton { IsChecked = true };
        var button = new IconToggleButton();
        var root = new StackPanel { Children = { standard, button } };
        TextElement.SetForeground(root, new SolidColorBrush(Color.Parse("#40245A80")));
        var window = Show(root);
        try
        {
            button.Theme = (ControlTheme)window.FindResource("MaterialOutlinedIconToggleButtonTheme")!; Flush(window);
            Assert.Equal(Color.Parse("#40245A80"), ColorOf(button.Foreground));
            Assert.Equal(Colors.Transparent, ColorOf(button.Background)); Assert.Equal(new Thickness(1), button.BorderThickness);
            button.IsChecked = true; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.InverseSurface), ColorOf(button.Background));
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.InverseOnSurface), ColorOf(button.Foreground));
            Assert.Equal(default, button.BorderThickness);
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(ColorOf(button.Foreground), ColorOf(surface.StateLayerBrush));
            window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(true, s => s.InverseOnSurface), ColorOf(button.Foreground));
            // contentColorFor preserves its role precedence, including local role aliases.
            button.Resources[Sys.Color.InverseSurface] = Colors.Red;
            button.Resources[Sys.Color.Primary] = Colors.Red;
            button.Resources[Sys.Color.OnPrimary] = Colors.Yellow; Flush(window);
            Assert.Equal(Colors.Red, ColorOf(button.Background)); Assert.Equal(Colors.Yellow, ColorOf(button.Foreground));
            button.IsEnabled = false; Flush(window);
            Assert.Equal(Colors.Transparent, ColorOf(button.Background)); Assert.Equal(Color.Parse("#61245A80"), ColorOf(button.Foreground));
            Assert.Equal(default, button.BorderThickness);
            button.IsChecked = false; Flush(window);
            Assert.Equal(new Thickness(1), button.BorderThickness); Assert.Equal(Color.Parse("#61245A80"), ColorOf(button.BorderBrush));
            // Explicit public overrides survive selected/disabled changes.
            button.Background = Brushes.Blue; button.BorderThickness = new Thickness(3); button.Foreground = Brushes.Green;
            button.IsChecked = true; Flush(window);
            Assert.Equal(Colors.Blue, ColorOf(button.Background)); Assert.Equal(Colors.Green, ColorOf(button.Foreground)); Assert.Equal(new Thickness(3), button.BorderThickness);
            button.ClearValue(IconButton.BackgroundProperty); button.ClearValue(IconButton.ForegroundProperty); button.ClearValue(IconButton.BorderThicknessProperty);
            button.IsEnabled = true; Flush(window);
            Assert.Equal(Colors.Red, ColorOf(button.Background)); Assert.Equal(Colors.Yellow, ColorOf(button.Foreground)); Assert.Equal(default, button.BorderThickness);
            root.Children.Remove(standard); root.Children.Add(new IconToggleButton()); Flush(window);
            Assert.Equal(Colors.Red, ColorOf(button.Background)); Assert.Equal(Colors.Yellow, ColorOf(button.Foreground));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void VibrantToggle_ColorsBorderAndInk_FollowStateResourcesAndOverrides(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconToggleButton();
        var root = new Border { Child = button };
        TextElement.SetForeground(root, Brushes.Blue);
        var window = Show(root);
        try
        {
            button.Theme = (ControlTheme)window.FindResource(outlined ? "MaterialOutlinedVibrantIconToggleButtonTheme" : "MaterialVibrantIconToggleButtonTheme")!;
            Flush(window);
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.OnSurfaceVariant), ColorOf(button.Foreground));
            Assert.Equal(Colors.Transparent, ColorOf(button.Background));
            Assert.Equal(outlined ? ColorOf(button.Foreground) : Colors.Blue, ColorOf(surface.StateLayerBrush));
            if (outlined)
            {
                Assert.Equal(new Thickness(1), button.BorderThickness);
                Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => s.OutlineVariant), ColorOf(button.BorderBrush));
            }
            button.IsChecked = true; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => outlined ? s.InverseOnSurface : s.Primary), ColorOf(button.Foreground));
            Assert.Equal(outlined ? ButtonThemeFixture.SchemeColor(false, s => s.InverseSurface) : Colors.Transparent, ColorOf(button.Background));
            Assert.Equal(default, button.BorderThickness);
            window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(true, s => outlined ? s.InverseOnSurface : s.Primary), ColorOf(button.Foreground));
            root.IsEnabled = false; Flush(window);
            var onSurface = ButtonThemeFixture.SchemeColor(true, s => s.OnSurface);
            Assert.Equal(Color.FromArgb(97, onSurface.R, onSurface.G, onSurface.B), ColorOf(button.Foreground));
            Assert.Equal(Colors.Transparent, ColorOf(button.Background));
            Assert.Equal(default, button.BorderThickness);
            button.IsChecked = false; Flush(window);
            if (outlined)
            {
                var outline = ButtonThemeFixture.SchemeColor(true, s => s.OutlineVariant);
                Assert.Equal(Color.FromArgb(97, outline.R, outline.G, outline.B), ColorOf(button.BorderBrush));
                Assert.Equal(new Thickness(1), button.BorderThickness);
            }
            root.IsEnabled = true; button.IsChecked = true;
            button.Resources[outlined ? Comp.OutlinedIconToggleButton.CheckedContentColor : Comp.StandardIconToggleButton.CheckedContentColor] = Colors.Magenta;
            Flush(window); Assert.Equal(Colors.Magenta, ColorOf(button.Foreground));
            var point = button.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Flush(window);
            TextElement.SetForeground(root, Brushes.Green); Flush(window);
            Assert.Equal(outlined ? Colors.Magenta : Colors.Green, ColorOf(surface.StateLayerBrush));
            window.MouseUp(point, MouseButton.Left);
            var userStyle = new Style { Setters = { new Setter(IconButton.ForegroundProperty, Brushes.Yellow), new Setter(IconButton.BackgroundProperty, Brushes.Red) } };
            button.Styles.Add(userStyle); button.ApplyStyling(); button.IsEnabled = false; Flush(window);
            Assert.Equal(Colors.Yellow, ColorOf(button.Foreground)); Assert.Equal(Colors.Red, ColorOf(button.Background));
            button.Styles.Remove(userStyle); button.IsEnabled = true; Flush(window);
            Assert.Equal(Colors.Magenta, ColorOf(button.Foreground));
            root.Child = null;
            var other = new Border { Child = button };
            TextElement.SetForeground(other, Brushes.Purple); window.Content = other; Flush(window);
            Assert.Equal(outlined ? Colors.Magenta : Colors.Purple, ColorOf(button.GetVisualDescendants().OfType<MaterialSurface>().Single().StateLayerBrush));
        }
        finally { window.Close(); }
    }

    private static Color ColorOf(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 240, Height = 180 };
        window.Styles.Add(new MaterialTheme()); window.Content = content; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}

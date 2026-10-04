using Avalonia;
using Avalonia.Controls;
using Path = Avalonia.Controls.Shapes.Path;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class SnackbarButtonThemeTests
{
    [AvaloniaFact]
    public void StandardAction_UsesSimpleTextButtonMetrics_AndTracksItsOwnContentColor()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = "Undo", Foreground = Brushes.Red };
        var window = DialogTests.Show(new Grid { Children = { button } });
        try
        {
            button.Theme = (ControlTheme)window.FindResource("MaterialSnackbarActionButtonTheme")!;
            Flush(window);
            Assert.Equal(new Thickness(12, 8), button.Padding);
            var text = button.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(14, text.FontSize); Assert.Equal(20, text.LineHeight);
            Assert.Equal(FontWeight.Medium, text.FontWeight); Assert.Equal(.1, text.LetterSpacing);
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(40, surface.Bounds.Height); Assert.Equal(48, button.Bounds.Height);
            Assert.Same(Brushes.Red, surface.StateLayerBrush);
            button.Foreground = Brushes.Blue;
            Assert.Same(Brushes.Blue, surface.StateLayerBrush);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StandardDismiss_UsesCoreVector_AndReservesMinimumInteractiveLayout()
    {
        using var clock = new MotionTestClockScope();
        var button = new IconButton { Foreground = Brushes.Red };
        var window = DialogTests.Show(new Grid { Children = { button } });
        try
        {
            button.Theme = (ControlTheme)window.FindResource("MaterialSnackbarDismissButtonTheme")!;
            Flush(window);
            Assert.Equal(new Size(48, 48), button.Bounds.Size);
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(new Size(40, 40), surface.Bounds.Size);
            Assert.Equal(new Point(4, 4), surface.TranslatePoint(default, button));
            var path = button.GetVisualDescendants().OfType<Path>().Single();
            Assert.Equal(new Size(24, 24), path.Bounds.Size);
            Assert.Equal(new Rect(5, 5, 14, 14), path.Data!.Bounds);
            Assert.Same(Brushes.Red, path.Fill); Assert.Same(Brushes.Red, surface.StateLayerBrush);
            button.Foreground = Brushes.Blue;
            Assert.Same(Brushes.Blue, path.Fill); Assert.Same(Brushes.Blue, surface.StateLayerBrush);
        }
        finally { window.Close(); }
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

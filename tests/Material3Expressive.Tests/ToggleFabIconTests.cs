using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ToggleFabIconTests
{
    [AvaloniaTheory]
    [InlineData(FloatingActionButtonSize.Default, 24)]
    [InlineData(FloatingActionButtonSize.Medium, 28)]
    [InlineData(FloatingActionButtonSize.Large, 36)]
    public void DefaultSizeAndTint_FollowTheContainingToggle(FloatingActionButtonSize size, double expected)
    {
        using var clock = new MotionTestClockScope();
        var icon = new ToggleFloatingActionButtonIcon { Source = Image() };
        var toggle = new ToggleFloatingActionButton { Size = size, Icon = icon };
        var window = Show(toggle);
        try
        {
            Assert.Equal(new Size(expected, expected), icon.Bounds.Size);
            Assert.Equal(toggle.IconColor, ((ISolidColorBrush)icon.Foreground!).Color);
            toggle.IsChecked = true;
            Frames(clock, window, 6);
            Assert.InRange(toggle.CheckedProgress, .01f, .99f);
            Assert.Equal(new Size(toggle.IconSize, toggle.IconSize), icon.Bounds.Size);
            Assert.Equal(toggle.IconColor, ((ISolidColorBrush)icon.Foreground!).Color);
            Frames(clock, window, 70);
            Assert.Equal(1, toggle.CheckedProgress);
            Assert.Equal(new Size(20, 20), icon.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CustomFunctions_UseUnclampedProgressAndPhysicalPixelRounding()
    {
        using var clock = new MotionTestClockScope();
        float observed = -1;
        var icon = new ToggleFloatingActionButtonIcon
        {
            // Dp uses Float: this converts to 25.25f before roundToPx at density 2.
            Source = Image(), Size = p => { observed = p; return 25.2499999 + 8 * p; },
            Color = p => p > .5f ? Colors.Gold : Color.FromArgb(128, 10, 20, 30)
        };
        var toggle = new ToggleFloatingActionButton { Content = icon };
        var window = Show(toggle);
        try
        {
            window.SetRenderScaling(2); window.UpdateLayout();
            Assert.Equal(new Size(25.5, 25.5), icon.Bounds.Size);
            Assert.Equal(0, observed);
            toggle.IsChecked = true; Frames(clock, window, 75);
            Assert.Equal(1, observed);
            Assert.Equal(new Size(33.5, 33.5), icon.Bounds.Size);
            Assert.Equal(Colors.Gold, ((ISolidColorBrush)icon.Foreground!).Color);
            icon.Size = null; icon.Color = null; window.UpdateLayout();
            Assert.Equal(new Size(20, 20), icon.Bounds.Size);
            Assert.Equal(toggle.IconColor, ((ISolidColorBrush)icon.Foreground!).Color);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DetachRestoresCallerValues_AndReparentUsesTheNewToggle()
    {
        using var clock = new MotionTestClockScope();
        var icon = new ToggleFloatingActionButtonIcon { Source = Image(), Width = 9, Height = 7, Foreground = Brushes.Red, IsTintEnabled = false };
        var first = new ToggleFloatingActionButton { Size = FloatingActionButtonSize.Large, Content = icon };
        var second = new ToggleFloatingActionButton { IsChecked = true };
        var row = new StackPanel { Orientation = Orientation.Horizontal, Children = { first, second } };
        var window = Show(row);
        try
        {
            Assert.Equal(36, icon.Width); Assert.True(icon.IsTintEnabled);
            first.Content = null; window.UpdateLayout();
            Assert.Equal(9, icon.Width); Assert.Equal(7, icon.Height);
            Assert.Same(Brushes.Red, icon.Foreground); Assert.False(icon.IsTintEnabled);
            second.Icon = icon; window.UpdateLayout();
            Assert.Equal(20, icon.Width);
            Assert.Equal(new Size(20, 20), icon.Bounds.Size);
            first.IsChecked = true; Frames(clock, window, 5);
            Assert.Equal(second.IconColor, ((ISolidColorBrush)icon.Foreground!).Color);
            second.Icon = null; window.UpdateLayout();
            Frames(clock, window, 70);
            Assert.Equal(0, clock.PendingFrameCount);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DrawingImageReplacementAndDescription_RetainImageSemantics()
    {
        using var clock = new MotionTestClockScope();
        var image = Image();
        var icon = new ToggleFloatingActionButtonIcon { Source = image, ContentDescription = "More actions" };
        var toggle = new ToggleFloatingActionButton { Content = icon };
        var window = Show(toggle);
        try
        {
            image.Drawing = new GeometryDrawing { Brush = Brushes.Blue, Geometry = new RectangleGeometry(new Rect(0, 0, 80, 30)) };
            window.UpdateLayout();
            Assert.Equal(new Size(24, 24), icon.Bounds.Size);
            var peer = ControlAutomationPeer.CreatePeerForElement(icon)!;
            Assert.Equal("More actions", peer.GetName());
            Assert.Equal(AutomationControlType.Image, peer.GetAutomationControlType());
            Assert.False(icon.Focusable);
            icon.Source = null; window.UpdateLayout();
            Assert.Equal(new Size(24, 24), icon.Bounds.Size);
        }
        finally { window.Close(); }
    }

    private static DrawingImage Image() => new(new GeometryDrawing
    { Brush = Brushes.Blue, Geometry = new RectangleGeometry(new Rect(0, 0, 32, 16)) });

    private static void Frames(MotionTestClockScope clock, Window window, int count)
    { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); } }

    private static MotionTestWindow Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 240, Height = 200 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}

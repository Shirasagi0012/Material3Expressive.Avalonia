using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;
using TimePicker = Material3Expressive.Components.TimePicker;

namespace Material3Expressive.Tests;

public class ClockDrawingTests
{
    [AvaloniaTheory]
    [InlineData(1, 1, 0)]
    [InlineData(.5, 1, 127)]
    [InlineData(1, .5, 127)]
    [InlineData(.5, .5, 191)]
    public void SelectorTrackHonorsBrushAndAncestorOpacity(double brushOpacity, double parentOpacity, int expected)
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { SelectedTime = new(0, 0), Is24Hour = false };
        var parent = new Border { Background = Brushes.White, Child = picker, Opacity = parentOpacity };
        var window = DialogTests.Show(new Border { Background = Brushes.White, Child = parent }, 600);
        try
        {
            var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
            dial.DialBrush = Brushes.White; dial.SelectorBrush = new SolidColorBrush(Colors.Black, brushOpacity);
            Assert.Equal((expected, expected, expected), Pixel(window, dial, new Point(128, 90)));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void IdleSelectorRepaintsForMutableBrushReplacementAndTrackWidth()
    {
        using var clock = new MotionTestClockScope();
        var picker = new TimePicker { SelectedTime = new(0, 0), Is24Hour = false };
        var window = DialogTests.Show(new Border { Background = Brushes.White, Child = picker }, 600);
        try
        {
            var dial = picker.GetVisualDescendants().OfType<ClockFace>().Single();
            var brush = new SolidColorBrush(Colors.Black); dial.DialBrush = Brushes.White; dial.SelectorBrush = brush;
            Assert.Equal((0, 0, 0), Pixel(window, dial, new Point(128, 90)));
            brush.Opacity = .5;
            Assert.Equal((127, 127, 127), Pixel(window, dial, new Point(128, 90)));
            brush.Color = Colors.Red;
            Assert.Equal((255, 127, 127), Pixel(window, dial, new Point(128, 90)));
            dial.SelectorBrush = Brushes.Blue;
            Assert.Equal((0, 0, 255), Pixel(window, dial, new Point(128, 90)));
            Assert.Equal((255, 255, 255), Pixel(window, dial, new Point(131, 90)));
            dial.TrackWidth = 8;
            Assert.Equal((0, 0, 255), Pixel(window, dial, new Point(131, 90)));
            dial.SelectorBrush = Brushes.Transparent; dial.SelectedTextBrush = Brushes.Lime;
            Assert.Equal((0, 255, 0), Pixel(window, dial, new Point(140, 27)));
            dial.SelectedTextBrush = Brushes.Blue;
            Assert.Equal((0, 0, 255), Pixel(window, dial, new Point(140, 27)));
        }
        finally { window.Close(); }
    }

    private static (int R, int G, int B) Pixel(Window window, Control dial, Point local)
    {
        Dispatcher.UIThread.RunJobs(); window.UpdateLayout();
        var point = dial.TranslatePoint(local, window)!.Value;
        using var frame = window.CaptureRenderedFrame()!; using var buffer = frame.Lock();
        var pixel = new byte[4];
        Marshal.Copy(buffer.Address + (int)point.Y * buffer.RowBytes + (int)point.X * 4, pixel, 0, 4);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (pixel[0], pixel[1], pixel[2]) : (pixel[2], pixel[1], pixel[0]);
    }
}

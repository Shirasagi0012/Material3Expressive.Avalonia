using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class ProgressStopIndicatorTests
{
    public static IEnumerable<object[]> DrawingCases() =>
        from bypass in new[] { false, true }
        from rtl in new[] { false, true }
        from density in new[] { 1d, 2d }
        from cap in new[] { PenLineCap.Round, PenLineCap.Flat, PenLineCap.Square }
        select new object[] { bypass, rtl, density, cap };

    [AvaloniaTheory, MemberData(nameof(DrawingCases))]
    public void StandaloneStop_UsesPhysicalTrailingEdgeAndRequestedShape(bool bypass, bool rtl, double density, PenLineCap cap)
    {
        using var clock = new MotionTestClockScope();
        var stop = new StopHost(bypass) { Width = 80, Height = 20, Cap = cap };
        var root = new Grid { FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight, Children = { stop } };
        var window = Show(root); window.SetRenderScaling(density);
        try
        {
            Draw(window);
            var bounds = new Rect(stop.Bounds.Size).TransformToAABB(stop.TransformToVisual(window)!.Value);
            using var frame = window.CaptureRenderedFrame()!;
            Assert.Equal((0, 0, 0), Pixel(frame, bounds.X + (rtl ? 10 : 70), bounds.Y + 10, density));
            Assert.Equal((255, 255, 255), Pixel(frame, bounds.X + (rtl ? 70 : 10), bounds.Y + 10, density));
            var corner = Pixel(frame, bounds.X + (rtl ? 6 : 66), bounds.Y + 6, density);
            if (cap == PenLineCap.Round) Assert.True(corner.R > 100);
            else Assert.Equal((0, 0, 0), corner);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StandaloneStop_ReevaluatesSizeDirectionColorAndOwnerOpacityAfterReparent()
    {
        using var clock = new MotionTestClockScope();
        var stop = new StopHost(false) { Width = 80, Height = 20, Cap = PenLineCap.Flat };
        var first = new Grid { Children = { stop } }; var root = new Grid { Children = { first } };
        var window = Show(root);
        try
        {
            Draw(window); Assert.Equal((0, 0, 0), At(70, 10));
            stop.StopSize = 40; stop.Height = 12; stop.InvalidateVisual(); Draw(window);
            Assert.Equal((0, 0, 0), At(79, 6)); // Stop is clamped to the actual height.
            stop.StopSize = 0; stop.InvalidateVisual(); Draw(window); Assert.Equal((255, 255, 255), At(79, 6));
            first.Children.Clear();
            stop.StopSize = 8; stop.Color = Color.FromArgb(128, 255, 0, 0); stop.Opacity = .5;
            root.Children.Add(new Grid { FlowDirection = FlowDirection.RightToLeft, Children = { stop } });
            stop.InvalidateVisual(); Draw(window);
            var pixel = At(6, 6); Assert.Equal(255, pixel.R); Assert.InRange(pixel.G, 190, 192); Assert.InRange(pixel.B, 190, 192);
            Assert.Equal((255, 255, 255), At(74, 6));
        }
        finally { window.Close(); }
        (int R, int G, int B) At(double x, double y)
        {
            var rect = new Rect(stop.Bounds.Size).TransformToAABB(stop.TransformToVisual(window)!.Value);
            using var frame = window.CaptureRenderedFrame()!; return Pixel(frame, rect.X + x, rect.Y + y, 1);
        }
    }

    [AvaloniaFact]
    public void NarrowOwner_RegistersOverflowAsDrawingBounds_AndClearsItWhenStopIsRemoved()
    {
        using var clock = new MotionTestClockScope();
        var stop = new StopHost(false) { Width = 4, Height = 12, Cap = PenLineCap.Flat };
        var window = Show(new Grid { Children = { stop } });
        try
        {
            Draw(window);
            var rect = new Rect(stop.Bounds.Size).TransformToAABB(stop.TransformToVisual(window)!.Value);
            using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((0, 0, 0), Pixel(frame, rect.X - 3, rect.Y + 6, 1));
            stop.StopSize = 0; stop.InvalidateVisual(); Draw(window);
            using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((255, 255, 255), Pixel(frame, rect.X - 3, rect.Y + 6, 1));
            Assert.Equal(new Size(4, 12), stop.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CallbackCanComposeDefaultStop_AfterProgress_AndIsSuppressedInIndeterminateMode()
    {
        using var clock = new MotionTestClockScope();
        var indicator = new LinearProgressIndicator { Width = 80, Height = 20, Progress = 1, Color = Colors.Blue,
            TrackColor = Colors.Transparent, StrokeCap = PenLineCap.Flat, StopSize = 8 };
        var calls = 0;
        indicator.DrawStopIndicator = (context, _) =>
        {
            calls++;
            ProgressIndicatorDefaults.DrawStopIndicator(context, indicator, 8, Colors.Red, PenLineCap.Flat);
        };
        var window = Show(indicator);
        try
        {
            Draw(window); Assert.True(calls > 0);
            using (var frame = window.CaptureRenderedFrame()!)
            { Assert.Equal((255, 0, 0), Pixel(frame, 70, 10, 1)); Assert.Equal((0, 0, 255), Pixel(frame, 30, 10, 1)); }
            var previous = calls; indicator.IsIndeterminate = true; Draw(window); Assert.Equal(previous, calls);
            indicator.IsIndeterminate = false; indicator.DrawStopIndicator = (_, _) => { }; Draw(window);
            using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((0, 0, 255), Pixel(frame, 70, 10, 1));
            indicator.DrawStopIndicator = null; indicator.Progress = 0; Draw(window);
            using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((0, 0, 255), Pixel(frame, 70, 10, 1));
        }
        finally { window.Close(); }
    }

    private sealed class StopHost(bool bypass) : Control
    {
        protected override bool BypassFlowDirectionPolicies => bypass;
        public double StopSize = 8;
        public Color Color = Colors.Black;
        public PenLineCap Cap = PenLineCap.Round;
        public override void Render(DrawingContext context) => ProgressIndicatorDefaults.DrawStopIndicator(context, this, StopSize, Color, Cap);
    }

    private static void Draw(Window window)
    {
        for (var i = 0; i < 4; i++)
        { window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); using (window.CaptureRenderedFrame()) { } }
    }
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 160, Height = 100, Background = Brushes.White };
        window.Styles.Add(new Material3Expressive.Styles.MaterialTheme());
        window.Content = content; window.Show(); return window;
    }
    private static (int R, int G, int B) Pixel(WriteableBitmap frame, double x, double y, double density)
    {
        using var buffer = frame.Lock(); var bytes = new byte[4];
        Marshal.Copy(buffer.Address + (int)(y * density) * buffer.RowBytes + (int)(x * density) * 4, bytes, 0, 4);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (bytes[0], bytes[1], bytes[2]) : (bytes[2], bytes[1], bytes[0]);
    }
}

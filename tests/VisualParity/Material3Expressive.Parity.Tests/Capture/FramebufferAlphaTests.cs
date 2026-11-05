using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Material3Expressive.Parity;
using SkiaSharp;
using Xunit;

namespace Material3Expressive.Parity.Tests;

public partial class ParityTests
{
    [AvaloniaTheory]
    [InlineData(0)]
    [InlineData(128)]
    [InlineData(255)]
    public async Task PassiveSnapshotPreservesActualFramebufferAlpha(int alpha)
    {
        var result = await Actual(async writer =>
        {
            await using var host = new AvaloniaHost(writer, 16, 16);
            host.Mount(new AlphaFixture((byte)alpha));
            host.Frame(0);

            // Independent evidence from the renderer-owned buffer, before the public headless
            // copy or our PNG capture. This test never writes to that buffer.
            var platform = host.Window.PlatformImpl!;
            var source = (WriteableBitmap)platform.GetType().GetField("_lastRenderedFrame",
                BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(platform)!;
            using (var buffer = source.Lock())
            {
                Assert.Equal(AlphaFormat.Premul, buffer.AlphaFormat);
                var pixel = new byte[4];
                Marshal.Copy(buffer.Address, pixel, 0, 4);
                Assert.Equal(alpha, pixel[3]);
                Assert.Equal(alpha, pixel[buffer.Format == PixelFormat.Rgba8888 ? 2 : 0]);
            }

            // Avalonia.Headless 12.1.5 labels this copy Opaque, even for the transparent buffer.
            using (var legacy = host.Window.GetLastRenderedFrame()!)
            using (var buffer = legacy.Lock())
                Assert.Equal(AlphaFormat.Opaque, buffer.AlphaFormat);

            var first = host.Snapshot("first", "fixture", []);
            var repeated = host.Snapshot("repeated", "fixture", []);
            writer.Submit(first); writer.Submit(repeated);
            var pixels = PixelData.Read(writer.Output, first.Rasters.Single());
            Assert.Equal(new byte[] { 0, 0, alpha == 0 ? (byte)0 : (byte)255, (byte)alpha }, pixels[..4]);
            Assert.Equal(pixels, PixelData.Read(writer.Output, repeated.Rasters.Single()));
            Assert.Equal(0, host.FrameTimeUs);
        });
        Assert.Empty(result.Capture.Errors);
        Assert.Single(result.Capture.Trace, t => t.Operation == "frame");
    }

    private sealed class AlphaFixture(byte alpha) : Control
    {
        public override void Render(DrawingContext context) => context.Custom(new AlphaOperation(new Rect(Bounds.Size), alpha));
    }

    private sealed class AlphaOperation(Rect bounds, byte alpha) : ICustomDrawOperation
    {
        public Rect Bounds => bounds;
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            using var lease = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()!.Lease();
            using var paint = new SKPaint { Color = new SKColor(0, 0, 255, alpha), BlendMode = SKBlendMode.Src };
            lease.SkCanvas.DrawRect(0, 0, (float)bounds.Width, (float)bounds.Height, paint);
        }
    }
}

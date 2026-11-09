// ProgressIndicator.kt / ProgressIndicatorDefaults.drawStopIndicator.
// AndroidX f3ed195f0c9e and Core a9e47e8aa04c, Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

public static partial class ProgressIndicatorDefaults
{
    /// <summary>Draws the default linear progress stop in the owner's local drawing coordinates.</summary>
    /// <param name="context">The drawing context passed to the owner's Render or DrawStopIndicator callback.</param>
    /// <param name="owner">Supplies the current arranged size, density and layout direction.</param>
    /// <param name="stopSize">Stop size in DIP, limited to the owner's actual height.</param>
    /// <param name="color">Stop color, including its alpha.</param>
    /// <param name="strokeCap">Round draws a circle; Flat and Square draw a square.</param>
    public static void DrawStopIndicator(DrawingContext context, Control owner, double stopSize, Color color, PenLineCap strokeCap)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(owner);
        // Ordinary Avalonia controls mirror their drawing coordinates in RTL. ProgressIndicator
        // bypasses that policy. Account for native mirroring so both receive one physical flip.
        var mirrored = owner.HasMirrorTransform;
        foreach (var ancestor in owner.GetVisualAncestors()) mirrored ^= ancestor.HasMirrorTransform;
        context.Custom(new StopIndicatorDrawOperation(owner.Bounds.Size, (float)LayoutHelper.GetLayoutScale(owner),
            (float)stopSize, color, strokeCap, (owner.FlowDirection == FlowDirection.RightToLeft) ^ mirrored));
    }

    private sealed class StopIndicatorDrawOperation(Size size, float scale, float stopSize, Color color,
        PenLineCap cap, bool rtl) : ICustomDrawOperation
    {
        public Rect Bounds
        {
            get
            {
                var height = (float)size.Height * scale;
                var stop = Math.Min(stopSize * scale, height);
                var offset = Math.Min((height - stop) / 2, 6 * scale);
                return stop <= 0 ? new Rect(size) : new Rect(size).Union(new Rect(
                    rtl ? offset / scale : ((float)size.Width * scale - stop - offset) / scale,
                    (height - stop) / 2 / scale, stop / scale, stop / scale));
            }
        }
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            using var lease = (context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Material Progress Indicator requires Avalonia.Skia.")).Lease();
            using var paint = ProgressPaint.Create(color, new ProgressIndicatorStroke(0, cap), scale, lease.CurrentOpacity);
            var canvas = lease.SkCanvas;
            var width = (float)size.Width * scale; var height = (float)size.Height * scale;
            var stop = Math.Min(stopSize * scale, height);
            var offset = Math.Min((height - stop) / 2, 6 * scale);
            canvas.Save();
            try
            {
                canvas.Scale(1 / scale);
                if (rtl) canvas.Scale(-1, 1, width / 2, height / 2);
                ProgressPaint.Stop(canvas, paint, width - stop - offset, height / 2, stop, cap == PenLineCap.Round);
            }
            finally { canvas.Restore(); }
        }
    }
}

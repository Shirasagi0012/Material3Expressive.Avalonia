// Portions translated from AndroidX TimePicker.kt (f3ed195f0c9e).
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
// AndroidX TimePicker.kt drawSelector: Clear -> content -> Xor -> line/dot -> DstOver.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Material3Expressive.Components;

internal sealed partial class ClockFace
{
    internal static readonly StyledProperty<IBrush?> SelectedTextBrushProperty =
        AvaloniaProperty.Register<ClockFace, IBrush?>(nameof(SelectedTextBrush));
    public IBrush? SelectedTextBrush { get => GetValue(SelectedTextBrushProperty); set => SetValue(SelectedTextBrushProperty, value); }
    internal static readonly StyledProperty<double> TrackWidthProperty = AvaloniaProperty.Register<ClockFace, double>(nameof(TrackWidth));
    public double TrackWidth { get => GetValue(TrackWidthProperty); set => SetValue(TrackWidthProperty, value); }

    private void DrawSelector(DrawingContext context, bool beforeContent)
    {
        if (SelectorBrush is not ISolidColorBrush selector || SelectedTextBrush is not ISolidColorBrush text) return;
        context.Custom(new SelectorDrawOperation(Bounds.Size, Density, SelectorPosition, SelectorRadius,
            (float)CenterSize / 2, (float)TrackWidth, _position, selector.Color, text.Color, beforeContent));
    }

    private sealed class ClockSelectorOverlay(ClockFace owner) : Control
    {
        public override void Render(DrawingContext context) => owner.DrawSelector(context, beforeContent: false);
    }

    private sealed class SelectorDrawOperation(Size size, float scale, Point position, float radius,
        float centerRadius, float trackWidth, float angle, Color selector, Color text, bool beforeContent) : ICustomDrawOperation
    {
        public Rect Bounds { get; } = new Rect(size);
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }
        public void Render(ImmediateDrawingContext context)
        {
            using var lease = (context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Material TimePicker requires the Avalonia.Skia renderer.")).Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            try
            {
                canvas.Scale(1 / scale);
                var x = (float)position.X * scale; var y = (float)position.Y * scale;
                var r = radius * scale;
                using var paint = new SKPaint { IsAntialias = true, Color = Ink(selector),
                    BlendMode = beforeContent ? SKBlendMode.Clear : SKBlendMode.Xor };
                canvas.DrawCircle(x, y, r, paint);
                if (beforeContent) return;
                paint.BlendMode = SKBlendMode.SrcOver; paint.StrokeWidth = trackWidth * scale;
                paint.StrokeCap = SKStrokeCap.Butt;
                var centerX = (float)size.Width * scale / 2; var centerY = (float)size.Height * scale / 2;
                canvas.DrawLine(centerX, centerY, x - r * MathF.Cos(angle), y - r * MathF.Sin(angle), paint);
                canvas.DrawCircle(centerX, centerY, centerRadius * scale, paint);
                paint.Color = Ink(text); paint.BlendMode = SKBlendMode.DstOver;
                canvas.DrawCircle(x, y, r, paint);
            }
            finally { canvas.Restore(); }
        }
        private static SKColor Ink(Color color) => new(color.R, color.G, color.B, color.A);
    }
}

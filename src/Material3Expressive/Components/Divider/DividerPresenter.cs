/*
 * Translated from AndroidX Divider.kt, Copyright 2022 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed baseline: docs/upstream-baseline.json.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using Material3Expressive.Foundation;
using SkiaSharp;

namespace Material3Expressive.Components;

// Template-only canvas. Keep layout's roundToPx separate from drawLine's thickness.toPx().
internal sealed class DividerPresenter : Control
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<DividerPresenter, Orientation>(nameof(Orientation));
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<DividerPresenter, double>(nameof(Thickness));
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<DividerPresenter, Color>(nameof(Color));
    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    static DividerPresenter()
    {
        AffectsMeasure<DividerPresenter>(OrientationProperty, ThicknessProperty);
        AffectsRender<DividerPresenter>(OrientationProperty, ThicknessProperty, ColorProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var thickness = Math.Max(0, MathUtils.DipToPx((float)Thickness, scale)) / (double)scale;
        // fillMaxWidth / fillMaxHeight does not fill an unbounded constraint.
        return Orientation == Orientation.Horizontal
            ? new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 0, Math.Min(thickness, availableSize.Height))
            : new Size(Math.Min(thickness, availableSize.Width), double.IsFinite(availableSize.Height) ? availableSize.Height : 0);
    }

    public override void Render(DrawingContext context)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        context.Custom(new DividerDrawOperation(Bounds.Size, scale, Orientation, (float)Thickness, Color));
    }

    private sealed class DividerDrawOperation(Size size, float scale, Orientation orientation, float thickness, Color color) : ICustomDrawOperation
    {
        // Hairlines and strokes under a tight constraint can extend outside the zero/small layout box.
        public Rect Bounds { get; } = new Rect(size).Union((orientation == Orientation.Horizontal
            ? new Rect(0, thickness / 2, size.Width, 0)
            : new Rect(thickness / 2, 0, 0, size.Height))
            .Inflate(Math.Max(1 / scale, thickness) / 2 + 1 / scale));
        public bool HitTest(Point point) => false;
        public bool Equals(ICustomDrawOperation? other) => ReferenceEquals(this, other);
        public void Dispose() { }

        public void Render(ImmediateDrawingContext context)
        {
            var feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>()
                ?? throw new NotSupportedException("Material Divider requires the Avalonia.Skia renderer.");
            using var lease = feature.Lease();
            var canvas = lease.SkCanvas;
            canvas.Save();
            try
            {
                canvas.Scale(1 / scale);
                var strokeWidth = thickness * scale;
                using var paint = new SKPaint
                {
                    IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = strokeWidth,
                    StrokeCap = SKStrokeCap.Butt, Color = new SKColor(color.R, color.G, color.B, color.A)
                };
                if (orientation == Orientation.Horizontal)
                    canvas.DrawLine(0, strokeWidth / 2, (float)size.Width * scale, strokeWidth / 2, paint);
                else
                    canvas.DrawLine(strokeWidth / 2, 0, strokeWidth / 2, (float)size.Height * scale, paint);
            }
            finally { canvas.Restore(); }
        }
    }
}

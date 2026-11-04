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
            (float)CenterSize / 2, (float)TrackWidth, _position, selector.Color, selector.Opacity, text.Color, text.Opacity, beforeContent));
    }

    private sealed class ClockSelectorOverlay : Control
    {
        private readonly ClockFace _owner;
        private AvaloniaObject[] _brushes = [];
        private bool _attached;
        static ClockSelectorOverlay() => AffectsRender<ClockSelectorOverlay>(SelectorBrushProperty, SelectedTextBrushProperty,
            CenterSizeProperty, HandleSizeProperty, TrackWidthProperty);
        public ClockSelectorOverlay(ClockFace owner)
        {
            _owner = owner;
            // The post-content pass has its own retained render list. Forward its inputs so
            // replacement AND mutable brush changes invalidate that list while the clock is idle.
            foreach (var property in new AvaloniaProperty[] { SelectorBrushProperty, SelectedTextBrushProperty, CenterSizeProperty, HandleSizeProperty, TrackWidthProperty })
                this.Bind(property, owner.GetObservable(property));
        }
        public override void Render(DrawingContext context) => _owner.DrawSelector(context, beforeContent: false);
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        { base.OnAttachedToVisualTree(e); _attached = true; ObserveBrushes(); }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        { _attached = false; ObserveBrushes(); base.OnDetachedFromVisualTree(e); }
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == SelectorBrushProperty || change.Property == SelectedTextBrushProperty) ObserveBrushes();
        }
        private void ObserveBrushes()
        {
            foreach (var brush in _brushes) brush.PropertyChanged -= BrushChanged;
            // Mutable brushes are compositor resources in Avalonia. Our draw operation stores
            // scalar snapshots, so those resource updates must also invalidate its render list.
            _brushes = _attached ? new[] { GetValue(SelectorBrushProperty), GetValue(SelectedTextBrushProperty) }
                .OfType<AvaloniaObject>().Distinct().ToArray() : [];
            foreach (var brush in _brushes) brush.PropertyChanged += BrushChanged;
        }
        private void BrushChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => InvalidateVisual();
    }

    private sealed class SelectorDrawOperation(Size size, float scale, Point position, float radius,
        float centerRadius, float trackWidth, float angle, Color selector, double selectorOpacity, Color text, double textOpacity, bool beforeContent) : ICustomDrawOperation
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
                using var paint = new SKPaint { IsAntialias = true, Color = Ink(selector, selectorOpacity * lease.CurrentOpacity),
                    BlendMode = beforeContent ? SKBlendMode.Clear : SKBlendMode.Xor };
                canvas.DrawCircle(x, y, r, paint);
                if (beforeContent) return;
                paint.BlendMode = SKBlendMode.SrcOver; paint.StrokeWidth = trackWidth * scale;
                paint.StrokeCap = SKStrokeCap.Butt;
                var centerX = (float)size.Width * scale / 2; var centerY = (float)size.Height * scale / 2;
                canvas.DrawLine(centerX, centerY, x - r * MathF.Cos(angle), y - r * MathF.Sin(angle), paint);
                canvas.DrawCircle(centerX, centerY, centerRadius * scale, paint);
                paint.Color = Ink(text, textOpacity * lease.CurrentOpacity); paint.BlendMode = SKBlendMode.DstOver;
                canvas.DrawCircle(x, y, r, paint);
            }
            finally { canvas.Restore(); }
        }
        private static SKColor Ink(Color color, double opacity) => new(color.R, color.G, color.B,
            (byte)Math.Clamp(Math.Round(color.A * opacity, MidpointRounding.AwayFromZero), 0, 255));
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Foundation.Overlays;

// Bitmap source rectangles use pixels, while the frozen layout and animation use DIP.
// Image's source.Size path mixes these units for a RenderTargetBitmap with non-96 DPI.
internal sealed class DialogSnapshot : Control, IDisposable
{
    private readonly Size _size;
    private readonly bool _bypassMirror;
    internal RenderTargetBitmap? Bitmap { get; }
    protected override bool BypassFlowDirectionPolicies => _bypassMirror;

    public DialogSnapshot(RenderTargetBitmap? bitmap, Size size, bool bypassMirror)
    {
        Bitmap = bitmap; _size = size; _bypassMirror = bypassMirror;
        Width = size.Width; Height = size.Height;
        HorizontalAlignment = HorizontalAlignment.Center; VerticalAlignment = VerticalAlignment.Center;
        RenderTransformOrigin = RelativePoint.Center;
    }

    protected override Size MeasureOverride(Size availableSize) => _size.Constrain(availableSize);
    public override void Render(DrawingContext context)
    {
        if (Bitmap is not null)
            context.DrawImage(Bitmap, new Rect(new Size(Bitmap.PixelSize.Width, Bitmap.PixelSize.Height)), new Rect(_size));
    }
    public void Dispose() => Bitmap?.Dispose();
}

// Core multiplies scrimColor.alpha before converting the color to sRGBA8. Applying
// opacity to an already quantized brush rounds at a different stage of compositing.
internal sealed class DialogScrim : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<DialogScrim>();
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public static readonly StyledProperty<float> PaintOpacityProperty = AvaloniaProperty.Register<DialogScrim, float>(nameof(PaintOpacity), 1);
    public float PaintOpacity { get => GetValue(PaintOpacityProperty); set => SetValue(PaintOpacityProperty, value); }
    static DialogScrim() => AffectsRender<DialogScrim>(BackgroundProperty, PaintOpacityProperty);
    public override void Render(DrawingContext context)
    {
        if (Background is ISolidColorBrush solid)
        {
            var alpha = (byte)Math.Clamp(MathF.Floor(solid.Color.A * PaintOpacity * (float)solid.Opacity + .5f), 0, 255);
            context.DrawRectangle(new ImmutableSolidColorBrush(Color.FromArgb(alpha, solid.Color.R, solid.Color.G, solid.Color.B)), null, new Rect(Bounds.Size));
        }
        else
        {
            using (context.PushOpacity(PaintOpacity)) context.DrawRectangle(Background, null, new Rect(Bounds.Size));
        }
    }
}

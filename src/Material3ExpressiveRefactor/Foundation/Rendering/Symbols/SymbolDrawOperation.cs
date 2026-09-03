using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using SkiaSharp;

namespace Material3ExpressiveRefactor.Foundation.Rendering.Symbols;

/// <summary>
/// Draws one symbol by leasing Avalonia's Skia canvas and filling the glyph outline.
/// </summary>
/// <remarks>
/// <para>
/// The font is resolved when the operation is built — on the UI thread — so a missing family
/// package surfaces as an ordinary exception from the control's render pass rather than as a fault
/// on the render thread. Outline extraction is left until <see cref="Render"/>, which runs on the
/// render thread, to keep the roughly half-millisecond cost of a cold variable-font instance off
/// the thread that handles input.
/// </para>
/// <para>
/// The axis position is held quantized rather than raw. Two frames of an animation whose axis
/// values round to the same step draw identical pixels, so holding the key is what lets
/// <see cref="Equals(ICustomDrawOperation)"/> report them as the same operation and lets Avalonia
/// skip the redraw. Comparing raw doubles would make every frame of a sweep look distinct.
/// </para>
/// </remarks>
internal sealed class SymbolDrawOperation : ICustomDrawOperation
{
    private readonly SymbolTypeface _typeface;
    private readonly int _codepoint;
    private readonly VariationKey _variation;
    private readonly Color _color;

    public SymbolDrawOperation(
        Rect bounds,
        SymbolTypeface typeface,
        int codepoint,
        VariationKey variation,
        Color color)
    {
        Bounds = bounds;
        _typeface = typeface;
        _codepoint = codepoint;
        _variation = variation;
        _color = color;
    }

    public Rect Bounds { get; }

    public bool HitTest(Point p) => Bounds.Contains(p);

    public void Render(ImmediateDrawingContext context)
    {
        // Symbols are square, so the box is the largest square that fits the control.
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;

        if (context.TryGetFeature<ISkiaSharpApiLeaseFeature>() is not { } feature)
            return;

        using var path = _typeface.GetGlyphPath(_codepoint, _variation);
        if (path is null)
            return;

        using var lease = feature.Lease();
        var canvas = lease.SkCanvas;

        // Opacity pushed by ancestors is handed to the lease rather than baked into the canvas,
        // so a custom operation has to apply it itself.
        var alpha = (byte)Math.Clamp(Math.Round(_color.A * lease.CurrentOpacity), 0, 255);
        if (alpha == 0)
            return;

        using var paint = new SKPaint
        {
            Color = new SKColor(_color.R, _color.G, _color.B, alpha),
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
        };

        // The outline arrives in canonical units on the em square x ∈ [0, em], y ∈ [-em, 0], with
        // the glyph origin on the baseline. Put that box in the middle of the control.
        var left = Bounds.X + (Bounds.Width - size) / 2d;
        var top = Bounds.Y + (Bounds.Height - size) / 2d;
        var scale = (float)(size / SymbolTypeface.CanonicalSize);

        canvas.Save();
        canvas.Translate((float)left, (float)(top + size));
        canvas.Scale(scale);
        canvas.DrawPath(path, paint);
        canvas.Restore();
    }

    public bool Equals(ICustomDrawOperation? other) =>
        other is SymbolDrawOperation o
        && Bounds.Equals(o.Bounds)
        && ReferenceEquals(_typeface, o._typeface)
        && _codepoint == o._codepoint
        && _variation.Equals(o._variation)
        && _color.Equals(o._color);

    public void Dispose()
    {
        // Nothing owned: the typeface is the shared Rounded instance and the path is disposed in Render.
    }
}

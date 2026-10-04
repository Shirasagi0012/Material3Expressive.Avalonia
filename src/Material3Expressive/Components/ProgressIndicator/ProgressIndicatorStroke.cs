using Avalonia.Media;
using SkiaSharp;

namespace Material3Expressive.Components;

/// <summary>Compose Stroke in DIP. PathEffect uses physical pixels and remains caller-owned.</summary>
public sealed record ProgressIndicatorStroke(double Width = 4, PenLineCap Cap = PenLineCap.Round,
    PenLineJoin Join = PenLineJoin.Miter, double Miter = 4, SKPathEffect? PathEffect = null)
{
    public ProgressIndicatorStroke() : this(4) { }
}

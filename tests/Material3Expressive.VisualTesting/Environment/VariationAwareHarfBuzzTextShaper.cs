using System.Reflection;
using Avalonia.Harfbuzz;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using HarfBuzzSharp;
using SkiaSharp;

namespace Material3Expressive.VisualTesting;

/// <summary>
/// Parity-host compatibility shim for Avalonia 12.1's HarfBuzz adapter.
/// </summary>
/// <remarks>
/// Avalonia's stock adapter creates an <see cref="HarfBuzzSharp.Font"/> from the raw OpenType
/// tables exposed by a platform typeface, but does not forward a Skia variable-font instance's
/// design coordinates. Consequently, its outlines can be wght=500 while its HarfBuzz advances are
/// calculated at the font default. Delegate all shaping to Avalonia's implementation and inject
/// only the missing coordinates into its per-<see cref="GlyphTypeface"/> cached font.
/// </remarks>
internal sealed class VariationAwareHarfBuzzTextShaper : ITextShaperImpl
{
    private const BindingFlags InstanceProperty = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    private readonly HarfBuzzTextShaper _inner = new();

    public ITextShaperTypeface CreateTypeface(GlyphTypeface glyphTypeface)
    {
        var shaperTypeface = _inner.CreateTypeface(glyphTypeface);
        var variations = GetVariationCoordinates(glyphTypeface);
        if (variations.Length == 0)
            return shaperTypeface;

        var fontProperty = shaperTypeface.GetType().GetProperty("HBFont", InstanceProperty)
            ?? throw new InvalidOperationException(
                $"Avalonia HarfBuzz typeface '{shaperTypeface.GetType().FullName}' no longer exposes HBFont.");
        if (fontProperty.GetValue(shaperTypeface) is not Font harfBuzzFont)
        {
            throw new InvalidOperationException(
                $"Avalonia HarfBuzz typeface '{shaperTypeface.GetType().FullName}' returned no HBFont.");
        }

        harfBuzzFont.SetVariations(variations);
        return shaperTypeface;
    }

    public ShapedBuffer ShapeText(ReadOnlyMemory<char> text, TextShaperOptions options) =>
        _inner.ShapeText(text, options);

    private static Variation[] GetVariationCoordinates(GlyphTypeface glyphTypeface)
    {
        var skTypefaceProperty = glyphTypeface.PlatformTypeface.GetType().GetProperty(
            "SKTypeface",
            InstanceProperty);
        if (skTypefaceProperty?.GetValue(glyphTypeface.PlatformTypeface) is not SKTypeface skTypeface)
            return [];

        return skTypeface.VariationDesignPosition
            .Select(position => new Variation
            {
                Tag = (uint)position.Axis,
                Value = position.Value
            })
            .ToArray();
    }
}

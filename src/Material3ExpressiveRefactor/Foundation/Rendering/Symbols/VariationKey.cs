using MaterialSymbols.Avalonia;

namespace Material3ExpressiveRefactor.Foundation.Rendering.Symbols;

/// <summary>
/// A <see cref="SymbolVariation"/> quantized down to the granularity a viewer can actually see,
/// so that a spring-driven <c>Fill</c> settles onto a bounded set of cache keys instead of minting
/// a fresh variable-font instance on every frame.
/// </summary>
/// <remarks>
/// Fill resolves to 1/100 of the axis (101 distinct instances across the whole 0→1 sweep); weight,
/// grade and optical size resolve to whole axis units. Building an instance costs roughly half a
/// millisecond, so bounding the key space is what keeps an animation from melting a frame budget.
/// </remarks>
internal readonly record struct VariationKey(short Weight, short Fill, short Grade, short OpticalSize)
{
    /// <summary>Number of quantization steps across the <c>FILL</c> axis.</summary>
    public const int FillSteps = 100;

    /// <summary>Quantizes <paramref name="variation"/>, which must already be clamped.</summary>
    public static VariationKey From(SymbolVariation variation) => new(
        (short)Math.Round(variation.Weight),
        (short)Math.Round(variation.Fill * FillSteps),
        (short)Math.Round(variation.Grade),
        (short)Math.Round(variation.OpticalSize));

    /// <summary>The axis values this key stands for.</summary>
    public SymbolVariation ToVariation() =>
        new(Weight, Fill / (double)FillSteps, Grade, OpticalSize);
}

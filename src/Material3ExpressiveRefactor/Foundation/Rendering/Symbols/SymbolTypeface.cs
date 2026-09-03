using Avalonia;
using Avalonia.Platform;
using MaterialSymbols.Avalonia;
using SkiaSharp;

namespace Material3ExpressiveRefactor.Foundation.Rendering.Symbols;

/// <summary>
/// One Material Symbols variable font, plus the caches that make animating its axes affordable.
/// </summary>
/// <remarks>
/// <para>
/// Glyph outlines come back from <see cref="SKFont.GetGlyphPath(ushort)"/> in text space: the glyph
/// origin sits on the baseline at x=0 and y grows downward, so ink above the baseline is negative.
/// Material Symbols are drawn on the em square — the advance is exactly 1em and the design box is
/// x ∈ [0, em], y ∈ [-em, 0] — which is why <see cref="CanonicalSize"/> can stand in for "one em"
/// throughout and every symbol lands in the same box regardless of which axes are applied. Using
/// the em box rather than each glyph's tight bounds is what keeps a symbol from jittering as
/// <c>FILL</c> animates.
/// </para>
/// <para>
/// Outlines scale exactly linearly with text size (verified against the shipped fonts: extracting
/// at one size and scaling matches extracting at another to the last float). Paths are therefore
/// pulled once at <see cref="CanonicalSize"/> and scaled by the canvas at draw time, so render
/// scaling costs nothing and never splits the cache.
/// </para>
/// </remarks>
internal sealed class SymbolTypeface : IDisposable
{
    /// <summary>
    /// The text size glyph outlines are extracted at. Every path this class hands out is in these
    /// units; scale by <c>size / CanonicalSize</c> to draw at <c>size</c>.
    /// </summary>
    public const float CanonicalSize = 128f;

    private static readonly SKFourByteTag FillAxis = SKFourByteTag.Parse("FILL");
    private static readonly SKFourByteTag WeightAxis = SKFourByteTag.Parse("wght");
    private static readonly SKFourByteTag GradeAxis = SKFourByteTag.Parse("GRAD");
    private static readonly SKFourByteTag OpticalSizeAxis = SKFourByteTag.Parse("opsz");

    // 101 fill steps at one weight/grade/opsz combination is the common animation case; the extra
    // headroom covers a few controls animating different weights at once.
    private const int InstanceCacheCapacity = 256;

    private static readonly Uri RoundedFontUri =
        new("avares://MaterialSymbols.Avalonia.Rounded/Assets/MaterialSymbolsRounded.ttf");

    private static readonly Lazy<SymbolTypeface> RoundedHolder = new(LoadRounded);

    private readonly SKTypeface _typeface;
    private readonly SKFont _lookupFont;
    private readonly Lock _glyphGate = new();
    private readonly Dictionary<int, ushort> _glyphs = new();

    private SymbolTypeface(SKTypeface typeface)
    {
        _typeface = typeface;
        _lookupFont = Configure(new SKFont(typeface, CanonicalSize));
        Instances = new LruCache<VariationKey, SKFont>(InstanceCacheCapacity);
    }

    /// <summary>
    /// The Material Symbols Rounded family — the one family this library draws with. Loaded on
    /// first use, so an app that never shows a symbol never pays the font load.
    /// </summary>
    public static SymbolTypeface Rounded => RoundedHolder.Value;

    /// <summary>The variable-font instances built so far. Exposed so tests can assert cache behaviour.</summary>
    public LruCache<VariationKey, SKFont> Instances { get; }

    /// <summary>The font's own family name, e.g. <c>Material Symbols Rounded</c>.</summary>
    public string FamilyName => _typeface.FamilyName;

    /// <summary>Loads a variable font from <paramref name="stream"/>, which is consumed in full.</summary>
    /// <exception cref="InvalidOperationException">The stream does not contain a usable font.</exception>
    public static SymbolTypeface Load(Stream stream)
    {
        var typeface = SKTypeface.FromStream(stream)
            ?? throw new InvalidOperationException(
                "The stream did not contain a font SkiaSharp could load.");

        return new SymbolTypeface(typeface);
    }

    private static SymbolTypeface LoadRounded()
    {
        // typeof() forces the family assembly to load, which is what publishes its Avalonia
        // resource index to the AssetLoader; a const reference (RoundedSymbols.FamilyName) would
        // be inlined at compile time and load nothing.
        _ = typeof(MaterialSymbols.Avalonia.RoundedSymbols);
        return Load(AssetLoader.Open(RoundedFontUri));
    }

    /// <summary>Maps a Unicode codepoint to a glyph id through the font's cmap. 0 means absent.</summary>
    public ushort GetGlyph(int codepoint)
    {
        lock (_glyphGate)
        {
            if (_glyphs.TryGetValue(codepoint, out var cached))
                return cached;

            var glyph = _lookupFont.GetGlyph(codepoint);
            _glyphs[codepoint] = glyph;
            return glyph;
        }
    }

    /// <summary>
    /// Extracts the outline of <paramref name="codepoint"/> at <paramref name="variation"/>, in
    /// <see cref="CanonicalSize"/> units. Returns <see langword="null"/> when the font has no glyph
    /// for the codepoint or the glyph has no ink. The caller owns the returned path.
    /// </summary>
    /// <param name="codepoint">The symbol's codepoint.</param>
    /// <param name="variation">Axis values; must already be clamped to the font's ranges.</param>
    public SKPath? GetGlyphPath(int codepoint, SymbolVariation variation) =>
        GetGlyphPath(codepoint, VariationKey.From(variation));

    /// <summary>
    /// Extracts the outline of <paramref name="codepoint"/> at the quantized axis position
    /// <paramref name="key"/>. This is the form the renderer uses: the key is what actually decides
    /// the pixels, so it is also what draw operations compare on.
    /// </summary>
    public SKPath? GetGlyphPath(int codepoint, VariationKey key)
    {
        var glyph = GetGlyph(codepoint);
        if (glyph == 0)
            return null;

        var font = GetInstance(key);
        var path = font.GetGlyphPath(glyph);
        if (path is null)
            return null;

        if (path.PointCount != 0)
            return path;

        path.Dispose();
        return null;
    }

    private SKFont GetInstance(VariationKey key) =>
        Instances.GetOrAdd(key, static (k, self) => self.CreateInstance(k), this);

    private SKFont CreateInstance(VariationKey key)
    {
        var variation = key.ToVariation();

        // SKTypeface.Clone takes the coordinates by span, so they can live on the stack.
        Span<SKFontVariationPositionCoordinate> coordinates =
        [
            new() { Axis = FillAxis, Value = (float)variation.Fill },
            new() { Axis = WeightAxis, Value = (float)variation.Weight },
            new() { Axis = GradeAxis, Value = (float)variation.Grade },
            new() { Axis = OpticalSizeAxis, Value = (float)variation.OpticalSize },
        ];

        var instance = _typeface.Clone(coordinates) ?? _typeface;
        return Configure(new SKFont(instance, CanonicalSize));
    }

    /// <summary>
    /// Turns off everything that would make an outline depend on anything but the design and the
    /// text size — hinting in particular, which is what keeps extraction exactly linear in size.
    /// </summary>
    private static SKFont Configure(SKFont font)
    {
        font.Hinting = SKFontHinting.None;
        font.Subpixel = true;
        font.LinearMetrics = true;
        return font;
    }

    /// <summary>Releases the base typeface. Cached instances are left to the finalizer; see <see cref="LruCache{TKey,TValue}"/>.</summary>
    public void Dispose()
    {
        Instances.Clear();
        _lookupFont.Dispose();
        _typeface.Dispose();
    }
}

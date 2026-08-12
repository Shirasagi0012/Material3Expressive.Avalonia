using Avalonia.Media;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Stable strongly typed keys for Material reference-palette tones.</summary>
/// <remarks>
/// Keys are cached by palette, tone and (for <see cref="RefPaletteToken.Custom"/>) custom palette
/// name. Custom names use the same case-insensitive identity as the color-scheme lookup.
/// </remarks>
public static class ReferencePaletteTokens
{
    private static readonly object Gate = new();
    private static readonly Dictionary<Key, MaterialToken<Color>> Tokens = [];
    private static readonly Dictionary<MaterialToken, Metadata> Reverse = [];

    /// <summary>Gets the stable reference token for a palette tone.</summary>
    /// <param name="palette">The reference tonal palette.</param>
    /// <param name="tone">A tone in the inclusive range 0..100.</param>
    /// <param name="customKey">
    /// The custom-palette name for <see cref="RefPaletteToken.Custom"/>; ignored for standard
    /// palettes.
    /// </param>
    public static MaterialToken<Color> Get(
        RefPaletteToken palette,
        byte tone,
        string? customKey = null)
    {
        if (tone > 100)
            throw new ArgumentOutOfRangeException(nameof(tone), tone, "Tone must be in range 0..100.");

        if (palette != RefPaletteToken.Custom)
            customKey = null;

        var key = new Key(palette, tone, customKey);
        lock (Gate)
        {
            if (Tokens.TryGetValue(key, out var token))
                return token;

            var customSegment = customKey is { Length: > 0 } ? $".{customKey}" : string.Empty;
            token = new MaterialToken<Color>(
                $"Reference.Palette.{palette}{customSegment}.{tone}",
                TokenLayer.Reference,
                Token.Literal(Colors.Transparent));
            Tokens.Add(key, token);
            Reverse.Add(token, new Metadata(palette, tone, customKey));
            return token;
        }
    }

    /// <summary>Gets palette metadata for a key created by this catalog.</summary>
    public static bool TryGetMetadata(
        MaterialToken token,
        out RefPaletteToken palette,
        out byte tone,
        out string? customKey)
    {
        ArgumentNullException.ThrowIfNull(token);
        lock (Gate)
        {
            if (Reverse.TryGetValue(token, out var metadata))
            {
                palette = metadata.Palette;
                tone = metadata.Tone;
                customKey = metadata.CustomKey;
                return true;
            }
        }

        palette = default;
        tone = default;
        customKey = null;
        return false;
    }

    private readonly struct Key : IEquatable<Key>
    {
        internal Key(RefPaletteToken palette, byte tone, string? customKey)
        {
            Palette = palette;
            Tone = tone;
            CustomKey = customKey;
        }

        private RefPaletteToken Palette { get; }
        private byte Tone { get; }
        private string? CustomKey { get; }

        public bool Equals(Key other) =>
            Palette == other.Palette && Tone == other.Tone &&
            StringComparer.OrdinalIgnoreCase.Equals(CustomKey, other.CustomKey);

        public override bool Equals(object? obj) => obj is Key other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Palette,
            Tone,
            CustomKey is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(CustomKey));
    }

    private readonly record struct Metadata(
        RefPaletteToken Palette,
        byte Tone,
        string? CustomKey);
}

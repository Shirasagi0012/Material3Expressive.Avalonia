using Avalonia.Media;
using MaterialColorUtilities.Avalonia.Tokens;
using SharedTokens = MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Compatibility forwarding facade for MCU-owned reference-palette keys.</summary>
[Obsolete(
    "Reference-palette token identity is owned by MaterialColorUtilities.Avalonia. Use its " +
    "ReferencePaletteTokens API directly.",
    DiagnosticId = "M3E1003")]
public static class ReferencePaletteTokens
{
    /// <summary>Gets the MCU canonical descriptor for a palette tone.</summary>
    public static SharedTokens.MaterialToken<Color> Get(
        RefPaletteToken palette,
        byte tone,
        string? customKey = null) =>
        MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens.Get(
            palette, tone, customKey);

    /// <summary>Gets the MCU canonical descriptor for a fractional palette tone.</summary>
    public static SharedTokens.MaterialToken<Color> Get(
        RefPaletteToken palette,
        double tone,
        string? customKey = null) =>
        MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens.Get(
            palette, tone, customKey);

    /// <summary>Gets compatibility metadata for an integral-tone descriptor.</summary>
    public static bool TryGetMetadata(
        SharedTokens.MaterialToken token,
        out RefPaletteToken palette,
        out byte tone,
        out string? customKey)
    {
        if (MaterialColorUtilities.Avalonia.Tokens.ReferencePaletteTokens.TryGetMetadata(
                token, out palette, out var rawTone, out customKey) &&
            rawTone == Math.Truncate(rawTone) && rawTone is >= byte.MinValue and <= byte.MaxValue)
        {
            tone = (byte)rawTone;
            return true;
        }

        palette = default;
        tone = default;
        customKey = null;
        return false;
    }
}

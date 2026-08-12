using Avalonia.Media;
using Avalonia.Metadata;
using Material3Expressive.Foundation.Tokens.Runtime;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Markups;

/// <summary>Returns a reference-palette token key for use by <see cref="TokenRefExtension"/>.</summary>
/// <example>
/// <c>{m3:TokenRef Token={m3:ReferencePaletteToken Primary, 40}}</c>
/// </example>
public sealed class ReferencePaletteTokenExtension
{
    public ReferencePaletteTokenExtension()
    {
    }

    public ReferencePaletteTokenExtension(RefPaletteToken palette, byte tone)
    {
        Palette = palette;
        Tone = tone;
    }

    [ConstructorArgument("palette")]
    public RefPaletteToken Palette { get; set; }

    [ConstructorArgument("tone")]
    public byte Tone
    {
        get;
        set => field = value <= 100
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "Tone must be in range 0..100.");
    } = 40;

    /// <summary>The named custom palette; ignored unless <see cref="Palette"/> is Custom.</summary>
    public string? CustomKey { get; set; }

    public MaterialToken<Color> ProvideValue() =>
        ReferencePaletteTokens.Get(Palette, Tone, CustomKey);
}

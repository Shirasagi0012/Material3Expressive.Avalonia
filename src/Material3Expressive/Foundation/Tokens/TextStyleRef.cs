using Avalonia.Media;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Assembles a typography role from scalar tokens at the consuming resource scope.</summary>
public sealed record TextStyleRef(
    MaterialTokenKey<FontFamily> Font,
    MaterialTokenKey<double> Size,
    MaterialTokenKey<FontWeight> Weight,
    MaterialTokenKey<double> LetterSpacing,
    MaterialTokenKey<double> LineHeight);

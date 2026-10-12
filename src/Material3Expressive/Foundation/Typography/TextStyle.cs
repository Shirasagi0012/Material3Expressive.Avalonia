using Avalonia.Media;

namespace Material3Expressive.Foundation.Typography;

/// <summary>A shareable text style. Null fields are unspecified; all lengths are DIPs.</summary>
public sealed record TextStyle
{
    public FontFamily? FontFamily { get; init; }
    public double? FontSize { get; init; }
    public FontWeight? FontWeight { get; init; }
    public FontStyle? FontStyle { get; init; }
    public FontStretch? FontStretch { get; init; }
    public double? LetterSpacing { get; init; }
    /// <summary>Null is unspecified; NaN explicitly selects automatic line height.</summary>
    public double? LineHeight { get; init; }
    public TextAlignment? TextAlignment { get; init; }
    public IBrush? Foreground { get; init; }
    public FontFeatureCollection? FontFeatures { get; init; }

    public static TextStyle Empty { get; } = new();

    /// <summary>Compose TextStyle.merge: only specified fields in overrides replace this value.</summary>
    public TextStyle Merge(TextStyle? overrides) => overrides is null ? this : this with
    {
        FontFamily = overrides.FontFamily ?? FontFamily,
        FontSize = overrides.FontSize ?? FontSize,
        FontWeight = overrides.FontWeight ?? FontWeight,
        FontStyle = overrides.FontStyle ?? FontStyle,
        FontStretch = overrides.FontStretch ?? FontStretch,
        LetterSpacing = overrides.LetterSpacing ?? LetterSpacing,
        LineHeight = overrides.LineHeight ?? LineHeight,
        TextAlignment = overrides.TextAlignment ?? TextAlignment,
        Foreground = overrides.Foreground ?? Foreground,
        FontFeatures = overrides.FontFeatures ?? FontFeatures,
    };
}

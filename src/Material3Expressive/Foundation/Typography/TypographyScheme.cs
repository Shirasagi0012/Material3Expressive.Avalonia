using Avalonia;
using Avalonia.Collections;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// The typefaces and weights a theme is set in.
/// </summary>
/// <remarks>
/// The per-slot metrics are not here: they live in the <c>TypeScale</c> system token table, and an
/// app retunes an individual slot with a <see cref="TypeScaleOverrides" /> in this scheme. What remains
/// is the part that is genuinely scheme-wide — which four faces and three weights the fifteen
/// slots draw on.
/// <para>
/// The emphasized faces default to the plain ones, matching upstream, where the emphasized styles
/// differ only by weight. They exist separately so an app can point them at a distinct face.
/// </para>
/// </remarks>
[DoNotGenerateOnPropertyChanged]
public partial class TypographyScheme : AvaloniaObject
{
    /// <summary>
    /// The type scale slot an element and its subtree are set in.
    /// </summary>
    /// <remarks>
    /// Written out rather than generated because <see cref="TypographyRole" /> comes from the
    /// <c>TypeScale</c> token table, and one source generator cannot see another's output.
    /// </remarks>
    public static readonly AttachedProperty<TypographyRole> TextStyleProperty =
        AvaloniaProperty.RegisterAttached<TypographyScheme, StyledElement, TypographyRole>(
            "TextStyle", TypographyRole.BodyMedium, inherits: true);

    // Keep defaults in metadata so the compatibility Raw bridge can distinguish untouched slots
    // from explicit local authoring without making every nested typography scheme dense.
    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultBrandFontFamily))]
    public partial FontFamily BrandFontFamily { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultPlainFontFamily))]
    public partial FontFamily PlainFontFamily { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultEmphasizedBrandFontFamily))]
    public partial FontFamily EmphasizedBrandFontFamily { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultEmphasizedPlainFontFamily))]
    public partial FontFamily EmphasizedPlainFontFamily { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultWeightBold))]
    public partial FontWeight WeightBold { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultWeightMedium))]
    public partial FontWeight WeightMedium { get; set; }

    [GeneratedStyledProperty(DefaultValueCallback = nameof(GetDefaultWeightRegular))]
    public partial FontWeight WeightRegular { get; set; }

    private static FontFamily GetDefaultBrandFontFamily() => TypefaceTokens.Brand;
    private static FontFamily GetDefaultPlainFontFamily() => TypefaceTokens.Plain;
    private static FontFamily GetDefaultEmphasizedBrandFontFamily() => TypefaceTokens.EmphasizedBrand;
    private static FontFamily GetDefaultEmphasizedPlainFontFamily() => TypefaceTokens.EmphasizedPlain;
    private static FontWeight GetDefaultWeightBold() => TypefaceTokens.WeightBold;
    private static FontWeight GetDefaultWeightMedium() => TypefaceTokens.WeightMedium;
    private static FontWeight GetDefaultWeightRegular() => TypefaceTokens.WeightRegular;

    /// <summary>
    /// Sparse per-slot system typography overrides.
    /// </summary>
    public AvaloniaList<TypeScaleOverrides> TypeScaleOverrides { get; } = new();

    public static TypographyRole GetTextStyle(StyledElement element)
    {
        return element.GetValue(TextStyleProperty);
    }

    public static void SetTextStyle(StyledElement element, TypographyRole value)
    {
        element.SetValue(TextStyleProperty, value);
    }

    public object ProvideValue()
    {
        return this;
    }
}

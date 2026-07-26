using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

/// <summary>
/// The typefaces and weights a theme is set in.
/// </summary>
/// <remarks>
/// The per-slot metrics are not here: they live in the <c>TypeScale</c> token table, and an app
/// retunes an individual slot with a <see cref="TypeScaleOverrides" /> on the theme. What remains
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

    public TypographyScheme()
    {
        BrandFontFamily = TypefaceTokens.Brand;
        PlainFontFamily = TypefaceTokens.Plain;
        EmphasizedBrandFontFamily = TypefaceTokens.EmphasizedBrand;
        EmphasizedPlainFontFamily = TypefaceTokens.EmphasizedPlain;
        WeightBold = TypefaceTokens.WeightBold;
        WeightMedium = TypefaceTokens.WeightMedium;
        WeightRegular = TypefaceTokens.WeightRegular;
    }

    [GeneratedStyledProperty] public partial FontFamily BrandFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontFamily PlainFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontFamily EmphasizedBrandFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontFamily EmphasizedPlainFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontWeight WeightBold { get; set; }
    [GeneratedStyledProperty] public partial FontWeight WeightMedium { get; set; }
    [GeneratedStyledProperty] public partial FontWeight WeightRegular { get; set; }

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

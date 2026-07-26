using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Foundation;

[DoNotGenerateOnPropertyChanged]
[GenerateAttachedProperty<StyledElement, TypographyRole>("TextStyle", Inherits = true, DefaultValue = TypographyRole.BodyMedium)]
public partial class TypographyScheme : AvaloniaObject
{
    public TypographyScheme()
    {
        BrandFontFamily = TypefaceTokens.Brand;
        PlainFontFamily = TypefaceTokens.Plain;        
        EmphasizedBrandFontFamily = TypefaceTokens.EmphasizedBrand;
        EmphasizedPlainFontFamily = TypefaceTokens.EmphasizedPlain;
        WeightBold = TypefaceTokens.WeightBold;
        WeightMedium = TypefaceTokens.WeightMedium;
        WeightRegular = TypefaceTokens.WeightRegular;
        DisplayLarge = TypeScaleTokens.DisplayLarge;
        DisplayMedium = TypeScaleTokens.DisplayMedium;
        DisplaySmall = TypeScaleTokens.DisplaySmall;
        HeadlineLarge = TypeScaleTokens.HeadlineLarge;
        HeadlineMedium = TypeScaleTokens.HeadlineMedium;
        HeadlineSmall = TypeScaleTokens.HeadlineSmall;
        TitleLarge = TypeScaleTokens.TitleLarge;
        TitleMedium = TypeScaleTokens.TitleMedium;
        TitleSmall = TypeScaleTokens.TitleSmall;
        BodyLarge = TypeScaleTokens.BodyLarge;
        BodyMedium = TypeScaleTokens.BodyMedium;
        BodySmall = TypeScaleTokens.BodySmall;
        LabelLarge = TypeScaleTokens.LabelLarge;
        LabelMedium = TypeScaleTokens.LabelMedium;
        LabelSmall = TypeScaleTokens.LabelSmall;
        DisplayLargeEmphasized = TypeScaleTokens.DisplayLargeEmphasized;
        DisplayMediumEmphasized = TypeScaleTokens.DisplayMediumEmphasized;
        DisplaySmallEmphasized = TypeScaleTokens.DisplaySmallEmphasized;
        HeadlineLargeEmphasized = TypeScaleTokens.HeadlineLargeEmphasized;
        HeadlineMediumEmphasized = TypeScaleTokens.HeadlineMediumEmphasized;
        HeadlineSmallEmphasized = TypeScaleTokens.HeadlineSmallEmphasized;
        TitleLargeEmphasized = TypeScaleTokens.TitleLargeEmphasized;
        TitleMediumEmphasized = TypeScaleTokens.TitleMediumEmphasized;
        TitleSmallEmphasized = TypeScaleTokens.TitleSmallEmphasized;
        BodyLargeEmphasized = TypeScaleTokens.BodyLargeEmphasized;
        BodyMediumEmphasized = TypeScaleTokens.BodyMediumEmphasized;
        BodySmallEmphasized = TypeScaleTokens.BodySmallEmphasized;
        LabelLargeEmphasized = TypeScaleTokens.LabelLargeEmphasized;
        LabelMediumEmphasized = TypeScaleTokens.LabelMediumEmphasized;
        LabelSmallEmphasized = TypeScaleTokens.LabelSmallEmphasized;
    }

    // Props
    [GeneratedStyledProperty] public partial FontFamily BrandFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontFamily PlainFontFamily { get; set; }  
    [GeneratedStyledProperty] public partial FontFamily EmphasizedBrandFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontFamily EmphasizedPlainFontFamily { get; set; }
    [GeneratedStyledProperty] public partial FontWeight WeightBold { get; set; }
    [GeneratedStyledProperty] public partial FontWeight WeightMedium { get; set; }
    [GeneratedStyledProperty] public partial FontWeight WeightRegular { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken DisplayLarge { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken DisplayMedium { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken DisplaySmall { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken HeadlineLarge { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken HeadlineMedium { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken HeadlineSmall { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken TitleLarge { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken TitleMedium { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken TitleSmall { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken BodyLarge { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken BodyMedium { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken BodySmall { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken LabelLarge { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken LabelMedium { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken LabelSmall { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken DisplayLargeEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken DisplayMediumEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken DisplaySmallEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken HeadlineLargeEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken HeadlineMediumEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken HeadlineSmallEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken TitleLargeEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken TitleMediumEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken TitleSmallEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken BodyLargeEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken BodyMediumEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken BodySmallEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken LabelLargeEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken LabelMediumEmphasized { get; set; }
    [GeneratedStyledProperty] public partial TypeScaleToken LabelSmallEmphasized { get; set; }

    internal TypeScaleToken GetTypeScale(TypographyRole role)
    {
        return role switch
        {
            TypographyRole.DisplayLarge => DisplayLarge,
            TypographyRole.DisplayMedium => DisplayMedium,
            TypographyRole.DisplaySmall => DisplaySmall,
            TypographyRole.HeadlineLarge => HeadlineLarge,
            TypographyRole.HeadlineMedium => HeadlineMedium,
            TypographyRole.HeadlineSmall => HeadlineSmall,
            TypographyRole.TitleLarge => TitleLarge,
            TypographyRole.TitleMedium => TitleMedium,
            TypographyRole.TitleSmall => TitleSmall,
            TypographyRole.BodyLarge => BodyLarge,
            TypographyRole.BodyMedium => BodyMedium,
            TypographyRole.BodySmall => BodySmall,
            TypographyRole.LabelLarge => LabelLarge,
            TypographyRole.LabelMedium => LabelMedium,
            TypographyRole.LabelSmall => LabelSmall,

            TypographyRole.DisplayLargeEmphasized => DisplayLargeEmphasized,
            TypographyRole.DisplayMediumEmphasized => DisplayMediumEmphasized,
            TypographyRole.DisplaySmallEmphasized => DisplaySmallEmphasized,
            TypographyRole.HeadlineLargeEmphasized => HeadlineLargeEmphasized,
            TypographyRole.HeadlineMediumEmphasized => HeadlineMediumEmphasized,
            TypographyRole.HeadlineSmallEmphasized => HeadlineSmallEmphasized,
            TypographyRole.TitleLargeEmphasized => TitleLargeEmphasized,
            TypographyRole.TitleMediumEmphasized => TitleMediumEmphasized,
            TypographyRole.TitleSmallEmphasized => TitleSmallEmphasized,
            TypographyRole.BodyLargeEmphasized => BodyLargeEmphasized,
            TypographyRole.BodyMediumEmphasized => BodyMediumEmphasized,
            TypographyRole.BodySmallEmphasized => BodySmallEmphasized,
            TypographyRole.LabelLargeEmphasized => LabelLargeEmphasized,
            TypographyRole.LabelMediumEmphasized => LabelMediumEmphasized,
            TypographyRole.LabelSmallEmphasized => LabelSmallEmphasized,

            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
    
    public object ProvideValue()
    {
        return this;
    }
}

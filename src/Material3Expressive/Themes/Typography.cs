using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Media;
using Material3Expressive.Themes.Tokens;
using PropertyGenerator.Avalonia;

namespace Material3Expressive.Themes;

[DoNotGenerateOnPropertyChanged]
public partial class Typography : AvaloniaObject
{
    internal static readonly AttachedProperty<Typography?> TypographyHostProperty =
        AvaloniaProperty.RegisterAttached<Typography, AvaloniaObject, Typography?>(
            "TypographyHost",
            inherits: true);

    internal static void SetTypographyHost(AvaloniaObject element, Typography? value)
    {
        element.SetValue(TypographyHostProperty, value);
    }

    internal static Typography? GetTypographyHost(AvaloniaObject element)
    {
        return element.GetValue(TypographyHostProperty);
    }

    public Typography()
    {
        BrandFontFamily = TypefaceTokens.Brand;
        PlainFontFamily = TypefaceTokens.Plain;
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

    internal TypeScaleToken GetTypeScale(TypographyRoleToken role)
    {
        return role switch
        {
            TypographyRoleToken.DisplayLarge => DisplayLarge,
            TypographyRoleToken.DisplayMedium => DisplayMedium,
            TypographyRoleToken.DisplaySmall => DisplaySmall,
            TypographyRoleToken.HeadlineLarge => HeadlineLarge,
            TypographyRoleToken.HeadlineMedium => HeadlineMedium,
            TypographyRoleToken.HeadlineSmall => HeadlineSmall,
            TypographyRoleToken.TitleLarge => TitleLarge,
            TypographyRoleToken.TitleMedium => TitleMedium,
            TypographyRoleToken.TitleSmall => TitleSmall,
            TypographyRoleToken.BodyLarge => BodyLarge,
            TypographyRoleToken.BodyMedium => BodyMedium,
            TypographyRoleToken.BodySmall => BodySmall,
            TypographyRoleToken.LabelLarge => LabelLarge,
            TypographyRoleToken.LabelMedium => LabelMedium,
            TypographyRoleToken.LabelSmall => LabelSmall,

            TypographyRoleToken.DisplayLargeEmphasized => DisplayLargeEmphasized,
            TypographyRoleToken.DisplayMediumEmphasized => DisplayMediumEmphasized,
            TypographyRoleToken.DisplaySmallEmphasized => DisplaySmallEmphasized,
            TypographyRoleToken.HeadlineLargeEmphasized => HeadlineLargeEmphasized,
            TypographyRoleToken.HeadlineMediumEmphasized => HeadlineMediumEmphasized,
            TypographyRoleToken.HeadlineSmallEmphasized => HeadlineSmallEmphasized,
            TypographyRoleToken.TitleLargeEmphasized => TitleLargeEmphasized,
            TypographyRoleToken.TitleMediumEmphasized => TitleMediumEmphasized,
            TypographyRoleToken.TitleSmallEmphasized => TitleSmallEmphasized,
            TypographyRoleToken.BodyLargeEmphasized => BodyLargeEmphasized,
            TypographyRoleToken.BodyMediumEmphasized => BodyMediumEmphasized,
            TypographyRoleToken.BodySmallEmphasized => BodySmallEmphasized,
            TypographyRoleToken.LabelLargeEmphasized => LabelLargeEmphasized,
            TypographyRoleToken.LabelMediumEmphasized => LabelMediumEmphasized,
            TypographyRoleToken.LabelSmallEmphasized => LabelSmallEmphasized,

            _ => throw new ArgumentOutOfRangeException(nameof(role), role, null)
        };
    }
    
    public object ProvideValue()
    {
        return this;
    }
}

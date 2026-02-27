using System;
using Avalonia.Media;

namespace Material3Expressive.Themes;

public readonly record struct TypeScaleToken(
    double FontSize,
    double LineHeight,
    double LetterSpacing
);

public static class TypeScaleTokens
{
    public static readonly TypeScaleToken BodyLarge = new(16, 24, 0.5);
    public static readonly TypeScaleToken BodyMedium = new(14, 20, 0.2);
    public static readonly TypeScaleToken BodySmall = new(12, 16, 0.4);

    public static readonly TypeScaleToken DisplayLarge = new(57, 64, -0.2);
    public static readonly TypeScaleToken DisplayMedium = new(45, 52, 0);
    public static readonly TypeScaleToken DisplaySmall = new(36, 44, 0);

    public static readonly TypeScaleToken HeadlineLarge = new(32, 40, 0);
    public static readonly TypeScaleToken HeadlineMedium = new(28, 36, 0);
    public static readonly TypeScaleToken HeadlineSmall = new(24, 32, 0);

    public static readonly TypeScaleToken LabelLarge = new(14, 20, 0.1);
    public static readonly TypeScaleToken LabelMedium = new(12, 16, 0.5);
    public static readonly TypeScaleToken LabelSmall = new(11, 16, 0.5);

    public static readonly TypeScaleToken TitleLarge = new(22, 28, 0);
    public static readonly TypeScaleToken TitleMedium = new(16, 24, 0.2);
    public static readonly TypeScaleToken TitleSmall = new(14, 20, 0.1);

    public static readonly TypeScaleToken BodyLargeEmphasized = new(16, 24, 0.15);
    public static readonly TypeScaleToken BodyMediumEmphasized = new(14, 20, 0.25);
    public static readonly TypeScaleToken BodySmallEmphasized = new(12, 16, 0.4);

    public static readonly TypeScaleToken DisplayLargeEmphasized = new(57, 64, 0);
    public static readonly TypeScaleToken DisplayMediumEmphasized = new(45, 52, 0);
    public static readonly TypeScaleToken DisplaySmallEmphasized = new(36, 44, 0);

    public static readonly TypeScaleToken HeadlineLargeEmphasized = new(32, 40, 0);
    public static readonly TypeScaleToken HeadlineMediumEmphasized = new(28, 36, 0);
    public static readonly TypeScaleToken HeadlineSmallEmphasized = new(24, 32, 0);

    public static readonly TypeScaleToken LabelLargeEmphasized = new(14, 20, 0.1);
    public static readonly TypeScaleToken LabelMediumEmphasized = new(12, 16, 0.5);
    public static readonly TypeScaleToken LabelSmallEmphasized = new(11, 16, 0.5);

    public static readonly TypeScaleToken TitleLargeEmphasized = new(22, 28, 0);
    public static readonly TypeScaleToken TitleMediumEmphasized = new(16, 24, 0.15);
    public static readonly TypeScaleToken TitleSmallEmphasized = new(14, 20, 0.1);

    public static TypeScaleToken Get(TypographyRoleToken role)
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
}

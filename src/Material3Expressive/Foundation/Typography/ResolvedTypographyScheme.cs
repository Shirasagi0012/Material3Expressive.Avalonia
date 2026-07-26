using System;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>Immutable type-scale lookup carried by a theme snapshot.</summary>
public sealed class ResolvedTypographyScheme
{
    private readonly TypeScaleToken[] _scales;

    public ResolvedTypographyScheme(TypographyScheme scheme)
    {
        ArgumentNullException.ThrowIfNull(scheme);

        BrandFontFamily = scheme.BrandFontFamily;
        PlainFontFamily = scheme.PlainFontFamily;
        EmphasizedBrandFontFamily = scheme.EmphasizedBrandFontFamily;
        EmphasizedPlainFontFamily = scheme.EmphasizedPlainFontFamily;
        WeightRegular = scheme.WeightRegular;
        WeightMedium = scheme.WeightMedium;
        WeightBold = scheme.WeightBold;

        var roles = Enum.GetValues<TypographyRole>();
        _scales = new TypeScaleToken[roles.Length];
        foreach (var role in roles)
            _scales[(int)role] = scheme.GetTypeScale(role);
    }

    public static ResolvedTypographyScheme Default { get; } = new(new TypographyScheme());

    public FontFamily BrandFontFamily { get; }
    public FontFamily PlainFontFamily { get; }
    public FontFamily EmphasizedBrandFontFamily { get; }
    public FontFamily EmphasizedPlainFontFamily { get; }
    public FontWeight WeightRegular { get; }
    public FontWeight WeightMedium { get; }
    public FontWeight WeightBold { get; }

    public TypeScaleToken GetTypeScale(TypographyRole role)
    {
        return _scales[(int)role];
    }

    public FontFamily GetFontFamily(FontFamilyToken token)
    {
        return token switch
        {
            FontFamilyToken.Brand => BrandFontFamily,
            FontFamilyToken.Plain => PlainFontFamily,
            FontFamilyToken.EmphasizedBrand => EmphasizedBrandFontFamily,
            FontFamilyToken.EmphasizedPlain => EmphasizedPlainFontFamily,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }

    public FontWeight GetFontWeight(FontWeightToken token)
    {
        return token switch
        {
            FontWeightToken.Regular => WeightRegular,
            FontWeightToken.Medium => WeightMedium,
            FontWeightToken.Bold => WeightBold,
            _ => throw new ArgumentOutOfRangeException(nameof(token), token, null)
        };
    }
}

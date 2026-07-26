using System;
using Avalonia.Media;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>Immutable type-scale lookup carried by a theme snapshot.</summary>
/// <remarks>
/// The metrics come from the generated <see cref="TypeScale" /> sets (after any theme overrides);
/// the faces and weights they name come from the <see cref="TypographyScheme" />. Resolving the
/// two together here is what lets a slot say "plain, medium" and an app answer that once.
/// </remarks>
public sealed class ResolvedTypographyScheme
{
    private static readonly int RoleCount = Enum.GetValues<TypographyRole>().Length;

    private readonly TypeScale[] _scales;

    public ResolvedTypographyScheme(TypographyScheme scheme, Func<TypographyRole, TypeScale> scales)
    {
        ArgumentNullException.ThrowIfNull(scheme);
        ArgumentNullException.ThrowIfNull(scales);

        BrandFontFamily = scheme.BrandFontFamily;
        PlainFontFamily = scheme.PlainFontFamily;
        EmphasizedBrandFontFamily = scheme.EmphasizedBrandFontFamily;
        EmphasizedPlainFontFamily = scheme.EmphasizedPlainFontFamily;
        WeightRegular = scheme.WeightRegular;
        WeightMedium = scheme.WeightMedium;
        WeightBold = scheme.WeightBold;

        _scales = new TypeScale[RoleCount];
        for (var i = 0; i < RoleCount; i++)
            _scales[i] = scales((TypographyRole)i);
    }

    public FontFamily BrandFontFamily { get; }
    public FontFamily PlainFontFamily { get; }
    public FontFamily EmphasizedBrandFontFamily { get; }
    public FontFamily EmphasizedPlainFontFamily { get; }
    public FontWeight WeightRegular { get; }
    public FontWeight WeightMedium { get; }
    public FontWeight WeightBold { get; }

    public TypeScale GetTypeScale(TypographyRole role)
    {
        return _scales[(int)role];
    }

    /// <summary>The face a slot is set in, after the scheme's substitutions.</summary>
    public FontFamily GetFontFamily(TypographyRole role)
    {
        return GetFontFamily(GetTypeScale(role).Font);
    }

    /// <summary>The weight a slot is set in, after the scheme's substitutions.</summary>
    public FontWeight GetFontWeight(TypographyRole role)
    {
        return GetFontWeight(GetTypeScale(role).Weight);
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

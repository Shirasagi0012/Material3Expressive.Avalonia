using Avalonia.Media;

namespace Material3Expressive.Themes.Tokens;

public static class TypefaceTokens
{
    public static readonly FontFamily Brand = FontFamily.Default;

    public static readonly FontFamily Plain = FontFamily.Default;

    public static readonly FontWeight WeightRegular = FontWeight.Normal;

    public static readonly FontWeight WeightMedium = FontWeight.Medium;

    public static readonly FontWeight WeightBold = FontWeight.Bold;
}

public enum FontFamilyToken
{
    Brand,
    Plain
}

public enum FontWeightToken
{
    Regular,
    Medium,
    Bold
}

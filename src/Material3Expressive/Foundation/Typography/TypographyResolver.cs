using Avalonia;
using Avalonia.Data;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation;

/// <summary>
/// Bindings for the typography markup extensions. Typography has no variant dimension, so these
/// depend on the theme alone.
/// </summary>
internal static class TypographyResolver
{
    public static BindingBase FontFamily(FontFamilyToken token)
    {
        return ThemeBinding.FromTheme<object?>(
            theme => theme.Typography.GetFontFamily(token),
            AvaloniaProperty.UnsetValue);
    }

    public static BindingBase FontWeight(FontWeightToken token)
    {
        return ThemeBinding.FromTheme<object?>(
            theme => theme.Typography.GetFontWeight(token),
            AvaloniaProperty.UnsetValue);
    }

    public static BindingBase FontSize(TypographyRole role)
    {
        return ThemeBinding.FromTheme<object?>(
            theme => theme.Typography.GetTypeScale(role).FontSize,
            AvaloniaProperty.UnsetValue);
    }

    public static BindingBase LineHeight(TypographyRole role)
    {
        return ThemeBinding.FromTheme<object?>(
            theme => theme.Typography.GetTypeScale(role).LineHeight,
            AvaloniaProperty.UnsetValue);
    }

    public static BindingBase LetterSpacing(TypographyRole role)
    {
        return ThemeBinding.FromTheme<object?>(
            theme => theme.Typography.GetTypeScale(role).LetterSpacing,
            AvaloniaProperty.UnsetValue);
    }
}

using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

internal sealed record DividerVisualColors(IImmutableBrush Color);

/// <summary>Component-owned policy for the Material divider.</summary>
internal static class DividerDefaults
{
    public const double Thickness = DividerTokens.Thickness;

    public static DividerVisualColors Colors(ResolvedTheme theme, bool isDark)
    {
        ArgumentNullException.ThrowIfNull(theme);
        return new(DividerTokens.Color.ResolveBrush(theme.ColorScheme, isDark));
    }
}

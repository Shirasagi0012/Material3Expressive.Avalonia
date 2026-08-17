using Avalonia.Media;
using MaterialColorUtilities.Avalonia.Tokens;
using SharedTokens = MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Compatibility forwarding facade for MCU-owned system-color keys.</summary>
[Obsolete(
    "System-color token identity is owned by MaterialColorUtilities.Avalonia. Use its " +
    "SystemColorTokens API directly.",
    DiagnosticId = "M3E1003")]
public static class SystemColorTokens
{
    public static SharedTokens.MaterialToken<Color> Get(SysColorToken role, string? customKey = null) =>
        MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens.Get(role, customKey);

    public static bool TryGetRole(
        SharedTokens.MaterialToken token,
        out SysColorToken role,
        out string? customKey) =>
        MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens.TryGetRole(
            token, out role, out customKey);

    public static SharedTokens.MaterialToken<Color> Background => Get(SysColorToken.Background);
    public static SharedTokens.MaterialToken<Color> OnBackground => Get(SysColorToken.OnBackground);
    public static SharedTokens.MaterialToken<Color> Surface => Get(SysColorToken.Surface);
    public static SharedTokens.MaterialToken<Color> SurfaceDim => Get(SysColorToken.SurfaceDim);
    public static SharedTokens.MaterialToken<Color> SurfaceBright => Get(SysColorToken.SurfaceBright);
    public static SharedTokens.MaterialToken<Color> SurfaceContainerLowest => Get(SysColorToken.SurfaceContainerLowest);
    public static SharedTokens.MaterialToken<Color> SurfaceContainerLow => Get(SysColorToken.SurfaceContainerLow);
    public static SharedTokens.MaterialToken<Color> SurfaceContainer => Get(SysColorToken.SurfaceContainer);
    public static SharedTokens.MaterialToken<Color> SurfaceContainerHigh => Get(SysColorToken.SurfaceContainerHigh);
    public static SharedTokens.MaterialToken<Color> SurfaceContainerHighest => Get(SysColorToken.SurfaceContainerHighest);
    public static SharedTokens.MaterialToken<Color> OnSurface => Get(SysColorToken.OnSurface);
    public static SharedTokens.MaterialToken<Color> SurfaceVariant => Get(SysColorToken.SurfaceVariant);
    public static SharedTokens.MaterialToken<Color> OnSurfaceVariant => Get(SysColorToken.OnSurfaceVariant);
    public static SharedTokens.MaterialToken<Color> InverseSurface => Get(SysColorToken.InverseSurface);
    public static SharedTokens.MaterialToken<Color> InverseOnSurface => Get(SysColorToken.InverseOnSurface);
    public static SharedTokens.MaterialToken<Color> Outline => Get(SysColorToken.Outline);
    public static SharedTokens.MaterialToken<Color> OutlineVariant => Get(SysColorToken.OutlineVariant);
    public static SharedTokens.MaterialToken<Color> Shadow => Get(SysColorToken.Shadow);
    public static SharedTokens.MaterialToken<Color> Scrim => Get(SysColorToken.Scrim);
    public static SharedTokens.MaterialToken<Color> SurfaceTint => Get(SysColorToken.SurfaceTint);
    public static SharedTokens.MaterialToken<Color> Primary => Get(SysColorToken.Primary);
    public static SharedTokens.MaterialToken<Color> OnPrimary => Get(SysColorToken.OnPrimary);
    public static SharedTokens.MaterialToken<Color> PrimaryContainer => Get(SysColorToken.PrimaryContainer);
    public static SharedTokens.MaterialToken<Color> OnPrimaryContainer => Get(SysColorToken.OnPrimaryContainer);
    public static SharedTokens.MaterialToken<Color> InversePrimary => Get(SysColorToken.InversePrimary);
    public static SharedTokens.MaterialToken<Color> Secondary => Get(SysColorToken.Secondary);
    public static SharedTokens.MaterialToken<Color> OnSecondary => Get(SysColorToken.OnSecondary);
    public static SharedTokens.MaterialToken<Color> SecondaryContainer => Get(SysColorToken.SecondaryContainer);
    public static SharedTokens.MaterialToken<Color> OnSecondaryContainer => Get(SysColorToken.OnSecondaryContainer);
    public static SharedTokens.MaterialToken<Color> Tertiary => Get(SysColorToken.Tertiary);
    public static SharedTokens.MaterialToken<Color> OnTertiary => Get(SysColorToken.OnTertiary);
    public static SharedTokens.MaterialToken<Color> TertiaryContainer => Get(SysColorToken.TertiaryContainer);
    public static SharedTokens.MaterialToken<Color> OnTertiaryContainer => Get(SysColorToken.OnTertiaryContainer);
    public static SharedTokens.MaterialToken<Color> Error => Get(SysColorToken.Error);
    public static SharedTokens.MaterialToken<Color> OnError => Get(SysColorToken.OnError);
    public static SharedTokens.MaterialToken<Color> ErrorContainer => Get(SysColorToken.ErrorContainer);
    public static SharedTokens.MaterialToken<Color> OnErrorContainer => Get(SysColorToken.OnErrorContainer);
    public static SharedTokens.MaterialToken<Color> PrimaryFixed => Get(SysColorToken.PrimaryFixed);
    public static SharedTokens.MaterialToken<Color> PrimaryFixedDim => Get(SysColorToken.PrimaryFixedDim);
    public static SharedTokens.MaterialToken<Color> OnPrimaryFixed => Get(SysColorToken.OnPrimaryFixed);
    public static SharedTokens.MaterialToken<Color> OnPrimaryFixedVariant => Get(SysColorToken.OnPrimaryFixedVariant);
    public static SharedTokens.MaterialToken<Color> SecondaryFixed => Get(SysColorToken.SecondaryFixed);
    public static SharedTokens.MaterialToken<Color> SecondaryFixedDim => Get(SysColorToken.SecondaryFixedDim);
    public static SharedTokens.MaterialToken<Color> OnSecondaryFixed => Get(SysColorToken.OnSecondaryFixed);
    public static SharedTokens.MaterialToken<Color> OnSecondaryFixedVariant => Get(SysColorToken.OnSecondaryFixedVariant);
    public static SharedTokens.MaterialToken<Color> TertiaryFixed => Get(SysColorToken.TertiaryFixed);
    public static SharedTokens.MaterialToken<Color> TertiaryFixedDim => Get(SysColorToken.TertiaryFixedDim);
    public static SharedTokens.MaterialToken<Color> OnTertiaryFixed => Get(SysColorToken.OnTertiaryFixed);
    public static SharedTokens.MaterialToken<Color> OnTertiaryFixedVariant => Get(SysColorToken.OnTertiaryFixedVariant);
}

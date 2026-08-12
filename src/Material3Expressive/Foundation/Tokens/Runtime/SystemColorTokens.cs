using Avalonia.Media;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>Stable strongly typed keys for Material system color roles.</summary>
public static class SystemColorTokens
{
    private readonly struct Key : IEquatable<Key>
    {
        public Key(SysColorToken role, string? customKey)
        {
            Role = role;
            CustomKey = customKey;
        }

        public SysColorToken Role { get; }
        public string? CustomKey { get; }

        public bool Equals(Key other) =>
            Role == other.Role &&
            StringComparer.OrdinalIgnoreCase.Equals(CustomKey, other.CustomKey);

        public override bool Equals(object? obj) => obj is Key other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(
            Role,
            CustomKey is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(CustomKey));
    }

    private static readonly object Gate = new();
    private static readonly Dictionary<Key, MaterialToken<Color>> Tokens = [];
    private static readonly Dictionary<MaterialToken, Key> Reverse = [];

    /// <summary>Gets the stable token key for a system color role.</summary>
    public static MaterialToken<Color> Get(SysColorToken role, string? customKey = null)
    {
        if (!IsCustom(role))
            customKey = null;

        var key = new Key(role, customKey);
        lock (Gate)
        {
            if (Tokens.TryGetValue(key, out var token))
                return token;

            var suffix = customKey is { Length: > 0 } ? $".{customKey}" : string.Empty;
            token = new MaterialToken<Color>(
                $"System.Color.{role}{suffix}",
                TokenLayer.System,
                Token.Literal(Colors.Transparent));
            Tokens.Add(key, token);
            Reverse.Add(token, key);
            return token;
        }
    }

    /// <summary>Gets role metadata for a token created by this catalog.</summary>
    public static bool TryGetRole(MaterialToken token, out SysColorToken role, out string? customKey)
    {
        ArgumentNullException.ThrowIfNull(token);
        lock (Gate)
        {
            if (Reverse.TryGetValue(token, out var key))
            {
                role = key.Role;
                customKey = key.CustomKey;
                return true;
            }
        }

        role = default;
        customKey = null;
        return false;
    }

    private static bool IsCustom(SysColorToken role) => role is
        SysColorToken.Custom or SysColorToken.OnCustom or
        SysColorToken.CustomContainer or SysColorToken.OnCustomContainer;

    public static MaterialToken<Color> Background => Get(SysColorToken.Background);
    public static MaterialToken<Color> OnBackground => Get(SysColorToken.OnBackground);
    public static MaterialToken<Color> Surface => Get(SysColorToken.Surface);
    public static MaterialToken<Color> SurfaceDim => Get(SysColorToken.SurfaceDim);
    public static MaterialToken<Color> SurfaceBright => Get(SysColorToken.SurfaceBright);
    public static MaterialToken<Color> SurfaceContainerLowest => Get(SysColorToken.SurfaceContainerLowest);
    public static MaterialToken<Color> SurfaceContainerLow => Get(SysColorToken.SurfaceContainerLow);
    public static MaterialToken<Color> SurfaceContainer => Get(SysColorToken.SurfaceContainer);
    public static MaterialToken<Color> SurfaceContainerHigh => Get(SysColorToken.SurfaceContainerHigh);
    public static MaterialToken<Color> SurfaceContainerHighest => Get(SysColorToken.SurfaceContainerHighest);
    public static MaterialToken<Color> OnSurface => Get(SysColorToken.OnSurface);
    public static MaterialToken<Color> SurfaceVariant => Get(SysColorToken.SurfaceVariant);
    public static MaterialToken<Color> OnSurfaceVariant => Get(SysColorToken.OnSurfaceVariant);
    public static MaterialToken<Color> InverseSurface => Get(SysColorToken.InverseSurface);
    public static MaterialToken<Color> InverseOnSurface => Get(SysColorToken.InverseOnSurface);
    public static MaterialToken<Color> Outline => Get(SysColorToken.Outline);
    public static MaterialToken<Color> OutlineVariant => Get(SysColorToken.OutlineVariant);
    public static MaterialToken<Color> Shadow => Get(SysColorToken.Shadow);
    public static MaterialToken<Color> Scrim => Get(SysColorToken.Scrim);
    public static MaterialToken<Color> SurfaceTint => Get(SysColorToken.SurfaceTint);
    public static MaterialToken<Color> Primary => Get(SysColorToken.Primary);
    public static MaterialToken<Color> OnPrimary => Get(SysColorToken.OnPrimary);
    public static MaterialToken<Color> PrimaryContainer => Get(SysColorToken.PrimaryContainer);
    public static MaterialToken<Color> OnPrimaryContainer => Get(SysColorToken.OnPrimaryContainer);
    public static MaterialToken<Color> InversePrimary => Get(SysColorToken.InversePrimary);
    public static MaterialToken<Color> Secondary => Get(SysColorToken.Secondary);
    public static MaterialToken<Color> OnSecondary => Get(SysColorToken.OnSecondary);
    public static MaterialToken<Color> SecondaryContainer => Get(SysColorToken.SecondaryContainer);
    public static MaterialToken<Color> OnSecondaryContainer => Get(SysColorToken.OnSecondaryContainer);
    public static MaterialToken<Color> Tertiary => Get(SysColorToken.Tertiary);
    public static MaterialToken<Color> OnTertiary => Get(SysColorToken.OnTertiary);
    public static MaterialToken<Color> TertiaryContainer => Get(SysColorToken.TertiaryContainer);
    public static MaterialToken<Color> OnTertiaryContainer => Get(SysColorToken.OnTertiaryContainer);
    public static MaterialToken<Color> Error => Get(SysColorToken.Error);
    public static MaterialToken<Color> OnError => Get(SysColorToken.OnError);
    public static MaterialToken<Color> ErrorContainer => Get(SysColorToken.ErrorContainer);
    public static MaterialToken<Color> OnErrorContainer => Get(SysColorToken.OnErrorContainer);
    public static MaterialToken<Color> PrimaryFixed => Get(SysColorToken.PrimaryFixed);
    public static MaterialToken<Color> PrimaryFixedDim => Get(SysColorToken.PrimaryFixedDim);
    public static MaterialToken<Color> OnPrimaryFixed => Get(SysColorToken.OnPrimaryFixed);
    public static MaterialToken<Color> OnPrimaryFixedVariant => Get(SysColorToken.OnPrimaryFixedVariant);
    public static MaterialToken<Color> SecondaryFixed => Get(SysColorToken.SecondaryFixed);
    public static MaterialToken<Color> SecondaryFixedDim => Get(SysColorToken.SecondaryFixedDim);
    public static MaterialToken<Color> OnSecondaryFixed => Get(SysColorToken.OnSecondaryFixed);
    public static MaterialToken<Color> OnSecondaryFixedVariant => Get(SysColorToken.OnSecondaryFixedVariant);
    public static MaterialToken<Color> TertiaryFixed => Get(SysColorToken.TertiaryFixed);
    public static MaterialToken<Color> TertiaryFixedDim => Get(SysColorToken.TertiaryFixedDim);
    public static MaterialToken<Color> OnTertiaryFixed => Get(SysColorToken.OnTertiaryFixed);
    public static MaterialToken<Color> OnTertiaryFixedVariant => Get(SysColorToken.OnTertiaryFixedVariant);
}

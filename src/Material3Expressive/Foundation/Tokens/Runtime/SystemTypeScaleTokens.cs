using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Foundation.Tokens.Runtime;

/// <summary>The five strongly typed system-token keys that make up one Material type-scale role.</summary>
public sealed class SystemTypeScaleTokenSet
{
    internal SystemTypeScaleTokenSet(TypographyRole role, TypeScale defaults)
    {
        Role = role;
        var prefix = $"System.TypeScale.{role}";
        FontSize = Create($"{prefix}.FontSize", defaults.FontSize);
        LineHeight = Create($"{prefix}.LineHeight", defaults.LineHeight);
        LetterSpacing = Create($"{prefix}.LetterSpacing", defaults.LetterSpacing);
        Font = Create($"{prefix}.Font", defaults.Font);
        Weight = Create($"{prefix}.Weight", defaults.Weight);
    }

    /// <summary>The typography role represented by this set.</summary>
    public TypographyRole Role { get; }

    public MaterialToken<double> FontSize { get; }
    public MaterialToken<double> LineHeight { get; }
    public MaterialToken<double> LetterSpacing { get; }
    public MaterialToken<FontFamilyToken> Font { get; }
    public MaterialToken<FontWeightToken> Weight { get; }

    private static MaterialToken<T> Create<T>(string path, T value) =>
        new(path, TokenLayer.System, Token.Literal(value));
}

/// <summary>
/// Stable keys for the record-shaped Material type-scale system table.
/// </summary>
/// <remarks>
/// <see cref="For"/> always returns the same set instance for a role. Its fallback values come
/// directly from the generated <see cref="TypeScale"/> table; a <see cref="MaterialTheme"/> may
/// provide theme-authored values through the resource cascade before those fallbacks are used.
/// </remarks>
public static class SystemTypeScaleTokens
{
    private static readonly IReadOnlyDictionary<TypographyRole, SystemTypeScaleTokenSet> Sets =
        CreateSets();
    private static readonly Dictionary<MaterialToken, Metadata> Reverse = CreateReverseMap();

    /// <summary>Gets the stable token set for a typography role.</summary>
    public static SystemTypeScaleTokenSet For(TypographyRole role)
    {
        if (!Sets.TryGetValue(role, out var set))
            throw new ArgumentOutOfRangeException(nameof(role), role, null);

        return set;
    }

    /// <summary>Gets role and field metadata for a key owned by this catalog.</summary>
    public static bool TryGetRole(
        MaterialToken token,
        out TypographyRole role,
        out SystemTypeScaleField field)
    {
        ArgumentNullException.ThrowIfNull(token);
        if (Reverse.TryGetValue(token, out var metadata))
        {
            role = metadata.Role;
            field = metadata.Field;
            return true;
        }

        role = default;
        field = default;
        return false;
    }

    private static IReadOnlyDictionary<TypographyRole, SystemTypeScaleTokenSet> CreateSets()
    {
        var roles = Enum.GetValues<TypographyRole>();
        var result = new Dictionary<TypographyRole, SystemTypeScaleTokenSet>(roles.Length);
        foreach (var role in roles)
            result.Add(role, new SystemTypeScaleTokenSet(role, TypeScale.For(role)));
        return result;
    }

    private static Dictionary<MaterialToken, Metadata> CreateReverseMap()
    {
        var result = new Dictionary<MaterialToken, Metadata>(Sets.Count * 5);
        foreach (var set in Sets.Values)
        {
            result.Add(set.FontSize, new(set.Role, SystemTypeScaleField.FontSize));
            result.Add(set.LineHeight, new(set.Role, SystemTypeScaleField.LineHeight));
            result.Add(set.LetterSpacing, new(set.Role, SystemTypeScaleField.LetterSpacing));
            result.Add(set.Font, new(set.Role, SystemTypeScaleField.Font));
            result.Add(set.Weight, new(set.Role, SystemTypeScaleField.Weight));
        }
        return result;
    }

    private readonly record struct Metadata(TypographyRole Role, SystemTypeScaleField Field);
}

/// <summary>A field in a <see cref="SystemTypeScaleTokenSet"/>.</summary>
public enum SystemTypeScaleField
{
    FontSize,
    LineHeight,
    LetterSpacing,
    Font,
    Weight
}

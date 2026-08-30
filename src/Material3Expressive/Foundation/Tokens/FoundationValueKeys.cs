using Avalonia.Media;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

/// <summary>Consumer-ready shape endpoints backed only by the foundation Raw shape slots.</summary>
internal static class ShapeValueKeys
{
    private static readonly IReadOnlyDictionary<ShapeRole, MaterialValueKey<RoundedCornerShape>> Keys =
        CreateKeys();

    internal static IReadOnlyCollection<MaterialValueKey<RoundedCornerShape>> All { get; } =
        Array.AsReadOnly(Keys.Values.ToArray());

    /// <summary>Gets the endpoint for one fixed shape role.</summary>
    internal static MaterialValueKey<RoundedCornerShape> For(ShapeRole role)
    {
        if (Keys.TryGetValue(role, out var key))
            return key;

        throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown shape role.");
    }

    /// <summary>
    /// Creates an internal endpoint which resolves a component Raw role through the foundation
    /// shape slots while leaving Bounds and flow-direction projection to the presenter.
    /// </summary>
    internal static MaterialValueKey<RoundedCornerShape> FromRawRole(
        string path,
        MaterialToken<ShapeRole> roleToken) =>
        new(path, new RawRecipePlan<RoundedCornerShape>(new RawShapeRoleRecipe(roleToken)));

    private static IReadOnlyDictionary<ShapeRole, MaterialValueKey<RoundedCornerShape>> CreateKeys()
    {
        var keys = new Dictionary<ShapeRole, MaterialValueKey<RoundedCornerShape>>();
        foreach (var role in Enum.GetValues<ShapeRole>())
        {
            keys.Add(
                role,
                new MaterialValueKey<RoundedCornerShape>(
                    $"Value.Foundation.Shape.{role}",
                    new RawRecipePlan<RoundedCornerShape>(new FixedShapeRoleRecipe(role))));
        }

        return keys;
    }
}

/// <summary>Consumer-ready spring endpoints. Animation execution remains presenter-owned.</summary>
internal static class MotionValueKeys
{
    private static readonly IReadOnlyDictionary<MotionRole, MaterialValueKey<SpringSpec>> Keys =
        CreateKeys();

    internal static IReadOnlyCollection<MaterialValueKey<SpringSpec>> All { get; } =
        Array.AsReadOnly(Keys.Values.ToArray());

    internal static MaterialValueKey<SpringSpec> For(MotionRole role)
    {
        if (Keys.TryGetValue(role, out var key))
            return key;

        throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown motion role.");
    }

    private static IReadOnlyDictionary<MotionRole, MaterialValueKey<SpringSpec>> CreateKeys()
    {
        var keys = new Dictionary<MotionRole, MaterialValueKey<SpringSpec>>();
        foreach (var role in Enum.GetValues<MotionRole>())
        {
            keys.Add(
                role,
                MaterialValue.FromRaw(
                    $"Value.Foundation.Motion.{role}",
                    SystemMotionTokens.For(role)));
        }

        return keys;
    }
}

/// <summary>Leaf typography endpoints for each generated type-scale role.</summary>
internal static class TypographyValueKeys
{
    private static readonly IReadOnlyDictionary<FontFamilyToken, MaterialValueKey<FontFamily>>
        FontFamilyKeys = CreateFontFamilyKeys();

    private static readonly IReadOnlyDictionary<FontWeightToken, MaterialValueKey<FontWeight>>
        FontWeightKeys = CreateFontWeightKeys();

    private static readonly IReadOnlyDictionary<TypographyRole, TypographyRoleValueKeys> RoleKeys =
        CreateRoleKeys();

    internal static IReadOnlyCollection<MaterialValueKey> All { get; } = CreateAllKeys();

    internal static TypographyRoleValueKeys For(TypographyRole role)
    {
        if (RoleKeys.TryGetValue(role, out var keys))
            return keys;

        throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown typography role.");
    }

    internal static MaterialValueKey<FontFamily> For(FontFamilyToken token)
    {
        if (FontFamilyKeys.TryGetValue(token, out var key))
            return key;

        throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown font-family token.");
    }

    internal static MaterialValueKey<FontWeight> For(FontWeightToken token)
    {
        if (FontWeightKeys.TryGetValue(token, out var key))
            return key;

        throw new ArgumentOutOfRangeException(nameof(token), token, "Unknown font-weight token.");
    }

    private static IReadOnlyDictionary<FontFamilyToken, MaterialValueKey<FontFamily>>
        CreateFontFamilyKeys()
    {
        var keys = new Dictionary<FontFamilyToken, MaterialValueKey<FontFamily>>();
        foreach (var token in Enum.GetValues<FontFamilyToken>())
        {
            keys.Add(
                token,
                MaterialValue.FromRaw(
                    $"Value.Foundation.Typography.FontFamily.{token}",
                    SystemTypographyTokens.For(token)));
        }

        return keys;
    }

    private static IReadOnlyDictionary<FontWeightToken, MaterialValueKey<FontWeight>>
        CreateFontWeightKeys()
    {
        var keys = new Dictionary<FontWeightToken, MaterialValueKey<FontWeight>>();
        foreach (var token in Enum.GetValues<FontWeightToken>())
        {
            keys.Add(
                token,
                MaterialValue.FromRaw(
                    $"Value.Foundation.Typography.FontWeight.{token}",
                    SystemTypographyTokens.For(token)));
        }

        return keys;
    }

    private static IReadOnlyDictionary<TypographyRole, TypographyRoleValueKeys> CreateRoleKeys()
    {
        var keys = new Dictionary<TypographyRole, TypographyRoleValueKeys>();
        foreach (var role in Enum.GetValues<TypographyRole>())
        {
            var raw = SystemTypeScaleTokens.For(role);
            var prefix = $"Value.Foundation.Typography.{role}";
            keys.Add(
                role,
                new TypographyRoleValueKeys(
                    MaterialValue.FromRaw($"{prefix}.FontFamily", raw.FontFamily),
                    MaterialValue.FromRaw($"{prefix}.FontWeight", raw.FontWeight),
                    MaterialValue.FromRaw($"{prefix}.FontSize", raw.FontSize),
                    MaterialValue.FromRaw($"{prefix}.LineHeight", raw.LineHeight),
                    MaterialValue.FromRaw($"{prefix}.LetterSpacing", raw.LetterSpacing)));
        }

        return keys;
    }

    private static IReadOnlyCollection<MaterialValueKey> CreateAllKeys()
    {
        var keys = new List<MaterialValueKey>(
            FontFamilyKeys.Count + FontWeightKeys.Count + (RoleKeys.Count * 5));
        keys.AddRange(FontFamilyKeys.Values);
        keys.AddRange(FontWeightKeys.Values);
        foreach (var role in RoleKeys.Values)
        {
            keys.Add(role.FontFamily);
            keys.Add(role.FontWeight);
            keys.Add(role.FontSize);
            keys.Add(role.LineHeight);
            keys.Add(role.LetterSpacing);
        }

        return Array.AsReadOnly(keys.ToArray());
    }
}

internal sealed class TypographyRoleValueKeys
{
    internal TypographyRoleValueKeys(
        MaterialValueKey<FontFamily> fontFamily,
        MaterialValueKey<FontWeight> fontWeight,
        MaterialValueKey<double> fontSize,
        MaterialValueKey<double> lineHeight,
        MaterialValueKey<double> letterSpacing)
    {
        FontFamily = fontFamily ?? throw new ArgumentNullException(nameof(fontFamily));
        FontWeight = fontWeight ?? throw new ArgumentNullException(nameof(fontWeight));
        FontSize = fontSize ?? throw new ArgumentNullException(nameof(fontSize));
        LineHeight = lineHeight ?? throw new ArgumentNullException(nameof(lineHeight));
        LetterSpacing = letterSpacing ?? throw new ArgumentNullException(nameof(letterSpacing));
    }

    internal MaterialValueKey<FontFamily> FontFamily { get; }

    internal MaterialValueKey<FontWeight> FontWeight { get; }

    internal MaterialValueKey<double> FontSize { get; }

    internal MaterialValueKey<double> LineHeight { get; }

    internal MaterialValueKey<double> LetterSpacing { get; }
}

internal sealed class FixedShapeRoleRecipe : IRawValueRecipe<RoundedCornerShape>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly ShapeRole _role;

    internal FixedShapeRoleRecipe(ShapeRole role)
    {
        _role = role;
        _dependencies = Array.AsReadOnly<MaterialToken>([ShapeRawProjection.DependencyFor(role)]);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public RoundedCornerShape Evaluate(MaterialValueContext context) =>
        ShapeRawProjection.Resolve(_role, context);
}

internal sealed class RawShapeRoleRecipe : IRawValueRecipe<RoundedCornerShape>
{
    private readonly IReadOnlyList<MaterialToken> _dependencies;
    private readonly MaterialToken<ShapeRole> _roleToken;

    internal RawShapeRoleRecipe(MaterialToken<ShapeRole> roleToken)
    {
        _roleToken = roleToken ?? throw new ArgumentNullException(nameof(roleToken));
        var dependencies = new MaterialToken[1 + SystemShapeTokens.All.Count];
        dependencies[0] = roleToken;
        var index = 1;
        foreach (var dependency in SystemShapeTokens.All)
            dependencies[index++] = dependency;
        _dependencies = Array.AsReadOnly(dependencies);
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public RoundedCornerShape Evaluate(MaterialValueContext context) =>
        ShapeRawProjection.Resolve(context.Get(_roleToken), context);
}

internal static class ShapeRawProjection
{
    internal static MaterialToken<RoundedCornerShape> DependencyFor(ShapeRole role) =>
        SystemShapeTokens.ForProjection(role);

    internal static RoundedCornerShape Resolve(ShapeRole role, MaterialValueContext context) => role switch
    {
        ShapeRole.CornerNone => context.Get(SystemShapeTokens.CornerNone),
        ShapeRole.CornerExtraSmall => context.Get(SystemShapeTokens.CornerExtraSmall),
        ShapeRole.CornerExtraSmallTop =>
            context.Get(SystemShapeTokens.CornerExtraSmall).Top(),
        ShapeRole.CornerSmall => context.Get(SystemShapeTokens.CornerSmall),
        ShapeRole.CornerMedium => context.Get(SystemShapeTokens.CornerMedium),
        ShapeRole.CornerLarge => context.Get(SystemShapeTokens.CornerLarge),
        ShapeRole.CornerLargeStart => context.Get(SystemShapeTokens.CornerLarge).Start(),
        ShapeRole.CornerLargeEnd => context.Get(SystemShapeTokens.CornerLarge).End(),
        ShapeRole.CornerLargeTop => context.Get(SystemShapeTokens.CornerLarge).Top(),
        ShapeRole.CornerLargeIncreased =>
            context.Get(SystemShapeTokens.CornerLargeIncreased),
        ShapeRole.CornerExtraLarge => context.Get(SystemShapeTokens.CornerExtraLarge),
        ShapeRole.CornerExtraLargeIncreased =>
            context.Get(SystemShapeTokens.CornerExtraLargeIncreased),
        ShapeRole.CornerExtraLargeTop =>
            context.Get(SystemShapeTokens.CornerExtraLarge).Top(),
        ShapeRole.CornerExtraExtraLarge =>
            context.Get(SystemShapeTokens.CornerExtraExtraLarge),
        ShapeRole.CornerFull => context.Get(SystemShapeTokens.CornerFull),
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown shape role."),
    };
}

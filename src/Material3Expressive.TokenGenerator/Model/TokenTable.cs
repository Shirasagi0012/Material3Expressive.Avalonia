using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Material3Expressive.TokenGenerator.Json;

namespace Material3Expressive.TokenGenerator.Model;

/// <summary>What a token's value means, and therefore what C# type it becomes.</summary>
internal enum TokenType
{
    /// <summary>A system colour role or a literal colour — <c>ColorTokenValue</c>.</summary>
    Color,

    /// <summary>A shape slot — <c>ShapeRole</c>.</summary>
    Shape,

    /// <summary>A type scale slot — <c>TypographyRole</c>.</summary>
    Typography,

    /// <summary>A motion scheme slot — <c>MotionRole</c>.</summary>
    Motion,

    /// <summary>A device-independent length, in Avalonia's logical pixels.</summary>
    Dimension,

    /// <summary>A bare number: opacity, elevation, ratio.</summary>
    Number,

    /// <summary>A flag.</summary>
    Boolean,

    /// <summary>A member of an enum the table names explicitly.</summary>
    Enum
}

internal static class TokenTypes
{
    public static bool TryParse(string? text, out TokenType type)
    {
        switch (text)
        {
            case "color": type = TokenType.Color; return true;
            case "shape": type = TokenType.Shape; return true;
            case "typography": type = TokenType.Typography; return true;
            case "motion": type = TokenType.Motion; return true;
            case "dimension": type = TokenType.Dimension; return true;
            case "number":
            case "opacity":
            case "elevation": type = TokenType.Number; return true;
            case "bool": type = TokenType.Boolean; return true;
            case "enum": type = TokenType.Enum; return true;
            default: type = TokenType.Number; return false;
        }
    }

    public static string ToClrType(TokenType type, string? enumType = null)
    {
        return type switch
        {
            TokenType.Color => "global::Material3Expressive.Foundation.ColorTokenValue",
            TokenType.Shape => "global::Material3Expressive.Foundation.ShapeRole",
            TokenType.Typography => "global::Material3Expressive.Foundation.TypographyRole",
            TokenType.Motion => "global::Material3Expressive.Foundation.MotionRole",
            TokenType.Boolean => "bool",
            TokenType.Enum => "global::" + (enumType ?? throw new InvalidTableException(
                "An 'enum' token must name its 'enumType'.")),
            _ => "double"
        };
    }

    /// <summary>Whether a value of this type can be a compile-time constant.</summary>
    public static bool IsConstant(TokenType type)
    {
        return type is TokenType.Dimension or TokenType.Number or TokenType.Boolean
            or TokenType.Shape or TokenType.Typography or TokenType.Motion or TokenType.Enum;
    }
}

/// <summary>A reference from one token value to a member of another generated token set.</summary>
internal sealed class TokenReference
{
    public TokenReference(string tableName, string memberName)
    {
        TableName = tableName;
        MemberName = memberName;
    }

    public string TableName { get; }

    public string MemberName { get; }
}

/// <summary>A typed value plus its emitted expression and optional cross-table reference.</summary>
internal sealed class TokenValue
{
    public TokenValue(TokenType type, string expression, string? enumType, TokenReference? reference)
    {
        Type = type;
        Expression = expression;
        EnumType = enumType;
        Reference = reference;
    }

    public TokenType Type { get; }

    public string Expression { get; }

    public string? EnumType { get; }

    public TokenReference? Reference { get; }
}

internal sealed class TokenEntry
{
    public TokenEntry(string name, TokenType type, TokenValue value, string? summary, string? enumType)
    {
        Name = name;
        Type = type;
        Value = value;
        Summary = summary;
        EnumType = enumType;
    }

    public string Name { get; }

    public TokenType Type { get; }

    public TokenValue Value { get; }

    /// <summary>The emitted C# initialiser.</summary>
    public string Expression => Value.Expression;

    public string? Summary { get; }

    public string? EnumType { get; }
}

internal sealed class TokenProperty
{
    public TokenProperty(string name, TokenType type, string? summary, string? opacityProperty, string? enumType)
    {
        Name = name;
        Type = type;
        Summary = summary;
        OpacityProperty = opacityProperty;
        EnumType = enumType;
    }

    public string Name { get; }

    public TokenType Type { get; }

    public string? Summary { get; }

    /// <summary>The fully qualified enum this property is a member of, for <c>enum</c> tokens.</summary>
    public string? EnumType { get; }

    /// <summary>
    /// For a colour property, the number property that scales its alpha. Upstream specifies the
    /// disabled states as a role plus an opacity rather than as a second role, so the pair has to
    /// travel together to resolve into a single brush.
    /// </summary>
    public string? OpacityProperty { get; }
}

internal sealed class TokenSet
{
    public TokenSet(string name, string? summary, IReadOnlyDictionary<string, TokenValue> values)
    {
        Name = name;
        Summary = summary;
        Values = values;
    }

    public string Name { get; }

    public string? Summary { get; }

    /// <summary>Property name to typed value, already flattened through <c>extends</c>.</summary>
    public IReadOnlyDictionary<string, TokenValue> Values { get; }
}

/// <summary>
/// The four logical table categories used by the token files. The root <c>kind</c> is the only
/// semantic discriminator; directory placement is organizational.
/// </summary>
internal enum TokenTableCategory
{
    ReferenceTokens,
    SystemTokens,
    ComponentTokens,
    ComponentDefaults
}

/// <summary>Where a table's values or assembly decisions originate.</summary>
internal enum TokenProvenance
{
    AndroidXToken,
    ComposeDefault,
    M3Spec,
    AvaloniaPolicy
}

internal static class TokenProvenances
{
    public static bool TryParse(string? text, out TokenProvenance provenance)
    {
        switch (text)
        {
            case "androidx-token": provenance = TokenProvenance.AndroidXToken; return true;
            case "compose-default": provenance = TokenProvenance.ComposeDefault; return true;
            case "m3-spec": provenance = TokenProvenance.M3Spec; return true;
            case "avalonia-policy": provenance = TokenProvenance.AvaloniaPolicy; return true;
            default: provenance = default; return false;
        }
    }

    public static string ToText(TokenProvenance provenance) => provenance switch
    {
        TokenProvenance.AndroidXToken => "androidx-token",
        TokenProvenance.ComposeDefault => "compose-default",
        TokenProvenance.M3Spec => "m3-spec",
        TokenProvenance.AvaloniaPolicy => "avalonia-policy",
        _ => throw new ArgumentOutOfRangeException(nameof(provenance), provenance, null)
    };

    public static TokenProvenance Infer(TokenTableCategory category)
    {
        return category switch
        {
            TokenTableCategory.ReferenceTokens or
                TokenTableCategory.SystemTokens or
                TokenTableCategory.ComponentTokens => TokenProvenance.AndroidXToken,
            TokenTableCategory.ComponentDefaults => TokenProvenance.ComposeDefault,
            _ => TokenProvenance.ComposeDefault
        };
    }
}

/// <summary>
/// Parser output. The generator consumes the typed <see cref="TokenTable"/> wrappers declared in
/// <c>TypedTokenTables.cs</c>; this intermediate keeps the JSON grammar implementation isolated
/// from the emitters.
/// </summary>
internal sealed class ParsedTokenTable
{
    private ParsedTokenTable(string kind, string declaredKind, TokenTableCategory category, string name, string @namespace, string? version,
        string? source, string? summary, TokenProvenance provenance, string? overridesName)
    {
        Kind = kind;
        DeclaredKind = declaredKind;
        Category = category;
        Name = name;
        Namespace = @namespace;
        Version = version;
        Source = source;
        Summary = summary;
        Provenance = provenance;
        OverridesName = overridesName;
    }

    public string Kind { get; }

    /// <summary>The explicit root <c>kind</c>; <see cref="Kind"/> is the normalized payload shape.</summary>
    public string DeclaredKind { get; }

    /// <summary>The schema category declared by the source file.</summary>
    public TokenTableCategory Category { get; }

    public string Name { get; }

    public string Namespace { get; }

    /// <summary>The upstream token-DB stamp, carried into the generated file's header.</summary>
    public string? Version { get; }

    public string? Source { get; }

    public string? Summary { get; }

    /// <summary>Machine-readable origin of the table's values or assembly.</summary>
    public TokenProvenance Provenance { get; }

    /// <summary>Optional compatibility name for the generated public override bag.</summary>
    public string? OverridesName { get; }

    /// <summary>Populated for <c>tokenSet</c> tables.</summary>
    public IReadOnlyList<TokenEntry> Tokens { get; private set; } = System.Array.Empty<TokenEntry>();

    /// <summary>Populated for <c>component</c> tables.</summary>
    public IReadOnlyList<TokenProperty> Properties { get; private set; } = System.Array.Empty<TokenProperty>();

    /// <summary>Populated for <c>component</c> tables.</summary>
    public IReadOnlyList<TokenSet> Sets { get; private set; } = System.Array.Empty<TokenSet>();

    /// <summary>The enum naming the sets, when a component table declares more than a default.</summary>
    public string? VariantEnum { get; private set; }

    public string? VariantEnumSummary { get; private set; }

    /// <summary>Whether to emit the sparse <c>*Overrides</c> authoring bag and theme plumbing.</summary>
    public bool Overridable { get; private set; }

    /// <summary>
    /// The record holding this table's colour tokens resolved to brushes, when it has any.
    /// </summary>
    public string? ResolvedName { get; private set; }

    public IReadOnlyList<TokenProperty> ColorProperties =>
        Properties.Where(p => p.Type == TokenType.Color).ToArray();

    public static ParsedTokenTable Parse(string json)
    {
        var root = JsonParser.Parse(json);
        if (root.Kind != JsonKind.Object)
            throw new InvalidTableException("A token table must be a JSON object.");

        var kindText = root["kind"].AsString() ?? throw new InvalidTableException("'kind' is required.");
        var (kind, category) = ParseKind(kindText, root);

        var name = Identifier(root["name"].AsString(), "name");
        var @namespace = root["namespace"].AsString() ?? "Material3Expressive.Foundation";

        var provenanceText = root["provenance"].AsString();
        var provenance = provenanceText is null
            ? TokenProvenances.Infer(category)
            : TokenProvenances.TryParse(provenanceText, out var declaredProvenance)
                ? declaredProvenance
                : throw new InvalidTableException(
                    $"'provenance' must be 'androidx-token', 'compose-default', 'm3-spec', or 'avalonia-policy', not '{provenanceText}'.");

        var table = new ParsedTokenTable(
            kind,
            kindText,
            category,
            name,
            @namespace,
            root["version"].AsString(),
            root["source"].AsString(),
            root["summary"].AsString(),
            provenance,
            root["overridesName"].AsString() is { } overridesName
                ? Identifier(overridesName, "overridesName")
                : null);

        if (kind == "tokenSet")
            table.ParseTokens(root);
        else
            table.ParseComponent(root);

        return table;
    }

    private static (string Kind, TokenTableCategory Category) ParseKind(string kind, JsonValue root)
    {
        switch (kind)
        {
            case "referenceTokens":
                return ("tokenSet", TokenTableCategory.ReferenceTokens);
            case "componentTokens":
                return ("tokenSet", TokenTableCategory.ComponentTokens);
            case "componentDefaults":
                return ("component", TokenTableCategory.ComponentDefaults);
            case "systemTokens":
                // System typography is a scheme-shaped table (TypeScale), while elevation/state
                // are direct token sets. Both are system tokens and share one schema category.
                var hasTokens = root["tokens"].Kind == JsonKind.Object;
                var hasProperties = root["properties"].Kind == JsonKind.Object;
                if (hasTokens == hasProperties)
                    throw new InvalidTableException(
                        "A 'systemTokens' table must define exactly one of 'tokens' or 'properties'.");

                return (hasProperties ? "component" : "tokenSet", TokenTableCategory.SystemTokens);
            default:
                throw new InvalidTableException(
                    $"'kind' must be 'referenceTokens', 'systemTokens', 'componentTokens', or 'componentDefaults', not '{kind}'.");
        }
    }

    private void ParseTokens(JsonValue root)
    {
        var tokens = root["tokens"];
        if (tokens.Kind != JsonKind.Object)
            throw new InvalidTableException("A 'tokenSet' table needs a 'tokens' object.");

        var entries = new List<TokenEntry>();
        foreach (var member in tokens.Members)
        {
            var entryName = Identifier(member.Key, "token name");
            var body = member.Value;

            if (body.Kind != JsonKind.Object)
                throw new InvalidTableException($"Token '{entryName}' must be an object with 'type' and 'value'.");

            var typeText = body["type"].AsString();
            if (!TokenTypes.TryParse(typeText, out var type))
                throw new InvalidTableException($"Token '{entryName}' has unknown type '{typeText}'.");

            var enumType = body["enumType"].AsString();

            entries.Add(new TokenEntry(
                entryName,
                type,
                ValueEmitter.Emit(entryName, type, body["value"], enumType),
                body["summary"].AsString(),
                enumType));
        }

        Tokens = entries
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToArray();
    }

    private void ParseComponent(JsonValue root)
    {
        var properties = root["properties"];
        if (properties.Kind != JsonKind.Object)
            throw new InvalidTableException("A 'component' table needs a 'properties' object.");

        var declared = new List<TokenProperty>();
        var types = new Dictionary<string, TokenType>(StringComparer.Ordinal);

        foreach (var member in properties.Members)
        {
            var propertyName = Identifier(member.Key, "property name");
            var body = member.Value;

            string? typeText;
            string? summary = null;
            string? opacity = null;
            string? enumType = null;

            if (body.Kind == JsonKind.String)
            {
                typeText = body.AsString();
            }
            else if (body.Kind == JsonKind.Object)
            {
                typeText = body["type"].AsString();
                summary = body["summary"].AsString();
                opacity = body["opacity"].AsString();
                enumType = body["enumType"].AsString();
            }
            else
            {
                throw new InvalidTableException($"Property '{propertyName}' must be a type name or an object.");
            }

            if (!TokenTypes.TryParse(typeText, out var type))
                throw new InvalidTableException($"Property '{propertyName}' has unknown type '{typeText}'.");

            if (opacity is { } && type != TokenType.Color)
                throw new InvalidTableException($"Property '{propertyName}' is not a colour, so it cannot take an 'opacity'.");

            if (type == TokenType.Enum && enumType is null)
                throw new InvalidTableException($"Property '{propertyName}' is an enum, so it must name its 'enumType'.");

            declared.Add(new TokenProperty(propertyName, type, summary, opacity, enumType));
            types[propertyName] = type;
        }

        foreach (var property in declared)
        {
            if (property.OpacityProperty is not { } opacityName)
                continue;

            if (!types.TryGetValue(opacityName, out var opacityType) || opacityType != TokenType.Number)
                throw new InvalidTableException(
                    $"Property '{property.Name}' names '{opacityName}' as its opacity, which is not a declared number property.");
        }

        if (declared.Count == 0)
            throw new InvalidTableException("A 'component' table needs at least one property.");

        Properties = declared;

        var sets = root["sets"];
        if (sets.Kind != JsonKind.Array || sets.Items.Count == 0)
            throw new InvalidTableException("A 'component' table needs a non-empty 'sets' array.");

        var enumTypes = declared.ToDictionary(p => p.Name, p => p.EnumType, StringComparer.Ordinal);
        var byName = new Dictionary<string, Dictionary<string, TokenValue>>(StringComparer.Ordinal);
        var parsed = new List<TokenSet>();

        foreach (var item in sets.Items)
        {
            if (item.Kind != JsonKind.Object)
                throw new InvalidTableException("Each entry of 'sets' must be an object.");

            var setName = Identifier(item["name"].AsString(), "set name");
            var values = new Dictionary<string, TokenValue>(StringComparer.Ordinal);

            if (item["extends"].AsString() is { } baseName)
            {
                if (!byName.TryGetValue(baseName, out var inherited))
                    throw new InvalidTableException(
                        $"Set '{setName}' extends '{baseName}', which is not defined above it.");

                foreach (var pair in inherited)
                    values[pair.Key] = pair.Value;
            }

            var declaredValues = item["values"];
            if (declaredValues.Kind != JsonKind.Object)
                throw new InvalidTableException($"Set '{setName}' needs a 'values' object.");

            foreach (var member in declaredValues.Members)
            {
                if (!types.TryGetValue(member.Key, out var type))
                    throw new InvalidTableException(
                        $"Set '{setName}' assigns '{member.Key}', which is not a declared property.");

                values[member.Key] = ValueEmitter.Emit(
                    $"{setName}.{member.Key}", type, member.Value, enumTypes[member.Key]);
            }

            var missing = declared.Where(p => !values.ContainsKey(p.Name)).Select(p => p.Name).ToArray();
            if (missing.Length > 0)
                throw new InvalidTableException(
                    $"Set '{setName}' is missing a value for {string.Join(", ", missing)}.");

            byName[setName] = values;
            parsed.Add(new TokenSet(setName, item["summary"].AsString(), values));
        }

        Sets = parsed;
        VariantEnum = root["variantEnum"].AsString() is { } variantEnum
            ? Identifier(variantEnum, "variantEnum")
            : null;
        VariantEnumSummary = root["variantEnumSummary"].AsString();
        Overridable = root["overridable"].AsBoolean() ?? true;

        if (VariantEnum is null && parsed.Count > 1)
            throw new InvalidTableException("A table with more than one set must name a 'variantEnum'.");

        if (ColorProperties.Count > 0)
            ResolvedName = root["resolvedName"].AsString() is { } resolved
                ? Identifier(resolved, "resolvedName")
                : "Resolved" + Name;
    }

    private static string Identifier(string? text, string what)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidTableException($"'{what}' is required.");

        var value = text!.Trim();
        if (!char.IsLetter(value[0]) && value[0] != '_')
            throw new InvalidTableException($"'{value}' is not a valid {what}.");

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                throw new InvalidTableException($"'{value}' is not a valid {what}.");
        }

        return value;
    }
}

internal sealed class InvalidTableException : Exception
{
    public InvalidTableException(string message) : base(message)
    {
    }
}

/// <summary>
/// Turns a table's JSON value into the C# initialiser for its declared type.
/// </summary>
/// <remarks>
/// A string beginning with <c>@</c> is a reference to another generated member — that is how a
/// component set points at the verbatim upstream transcription rather than restating its value.
/// </remarks>
internal static class ValueEmitter
{
    private const string TokensNamespace = "global::Material3Expressive.Foundation.Tokens";

    public static TokenValue Emit(string context, TokenType type, JsonValue value, string? enumType = null)
    {
        if (value.IsNull)
            throw new InvalidTableException($"'{context}' has no value.");

        if (value.Kind == JsonKind.String && value.AsString() is { Length: > 0 } text && text[0] == '@')
            return Reference(context, type, text.Substring(1), enumType);

        var expression = type switch
        {
            TokenType.Color => Color(context, value),
            TokenType.Shape => EnumMember(context, "global::Material3Expressive.Foundation.ShapeRole", value),
            TokenType.Typography => EnumMember(context, "global::Material3Expressive.Foundation.TypographyRole", value),
            TokenType.Motion => EnumMember(context, "global::Material3Expressive.Foundation.MotionRole", value),
            TokenType.Enum => EnumMember(context, TokenTypes.ToClrType(TokenType.Enum, enumType), value),
            TokenType.Boolean => value.AsBoolean() is { } flag
                ? flag ? "true" : "false"
                : throw new InvalidTableException($"'{context}' must be true or false."),
            _ => Number(context, value)
        };

        return new TokenValue(type, expression, enumType, reference: null);
    }

    private static TokenValue Reference(string context, TokenType type, string path, string? enumType)
    {
        if (path.Length == 0)
            throw new InvalidTableException($"'{context}' has an empty '@' reference.");

        foreach (var c in path)
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '.')
                throw new InvalidTableException($"'{context}' has a malformed reference '@{path}'.");
        }

        // Unqualified references name a member of a generated token set. Qualified references are
        // retained as structured data so the catalog can check existence, type and layer before
        // the expression is emitted.
        if (path.IndexOf('.') < 0)
            return new TokenValue(type, path, enumType, reference: null);

        var separator = path.IndexOf('.');
        var hasOneSeparator = separator > 0 && separator < path.Length - 1 &&
                              path.IndexOf('.', separator + 1) < 0;
        if (!hasOneSeparator)
            throw new InvalidTableException($"'{context}' has a malformed reference '@{path}'.");

        var reference = new TokenReference(path.Substring(0, separator), path.Substring(separator + 1));

        return new TokenValue(type, $"{TokensNamespace}.{path}", enumType, reference);
    }

    private static string Number(string context, JsonValue value)
    {
        var number = value.AsNumber()
                     ?? throw new InvalidTableException($"'{context}' must be a number.");

        return number == Math.Floor(number) && Math.Abs(number) < 1e15
            ? number.ToString("0", CultureInfo.InvariantCulture)
            : number.ToString("R", CultureInfo.InvariantCulture);
    }

    private static string EnumMember(string context, string enumType, JsonValue value)
    {
        var text = value.AsString()
                   ?? throw new InvalidTableException($"'{context}' must name a member of {enumType}.");

        return $"{enumType}.{Member(context, text)}";
    }

    private static string Color(string context, JsonValue value)
    {
        var text = value.AsString()
                   ?? throw new InvalidTableException($"'{context}' must be a colour role or a literal colour.");

        if (text.StartsWith("#", StringComparison.Ordinal))
            return $"global::Material3Expressive.Foundation.ColorTokenValue.Literal(global::Avalonia.Media.Color.Parse(\"{text}\"))";

        var separator = text.IndexOf(':');
        if (separator > 0)
        {
            var role = Member(context, text.Substring(0, separator));
            var key = text.Substring(separator + 1);
            return "global::Material3Expressive.Foundation.ColorTokenValue.Role(" +
                   $"global::MaterialColorUtilities.Avalonia.Tokens.SysColorToken.{role}, \"{key}\")";
        }

        return "global::MaterialColorUtilities.Avalonia.Tokens.SysColorToken." + Member(context, text);
    }

    private static string Member(string context, string text)
    {
        var value = text.Trim();
        if (value.Length == 0)
            throw new InvalidTableException($"'{context}' has an empty value.");

        foreach (var c in value)
        {
            if (!char.IsLetterOrDigit(c) && c != '_')
                throw new InvalidTableException($"'{context}' has a malformed value '{text}'.");
        }

        return value;
    }
}

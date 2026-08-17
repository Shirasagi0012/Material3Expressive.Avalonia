using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Material3Expressive.TokenGenerator.Json;

namespace Material3Expressive.TokenGenerator.Model;

/// <summary>Validated, read-only declarations supplied by an external canonical token owner.</summary>
internal sealed class ExternalCanonicalManifest : IEquatable<ExternalCanonicalManifest>
{
    public const string FileSuffix = ".canonical-tokens.json";
    public const string ExpectedSchema = "material-design-token-canonical-declarations";
    public const int SupportedSchemaVersion = 1;
    public const string ExpectedOwner = "MaterialColorUtilities.Avalonia";
    public const string ExpectedContributionId = "MaterialColorUtilities.Avalonia.ColorTokens";
    public const int ExpectedContributionSchemaVersion = 1;

    private readonly IReadOnlyDictionary<string, ExternalCanonicalDeclaration> _standardColors;
    private readonly IReadOnlyDictionary<string, ExternalCanonicalDeclaration> _customColors;

    private ExternalCanonicalManifest(
        string owner,
        string contributionId,
        int contributionSchemaVersion,
        IReadOnlyList<ExternalCanonicalDeclaration> declarations)
    {
        Owner = owner;
        ContributionId = contributionId;
        ContributionSchemaVersion = contributionSchemaVersion;
        Declarations = declarations;
        _standardColors = declarations
            .Where(static declaration => declaration.CustomRole is null)
            .ToDictionary(static declaration => declaration.Role, StringComparer.Ordinal);
        _customColors = declarations
            .Where(static declaration => declaration.CustomRole is not null)
            .ToDictionary(static declaration => declaration.Role, StringComparer.Ordinal);
    }

    public string Owner { get; }

    public string ContributionId { get; }

    public int ContributionSchemaVersion { get; }

    public IReadOnlyList<ExternalCanonicalDeclaration> Declarations { get; }

    public static ExternalCanonicalManifest Parse(string json)
    {
        var root = JsonParser.Parse(json);
        RequireKind(root, JsonKind.Object, "The external canonical declaration manifest must be an object.");

        var schema = RequiredString(root, "schema");
        if (!string.Equals(schema, ExpectedSchema, StringComparison.Ordinal))
            throw Invalid($"'schema' must be '{ExpectedSchema}', not '{schema}'.");

        var schemaVersion = RequiredPositiveInteger(root, "schemaVersion");
        if (schemaVersion != SupportedSchemaVersion)
            throw Invalid($"schema version {schemaVersion.ToString(CultureInfo.InvariantCulture)} is incompatible; version {SupportedSchemaVersion} is required.");

        var owner = RequiredString(root, "owner");
        if (!string.Equals(owner, ExpectedOwner, StringComparison.Ordinal))
            throw Invalid($"'owner' must be '{ExpectedOwner}', not '{owner}'.");

        var contribution = root["contribution"];
        RequireKind(contribution, JsonKind.Object, "'contribution' must be an object.");
        var contributionId = RequiredString(contribution, "id");
        if (!string.Equals(contributionId, ExpectedContributionId, StringComparison.Ordinal))
            throw Invalid($"contribution 'id' must be '{ExpectedContributionId}', not '{contributionId}'.");

        var contributionSchemaVersion = RequiredPositiveInteger(contribution, "schemaVersion");
        if (contributionSchemaVersion != ExpectedContributionSchemaVersion)
            throw Invalid($"contribution schema {contributionSchemaVersion.ToString(CultureInfo.InvariantCulture)} is incompatible; schema {ExpectedContributionSchemaVersion} is required.");

        var declarationValues = root["declarations"];
        RequireKind(declarationValues, JsonKind.Array, "'declarations' must be an array.");
        if (declarationValues.Items.Count == 0)
            throw Invalid("'declarations' must not be empty.");

        var declarations = new List<ExternalCanonicalDeclaration>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        var roles = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in declarationValues.Items)
        {
            RequireKind(value, JsonKind.Object, "Each external declaration must be an object.");
            var path = RequiredString(value, "path");
            if (!path.StartsWith("System.Color.", StringComparison.Ordinal) || path.Length == "System.Color.".Length)
                throw Invalid($"declaration path '{path}' is not a System.Color canonical path.");
            if (!paths.Add(path))
                throw Invalid($"declaration path '{path}' is declared twice.");

            var layer = RequiredString(value, "layer");
            if (!string.Equals(layer, "System", StringComparison.Ordinal))
                throw Invalid($"declaration '{path}' must use layer 'System', not '{layer}'.");

            var valueType = RequiredString(value, "valueType");
            if (!string.Equals(valueType, "Avalonia.Media.Color", StringComparison.Ordinal))
                throw Invalid($"declaration '{path}' must use value type 'Avalonia.Media.Color', not '{valueType}'.");

            var provenance = RequiredString(value, "provenance");
            if (!string.Equals(provenance, "material-color-utilities", StringComparison.Ordinal))
                throw Invalid($"declaration '{path}' has unsupported provenance '{provenance}'.");

            var family = RequiredString(value, "family");
            if (!string.Equals(family, "MaterialColorUtilities.Avalonia.SystemColor", StringComparison.Ordinal))
                throw Invalid($"declaration '{path}' has incompatible family '{family}'.");

            var role = RequiredString(value, "role");
            if (!IsIdentifier(role))
                throw Invalid($"declaration '{path}' has invalid role '{role}'.");
            if (!roles.Add(role))
                throw Invalid($"canonical role '{role}' is declared twice.");

            var symbol = OptionalString(value, "canonicalSymbol");
            var factory = OptionalString(value, "canonicalFactory");
            if ((symbol is null) == (factory is null))
                throw Invalid($"declaration '{path}' must name exactly one canonical symbol or factory.");

            var isCustom = path.EndsWith(".*", StringComparison.Ordinal);
            if (isCustom)
            {
                if (factory is null || symbol is not null || !factory.Contains("{customName}", StringComparison.Ordinal))
                    throw Invalid($"custom declaration '{path}' must use a canonical factory containing '{{customName}}'.");

                var expectedPath = "System.Color." + role + ".*";
                var expectedFactory =
                    "global::MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens.Get(" +
                    "global::MaterialColorUtilities.Avalonia.Tokens.SysColorToken." + role +
                    ", {customName})";
                if (!string.Equals(path, expectedPath, StringComparison.Ordinal) ||
                    !string.Equals(factory, expectedFactory, StringComparison.Ordinal))
                    throw Invalid($"custom declaration '{path}' has an incompatible canonical factory for role '{role}'.");
            }
            else if (symbol is null || factory is not null)
            {
                throw Invalid($"finite declaration '{path}' must use a canonical singleton symbol.");
            }
            else
            {
                var expectedPath = "System.Color." + role;
                var expectedSymbol =
                    "global::MaterialColorUtilities.Avalonia.Tokens.SystemColorTokens." + role;
                if (!string.Equals(path, expectedPath, StringComparison.Ordinal) ||
                    !string.Equals(symbol, expectedSymbol, StringComparison.Ordinal))
                    throw Invalid($"finite declaration '{path}' has an incompatible canonical symbol for role '{role}'.");
            }

            declarations.Add(new ExternalCanonicalDeclaration(
                path,
                role,
                isCustom ? role : null,
                symbol,
                factory,
                provenance,
                family));
        }

        declarations.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Path, right.Path));
        return new ExternalCanonicalManifest(owner, contributionId, contributionSchemaVersion, declarations);
    }

    public bool TryResolve(ColorRoleReference reference, out ExternalCanonicalDeclaration declaration)
    {
        return reference.CustomName is null
            ? _standardColors.TryGetValue(reference.Role, out declaration!)
            : _customColors.TryGetValue(reference.Role, out declaration!);
    }

    /// <summary>Finds an external finite identity or parameterized family overlapping a generated path.</summary>
    public bool TryFindOwnershipOverlap(
        string generatedPath,
        out ExternalCanonicalDeclaration declaration)
    {
        foreach (var candidate in Declarations)
        {
            if (candidate.CustomRole is null)
            {
                if (string.Equals(candidate.Path, generatedPath, StringComparison.Ordinal))
                {
                    declaration = candidate;
                    return true;
                }

                continue;
            }

            var familyRoot = candidate.Path.Substring(0, candidate.Path.Length - 2);
            if (string.Equals(generatedPath, familyRoot, StringComparison.Ordinal) ||
                generatedPath.StartsWith(familyRoot + ".", StringComparison.Ordinal))
            {
                declaration = candidate;
                return true;
            }
        }

        declaration = null!;
        return false;
    }

    public bool Equals(ExternalCanonicalManifest? other)
    {
        return other is not null &&
               string.Equals(Owner, other.Owner, StringComparison.Ordinal) &&
               string.Equals(ContributionId, other.ContributionId, StringComparison.Ordinal) &&
               ContributionSchemaVersion == other.ContributionSchemaVersion &&
               Declarations.SequenceEqual(other.Declarations);
    }

    public override bool Equals(object? obj) => Equals(obj as ExternalCanonicalManifest);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = (Owner.GetHashCode() * 397) ^ ContributionId.GetHashCode();
            hash = (hash * 397) ^ ContributionSchemaVersion;
            foreach (var declaration in Declarations)
                hash = (hash * 397) ^ declaration.GetHashCode();
            return hash;
        }
    }

    private static string RequiredString(JsonValue value, string name)
    {
        var text = value[name].Kind == JsonKind.String ? value[name].AsString() : null;
        if (string.IsNullOrWhiteSpace(text))
            throw Invalid($"'{name}' is required and must be a non-empty string.");
        return text!;
    }

    private static string? OptionalString(JsonValue value, string name)
    {
        if (value[name].IsNull)
            return null;
        if (value[name].Kind != JsonKind.String || string.IsNullOrWhiteSpace(value[name].AsString()))
            throw Invalid($"'{name}' must be a non-empty string when present.");
        return value[name].AsString();
    }

    private static int RequiredPositiveInteger(JsonValue value, string name)
    {
        var number = value[name].AsNumber();
        if (number is null || number <= 0 || number != Math.Floor(number.Value) || number > int.MaxValue)
            throw Invalid($"'{name}' must be a positive integer.");
        return (int)number.Value;
    }

    private static void RequireKind(JsonValue value, JsonKind kind, string message)
    {
        if (value.Kind != kind)
            throw Invalid(message);
    }

    private static bool IsIdentifier(string text)
    {
        if (text.Length == 0 || !char.IsLetter(text[0]) && text[0] != '_')
            return false;
        return text.All(static character => char.IsLetterOrDigit(character) || character == '_');
    }

    private static InvalidManifestException Invalid(string message) => new(message);
}

internal sealed class ExternalCanonicalDeclaration : IEquatable<ExternalCanonicalDeclaration>
{
    public ExternalCanonicalDeclaration(string path, string role, string? customRole,
        string? canonicalSymbol, string? canonicalFactory, string provenance, string family)
    {
        Path = path;
        Role = role;
        CustomRole = customRole;
        CanonicalSymbol = canonicalSymbol;
        CanonicalFactory = canonicalFactory;
        Provenance = provenance;
        Family = family;
    }

    public string Path { get; }
    public string Role { get; }
    public string? CustomRole { get; }
    public string? CanonicalSymbol { get; }
    public string? CanonicalFactory { get; }
    public string Provenance { get; }
    public string Family { get; }

    public string EmitCanonicalReference(string? customName)
    {
        if (CanonicalSymbol is { } symbol)
            return symbol;
        if (customName is null)
            throw new InvalidOperationException("A canonical factory requires a custom name.");
        return CanonicalFactory!.Replace("{customName}", Quote(customName));
    }

    public bool Equals(ExternalCanonicalDeclaration? other)
    {
        return other is not null && Path == other.Path && Role == other.Role &&
               CustomRole == other.CustomRole && CanonicalSymbol == other.CanonicalSymbol &&
               CanonicalFactory == other.CanonicalFactory && Provenance == other.Provenance &&
               Family == other.Family;
    }

    public override bool Equals(object? obj) => Equals(obj as ExternalCanonicalDeclaration);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Path.GetHashCode();
            hash = hash * 397 ^ Role.GetHashCode();
            hash = hash * 397 ^ (CanonicalSymbol?.GetHashCode() ?? 0);
            hash = hash * 397 ^ (CanonicalFactory?.GetHashCode() ?? 0);
            return hash;
        }
    }

    private static string Quote(string text)
    {
        return "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"")
            .Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t") + "\"";
    }
}

internal sealed class InvalidManifestException : Exception
{
    public InvalidManifestException(string message) : base(message)
    {
    }
}

using Avalonia.Styling;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Foundation.Tokens;

internal enum RawTokenCatalogLookupKind
{
    Definition,
    CatalogRequiredMissing,
    SourceRequiredMissing,
}

internal readonly record struct RawTokenCatalogLookup(
    RawTokenCatalogLookupKind Kind,
    TokenDefinition? Definition,
    string? ContributionId)
{
    internal static RawTokenCatalogLookup Defined(
        TokenDefinition definition,
        string contributionId) =>
        new(RawTokenCatalogLookupKind.Definition, definition, contributionId);

    internal static RawTokenCatalogLookup CatalogRequired(string contributionId) =>
        new(RawTokenCatalogLookupKind.CatalogRequiredMissing, null, contributionId);

    internal static RawTokenCatalogLookup SourceRequired(string? contributionId = null) =>
        new(RawTokenCatalogLookupKind.SourceRequiredMissing, null, contributionId);
}

internal interface IRawTokenCatalog
{
    RawTokenCatalogLookup Lookup(MaterialToken token, ThemeVariant? queryTheme);
}

internal sealed class CompositeRawTokenCatalog : IRawTokenCatalog
{
    private readonly IReadOnlyList<IMaterialTokenCatalogContribution> _contributions;
    private readonly IReadOnlyDictionary<string, IMaterialTokenCatalogContribution> _byId;

    private CompositeRawTokenCatalog(
        IReadOnlyList<IMaterialTokenCatalogContribution> contributions,
        IReadOnlyDictionary<string, IMaterialTokenCatalogContribution> byId)
    {
        _contributions = contributions;
        _byId = byId;
    }

    internal static CompositeRawTokenCatalog Create(
        params IMaterialTokenCatalogContribution[] contributions) =>
        Create((IEnumerable<IMaterialTokenCatalogContribution>)contributions);

    internal static CompositeRawTokenCatalog Create(
        IEnumerable<IMaterialTokenCatalogContribution> contributions)
    {
        ArgumentNullException.ThrowIfNull(contributions);

        var snapshot = contributions.ToArray();
        if (snapshot.Any(static contribution => contribution is null))
        {
            throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.InvalidCatalogContribution,
                "A raw-token catalog contribution cannot be null.");
        }

        var byId = new Dictionary<string, IMaterialTokenCatalogContribution>(StringComparer.Ordinal);
        foreach (var contribution in snapshot)
        {
            if (string.IsNullOrWhiteSpace(contribution.Id) || contribution.SchemaVersion <= 0)
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.InvalidCatalogContribution,
                    "A raw-token catalog contribution must have a non-empty id and a positive schema version.");
            }

            if (!byId.TryAdd(contribution.Id, contribution))
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.DuplicateCatalogContribution,
                    $"Raw-token catalog contribution id '{contribution.Id}' is duplicated.",
                    contributionIds: [contribution.Id]);
            }
        }

        ValidateRequirements(snapshot, byId);
        ValidateDeclaredOwnership(snapshot);
        ValidateFamilyOwnership(snapshot);

        return new CompositeRawTokenCatalog(
            Array.AsReadOnly(snapshot),
            byId);
    }

    public RawTokenCatalogLookup Lookup(MaterialToken token, ThemeVariant? queryTheme)
    {
        ArgumentNullException.ThrowIfNull(token);

        var owners = FindOwners(token);
        if (owners.Count == 0)
            return RawTokenCatalogLookup.SourceRequired();

        if (owners.Count > 1)
            throw DuplicateOwner(token, owners);

        var owner = owners[0];
        foreach (var candidateTheme in EnumerateThemeCandidates(queryTheme))
        {
            if (!owner.TryGetDefinition(token, candidateTheme, out var definition))
                continue;

            if (definition is null)
            {
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.InvalidCatalogContribution,
                    $"Catalog contribution '{owner.Id}' returned a null definition for '{token.Path}'.",
                    token: token,
                    theme: candidateTheme,
                    contributionIds: [owner.Id]);
            }

            return RawTokenCatalogLookup.Defined(definition, owner.Id);
        }

        return owner.GetMissingPolicy(token) switch
        {
            MaterialTokenMissingPolicy.CatalogRequired =>
                RawTokenCatalogLookup.CatalogRequired(owner.Id),
            MaterialTokenMissingPolicy.SourceRequired =>
                RawTokenCatalogLookup.SourceRequired(owner.Id),
            _ => throw MaterialResolutionFailures.Create(
                MaterialResolutionFailureKind.InvalidCatalogContribution,
                $"Catalog contribution '{owner.Id}' returned an unknown missing policy for '{token.Path}'.",
                token: token,
                theme: queryTheme,
                contributionIds: [owner.Id]),
        };
    }

    private static IEnumerable<ThemeVariant?> EnumerateThemeCandidates(ThemeVariant? queryTheme)
    {
        if (queryTheme is null)
        {
            yield return ThemeVariant.Default;
            yield break;
        }

        var seen = new HashSet<ThemeVariant>();
        var current = queryTheme;
        while (current is not null && seen.Add(current))
        {
            yield return current;
            if (current == ThemeVariant.Default)
                yield break;

            current = current.InheritVariant;
        }

        if (seen.Add(ThemeVariant.Default))
            yield return ThemeVariant.Default;
    }

    private static void ValidateRequirements(
        IReadOnlyList<IMaterialTokenCatalogContribution> contributions,
        IReadOnlyDictionary<string, IMaterialTokenCatalogContribution> byId)
    {
        foreach (var contribution in contributions)
        {
            var requirements = contribution.Requirements ??
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.InvalidCatalogContribution,
                    $"Catalog contribution '{contribution.Id}' returned null requirements.",
                    contributionIds: [contribution.Id]);

            foreach (var requirement in requirements)
            {
                var validRange = !string.IsNullOrWhiteSpace(requirement.ContributionId) &&
                    requirement.MinimumSchemaVersion > 0 &&
                    requirement.MaximumSchemaVersion >= requirement.MinimumSchemaVersion;
                var satisfied = validRange &&
                    byId.TryGetValue(requirement.ContributionId, out var dependency) &&
                    dependency.SchemaVersion >= requirement.MinimumSchemaVersion &&
                    dependency.SchemaVersion <= requirement.MaximumSchemaVersion;

                if (!satisfied)
                {
                    throw MaterialResolutionFailures.Create(
                        MaterialResolutionFailureKind.MissingCatalogRequirement,
                        $"Catalog contribution '{contribution.Id}' requires '{requirement.ContributionId}' " +
                        $"schema {requirement.MinimumSchemaVersion}..{requirement.MaximumSchemaVersion}.",
                        contributionIds: [contribution.Id, requirement.ContributionId]);
                }
            }
        }
    }

    private static void ValidateFamilyOwnership(
        IReadOnlyList<IMaterialTokenCatalogContribution> contributions)
    {
        var owners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var contribution in contributions)
        {
            var families = contribution.DeclaredFamilies ??
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.InvalidCatalogContribution,
                    $"Catalog contribution '{contribution.Id}' returned null declared families.",
                    contributionIds: [contribution.Id]);

            foreach (var family in families)
            {
                if (string.IsNullOrWhiteSpace(family.Id))
                {
                    throw MaterialResolutionFailures.Create(
                        MaterialResolutionFailureKind.InvalidCatalogContribution,
                        $"Catalog contribution '{contribution.Id}' declared an invalid parameterized family.",
                        contributionIds: [contribution.Id]);
                }

                if (owners.TryGetValue(family.Id, out var existing))
                {
                    throw MaterialResolutionFailures.Create(
                        MaterialResolutionFailureKind.DuplicateCatalogOwner,
                        $"Parameterized raw-token family '{family.Id}' is owned by multiple catalog " +
                        $"contributions: {existing}, {contribution.Id}.",
                        contributionIds: [existing, contribution.Id]);
                }

                owners.Add(family.Id, contribution.Id);
            }
        }
    }

    private static void ValidateDeclaredOwnership(
        IReadOnlyList<IMaterialTokenCatalogContribution> contributions)
    {
        var declared = new HashSet<MaterialToken>(ReferenceEqualityComparer.Instance);
        foreach (var contribution in contributions)
        {
            var tokens = contribution.DeclaredTokens ??
                throw MaterialResolutionFailures.Create(
                    MaterialResolutionFailureKind.InvalidCatalogContribution,
                    $"Catalog contribution '{contribution.Id}' returned null declared tokens.",
                    contributionIds: [contribution.Id]);

            foreach (var token in tokens)
            {
                if (token is null || !contribution.Owns(token))
                {
                    throw MaterialResolutionFailures.Create(
                        MaterialResolutionFailureKind.InvalidCatalogContribution,
                        $"Catalog contribution '{contribution.Id}' declared a token it does not own.",
                        token: token,
                        contributionIds: [contribution.Id]);
                }

                declared.Add(token);
            }
        }

        foreach (var token in declared)
        {
            var owners = contributions.Where(contribution => contribution.Owns(token)).ToArray();
            if (owners.Length > 1)
                throw DuplicateOwner(token, owners);
        }
    }

    private List<IMaterialTokenCatalogContribution> FindOwners(MaterialToken token)
    {
        var owners = new List<IMaterialTokenCatalogContribution>(1);
        foreach (var contribution in _contributions)
        {
            if (contribution.Owns(token))
                owners.Add(contribution);
        }

        return owners;
    }

    private static MaterialValueResolutionException DuplicateOwner(
        MaterialToken token,
        IEnumerable<IMaterialTokenCatalogContribution> owners)
    {
        var ids = owners.Select(static owner => owner.Id).ToArray();
        return MaterialResolutionFailures.Create(
            MaterialResolutionFailureKind.DuplicateCatalogOwner,
            $"Raw token '{token.Path}' is owned by multiple catalog contributions: {string.Join(", ", ids)}.",
            token: token,
            contributionIds: ids);
    }
}

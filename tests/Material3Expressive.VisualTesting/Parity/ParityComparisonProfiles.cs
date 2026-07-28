using Material3Expressive.VisualTesting.Storage;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>
/// Auditable comparison profiles. A profile never registers or translates images; it only states
/// which contract dimensions a case owns and their fixed physical-pixel tolerances.
/// </summary>
public sealed record ParityComparisonProfile(
    string Id,
    ParityTolerance Tolerance,
    bool CompareGeometry = true,
    bool CompareTextLayout = true,
    bool CompareRaster = true);

public static class ParityComparisonProfiles
{
    private static readonly Lazy<ParityComparisonProfileDocument> Document = new(Load);

    public static ParityComparisonProfile ColorSwatch => Get("color-swatch");
    public static ParityComparisonProfile Container => Get("container");
    public static ParityComparisonProfile ComponentWithText => Get("component-with-text");
    public static ParityComparisonProfile TextField => Get("text-field");

    public static ParityComparisonProfile ForCase(string caseId)
    {
        var rule = Document.Value.CaseProfileRules
            .FirstOrDefault(item => caseId.StartsWith(item.Prefix, StringComparison.Ordinal));
        if (rule is not null)
            return Get(rule.ProfileId);
        throw new InvalidOperationException($"No visual-parity comparison profile is registered for '{caseId}'.");
    }

    private static ParityComparisonProfile Get(string id) =>
        Document.Value.Profiles.SingleOrDefault(item => item.Id == id)
        ?? throw new InvalidOperationException($"Visual-parity comparison profile '{id}' is not defined.");

    private static ParityComparisonProfileDocument Load()
    {
        var path = Path.Combine(RepositoryRoot.Find(), "tests", "VisualParity", "comparison-profiles.json");
        var document = ParityJson.Read<ParityComparisonProfileDocument>(path);
        if (document.SchemaVersion != 2 || document.Profiles.Count == 0 || document.CaseProfileRules.Count == 0)
            throw new InvalidDataException("The visual-parity comparison profile document is incomplete.");
        var ids = document.Profiles.Select(item => item.Id).ToHashSet(StringComparer.Ordinal);
        if (document.Profiles.Any(item => string.IsNullOrWhiteSpace(item.Id)
                                          || item.Tolerance.GeometryPx < 0
                                          || item.Tolerance.TextPx < 0)
            || document.CaseProfileRules.Any(item => string.IsNullOrWhiteSpace(item.Prefix)
                                                      || !ids.Contains(item.ProfileId)))
        {
            throw new InvalidDataException("The visual-parity comparison profile document is invalid.");
        }
        return document;
    }
}

public sealed record ParityComparisonProfileDocument(
    int SchemaVersion,
    IReadOnlyList<ParityComparisonProfile> Profiles,
    IReadOnlyList<ParityCaseProfileRule> CaseProfileRules);

public sealed record ParityCaseProfileRule(string Prefix, string ProfileId);

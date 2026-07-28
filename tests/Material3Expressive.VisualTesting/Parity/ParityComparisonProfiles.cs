using Material3Expressive.VisualTesting.Storage;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Material3Expressive.VisualTesting.Parity;

/// <summary>
/// Auditable comparison profiles. A profile never registers or translates images; it only states
/// which contract dimensions a case owns and their fixed physical-pixel tolerances.
/// </summary>
public sealed record ParityComparisonProfile(
    string Id,
    ParityLayoutTolerance LayoutTolerance,
    bool CompareGeometry = true,
    bool CompareTextLayout = true,
    bool CompareRaster = true,
    ParityRasterPolicy? RasterPolicy = null);

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
        var document = ReadDocument(path);
        if (document.SchemaVersion != 6 || document.Profiles.Count == 0 || document.CaseProfileRules.Count == 0)
            throw new InvalidDataException("The visual-parity comparison profile document is incomplete.");
        var ids = document.Profiles.Select(item => item.Id).ToArray();
        var idSet = ids.ToHashSet(StringComparer.Ordinal);
        if (ids.Length != idSet.Count
            || document.Profiles.Any(item => !IsValidProfile(item))
            || document.CaseProfileRules.Any(item => string.IsNullOrWhiteSpace(item.Prefix)
                                                      || !idSet.Contains(item.ProfileId))
            || HasUnreachableRule(document.CaseProfileRules))
        {
            throw new InvalidDataException("The visual-parity comparison profile document is invalid.");
        }
        return document;
    }

    private static ParityComparisonProfileDocument ReadDocument(string path)
    {
        var options = new JsonSerializerOptions(ParityJson.Options)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
        };
        try
        {
            return JsonSerializer.Deserialize<ParityComparisonProfileDocument>(File.ReadAllText(path), options)
                   ?? throw new InvalidDataException("The visual-parity comparison profile document is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The visual-parity comparison profile document is malformed.", exception);
        }
    }

    private static bool HasUnreachableRule(IReadOnlyList<ParityCaseProfileRule> rules)
    {
        for (var index = 0; index < rules.Count; index++)
        {
            for (var earlier = 0; earlier < index; earlier++)
            {
                if (rules[index].Prefix.StartsWith(rules[earlier].Prefix, StringComparison.Ordinal))
                    return true;
            }
        }
        return false;
    }

    private static bool IsValidProfile(ParityComparisonProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profile.Id)
            || profile.LayoutTolerance is null
            || profile.LayoutTolerance.GeometryPx < 0
            || !float.IsFinite(profile.LayoutTolerance.GeometryPx)
            || profile.LayoutTolerance.TextPx < 0
            || !float.IsFinite(profile.LayoutTolerance.TextPx)
            || profile.LayoutTolerance.TextBaselinePx < 0
            || !float.IsFinite(profile.LayoutTolerance.TextBaselinePx)
            || profile.LayoutTolerance.TextLineBoundsPx < 0
            || !float.IsFinite(profile.LayoutTolerance.TextLineBoundsPx)
            || profile.RasterPolicy is null)
        {
            return false;
        }

        var policy = profile.RasterPolicy;
        if (!string.Equals(policy.Algorithm, "semantic-raster-v1", StringComparison.Ordinal)
            || policy.StrictChannelDelta != 0
            || policy.EdgeDetectionChannelDelta == 0
            || policy.MinimumShadowPaddingPx < 0
            || !float.IsFinite(policy.ShadowPaddingPerElevationPx)
            || policy.ShadowPaddingPerElevationPx < 0
            || (policy.TextInk is not null && (!profile.CompareGeometry || !profile.CompareTextLayout))
            || ((policy.ShadowSupport is not null || policy.ShadowEdge is not null) && !profile.CompareGeometry)
            || (policy.ShadowEdge is not null && policy.ShadowSupport is null))
        {
            return false;
        }

        return IsValidRegion(policy.TextInk)
               && IsValidRegion(policy.ShapeEdge)
               && IsValidRegion(policy.ShadowSupport)
               && IsValidRegion(policy.ShadowEdge);
    }

    private static bool IsValidRegion(ParityRasterRegionPolicy? region)
    {
        if (region is null)
            return true;
        if (region.DilationPx < 0 || region.Budgets is null || region.Budgets.Count == 0)
            return false;

        byte previous = 0;
        foreach (var budget in region.Budgets)
        {
            if (budget.MaxChannelDelta == 0
                || budget.MaxChannelDelta <= previous
                || budget.MinimumPixelsPerOwner < 0
                || budget.BasePixelsPerOwner < 0
                || budget.PixelsPer10kEligiblePixels < 0 || budget.PixelsPer10kEligiblePixels > 10_000
                || budget.MaxPixelsPerOwner < budget.MinimumPixelsPerOwner
                || budget.BasePixelsPerOwner > budget.MaxPixelsPerOwner
                || budget.MinimumEnergyPerOwner < 0
                || budget.BaseEnergyPerOwner < 0
                || budget.EnergyPer10kEligiblePixels < 0
                || budget.MaxEnergyPerOwner < budget.MinimumEnergyPerOwner
                || budget.BaseEnergyPerOwner > budget.MaxEnergyPerOwner
                || budget.MaxConnectedComponentPixels <= 0
                || budget.MaxConnectedComponentThicknessPx <= 0)
            {
                return false;
            }
            previous = budget.MaxChannelDelta;
        }
        return true;
    }
}

public sealed record ParityComparisonProfileDocument(
    int SchemaVersion,
    IReadOnlyList<ParityComparisonProfile> Profiles,
    IReadOnlyList<ParityCaseProfileRule> CaseProfileRules);

public sealed record ParityCaseProfileRule(string Prefix, string ProfileId);

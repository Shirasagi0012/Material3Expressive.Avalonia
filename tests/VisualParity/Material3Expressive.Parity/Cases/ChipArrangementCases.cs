using Material3Expressive.Foundation;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

[ParitySuite("chip-arrangement")]
public sealed class ChipArrangementCases
{
    public static IEnumerable<CaseData<ChipConfig>> StaticCases()
    {
        foreach (var kind in new[] { "assist", "elevated-assist", "suggestion", "elevated-suggestion", "filter", "elevated-filter", "input" })
        foreach (var arrangement in new[] { "Start", "End", "Center", "SpaceBetween", "SpaceAround", "SpaceEvenly" }.Select(n => n.ToLowerInvariant()))
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{arrangement}.{(rtl ? "rtl" : "ltr")}", "Standard row arrangement with explicit width",
                new(kind, Width: 161, Icons: kind.Contains("suggestion") ? "start" : "both", Rtl: rtl, Arrangement: arrangement));
        foreach (var arrangement in new[] { "Start", "End", "Center", "SpaceBetween", "SpaceAround", "SpaceEvenly" }.Select(n => n.ToLowerInvariant()))
        foreach (var width in new[] { 161, 41 })
            yield return new($"static.empty.{arrangement}.{width}", "Empty icon slots and constrained label",
                new("filter", Width: width, Arrangement: arrangement));
        foreach (var kind in new[] { "assist", "elevated-assist", "suggestion", "elevated-suggestion", "filter", "elevated-filter", "input" })
        foreach (var arrangement in new[] { "spaced-start", "spaced-center", "spaced-end", "overlap", "absolute.left", "absolute.right", "absolute.center", "absolute.spacebetween", "absolute.spacearound", "absolute.spaceevenly", "absolute-spaced", "custom" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.extended.{kind}.{arrangement}.{(rtl ? "rtl" : "ltr")}", "Fixed spacing, physical order and application arrangement",
                new(kind, Width: 161, Icons: kind.Contains("suggestion") ? "start" : "both", Rtl: rtl, Arrangement: arrangement));
        foreach (var arrangement in new[] { "spaced-center", "overlap", "absolute-spaced", "custom" })
        foreach (var width in new double?[] { null, 41, 68 })
        foreach (var icons in new[] { "none", "both" })
            yield return new($"static.extended.constraint.{arrangement}.{width?.ToString() ?? "auto"}.{icons}", "Natural and constrained sizing with empty slots",
                new("filter", Width: width, Icons: icons, Arrangement: arrangement));
    }
    public static IEnumerable<CaseData<ChipConfig>> DynamicCases()
    {
        foreach (var kind in new[] { "filter", "input" })
        foreach (var arrangement in new[] { "start", "center", "spaceevenly", "spaced-center", "overlap", "absolute-spaced", "custom" })
        foreach (var action in new[] { "reverse-icons", "arrangement" })
            yield return new($"dynamic.{kind}.{arrangement}.{action}", "Animated slots and runtime arrangement changes",
                new(kind, Morph: true, Width: 161, Arrangement: arrangement, Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new ChipCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicCases))]
    public Task Behavior(CaptureWriter writer) => new ChipCases().Behavior(writer);
}

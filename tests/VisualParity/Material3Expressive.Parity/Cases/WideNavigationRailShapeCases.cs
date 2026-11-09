namespace Material3Expressive.Parity;

[ParitySuite("wide-navigation-rail-shapes")]
public sealed class WideNavigationRailShapeCases
{
    public static IEnumerable<CaseData<WideNavigationRailConfig>> StaticConfigurations()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var shape in new[] { "cut", "round-percent" })
            yield return new($"static.{expanded.ToString().ToLowerInvariant()}.{rtl.ToString().ToLowerInvariant()}.{shape}", "Logical shape at both endpoints", new(Expanded: expanded, Rtl: rtl, Header: true, Shape: shape));
    }
    public static IEnumerable<CaseData<WideNavigationRailConfig>> DynamicConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "reverse", "shape-change" })
            yield return new($"dynamic.{rtl.ToString().ToLowerInvariant()}.{action}", "Shape across interrupted expansion and replacement", new(Expanded: action == "shape-change", Rtl: rtl, Header: true, Shape: "cut", Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new WideNavigationRailCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new WideNavigationRailCases().Behavior(writer);
}

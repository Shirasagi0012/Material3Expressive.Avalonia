namespace Material3Expressive.Parity;

[ParitySuite("modal-wide-navigation-rail-shapes")]
public sealed class ModalWideNavigationRailShapeCases
{
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> StaticConfigurations()
    {
        foreach (var hide in new[] { false, true })
        foreach (var expanded in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var shape in new[] { "cut", "round-percent" })
            yield return new($"static.{hide.ToString().ToLowerInvariant()}.{expanded.ToString().ToLowerInvariant()}.{rtl.ToString().ToLowerInvariant()}.{shape}", "Logical shape at both endpoints", new(Hide: hide, Expanded: expanded, Rtl: rtl, Header: true, Shape: shape));
    }
    public static IEnumerable<CaseData<ModalWideNavigationRailConfig>> DynamicConfigurations()
    {
        foreach (var hide in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "reverse", "shape-change" })
            yield return new($"dynamic.{hide.ToString().ToLowerInvariant()}.{rtl.ToString().ToLowerInvariant()}.{action}", "Shape across interrupted expansion and replacement", new(Hide: hide, Expanded: action == "shape-change", Rtl: rtl, Header: true, Shape: "cut", Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new ModalWideNavigationRailCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new ModalWideNavigationRailCases().Behavior(writer);
}

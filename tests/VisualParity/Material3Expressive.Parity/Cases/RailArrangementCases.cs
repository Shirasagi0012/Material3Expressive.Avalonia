namespace Material3Expressive.Parity;

[ParitySuite("rail-arrangement")]
public sealed class RailArrangementCases
{
    public static IEnumerable<CaseData<WideNavigationRailConfig>> StaticCases()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var header in new[] { false, true })
        foreach (var arrangement in new[] { "custom-inset", "custom-reverse" })
            yield return new($"static.{expanded}.{rtl}.{header}.{arrangement}".ToLowerInvariant(), "Custom vertical arrangement with animated rail gaps",
                new(expanded, arrangement, header, Rtl: rtl));
        yield return new("empty", "Empty custom callback", new(Arrangement: "custom-inset", Empty: true));
        yield return new("single", "Single custom callback", new(Arrangement: "custom-reverse", Header: true, SingleItem: true));
        yield return new("constrained", "Custom callback with constrained height", new(Arrangement: "custom-inset", Header: true, Height: 120));
    }
    public static IEnumerable<CaseData<WideNavigationRailConfig>> DynamicCases()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "expand", "reverse", "arrangement-change" })
            yield return new($"dynamic.{rtl}.{action}".ToLowerInvariant(), "Custom vertical arrangement during expansion and replacement",
                new(Arrangement: "custom-inset", Header: true, Rtl: rtl, Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new WideNavigationRailCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicCases))]
    public Task Behavior(CaptureWriter writer) => new WideNavigationRailCases().Behavior(writer);
}

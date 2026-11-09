namespace Material3Expressive.Parity;
[ParitySuite("tab-indicator-width")]
public sealed class TabIndicatorWidthCases
{
    public static IEnumerable<CaseData<TabRowConfig>> StaticConfigurations() => TabRowCases.StaticConfigurations().Where(c => c.Id.StartsWith("static.indicator-width.", StringComparison.Ordinal));
    public static IEnumerable<CaseData<TabRowConfig>> DynamicConfigurations() => TabRowCases.DynamicConfigurations().Where(c => c.Id.StartsWith("dynamic.indicator-width.", StringComparison.Ordinal));
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new TabRowCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public Task Behavior(CaptureWriter writer) => new TabRowCases().Behavior(writer);
}

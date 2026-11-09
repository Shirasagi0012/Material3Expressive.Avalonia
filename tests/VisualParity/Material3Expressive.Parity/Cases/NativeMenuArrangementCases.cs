namespace Material3Expressive.Parity;
[ParitySuite("native-menu-arrangement")]
public sealed class NativeMenuArrangementCases
{
    public static IEnumerable<CaseData<MenuArrangementConfig>> StaticCases() => MenuArrangementCases.StaticCases()
        .Where(c => c.Config.Kind == "selectable").Select(c => c with { Config = c.Config with { Native = true } });
    public static IEnumerable<CaseData<MenuArrangementConfig>> DynamicCases() => MenuArrangementCases.DynamicCases()
        .Where(c => c.Config.Kind == "selectable").Select(c => c with { Config = c.Config with { Native = true, SelectedIcon = false } });
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new MenuArrangementCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicCases))]
    public Task Dynamic(CaptureWriter writer) => new MenuArrangementCases().Dynamic(writer);
}

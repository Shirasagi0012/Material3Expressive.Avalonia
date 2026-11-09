namespace Material3Expressive.Parity;
[ParitySuite("native-menu-shapes")]
public sealed class NativeMenuShapeCases
{
    public static IEnumerable<CaseData<MenuConfig>> StaticConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var path in new[] { "cut", "round-cut", "generic" })
        foreach (var selected in new[] { false, true })
            yield return new($"static.{path}.{rtl.ToString().ToLowerInvariant()}.{selected.ToString().ToLowerInvariant()}", "Native menu item shape", new(Rtl: rtl, Path: path, Selected: selected));
    }
    public static IEnumerable<CaseData<MenuConfig>> DynamicConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var path in new[] { "cut", "round-cut", "generic" })
        foreach (var action in new[] { "selection", "reverse", "press" })
            yield return new($"dynamic.{path}.{rtl.ToString().ToLowerInvariant()}.{action}", "Native menu selected outline and feedback", new(Rtl: rtl, Path: path, Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new MenuCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new MenuCases().Dynamic(writer);
}

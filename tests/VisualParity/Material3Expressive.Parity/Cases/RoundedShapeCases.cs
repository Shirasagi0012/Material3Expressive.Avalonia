namespace Material3Expressive.Parity;

[ParitySuite("rounded-shape")]
public sealed class RoundedShapeCases
{
    public static IEnumerable<CaseData<MaterialShapeConfig>> StaticConfigurations()
    {
        foreach (var kind in new[] { "surface", "click", "card", "elevated", "outlined", "button-filled", "button-outlined", "toggle-filled", "icon-filled", "icon-toggle-filled", "chip-assist", "chip-filter" })
        foreach (var shape in new[] { "rounded", "round-percent" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{shape}.{(rtl ? "rtl" : "ltr")}", "Logical rounded corners with DIP and percentage normalization", new(kind, shape, rtl, Border: kind.StartsWith("icon-") ? 0 : 1.1));
        foreach (var layer in new[] { "content", "shadow", "thick" })
            yield return new($"static.surface.{layer}", "Round clipping, elevation and border", new(Shape: "rounded", Content: layer == "content", Elevation: layer == "shadow" ? 6 : 0, Border: layer == "thick" ? 40 : 1.1));
    }

    public static IEnumerable<CaseData<MaterialShapeConfig>> DynamicConfigurations()
    {
        foreach (var kind in new[] { "button-filled", "button-outlined", "toggle-filled", "icon-filled", "icon-toggle-filled" })
        foreach (var mixed in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "press", "morph-resize", kind.Contains("toggle") ? "toggle-morph" : "reverse" })
            yield return new($"dynamic.{kind}.{(mixed ? "mixed" : "round")}.{action}.{(rtl ? "rtl" : "ltr")}",
                "Corner interpolation, family switch and interruption", new(kind, mixed ? "asymmetric" : "rounded", rtl, Border: kind.StartsWith("icon-") ? 0 : 1.1, Action: action, PressedShape: "round-percent", CheckedShape: "round-small"));
    }

    public static IEnumerable<CaseData<ListItemConfig>> ListConfigurations()
    {
        foreach (var path in new[] { "rounded", "round-cut" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "path-resize", "space", "drag" })
            yield return new($"list.{path}.{action}.{(rtl ? "rtl" : "ltr")}", "Six-state rounded list shape", new(Mode: "multiple", Rtl: rtl, Path: path, Action: action));
    }
    public static IEnumerable<CaseData<MenuConfig>> MenuConfigurations()
    {
        foreach (var path in new[] { "rounded", "round-cut" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "selection", "reverse", "press" })
            yield return new($"menu.{path}.{action}.{(rtl ? "rtl" : "ltr")}", "Menu rounded selection shape", new(Rtl: rtl, Path: path, Action: action));
    }
    [ParityScenario("list", Data = nameof(ListConfigurations))]
    public Task List(CaptureWriter writer) => new ListItemCases().Behavior(writer);
    [ParityScenario("menu", Data = nameof(MenuConfigurations))]
    public Task Menu(CaptureWriter writer) => new DropdownMenuItemCases().Dynamic(writer);

    public static IEnumerable<CaseData<SplitButtonConfig>> SplitConfigurations()
    {
        foreach (var shape in new[] { "rounded", "mixed" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "none", "press", "reverse", "selection", "shape" })
            yield return new($"split.{shape}.{action}.{(rtl ? "rtl" : "ltr")}", "Split logical rounded corners and mixed selection", new(Rtl: rtl, Action: action, Shape: shape));
    }
    [ParityScenario("split", Data = nameof(SplitConfigurations))]
    public Task Split(CaptureWriter writer) => new SplitButtonCases().Behavior(writer);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new MaterialShapeCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new MaterialShapeCases().Dynamic(writer);
}

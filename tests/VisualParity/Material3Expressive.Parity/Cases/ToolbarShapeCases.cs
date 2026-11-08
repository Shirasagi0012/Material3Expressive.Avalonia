namespace Material3Expressive.Parity;

[ParitySuite("toolbar-shape")]
public sealed class ToolbarShapeCases
{
    public static IEnumerable<CaseData<FloatingToolbarConfig>> StaticConfigurations()
    {
        foreach (var horizontal in new[] { false, true })
        foreach (var fab in new[] { "none", "start", "end" })
        foreach (var shape in new[] { "asymmetric", "rounded", "percent" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{horizontal}.{fab}.{shape}.{rtl}".ToLowerInvariant(), "Toolbar and FAB container outlines",
                new(horizontal, fab, Rtl: rtl, Shape: shape, FabShape: "asymmetric"));
        foreach (var horizontal in new[] { false, true })
        foreach (var expanded in new[] { false, true })
            yield return new($"static.template.{horizontal}.{expanded}".ToLowerInvariant(), "Template-only FAB shape and constrained toolbar", new(horizontal, "end", expanded, Vibrant: true,
                MaxWidth: horizontal ? 120 : null, MaxHeight: horizontal ? null : 120, FabTemplateOnly: true, Shape: "rounded", FabShape: "percent"));
    }
    public static IEnumerable<CaseData<FloatingToolbarConfig>> DynamicConfigurations()
    {
        foreach (var horizontal in new[] { false, true })
        foreach (var fab in new[] { "none", "start", "end" })
        foreach (var action in new[] { "expand", "collapse", "reverse" })
            yield return new($"dynamic.{horizontal}.{fab}.{action}".ToLowerInvariant(), "Shape follows toolbar clipping and independent FAB sizing",
                new(horizontal, fab, Expanded: action != "expand", Action: action, Shape: "percent", FabShape: "asymmetric"));
        foreach (var horizontal in new[] { false, true })
            yield return new($"dynamic.scroll.{horizontal}".ToLowerInvariant(), "Path shadow follows toolbar scroll", new(horizontal, Shape: "asymmetric", Action: "scroll"));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new FloatingToolbarCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new FloatingToolbarCases().Dynamic(writer);
}

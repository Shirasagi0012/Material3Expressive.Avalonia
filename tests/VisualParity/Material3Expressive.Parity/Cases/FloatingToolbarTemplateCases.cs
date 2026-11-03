namespace Material3Expressive.Parity;

[ParitySuite("floating-toolbar-template")]
public sealed class FloatingToolbarTemplateCases
{
    public static IEnumerable<CaseData<FloatingToolbarConfig>> StaticConfigurations()
    {
        foreach (var horizontal in new[] { true, false })
        foreach (var fab in new[] { "start", "end" })
        foreach (var expanded in new[] { true, false })
        foreach (var vibrant in new[] { true, false })
        foreach (var rtl in new[] { true, false })
        {
            var id = $"static.{(horizontal ? "horizontal" : "vertical")}.{fab}.{(expanded ? "expanded" : "collapsed")}.{(vibrant ? "vibrant" : "standard")}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(horizontal, fab, expanded, vibrant, Rtl: rtl, FabTemplateOnly: true));
        }
    }
    public static IEnumerable<CaseData<FloatingToolbarConfig>> DynamicConfigurations()
    {
        foreach (var horizontal in new[] { true, false })
        foreach (var fab in new[] { "start", "end" })
        foreach (var action in new[] { "expand", "collapse", "reverse" })
        {
            var id = $"dynamic.{(horizontal ? "horizontal" : "vertical")}.{fab}.{action}";
            yield return new(id, id, new(horizontal, fab, Expanded: action != "expand", Action: action, FabTemplateOnly: true));
        }
    }
    // Both entry mechanisms must exercise the same layout, transition, and measurement timeline.
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new FloatingToolbarCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new FloatingToolbarCases().Dynamic(writer);
}

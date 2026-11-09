namespace Material3Expressive.Parity;

[ParitySuite("legacy-tab-row")]
public sealed class LegacyTabRowCases
{
    public static IEnumerable<CaseData<TabRowConfig>> StaticCases()
    {
        foreach (var scrollable in new[] { false, true })
        foreach (var native in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        {
            var key = $"{(scrollable ? "scroll" : "fixed")}.{(native ? "native" : "controlled")}.{(rtl ? "rtl" : "ltr")}";
            foreach (var content in new[] { "label", "icon", "custom" })
                yield return new($"static.{key}.{content}", "Legacy row defaults and measured content widths",
                    new("legacy", scrollable, content, native, rtl));
            yield return new($"static.{key}.slots", "Legacy custom divider extent and indicator slot",
                new("legacy", scrollable, Native: native, Rtl: rtl, CustomSlots: true, Selected: 1));
            yield return new($"static.{key}.short", "Divider follows content extent under a height constraint",
                new("legacy", scrollable, Native: native, Rtl: rtl, Height: 24));
        }
        yield return new("static.fixed.empty", "Empty legacy fixed row", new("legacy", Count: 0, NoSlots: true));
        yield return new("static.scroll.fractional-padding", "Legacy positions start at rounded padding", new("legacy", true, EdgePadding: 7.375));
        yield return new("static.scroll.dark", "Legacy primary content color in dark theme", new("legacy", true, Dark: true));
    }
    public static IEnumerable<CaseData<TabRowConfig>> DynamicCases()
    {
        foreach (var scrollable in new[] { false, true })
        foreach (var native in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "select", "reverse", "content-interrupt" })
            yield return new($"dynamic.{(scrollable ? "scroll" : "fixed")}.{(native ? "native" : "controlled")}.{(rtl ? "rtl" : "ltr")}.{action}",
                "Legacy animateDpAsState and scrolling", new("legacy", scrollable, Native: native, Rtl: rtl, Count: 5, Action: action));
        yield return new("dynamic.scroll.initial-last", "Initial selection starts legacy centering", new("legacy", true, Count: 5, Selected: 4, Action: "initial-scroll"));
        yield return new("dynamic.fixed.resize", "Legacy width animation follows constraint changes", new("legacy", Count: 5, Action: "resize"));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => new TabRowCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicCases))]
    public Task Behavior(CaptureWriter writer) => new TabRowCases().Behavior(writer);
}

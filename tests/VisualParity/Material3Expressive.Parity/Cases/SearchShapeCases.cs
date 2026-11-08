namespace Material3Expressive.Parity;

[ParitySuite("search-shape")]
public sealed class SearchShapeCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> Presentations()
    {
        foreach (var variant in new[] { "docked", "gap", "full", "contained", "app-bar" })
        foreach (var shape in new[] { "asymmetric", "round-percent", "hole" })
        foreach (var rtl in new[] { false, true })
        {
            var config = new ExpandedSearchBarConfig(Rtl: rtl, Gap: variant == "gap", NoScrim: true,
                FullScreen: variant is "full" or "contained", Contained: variant == "contained", AppBar: variant == "app-bar",
                Shape: shape, DropdownShape: "percent", FilledInput: true);
            yield return new($"static.{variant}.{shape}.{(rtl ? "rtl" : "ltr")}", "Search shapes and clipped input", config);
        }
        foreach (var variant in new[] { "docked", "gap", "full", "contained" })
        foreach (var action in new[] { "open-close", "reverse", "replace-shape" })
            yield return new($"dynamic.{variant}.{action}", "Shape lifetime through expansion and reversal",
                new(Expanded: false, Gap: variant == "gap", NoScrim: true, FullScreen: variant is "full" or "contained",
                    Contained: variant == "contained", Shape: "asymmetric", DropdownShape: "percent", FilledInput: true, Action: action));
        foreach (var contained in new[] { false, true })
        foreach (var stableDefaults in new[] { false, true })
            yield return new($"dynamic.{(contained ? "contained" : "full")}.default-to-shape{(stableDefaults ? "-stable-defaults" : "")}", "Remember default Circle before replacing input shape",
                new(Expanded: false, NoScrim: true, FullScreen: true, Contained: contained,
                    FilledInput: true, Action: "replace-shape", StableShapeDefaults: stableDefaults));
    }
    public static IEnumerable<CaseData<SearchInputFocusRingConfig>> Rings()
    {
        foreach (var shape in new[] { "asymmetric", "round-percent", "curve", "hole" })
        foreach (var rtl in new[] { false, true })
            yield return new($"ring.{shape}.{(rtl ? "rtl" : "ltr")}", "Search input path focus ring",
                new(Rtl: rtl, ReadOnly: true, Shape: shape, Action: "reverse"));
    }
    public static IEnumerable<CaseData<DockedSearchBarConfig>> LegacyCases()
    {
        foreach (var shape in new[] { "asymmetric", "round-percent", "hole" })
        foreach (var rtl in new[] { false, true })
            yield return new($"legacy.{shape}.{(rtl ? "rtl" : "ltr")}", "Legacy docked shape through expansion", new(Rtl: rtl, Action: "open-close", Shape: shape));
    }
    [ParityScenario("legacy", Data = nameof(LegacyCases))]
    public Task Legacy(CaptureWriter writer) => new DockedSearchBarCases().Capture(writer);
    [ParityScenario("presentation", Data = nameof(Presentations))]
    public Task Presentation(CaptureWriter writer) => new ExpandedSearchBarCases().Capture(writer);
    [ParityScenario("ring", Data = nameof(Rings))]
    public Task Ring(CaptureWriter writer) => new SearchInputFocusRingCases().Capture(writer);
}

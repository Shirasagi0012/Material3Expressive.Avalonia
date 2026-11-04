namespace Material3Expressive.Parity;

[ParitySuite("search-bar-gap")]
public sealed class SearchBarGapCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> Configurations()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var noScrim in new[] { false, true })
            yield return new($"static.{(expanded ? "expanded" : "collapsed")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}.{(noScrim ? "no-scrim" : "scrim")}",
                "separate input and dropdown surfaces", new(expanded, dark, rtl, Gap: true, NoScrim: noScrim));
        yield return new("static.tall", "half-window height maximum", new(ContentHeight: 700, Gap: true));
        yield return new("static.small-window", "minimum height constrained by window", new(HostWidth: 240, HostHeight: 240, ContentHeight: 700, Gap: true));
        yield return new("static.input-size", "intrinsic input size and dropdown width", new(InputWidth: 280, InputHeight: 72, Gap: true));
        yield return new("static.custom", "custom gap, corners and scrim", new(Custom: true, Gap: true));
        yield return new("static.no-anchor", "popup without an anchor", new(Anchor: false, Gap: true));
        yield return new("static.elevation", "tonal elevation does not tint inner backgrounds", new(TonalElevation: 3, ShadowElevation: 3, Gap: true));
        foreach (var motion in new[] { "default", "tween", "overshoot", "content" })
            yield return new($"dynamic.motion-{motion}", "independent progress, slide and content alpha", new(Expanded: false, Action: "open-close", Motion: motion, Gap: true));
        foreach (var action in new[] { "open-close", "reverse", "escape", "outside" })
            yield return new($"dynamic.gap-default.{action}", "gap state factory motion and content fade", new(Expanded: action is "escape" or "outside", Action: action, Motion: "gap-default", Gap: true));
        foreach (var action in new[] { "reverse", "snap", "anchor", "content", "escape", "outside" })
            yield return new($"dynamic.{action}", "progress thresholds and popup lifecycle", new(Expanded: action is "anchor" or "escape" or "outside", Action: action, Gap: true));
        yield return new("dynamic.outside-no-scrim", "no-scrim dismissal happens on press", new(Action: "outside", Gap: true, NoScrim: true));
        yield return new("dynamic.remove-anchor", "remaining popup retains the gap state animation", new(Expanded: false, Action: "remove-anchor", Motion: "gap-default", Gap: true));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => new ExpandedSearchBarCases().Capture(writer);
}

namespace Material3Expressive.Parity;

[ParitySuite("full-screen-search-bar")]
public sealed class FullScreenSearchBarCases
{
    public static IEnumerable<CaseData<ExpandedSearchBarConfig>> Configurations()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{(expanded ? "expanded" : "collapsed")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}",
                "full-screen search appearance", new(expanded, dark, rtl, FullScreen: true));
        foreach (var rtl in new[] { false, true })
            yield return new($"static.insets.{(rtl ? "rtl" : "ltr")}", "physical window insets", new(Rtl: rtl, FullScreen: true, Insets: true));
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.input-clip.{(rtl ? "rtl" : "ltr")}", "padding precedes input clipping", new(Expanded: false, Rtl: rtl, Action: "open-close", FullScreen: true, Insets: true, FilledInput: true));
        yield return new("static.tall", "content constrained below input and padding", new(ContentHeight: 700, FullScreen: true));
        yield return new("static.small-window", "small-window layout", new(HostWidth: 240, HostHeight: 240, ContentHeight: 700, FullScreen: true));
        yield return new("static.input-size", "custom input height", new(InputWidth: 280, InputHeight: 72, FullScreen: true));
        yield return new("static.custom", "custom collapsed shape and colors", new(Custom: true, FullScreen: true));
        yield return new("static.no-anchor", "fallback input dimensions", new(Anchor: false, FullScreen: true));
        yield return new("static.zero-anchor", "zero anchor dimensions use defaults", new(InputWidth: 0, InputHeight: 0, FullScreen: true));
        foreach (var motion in new[] { "default", "tween", "overshoot", "content" })
            yield return new($"dynamic.motion-{motion}", "surface, input position and alpha", new(Expanded: false, Action: "open-close", Motion: motion, FullScreen: true));
        foreach (var action in new[] { "reverse", "snap", "anchor", "content", "escape", "outside" })
            yield return new($"dynamic.{action}", "state thresholds and lifecycle", new(Expanded: action is "anchor" or "escape" or "outside", Action: action, FullScreen: true));
        yield return new("dynamic.insets", "animated inset padding", new(Expanded: false, Action: "open-close", FullScreen: true, Insets: true));
        yield return new("dynamic.custom-shape", "custom corners switch at half progress", new(Expanded: false, Custom: true, Action: "open-close", FullScreen: true));
        yield return new("dynamic.rtl", "physical interpolation with RTL content", new(Expanded: false, Rtl: true, Action: "open-close", FullScreen: true));
        yield return new("dynamic.remove-anchor", "remaining full-screen search retains its state", new(Expanded: false, Action: "remove-anchor", FullScreen: true));
    }

    [ParityScenario("presentation", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => new ExpandedSearchBarCases().Capture(writer);
}

namespace Material3Expressive.Parity;

[ParitySuite("split-button-shape")]
public sealed class SplitButtonShapeCases
{
    public static IEnumerable<CaseData<SplitButtonConfig>> StaticConfigurations()
    {
        foreach (var style in new[] { "filled", "tonal", "elevated", "outlined" })
        foreach (var size in new[] { "xsmall", "small", "medium", "large", "xlarge" })
        foreach (var selected in new[] { false, true })
            yield return new($"static.{style}.{size}.{(selected ? "on" : "off")}", "Cut shape appearance and optical centering",
                new(style, size, Checked: selected, Shape: "cut"));
        // Direction and percentage resolution share the same layout across appearances.
        foreach (var shape in new[] { "cut", "percent", "generic" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{shape}.{(rtl ? "rtl" : "ltr")}", "Logical corners and direct corner optical offset",
                new(Rtl: rtl, Shape: shape));
        yield return new("static.disabled", "Disabled checked path overlay", new(Checked: true, Enabled: false, Shape: "cut"));
        yield return new("static.narrow", "Constrained path content", new(Width: 80, Shape: "percent"));
        yield return new("static.plain", "Non-toggle trailing path", new(Toggle: false, Shape: "cut"));
    }

    public static IEnumerable<CaseData<SplitButtonConfig>> DynamicConfigurations()
    {
        foreach (var style in new[] { "filled", "tonal", "elevated", "outlined" })
        foreach (var part in new[] { "leading", "trailing" })
            yield return new($"dynamic.{style}.{part}.press", "Actual input with cut morph", new(style, Action: "press", Part: part, Shape: "cut"));
        foreach (var action in new[] { "selection", "reverse", "shape", "cancel", "disable", "quick" })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.{action}.{(rtl ? "rtl" : "ltr")}", "Selection, interruption and retained shape configuration",
                new(Rtl: rtl, Action: action, Shape: "percent"));
        foreach (var shape in new[] { "generic", "null-pressed" })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.{shape}.{(rtl ? "rtl" : "ltr")}", "Direct path and null pressed fallback",
                new(Checked: true, Rtl: rtl, Action: "press", Shape: shape));
        yield return new("dynamic.plain", "Plain trailing press and release", new(Toggle: false, Action: "press", Shape: "cut"));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new SplitButtonCases().Static(writer);

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public Task Behavior(CaptureWriter writer) => new SplitButtonCases().Behavior(writer);
}

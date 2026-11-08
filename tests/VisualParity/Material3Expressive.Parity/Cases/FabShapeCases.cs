namespace Material3Expressive.Parity;

[ParitySuite("fab-shape")]
public sealed class FabShapeCases
{
    public static IEnumerable<CaseData<FloatingActionButtonConfig>> StaticConfigurations()
    {
        foreach (var size in new[] { "default", "small", "medium", "large" })
        foreach (var kind in new[] { "icon", "content", "extended" })
        foreach (var shape in new[] { "cut", "percent", "generic" })
            yield return new($"static.{kind}.{size}.{shape}", "Container shape across FAB sizes and content overloads",
                new(kind, size, Shape: shape));
        foreach (var kind in new[] { "icon", "content", "extended" })
        {
            yield return new($"static.{kind}.rtl", "Logical corner direction", new(kind, Rtl: true, Shape: "cut"));
            yield return new($"static.{kind}.constrained", "Constrained outline and content clip", new(kind, Width: 32, Height: 32, Shape: "percent"));
        }
        yield return new("static.extended.collapsed", "Collapsed extended shape", new("extended", Expanded: false, Shape: "percent"));
        yield return new("static.icon.dark", "Tonal path elevation", new(Dark: true, Color: "surface", Shape: "cut"));
    }

    public static IEnumerable<CaseData<FloatingActionButtonConfig>> DynamicConfigurations()
    {
        foreach (var size in new[] { "default", "small", "medium", "large" })
        foreach (var action in new[] { "expand", "collapse", "reverse" })
            yield return new($"dynamic.extended.{size}.{action}", "Path follows extended width and clipping during interruption",
                new("extended", size, Expanded: action != "expand", Action: action, Shape: "percent"));
        foreach (var kind in new[] { "icon", "content", "extended" })
        foreach (var action in new[] { "press", "cancel", "hover", "visibility-reverse" })
            yield return new($"dynamic.{kind}.{action}", "Feedback, shadow and visibility on custom paths",
                new(kind, Action: action, Shape: action == "cancel" ? "generic" : "cut"));
        yield return new("dynamic.extended.rtl", "RTL extended path animation", new("extended", Rtl: true, Action: "reverse", Shape: "percent"));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new FloatingActionButtonCases().Static(writer);
    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public Task Behavior(CaptureWriter writer) => new FloatingActionButtonCases().Behavior(writer);
}

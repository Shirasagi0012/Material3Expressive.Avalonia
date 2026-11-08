namespace Material3Expressive.Parity;

[ParitySuite("icon-button-content")]
public sealed class IconButtonContentCases
{
    private static readonly string[] Kinds = ["icon-standard", "icon-filled", "icon-tonal", "icon-outlined", "icon-vibrant", "icon-outlined-vibrant",
        "icon-toggle-standard", "icon-toggle-filled", "icon-toggle-tonal", "icon-toggle-outlined"];
    public static IEnumerable<CaseData<MaterialShapeConfig>> StaticConfigurations()
    {
        foreach (var kind in Kinds)
        foreach (var shape in new[] { "asymmetric", "rounded" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{shape}.{rtl}".ToLowerInvariant(), "Custom content fills and clips through all icon themes",
                new(kind, shape, rtl, Content: true, Border: kind.Contains("outlined") ? 1.1 : 0));
    }
    public static IEnumerable<CaseData<MaterialShapeConfig>> DynamicConfigurations()
    {
        foreach (var kind in Kinds)
        foreach (var action in new[] { "press", "morph-resize" })
            yield return new($"dynamic.{kind}.{action}", "Custom content follows actual input and changing outline",
                new(kind, "asymmetric", Content: true, Border: kind.Contains("outlined") ? 1.1 : 0, Action: action, PressedShape: "round-percent"));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new MaterialShapeCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new MaterialShapeCases().Dynamic(writer);
}

namespace Material3Expressive.Parity;

[ParitySuite("text-field-shape")]
public sealed class TextFieldShapeCases
{
    public static IEnumerable<CaseData<TextFieldConfig>> StaticConfigurations()
    {
        foreach (var outlined in new[] { false, true })
        foreach (var position in new[] { "Inside", "Cutout", "Above" })
        foreach (var rtl in new[] { false, true })
        foreach (var shape in new[] { "asymmetric", "percent", "rounded", "round-percent" })
            yield return new($"static.{outlined}.{position}.{shape}.{rtl}".ToLowerInvariant(), "Shape and label placement",
                new(outlined, position, Minimized: true, Slots: true, Rtl: rtl, Height: 96, Shape: shape));
        foreach (var outlined in new[] { false, true })
        {
            foreach (var shape in new[] { "offset", "curve", "hole", "zero" })
                yield return new($"static.{outlined}.{shape}".ToLowerInvariant(), "Generic path fill and bottom intersection",
                    new(outlined, Label: false, Height: 64, Shape: shape));
            foreach (var state in new[] { "dark", "disabled", "error" })
                yield return new($"static.{outlined}.{state}".ToLowerInvariant(), "State colors with shape",
                    new(outlined, outlined ? "Cutout" : "Inside", Minimized: true, Height: 80,
                        Shape: "asymmetric", Dark: state == "dark", Enabled: state != "disabled", Error: state == "error"));
        }
    }

    public static IEnumerable<CaseData<TextFieldConfig>> DynamicConfigurations()
    {
        foreach (var outlined in new[] { false, true })
        foreach (var action in new[] { "error", "theme", "shape", "reverse" })
            yield return new($"dynamic.{outlined}.{action}".ToLowerInvariant(), "Color, shape replacement and label interruption",
                new(outlined, outlined ? "Cutout" : "Inside", Slots: true, Height: 96, Shape: "asymmetric", Action: action));
    }

    public static IEnumerable<CaseData<SecureTextFieldConfig>> SecureConfigurations()
    {
        foreach (var outlined in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"secure.{outlined}.{rtl}".ToLowerInvariant(), "Secure field consumes shared shape decoration",
                new(outlined, outlined ? "Cutout" : "Inside", Minimized: true, Slots: true, Height: 96, Rtl: rtl, Shape: "asymmetric"));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => new TextFieldCases().Static(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => new TextFieldCases().Changes(writer);
    [ParityScenario("secure", Data = nameof(SecureConfigurations))]
    public Task Secure(CaptureWriter writer) => new SecureTextFieldCases().Static(writer);
}

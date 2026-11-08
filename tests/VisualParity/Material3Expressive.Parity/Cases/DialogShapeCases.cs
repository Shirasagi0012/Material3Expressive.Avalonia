namespace Material3Expressive.Parity;

[ParitySuite("dialog-shape")]
public sealed class DialogShapeCases
{
    public static IEnumerable<CaseData<AlertDialogConfig>> AlertConfigurations()
    {
        foreach (var shape in new[] { "asymmetric", "percent", "rounded" })
        foreach (var rtl in new[] { false, true })
            yield return new($"alert.static.{shape}.{rtl}".ToLowerInvariant(), "Alert container outline", new(Shape: shape, Rtl: rtl));
        foreach (var action in new[] { "transition", "interrupt", "resize", "slots", "confirm", "theme" })
            yield return new($"alert.dynamic.{action}", "Shape follows dialog lifetime and content", new(Shape: "asymmetric", Action: action));
    }
    public static IEnumerable<CaseData<PickerDialogConfig>> PickerConfigurations()
    {
        foreach (var kind in new[] { "date", "time", "vibrant" })
        {
            foreach (var shape in new[] { "asymmetric", "percent", "rounded" })
            foreach (var rtl in new[] { false, true })
                yield return new($"{kind}.static.{shape}.{rtl}".ToLowerInvariant(), "Picker dialog container outline",
                    new(Date: kind == "date", Vibrant: kind == "vibrant", Shape: shape, Rtl: rtl));
            foreach (var action in new[] { "transition", "interrupt", "mode", "slots", "confirm", "theme" })
                yield return new($"{kind}.dynamic.{action}", "Shape clipping through dialog transitions and editor resize",
                    new(Date: kind == "date", Vibrant: kind == "vibrant", Shape: "asymmetric", Action: action));
            yield return new($"{kind}.constrained", "Required size and custom outline under constraints",
                new(Date: kind == "date", Vibrant: kind == "vibrant", Width: 160, Height: 240, Shape: "percent"));
        }
    }
    [ParityScenario("alert", Data = nameof(AlertConfigurations))]
    public Task Alert(CaptureWriter writer) => AlertDialogScene.Capture(writer, writer.Metadata.ReadConfig<AlertDialogConfig>().Action != "none");
    [ParityScenario("picker", Data = nameof(PickerConfigurations))]
    public Task Picker(CaptureWriter writer) => PickerDialogScene.Capture(writer, writer.Metadata.ReadConfig<PickerDialogConfig>().Action != "none");
}

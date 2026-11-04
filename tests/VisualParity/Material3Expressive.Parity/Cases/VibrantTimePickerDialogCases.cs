namespace Material3Expressive.Parity;

[ParitySuite("vibrant-time-picker-dialog")]
public sealed class VibrantTimePickerDialogCases
{
    public static IEnumerable<CaseData<PickerDialogConfig>> StaticConfigurations()
    {
        foreach (var data in TimePickerDialogCases.StaticConfigurations())
            yield return data with { Config = data.Config with { Vibrant = true } };
        foreach (var color in new[] { "custom", "transparent" })
        foreach (var dark in new[] { false, true })
            yield return new($"static.ambient.{color}.{(dark ? "dark" : "light")}", "Explicit ambient content color fallback",
                new(Vibrant: true, Colors: color, Dark: dark, Ambient: "#FF448855"));
    }
    public static IEnumerable<CaseData<PickerDialogConfig>> DynamicConfigurations() =>
        TimePickerDialogCases.DynamicConfigurations().Select(data => data with { Config = data.Config with { Vibrant = true } });
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => PickerDialogScene.Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => PickerDialogScene.Capture(writer, true);
}

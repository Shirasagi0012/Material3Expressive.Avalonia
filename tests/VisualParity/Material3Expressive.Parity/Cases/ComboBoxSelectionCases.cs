namespace Material3Expressive.Parity;

// Isolate the real native item from the ComboBox field's accepted focus/close behavior.
[ParitySuite("combo-box-selection")]
public sealed class ComboBoxSelectionCases
{
    public static IEnumerable<CaseData<MenuConfig>> StaticConfigurations()
    {
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var dark in new[] { false, true })
        {
            var id = $"static.{(selected ? "selected" : "normal")}.{(enabled ? "enabled" : "disabled")}.{(rtl ? "rtl" : "ltr")}.{(dark ? "dark" : "light")}";
            yield return new(id, id, new(Selected: selected, Enabled: enabled, Dark: dark, Rtl: rtl, Icon: false, NativeCheck: true, ComboItem: true));
        }
    }
    public static IEnumerable<CaseData<MenuConfig>> DynamicConfigurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "selection", "reverse" })
        {
            var id = $"dynamic.{action}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(Rtl: rtl, Icon: false, Action: action, NativeCheck: true, ComboItem: true));
        }
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => MenuCases.Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => MenuCases.Capture(writer, true);
}

namespace Material3Expressive.Parity;

[ParitySuite("precision-menu-item")]
public sealed class PrecisionMenuItemCases
{
    public static IEnumerable<CaseData<MenuConfig>> Configurations()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var icon in new[] { false, true })
        foreach (var trailing in new[] { false, true })
        foreach (var dynamic in new[] { false, true })
        {
            var id = $"sizing.{rtl}.{icon}.{trailing}.{dynamic}".ToLowerInvariant();
            yield return new(id, id, new(Rtl: rtl, Icon: icon, Trailing: trailing, Precision: !dynamic, Action: dynamic ? "precision" : "none"));
        }
    }
    [ParityScenario("sizing", Data = nameof(Configurations))]
    public Task Sizing(CaptureWriter writer) => MenuCases.Capture(writer, writer.Metadata.ReadConfig<MenuConfig>().Action != "none", independent: true);
}

// The same upstream item contract is exercised through both Avalonia entry points.
// Keep the native suite intact; a separate identity prevents reusing its actual evidence.
[ParitySuite("dropdown-menu-item")]
public sealed class DropdownMenuItemCases
{
    public static IEnumerable<CaseData<MenuConfig>> StaticConfigurations() => MenuCases.StaticConfigurations();
    public static IEnumerable<CaseData<MenuConfig>> DynamicConfigurations()
    {
        foreach (var existing in MenuCases.DynamicConfigurations()) yield return existing;
        foreach (var exiting in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        {
            var action = exiting ? "icon-exit" : "icon-enter";
            var id = $"dynamic.{action}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(Selected: exiting, Rtl: rtl, Icon: false, Action: action, SelectedIcon: true));
        }
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => MenuCases.Capture(writer, false, independent: true);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => MenuCases.Capture(writer, true, independent: true);
}

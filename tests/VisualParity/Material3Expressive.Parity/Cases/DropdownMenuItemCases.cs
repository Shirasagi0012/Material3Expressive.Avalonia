namespace Material3Expressive.Parity;

// The same upstream item contract is exercised through both Avalonia entry points.
// Keep the native suite intact; a separate identity prevents reusing its actual evidence.
[ParitySuite("dropdown-menu-item")]
public sealed class DropdownMenuItemCases
{
    public static IEnumerable<CaseData<MenuConfig>> StaticConfigurations() => MenuCases.StaticConfigurations();
    public static IEnumerable<CaseData<MenuConfig>> DynamicConfigurations() => MenuCases.DynamicConfigurations();
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => MenuCases.Capture(writer, false, independent: true);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => MenuCases.Capture(writer, true, independent: true);
}

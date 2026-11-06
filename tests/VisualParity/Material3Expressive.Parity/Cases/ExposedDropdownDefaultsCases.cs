using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

[ParitySuite("exposed-dropdown-field")]
public sealed class ExposedDropdownFieldCases
{
    public static IEnumerable<CaseData<TextFieldConfig>> Configurations()
    {
        foreach (var outlined in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var error in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        {
            var id = $"{(outlined ? "outlined" : "filled")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}.{(error ? "error" : "normal")}.{(rtl ? "rtl" : "ltr")}";
            yield return new(id, id, new(Outlined: outlined, Position: outlined ? "Cutout" : "Inside", Minimized: true, Slots: true,
                Enabled: enabled, Error: error, Dark: dark, Rtl: rtl, Height: 96, Exposed: true));
        }
    }
    [ParityScenario("colors", Data = nameof(Configurations))]
    public Task Capture(CaptureWriter writer) => new TextFieldCases().Static(writer);
}

public sealed record ExposedIconConfig(bool Expanded = false, bool Rtl = false, int Size = 24);
[ParitySuite("exposed-dropdown-icon")]
public sealed class ExposedDropdownIconCases
{
    public static IEnumerable<CaseData<ExposedIconConfig>> Configurations()
    {
        foreach (var expanded in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var size in new[] { 16, 24, 48 })
        {
            var id = $"{(expanded ? "expanded" : "collapsed")}.{(rtl ? "rtl" : "ltr")}.{size}";
            yield return new(id, id, new(expanded, rtl, size));
        }
    }
    [ParityScenario("icon", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ExposedIconConfig>();
        await using var host = new AvaloniaHost(writer, 96, 96);
        var icon = new ExposedDropdownMenuTrailingIcon { IsExpanded = config.Expanded, Width = config.Size, Height = config.Size,
            Foreground = Brushes.Red, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        host.Mount(CaseDefaults.Center(icon)); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, [new("icon", "icon", null,
            [.. AvaloniaMeasurements.Geometry(icon, host.Window).Where(m => m.Name is "size" or "box"), Measurement.Flag("expanded", icon.IsExpanded, definition: "exposed.icon-expanded")])]));
        Sample("initial"); host.AdvanceTo(1000); icon.IsExpanded = !icon.IsExpanded;
        writer.Record(1000, "mutation", "toggle"); host.Draw(); Sample("toggle"); host.Frame(16000); Sample("next-frame");
    }
}

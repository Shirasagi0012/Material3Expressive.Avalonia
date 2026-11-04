using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record NativeMenuSupportingConfig(bool Vibrant = false, bool Selected = false, bool Enabled = true,
    bool Rtl = false, bool Dark = false, bool TemplateOnly = false, bool Dynamic = false, bool Trailing = false, double Width = 200);

[ParitySuite("native-menu-supporting")]
public sealed class NativeMenuSupportingCases
{
    public static IEnumerable<CaseData<NativeMenuSupportingConfig>> Configurations()
    {
        foreach (var vibrant in new[] { false, true })
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{(vibrant ? "vibrant" : "standard")}.{(selected ? "selected" : "unselected")}.{(enabled ? "enabled" : "disabled")}.{(rtl ? "rtl" : "ltr")}",
                "Supporting row colors and layout", new(vibrant, selected, enabled, rtl));
        foreach (var vibrant in new[] { false, true })
        foreach (var selected in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"trailing.{(vibrant ? "vibrant" : "standard")}.{(selected ? "selected" : "unselected")}.{(enabled ? "enabled" : "disabled")}.{(rtl ? "rtl" : "ltr")}",
                "Small trailing content colors and alignment", new(vibrant, selected, enabled, rtl, Trailing: true));
        foreach (var width in new[] { 80d, 112d, 160d })
        foreach (var rtl in new[] { false, true })
            yield return new($"trailing.width-{width}.{(rtl ? "rtl" : "ltr")}", "Constrained three-slot row", new(Rtl: rtl, Trailing: true, Width: width));
        foreach (var vibrant in new[] { false, true })
        {
            yield return new($"trailing.template-only.{(vibrant ? "vibrant" : "standard")}", "Template-only trailing slot", new(vibrant, TemplateOnly: true, Trailing: true));
            yield return new($"trailing.dynamic.{(vibrant ? "vibrant" : "standard")}", "Remove and restore both slots", new(vibrant, Dynamic: true, Trailing: true));
            yield return new($"static.dark.{(vibrant ? "vibrant" : "standard")}", "Dark theme", new(vibrant, Dark: true));
            yield return new($"static.template-only.{(vibrant ? "vibrant" : "standard")}", "Template-only supporting slot", new(vibrant, TemplateOnly: true));
            yield return new($"dynamic.{(vibrant ? "vibrant" : "standard")}", "Remove and restore the supporting slot", new(vibrant, Dynamic: true));
        }
    }

    [ParityScenario("content", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NativeMenuSupportingConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160);
        MenuCases.SetColors(host, config.Dark);
        if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var header = new Marker(48, 16); var support = new Marker(80, 12); var trailing = new Marker(8, 12);
        var item = new MenuItem { Header = header, IsChecked = config.Selected, IsEnabled = config.Enabled,
            Icon = new Marker(20, 20), Theme = (ControlTheme)host.Window.FindResource("MaterialVerticalMenuItemTheme")! };
        MenuAppearance.SetColorStyle(item, config.Vibrant ? MenuColorStyle.Vibrant : MenuColorStyle.Standard);
        if (config.TemplateOnly) MenuAppearance.SetSupportingTextTemplate(item, new FuncDataTemplate<object?>((_, _) => support));
        else MenuAppearance.SetSupportingText(item, support);
        if (config.Trailing)
        {
            if (config.TemplateOnly) MenuAppearance.SetTrailingContentTemplate(item, new FuncDataTemplate<object?>((_, _) => trailing));
            else MenuAppearance.SetTrailingContent(item, trailing);
        }
        var box = new Grid { Width = config.Width, Children = { item } };
        host.Mount(CaseDefaults.Center(box)); host.Frame(-1000); host.Frame(0);
        void Sample(string id, bool supporting)
        {
            var points = new List<ProbePoint> { Geometry("allocation", box), Geometry("header", header) };
            if (supporting) points.Add(Geometry("supporting", support));
            if (supporting && config.Trailing) points.Add(Geometry("trailing", trailing));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        ProbePoint Geometry(string id, Control control) => new(id, id, null,
            AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray());
        Sample("initial", true);
        if (!config.Dynamic) return;
        host.AdvanceTo(16000); MenuAppearance.SetSupportingText(item, null); MenuAppearance.SetTrailingContent(item, null); host.Draw(); host.Frame(16000); Sample("removed", false);
        host.AdvanceTo(32000); MenuAppearance.SetSupportingText(item, support); if (config.Trailing) MenuAppearance.SetTrailingContent(item, trailing); host.Draw(); host.Frame(32000); Sample("restored", true);
    }

    private sealed class Marker(double width, double height) : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context)
        { if (TextElement.GetForeground(this) is { } brush) context.FillRectangle(brush, new Rect(Bounds.Size)); }
    }
}

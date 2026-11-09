using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record NativeMenuPrecisionConfig(string Kind = "native", bool Precision = false, bool Rtl = false,
    bool DefaultIconSize = true, bool Supporting = false, double Width = 200, bool Dark = false,
    bool CustomPadding = false, string Action = "none");

[ParitySuite("native-menu-precision")]
public sealed class NativeMenuPrecisionCases
{
    public static IEnumerable<CaseData<NativeMenuPrecisionConfig>> StaticCases()
    {
        foreach (var kind in new[] { "native", "ordinary", "checkable", "selectable" })
        foreach (var precision in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var hint in new[] { false, true })
            yield return new($"static.{kind}.{(precision ? "precise" : "normal")}.{(rtl ? "rtl" : "ltr")}.{(hint ? "icon" : "small")}",
                "Menu padding, arrangement and trailing size", new(kind, precision, rtl, hint));
        foreach (var kind in new[] { "native", "selectable" })
        foreach (var rtl in new[] { false, true })
        {
            foreach (var precision in new[] { false, true })
                yield return new($"static.{kind}.natural.{(precision ? "precise" : "normal")}.{(rtl ? "rtl" : "ltr")}",
                    "Natural width includes effective padding and icon hint", new(kind, precision, rtl, Width: 0));
            yield return new($"static.{kind}.supporting.{(rtl ? "rtl" : "ltr")}", "Supporting content and explicit small trailing content",
                new(kind, true, rtl, false, Supporting: true));
            yield return new($"static.{kind}.constrained.{(rtl ? "rtl" : "ltr")}", "Constrained precision content", new(kind, true, rtl, Width: 90));
            yield return new($"static.{kind}.override.{(rtl ? "rtl" : "ltr")}", "Explicit padding remains authoritative", new(kind, true, rtl, CustomPadding: true));
        }
        yield return new("static.native.dark", "Dark precision colors", new(Precision: true, Dark: true));
    }
    public static IEnumerable<CaseData<NativeMenuPrecisionConfig>> DynamicCases()
    {
        foreach (var kind in new[] { "native", "selectable" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "mode", "press" })
            yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}", "Precision changes during active use", new(kind, Rtl: rtl, Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicCases))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var c = writer.Metadata.ReadConfig<NativeMenuPrecisionConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160);
        MenuCases.SetColors(host, c.Dark);
        if (c.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var header = new Marker(48, 16); var leading = new Marker(20, 20);
        var trailing = new Marker(c.DefaultIconSize ? null : 8, c.DefaultIconSize ? null : 12);
        var support = new Marker(80, 12);
        var clicks = 0;
        Control item;
        if (c.Kind == "native")
        {
            var native = new MenuItem { Header = header, Icon = leading,
                Theme = (ControlTheme)host.Window.FindResource("MaterialVerticalMenuItemTheme")! };
            MenuAppearance.SetTrailingContent(native, trailing);
            if (c.Supporting) MenuAppearance.SetSupportingText(native, support);
            if (c.CustomPadding) native.Padding = new Thickness(3, 5, 9, 7);
            native.Click += (_, _) => clicks++;
            item = native;
        }
        else
        {
            DropdownMenuItem dropdown = c.Kind switch { "checkable" => new CheckableDropdownMenuItem(), "selectable" => new SelectableDropdownMenuItem(),
                _ => new DropdownMenuItem { Theme = (ControlTheme)host.Window.FindResource("MaterialExpressiveDropdownMenuItemTheme")! } };
            dropdown.Content = header; dropdown.LeadingIcon = leading; dropdown.TrailingContent = trailing;
            if (c.Supporting) dropdown.SupportingText = support;
            if (c.CustomPadding) dropdown.Padding = new Thickness(3, 5, 9, 7);
            dropdown.Click += (_, _) => clicks++;
            item = dropdown;
        }
        var allocation = new Grid { Children = { item } };
        if (c.Width > 0) allocation.Width = c.Width;
        TextElement.SetForeground(allocation, new SolidColorBrush((Color)host.Window.FindResource(Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnSurface)!));
        MaterialPrecisionPointer.SetIsEnabled(allocation, c.Precision);
        host.Mount(CaseDefaults.Center(allocation)); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            var points = new List<ProbePoint> { Geometry("allocation", allocation), Geometry("header", header), Geometry("leading", leading), Geometry("trailing", trailing),
                new("activation", "activation", null, [Measurement.Numeric("clicks", clicks, "count", definition: "menu.click-count")]) };
            if (c.Supporting) points.Add(Geometry("supporting", support));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        ProbePoint Geometry(string id, Control control) => new(id, id, null,
            AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray());
        Sample("initial"); if (!dynamic) return;
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Mode(long time, bool precise, string id) { Advance(time); MaterialPrecisionPointer.SetIsEnabled(allocation, precise); host.Draw(); Sample(id); }
        if (c.Action == "mode")
        {
            Mode(1000, true, "precision-on"); Times(16000);
            Mode(17000, false, "precision-off"); Times(32000);
            Mode(33000, true, "precision-again"); Times(48000);
        }
        else
        {
            var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), host.Window)!.Value;
            Advance(1000); host.Input("move", center); host.Draw();
            Advance(2000); host.Input("down", center); host.Draw(); Sample("pressed"); Times(16000, 32000);
            Mode(33000, true, "precision-pressed"); Times(48000, 64000);
            Advance(65000); host.Input("up", center); host.Draw(); Sample("released"); Times(80000, 240000, 400000);
        }
    }

    // A geometric icon consumes the same inherited size hint as MaterialSymbol, avoiding font rasterization.
    private sealed class Marker(double? width, double? height) : Control
    {
        static Marker()
        {
            AffectsRender<Marker>(TextElement.ForegroundProperty);
            AffectsMeasure<Marker>(MaterialSymbol.DefaultSizeProperty);
        }
        protected override Size MeasureOverride(Size availableSize) => new(
            Math.Min(width ?? MaterialSymbol.GetDefaultSize(this), availableSize.Width),
            Math.Min(height ?? MaterialSymbol.GetDefaultSize(this), availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}

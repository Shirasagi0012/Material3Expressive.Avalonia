using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Components;
using Material3Expressive.Foundation;

namespace Material3Expressive.Parity;

public sealed record MenuArrangementConfig(string Kind = "selectable", string Arrangement = "center",
    bool Rtl = false, bool Leading = true, bool Trailing = true, bool SelectedIcon = false,
    double Width = 200, string Action = "none", bool Dark = false, bool Native = false);

[ParitySuite("menu-arrangement")]
public sealed class MenuArrangementCases
{
    public static IEnumerable<CaseData<MenuArrangementConfig>> StaticCases()
    {
        foreach (var kind in new[] { "ordinary", "checkable", "selectable" })
        foreach (var arrangement in new[] { "start", "end", "center", "spacebetween", "spacearound", "spaceevenly", "spaced-center", "overlap", "absolute.spaceevenly", "absolute-spaced", "custom" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{arrangement}.{(rtl ? "rtl" : "ltr")}", "Expressive item content arrangements",
                new(kind, arrangement, rtl));
        foreach (var arrangement in new[] { "spacebetween", "spaced-center", "overlap", "custom" })
        foreach (var leading in new[] { false, true })
        foreach (var trailing in new[] { false, true })
        foreach (var width in new[] { 80d, 200d })
            yield return new($"static.slots.{arrangement}.{leading}.{trailing}.{width}".ToLowerInvariant(), "Missing slots and constrained content",
                new(Arrangement: arrangement, Leading: leading, Trailing: trailing, Width: width));
        yield return new("static.dark.rtl", "Dark physical arrangement", new(Arrangement: "absolute-spaced", Rtl: true, Dark: true));
    }
    public static IEnumerable<CaseData<MenuArrangementConfig>> DynamicCases()
    {
        foreach (var kind in new[] { "ordinary", "checkable", "selectable" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in kind == "ordinary" ? new[] { "arrangement", "slots", "press" } : new[] { "selection", "arrangement", "slots", "press" })
            yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}", "Live arrangements, selected icon and activation",
                new(kind, "spaced-center", rtl, Leading: false, SelectedIcon: kind != "ordinary", Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicCases))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<MenuArrangementConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160);
        MenuCases.SetColors(host, config.Dark);
        if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        DropdownMenuItem dropdown = config.Kind switch
        {
            "checkable" => new CheckableDropdownMenuItem(),
            "selectable" => new SelectableDropdownMenuItem(),
            _ => new DropdownMenuItem { Theme = (ControlTheme)host.Window.FindResource("MaterialExpressiveDropdownMenuItemTheme")! }
        };
        var content = new Marker(48, 16);
        Control item = config.Native ? new MenuItem { Header = content,
            Theme = (ControlTheme)host.Window.FindResource("MaterialVerticalMenuItemTheme")! } : dropdown;
        void Leading(bool value)
        {
            var marker = value ? new Marker(20, 20) : null;
            if (item is MenuItem native) native.Icon = marker; else dropdown.LeadingIcon = marker;
        }
        void Trailing(bool value)
        {
            var marker = value ? new Marker(12, 12) : null;
            if (item is MenuItem native) MenuAppearance.SetTrailingContent(native, marker); else dropdown.TrailingContent = marker;
        }
        void Arrange(MaterialHorizontalArrangement? value)
        {
            if (item is MenuItem native) MenuAppearance.SetHorizontalArrangement(native, value); else dropdown.HorizontalArrangement = value;
        }
        dropdown.Content = config.Native ? null : content;
        Leading(config.Leading); Trailing(config.Trailing);
        dropdown.SelectedLeadingIcon = config.SelectedIcon ? new Marker(20, 20) : null;
        Arrange(ChipCases.ResolveArrangement(config.Arrangement));
        var clicks = 0;
        if (item is MenuItem nativeItem) nativeItem.Click += (_, _) => clicks++; else dropdown.Click += (_, _) => clicks++;
        var allocation = new Grid { Width = config.Width, Children = { item } };
        // The transparent ordinary Surface inherits its ripple color. Fix the same ambient on both ends.
        TextElement.SetForeground(allocation, new SolidColorBrush((Color)host.Window.FindResource(Material3Expressive.Tokens.MaterialTokens.Sys.Color.OnSurface)!));
        host.Mount(CaseDefaults.Center(allocation));
        host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [
            new("content", "content", null, AvaloniaMeasurements.Geometry(content, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
            new("item", "item", null, [Measurement.Numeric("clicks", clicks, "count", definition: "menu.click-count")])
        ], true));
        Sample("initial");
        if (!dynamic) return;
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Change(long time, string id, Action action) { Advance(time); action(); host.Draw(); host.Frame(time); Sample(id); }
        void Select(bool value)
        {
            if (item is MenuItem native) native.IsChecked = value;
            if (item is CheckableDropdownMenuItem checkable) checkable.IsChecked = value;
            if (item is SelectableDropdownMenuItem selectable) selectable.IsSelected = value;
        }
        switch (config.Action)
        {
            case "selection":
                Change(1000, "selected", () => Select(true)); Times(16000, 32000, 64000);
                Change(65000, "reversed", () => Select(false)); Times(80000, 160000, 400000); break;
            case "arrangement":
                Change(1000, "end", () => Arrange(MaterialHorizontalArrangement.End)); Times(16000);
                Change(17000, "absolute", () => Arrange(MaterialHorizontalArrangement.Absolute.SpaceEvenly)); Times(32000);
                Change(33000, "default", () => Arrange(null)); Times(48000); break;
            case "slots":
                Change(1000, "selected", () => Select(true)); Times(16000, 64000);
                Change(65000, "leading", () => Leading(true)); Times(80000);
                Change(81000, "remove-trailing", () => Trailing(false)); Times(96000);
                Change(97000, "remove-leading", () => { Leading(false); dropdown.SelectedLeadingIcon = null; }); Times(112000); break;
            case "press":
                var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), host.Window)!.Value;
                Advance(1000); host.Input("move", center); host.Draw(); host.Frame(1000);
                Advance(2000); host.Input("down", center); host.Draw(); host.Frame(2000); Sample("pressed"); Times(16000, 64000);
                Advance(65000); host.Input("up", center); host.Draw(); host.Frame(65000); Sample("released"); Times(80000, 240000, 400000); break;
        }
    }
    private sealed class Marker(double width, double height) : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}

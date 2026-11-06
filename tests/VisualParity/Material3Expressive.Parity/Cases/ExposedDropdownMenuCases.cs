using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record ExposedMenuConfig(int X = 80, int Y = 64, int Width = 160, int Count = 3,
    bool Editable = false, bool Rtl = false, bool Match = true, bool Dark = false, string Action = "none", double? MenuWidth = null, double? MenuHeight = null, double MinHeight = 0, bool Field = false, bool LegacyField = true);

[ParitySuite("exposed-dropdown-menu")]
public sealed class ExposedDropdownMenuCases
{
    public static IEnumerable<CaseData<ExposedMenuConfig>> Configurations()
    {
        foreach (var editable in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var match in new[] { false, true })
        foreach (var y in new[] { 8, 64, 200, 280 })
        {
            var id = $"static.{(editable ? "editable" : "readonly")}.{(rtl ? "rtl" : "ltr")}.{(match ? "match" : "intrinsic")}.{y}";
            yield return new(id, id, new(Y: y, Editable: editable, Rtl: rtl, Match: match));
        }
        foreach (var rtl in new[] { false, true })
        {
            yield return new($"edge.{(rtl ? "rtl" : "ltr")}", "Anchor near the right edge", new(X: 280, Width: 100, Rtl: rtl, Match: false));
            yield return new($"dark.{(rtl ? "rtl" : "ltr")}", "Dark exposed menu", new(Dark: true, Rtl: rtl));
        }
        foreach (var editable in new[] { false, true })
        foreach (var action in new[] { "none", "show" })
            yield return new($"field.{(editable ? "editable" : "readonly")}.{action}", "Real empty field and floating label", new(Width: 280, Editable: editable, Field: true, Action: action));
        foreach (var editable in new[] { false, true })
        foreach (var action in new[] { "none", "show" })
            yield return new($"state-field.{(editable ? "editable" : "readonly")}.{action}", "State-based field used by fixed exposed menu samples", new(Width: 280, Editable: editable, Field: true, LegacyField: false, Action: action));
        yield return new("constraints.width", "Explicit popup width precedes anchor width", new(MenuWidth: 100));
        yield return new("constraints.height", "Explicit popup height precedes available height", new(MenuHeight: 240));
        yield return new("constraints.min-height", "Minimum popup height precedes available height", new(MinHeight: 240));
        yield return new("constraints.offscreen", "Fully offscreen anchor uses the window", new(Y: 340, Count: 8));
        foreach (var editable in new[] { false, true })
        foreach (var action in new[] { "show", "reverse", "resize", "move", "outside", "click", "match" })
            yield return new($"{action}.{(editable ? "editable" : "readonly")}", action, new(Editable: editable, Action: action));
    }

    [ParityScenario("menu", Data = nameof(Configurations))]
    public async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ExposedMenuConfig>();
        await using var host = new AvaloniaHost(writer, 400, 320);
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            names.Register(presenter.Name, presenter);
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            names.Register(layers.Name, layers); return layers;
        });
        MenuCases.SetColors(host, config.Dark);
        Control anchor = config.Field ? new TextBox { IsReadOnly = !config.Editable, CaretBrush = Brushes.Transparent } : new Border { Background = Brushes.Blue, Focusable = true };
        if (anchor is TextBox field) MaterialTextField.SetLabel(field, new Border { Width = 40, Height = 12, Background = Brushes.Red });
        host.Window.Resources[Tokens.MaterialTokens.Sys.Color.SurfaceContainerHighest] = Color.Parse(config.Dark ? "#36343B" : "#E6E0E9");
        ExposedDropdownMenuBox.SetAnchorType(anchor, config.Editable ? ExposedDropdownMenuAnchorType.PrimaryEditable : ExposedDropdownMenuAnchorType.PrimaryNotEditable);
        var menu = new ExposedDropdownMenu { MatchAnchorWidth = config.Match };
        if (config.MenuWidth is { } menuWidth) menu.Width = menuWidth;
        if (config.MenuHeight is { } menuHeight) menu.Height = menuHeight;
        menu.MinHeight = config.MinHeight;
        for (var i = 0; i < config.Count; i++) menu.Items.Add(new DropdownMenuItem { Content = new Border { Width = 48, Height = 16, Background = Brushes.Red } });
        var box = new ExposedDropdownMenuBox { Width = config.Width, Height = config.Field ? 96 : 56, Content = anchor, Menu = menu,
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            IsExpanded = config.Action is not ("show" or "reverse" or "click") };
        var requests = 0;
        box.ExpandedChangeRequested += (_, e) => { requests++; box.IsExpanded = e.Expanded; };
        var canvas = new Canvas { Background = Brushes.White, Children = { box } };
        Canvas.SetLeft(box, config.X); Canvas.SetTop(box, config.Y);
        host.Mount(canvas); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("state", "state", null,
                [Measurement.Flag("expanded", box.IsExpanded, definition: "exposed.expanded"),
                 Measurement.Flag("presented", menu.IsPresented, definition: "exposed.presented"),
                 Measurement.Numeric("requests", requests, "count", definition: "exposed.requests")]) };
            if (anchor is TextBox field)
                points.Add(new("field", "field", null, [Measurement.Flag("focused", TextFieldThemeDefaults.GetIsFocused(field), definition: "exposed.field-focus")]));
            if (menu.IsPresented && menu.Presentation is { } layer)
                points.Add(new("menu", "popup", null, [Measurement.Numeric("width", layer.Bounds.Width, "dp", category: "geometry", definition: "exposed.width"),
                    Measurement.Numeric("height", layer.Bounds.Height, "dp", category: "geometry", definition: "exposed.height")]));
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        var origin = box.IsExpanded ? 800000L : 0;
        Advance(origin); host.Draw(); Sample("initial");
        void Change(long t, string id, Action action) { Advance(origin + t); action(); writer.Record(origin + t, "mutation", id); host.Draw(); host.Frame(origin + t); Sample(id); }
        void Times(params long[] times) { foreach (var t in times) { Advance(origin + t); host.Draw(); Sample($"time-{t}"); } }
        switch (config.Action)
        {
            case "show": case "reverse":
                Change(1000, "open", () => box.IsExpanded = true); Times(16000, 32000, 64000);
                if (config.Action == "reverse") { Change(65000, "close", () => box.IsExpanded = false); Times(80000, 96000); Change(97000, "reopen", () => box.IsExpanded = true); }
                Times(112000, 224000, 800000); Change(801000, "final-close", () => box.IsExpanded = false); Times(816000, 864000, 960000, 1200000); break;
            case "resize": Change(1000, "resize", () => box.Width = 240); Times(16000, 800000); break;
            case "move": Change(1000, "move", () => Canvas.SetTop(box, 230)); Times(16000, 800000); break;
            case "match": Change(1000, "intrinsic", () => menu.MatchAnchorWidth = false); Times(16000, 800000); break;
            case "outside": Change(1000, "outside", () => { host.Input("move", new(8, 8)); host.Input("down", new(8, 8)); }); Change(2000, "release", () => host.Input("up", new(8, 8))); Times(16000, 800000); break;
            case "click":
                var point = new Point(config.X + 20, config.Y + 20);
                Change(1000, "press", () => { host.Input("move", point); host.Input("down", point); });
                Change(2000, "release", () => host.Input("up", point)); Times(16000, 64000, 800000); break;
        }
    }
}

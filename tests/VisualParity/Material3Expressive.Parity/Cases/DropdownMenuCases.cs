using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record DropdownMenuConfig(bool Grouped = false, bool Rtl = false, bool Dark = false,
    int X = 112, int Y = 64, string Action = "none", bool NoShadow = false, bool Divider = false);

[ParitySuite("dropdown-menu")]
public sealed class DropdownMenuCases
{
    public static IEnumerable<CaseData<DropdownMenuConfig>> StaticConfigurations()
    {
        foreach (var grouped in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{(grouped ? "grouped" : "baseline")}.{(rtl ? "rtl" : "ltr")}.{(dark ? "dark" : "light")}", "Real popup and content slots", new(grouped, rtl, dark));
        foreach (var grouped in new[] { false, true })
            yield return new($"static.edge.{(grouped ? "grouped" : "baseline")}", "Popup edge fallback", new(grouped, X: 292, Y: 208));
        foreach (var grouped in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.divider.{(grouped ? "grouped" : "baseline")}.{(rtl ? "rtl" : "ltr")}",
                "Divider fills the intrinsic popup width", new(grouped, rtl, Divider: true));
    }
    public static IEnumerable<CaseData<DropdownMenuConfig>> DynamicConfigurations()
    {
        foreach (var grouped in new[] { false, true })
        foreach (var action in new[] { "show", "reverse", "outside" })
            yield return new($"dynamic.{(grouped ? "grouped" : "baseline")}.{action}", "Real popup transition and dismissal", new(grouped, Action: action));
        foreach (var action in new[] { "selection", "hover" })
            yield return new($"dynamic.grouped.{action}", "Selected icon and group shape motion", new(true, Action: action));
        foreach (var grouped in new[] { false, true })
            yield return new($"dynamic.{(grouped ? "grouped" : "baseline")}.reverse-no-shadow",
                "Isolate popup interruption from Surface shadow compositing", new(grouped, Action: "reverse", NoShadow: true));
    }
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<DropdownMenuConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        // Like the existing TooltipBox/Dialog fixtures, supply a real overlay host.
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            names.Register(presenter.Name, presenter);
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            names.Register(layers.Name, layers);
            return layers;
        });
        MenuCases.SetColors(host, config.Dark);
        var marker = new Marker(48, 16);
        var selectedIcon = new Marker(20, 20);
        var anchor = new Border { Width = 24, Height = 24, Background = Brushes.Blue };
        DropdownMenuPopup menu = config.Grouped ? new DropdownMenuPopup() : new DropdownMenu();
        if (config.NoShadow && menu is DropdownMenu baseline) baseline.ShadowElevation = 0;
        menu.PlacementTarget = anchor;
        menu.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        CheckableDropdownMenuItem? check = null;
        var divider = new Separator();
        if (config.Grouped)
        {
            check = new CheckableDropdownMenuItem { Content = marker, SupportingText = new Marker(32, 12),
                SelectedLeadingIcon = selectedIcon, TrailingContent = new Marker(16, 16) };
            var group = new DropdownMenuGroup { Items = { check } };
            if (config.Divider) group.Items.Add(divider);
            if (config.NoShadow) group.ShadowElevation = 0;
            menu.Items.Add(group);
        }
        else
        {
            menu.Items.Add(new DropdownMenuItem { Content = marker, TrailingContent = new Marker(16, 16) });
            if (config.Divider) menu.Items.Add(divider);
        }
        var requests = 0;
        menu.DismissRequested += (_, _) => requests++;
        var canvas = new Canvas { Background = Brushes.White, Children = { anchor, menu } };
        Canvas.SetLeft(anchor, config.X); Canvas.SetTop(anchor, config.Y);
        menu.IsOpen = config.Action is not ("show" or "reverse");
        host.Mount(canvas); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("state", "state", null,
                [Measurement.Flag("expanded", menu.IsOpen, definition: "dropdown.expanded"),
                 Measurement.Flag("presented", marker.IsAttachedToVisualTree(), definition: "dropdown.content-presented"),
                 Measurement.Flag("selected", check?.IsChecked == true, definition: "menu.selected"),
                 Measurement.Numeric("requests", requests, "count", definition: "dropdown.dismiss-requests")]) };
            if (config.Action == "selection")
                points.Add(new("selected-icon", "content", null,
                    [Measurement.Flag("presented", selectedIcon.IsAttachedToVisualTree(), definition: "menu.selected-icon-presented")]));
            // Raster observes transformed position and appearance. Local size avoids treating
            // float vs double transform-matrix representations as a layout contract.
            if (marker.IsAttachedToVisualTree())
            {
                if (config.Divider)
                    points.Add(new("divider", "divider", null,
                        AvaloniaMeasurements.Geometry(divider, host.Window).Where(m => m.Name is "box" or "size").ToArray()));
                points.Add(new("content", "content", null,
                    AvaloniaMeasurements.Geometry(marker, host.Window).Where(m => m.Name == "size" || config.Action == "selection" && m.Name == "box").ToArray()));
                var layer = host.Window.GetVisualDescendants().OfType<Control>().Single(c => c.Name == "PART_MenuLayer");
                points.Add(new("appearance", "popup", null,
                    [Measurement.Numeric("scale", ((ScaleTransform)layer.RenderTransform!).ScaleX, "none", category: "motion", definition: "dropdown.appearance.scale"),
                     Measurement.Numeric("alpha", layer.Opacity, "none", category: "motion", definition: "dropdown.appearance.alpha")]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        var origin = menu.IsOpen ? 800000L : 0;
        Advance(origin); host.Draw(); Sample("initial");
        if (!dynamic) return;
        void Change(long time, string id, Action action)
        { Advance(origin + time); action(); writer.Record(origin + time, "mutation", id); host.Draw(); host.Frame(origin + time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(origin + time); host.Draw(); Sample($"time-{time}"); } }
        switch (config.Action)
        {
            case "show": case "reverse":
                Change(1000, "open", () => menu.IsOpen = true); Times(16000, 32000, 64000);
                if (config.Action == "reverse")
                {
                    Change(65000, "close", () => menu.IsOpen = false); Times(80000, 96000);
                    Change(97000, "reopen", () => menu.IsOpen = true); Times(112000, 224000, 800000);
                }
                else
                {
                    Times(224000, 800000); Change(801000, "close", () => menu.IsOpen = false);
                    Times(816000, 864000, 960000, 1200000);
                }
                break;
            case "outside":
                Change(1000, "outside-down", () => { host.Input("move", new(8, 8)); host.Input("down", new(8, 8)); });
                Change(2000, "outside-up", () => host.Input("up", new(8, 8))); Times(16000); break;
            case "selection":
                Change(1000, "selected", () => check!.IsChecked = true); Times(16000, 32000, 64000);
                Change(65000, "reversed", () => check!.IsChecked = false); Times(80000, 160000, 400000, 800000); break;
            case "hover":
                var center = marker.TranslatePoint(new(24, 8), host.Window)!.Value;
                Change(1000, "enter", () => host.Input("move", center)); Times(16000, 80000);
                Change(81000, "exit", () => host.Input("move", new(8, 8))); Times(96000, 160000, 400000); break;
        }
    }
    private sealed class Marker(double width, double height) : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.FillRectangle(TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record AppBarOverflowConfig(bool Vertical = false, bool Rtl = false, int Space = 128,
    int Maximum = int.MaxValue, string Action = "none", bool Dark = false);

[ParitySuite("appbar-overflow")]
public sealed class AppBarOverflowCases
{
    public static IEnumerable<CaseData<AppBarOverflowConfig>> StaticConfigurations()
    {
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        {
            foreach (var space in new[] { 160, 128, 127, 96, 48, 20, 0 })
                yield return new($"static.{(vertical ? "column" : "row")}.{(rtl ? "rtl" : "ltr")}.space-{space}",
                    "Intrinsic overflow reservation and constrained content", new(vertical, rtl, space));
            foreach (var maximum in new[] { 0, 1, 2, 3 })
                yield return new($"static.{(vertical ? "column" : "row")}.{(rtl ? "rtl" : "ltr")}.count-{maximum}",
                    "Count limit including the final-item exception", new(vertical, rtl, 160, maximum));
        }
    }

    public static IEnumerable<CaseData<AppBarOverflowConfig>> DynamicConfigurations()
    {
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "resize", "menu", "reverse", "outside" })
            yield return new($"dynamic.{(vertical ? "column" : "row")}.{(rtl ? "rtl" : "ltr")}.{action}",
                "Real overflow layout and popup lifecycle", new(vertical, rtl, vertical ? 72 : 96, Action: action, Dark: rtl));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var config = writer.Metadata.ReadConfig<AppBarOverflowConfig>();
        await using var host = new AvaloniaHost(writer, 320, 280);
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            names.Register(presenter.Name, presenter);
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            names.Register(layers.Name, layers); return layers;
        });
        MenuCases.SetColors(host, config.Dark);
        AppBarRow bar = config.Vertical ? new AppBarColumn() : new AppBarRow();
        bar.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        bar.MaxItemCount = config.Maximum;
        void Resize(int space) { if (config.Vertical) bar.MaxHeight = space; else bar.MaxWidth = space; }
        Resize(config.Space);
        Button? indicator = null;
        bar.OverflowIndicatorTemplate = new FuncDataTemplate<AppBarMenuState>((state, _) =>
        {
            // Compose size() is constrained by its parent. Avalonia Width/Height force
            // ArrangeCore past that constraint, so express the same preferred size in Measure.
            indicator = new Button {
                Template = new FuncControlTemplate<Button>((_, _) => new Marker(32, 40, Brushes.Green)) };
            indicator.Click += (_, _) => { if (state!.IsShowing) state.Dismiss(); else state.Show(); };
            return indicator;
        });
        var clicks = 0;
        var menuItems = new List<DropdownMenuItem>();
        var sizes = new[] { new Size(40, 24), new Size(56, 32), new Size(32, 40) };
        for (var i = 0; i < sizes.Length; i++)
        {
            var menuItem = new DropdownMenuItem { Content = new Marker(48, 16) };
            menuItem.Click += (_, _) => { clicks++; bar.MenuState.Dismiss(); };
            menuItems.Add(menuItem);
            bar.Items.Add(new AppBarCustomItem
            {
                Content = new Marker(sizes[i].Width, sizes[i].Height, Brushes.Blue),
                MenuContent = menuItem
            });
        }
        var canvas = new Canvas { Background = Brushes.White, Children = { bar } };
        Canvas.SetLeft(bar, 48); Canvas.SetTop(bar, 32);
        host.Mount(canvas); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [new("bar", "container", null, AvaloniaMeasurements.Geometry(bar, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
         new("state", "state", null, [Measurement.Flag("expanded", bar.MenuState.IsShowing, definition: "appbar.menu.showing"),
             Measurement.Numeric("clicks", clicks, "count", definition: "appbar.menu.clicks")])]));
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Click(Control control)
        {
            var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), host.Window)!.Value;
            host.Input("move", center); host.Input("down", center); host.Input("up", center);
        }
        Sample("initial");
        if (!dynamic) return;
        if (config.Action == "resize")
        {
            Change(1000, "all-visible", () => Resize(160));
            Change(2000, "none-visible", () => Resize(20));
            Change(3000, "restore", () => Resize(config.Space));
            return;
        }
        Change(1000, "open", () => Click(indicator!)); Times(16000, 32000, 64000);
        if (config.Action == "reverse")
        {
            Change(65000, "close", bar.MenuState.Dismiss); Times(80000, 96000);
            Change(97000, "reopen", bar.MenuState.Show); Times(112000, 224000, 800000);
        }
        else
        {
            Times(224000, 800000);
            if (config.Action == "outside") Change(801000, "outside", () =>
            { host.Input("move", new(8, 8)); host.Input("down", new(8, 8)); host.Input("up", new(8, 8)); });
            else Change(801000, "select", () => Click(menuItems[^1]));
            Times(816000, 864000, 960000, 1200000);
        }
    }

    private sealed class Marker(double width, double height, IBrush? brush = null) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.FillRectangle(brush ?? Avalonia.Controls.Documents.TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}

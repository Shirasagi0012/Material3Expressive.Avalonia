using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record NativeMenuMotionConfig(bool Baseline = false, bool Rtl = false, string Action = "show");

[ParitySuite("native-menu-motion")]
public sealed class NativeMenuMotionCases
{
    public static IEnumerable<CaseData<NativeMenuMotionConfig>> Configurations()
    {
        foreach (var baseline in new[] { false, true })
        {
            var kind = baseline ? "baseline" : "standard";
            foreach (var action in new[] { "show", "reverse", "command", "escape", "outside", "unload", "new-anchor", "edge" })
                yield return new($"dynamic.{kind}.{action}", "Native popup lifecycle with Material appearance", new(baseline, false, action));
            foreach (var action in new[] { "show", "edge", "reverse" })
                yield return new($"dynamic.{kind}.rtl.{action}", "RTL native popup appearance and edge origin", new(baseline, true, action));
        }
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<NativeMenuMotionConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var content = new ContentPresenter { Name = "PART_ContentPresenter" };
            content.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            names.Register(content.Name, content);
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = content };
            names.Register(layers.Name, layers); return layers;
        });
        MenuCases.SetColors(host, false);
        var anchor = new Border { Width = 24, Height = 24, Background = Brushes.Blue, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var second = new Border { Width = 24, Height = 24, Background = Brushes.Blue, FlowDirection = anchor.FlowDirection };
        Canvas.SetLeft(anchor, config.Action == "edge" ? 292 : 112); Canvas.SetTop(anchor, config.Action == "edge" ? 208 : 64);
        Canvas.SetLeft(second, 48); Canvas.SetTop(second, 120);
        var root = new Canvas { Background = Brushes.White, Children = { anchor } };
        var marker = new Marker();
        var calls = 0;
        var item = new MenuItem { Header = marker };
        item.Click += (_, _) => calls++;
        var flyout = new MaterialMenuFlyout { Placement = PlacementMode.BottomEdgeAlignedLeft, Items = { item } };
        flyout.Popup.ShouldUseOverlayLayer = true;
        if (config.Baseline) flyout.FlyoutPresenterTheme = (ControlTheme)host.Window.FindResource("MaterialBaselineMenuTheme")!;
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        var next = 16000L;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var visible = marker.IsAttachedToVisualTree() && marker.IsEffectivelyVisible;
            var points = new List<ProbePoint>
            {
                new("state", "state", null,
                [Measurement.Flag("expanded", flyout.IsOpen, definition: "dropdown.expanded"),
                    Measurement.Flag("presented", visible, definition: "dropdown.content-presented"),
                    Measurement.Numeric("activations", calls, "count", definition: "menu.activations")])
            };
            if (visible)
            {
                points.Add(new("content", "content", null, AvaloniaMeasurements.Geometry(marker, host.Window).Where(m => m.Name is "box" or "size").ToArray()));
                points.Add(new("appearance", "popup", null,
                [Measurement.Numeric("scale", flyout.AppearanceScale, "none", category: "motion", definition: "dropdown.appearance.scale"),
                    Measurement.Numeric("alpha", flyout.AppearanceAlpha, "none", category: "motion", definition: "dropdown.appearance.alpha")]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); }
        void At(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        Sample("initial");
        Change(1000, "open", () => flyout.ShowAt(anchor)); At(16000, 32000, 64000);
        if (config.Action == "reverse")
        {
            Change(65000, "close", flyout.Hide); At(80000, 96000);
            Change(97000, "reopen", () => flyout.ShowAt(anchor)); At(112000, 160000, 400000, 800000);
        }
        else
        {
            At(160000, 400000, 800000);
            Change(801000, config.Action, () =>
            {
                switch (config.Action)
                {
                    case "command":
                        var point = marker.TranslatePoint(new Rect(marker.Bounds.Size).Center, host.Window)!.Value;
                        host.Input("move", point); host.Input("down", point); host.Input("up", point); break;
                    case "escape":
                        host.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null);
                        host.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null); break;
                    case "outside": host.Input("move", new Point(8, 8)); host.Input("down", new Point(8, 8)); host.Input("up", new Point(8, 8)); break;
                    case "unload": root.Children.Remove(anchor); break;
                    case "new-anchor": root.Children.Add(second); host.Draw(); flyout.ShowAt(second); break;
                    default: flyout.Hide(); break;
                }
            });
            At(816000, 832000, 864000, 960000, 1200000);
        }
        // Detaching the actual anchor exercises the native non-cancellable cleanup path.
        root.Children.Clear(); host.Draw();
    }

    private sealed class Marker : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(48, 16);
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}

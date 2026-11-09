using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ListItemLongClickConfig(bool Segmented = false, string Mode = "click", string Action = "invoke");

[ParitySuite("list-item-long-click")]
public sealed class ListItemLongClickCases
{
    public static IEnumerable<CaseData<ListItemLongClickConfig>> Configurations()
    {
        foreach (var segmented in new[] { false, true })
        {
            var kind = segmented ? "segmented" : "standard";
            foreach (var mode in new[] { "click", "single", "multiple" })
            foreach (var action in new[] { "invoke", "disabled" })
                yield return new($"dynamic.{kind}.{mode}.{action}", "Invoke the named accessibility long-click action", new(segmented, mode, action));
            foreach (var action in new[] { "lifecycle", "mode-change", "no-label", "no-handler", "passive" })
                yield return new($"dynamic.{kind}.{action}", "Named long-click availability and callback changes", new(segmented, "click", action));
        }
    }

    [ParityScenario("behavior", Data = nameof(Configurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ListItemLongClickConfig>();
        await using var host = new AvaloniaHost(writer, 320, 180);
        CaseDefaults.SetThemeColors(host, false);
        host.Window.Resources[Sys.Color.Surface] = Color.Parse("#FEF7FF");
        host.Window.Resources[Sys.Color.TertiaryContainer] = Color.Parse("#FFD8E4");
        host.Window.Resources[Sys.Color.OnTertiaryContainer] = Color.Parse("#31111D");
        ListItem item = config.Segmented ? new SegmentedListItem() : new ListItem();
        item.Width = 280;
        item.HorizontalAlignment = HorizontalAlignment.Center;
        item.VerticalAlignment = VerticalAlignment.Center;
        item.InteractionMode = config.Action == "passive" ? ListItemInteractionMode.None : config.Mode switch
        {
            "single" => ListItemInteractionMode.SingleSelection,
            "multiple" => ListItemInteractionMode.MultipleSelection,
            _ => ListItemInteractionMode.Click
        };
        item.IsInteractionEnabled = config.Action != "disabled";
        item.LongClickLabel = config.Action == "no-label" ? null : "More actions";
        item.Content = new Marker();
        var longClicks = 0;
        var clicks = 0;
        var requests = 0;
        EventHandler callback = (_, _) =>
        {
            longClicks++;
            if (config.Action == "mode-change")
            {
                item.InteractionMode = ListItemInteractionMode.MultipleSelection;
                item.IsChecked = true;
            }
        };
        if (config.Action != "no-handler") item.LongClick += callback;
        item.Click += (_, _) => clicks++;
        item.CheckedChangeRequested += (_, _) => requests++;
        host.Mount(new Grid { Children = { item } });
        host.Frame(-1000);
        host.Frame(0);
        var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
        AutomationPeer? Action() => peer.GetChildren().SingleOrDefault(p => p.GetAutomationId() == "ListItem.LongClick");
        ProbePoint[] Measure()
        {
            var action = Action();
            return [new("item", "list-item", null,
            [..AvaloniaMeasurements.Geometry(item, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Flag("hasNamedAction", action is not null, definition: "list-item.named-long-click-present"),
                Measurement.Flag("actionEnabled", action?.IsEnabled() == true, definition: "list-item.named-long-click-enabled"),
                Measurement.Text("label", action?.GetName() ?? "", definition: "list-item.named-long-click-label"),
                Measurement.Numeric("longClicks", longClicks, "count", definition: "list-item.long-clicks"),
                Measurement.Numeric("clicks", clicks, "count", definition: "list-item.clicks"),
                Measurement.Numeric("requests", requests, "count", definition: "list-item.checked-requests"),
                Measurement.Flag("selected", item.Selected, definition: "list-item.selected")])];
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, Measure()));
        var next = 16000L;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action mutation)
        {
            Advance(time);
            mutation();
            writer.Record(time, "mutation", id);
            host.Draw();
        }
        void At(long time) { Advance(time); host.Draw(); Sample($"time-{time}"); }
        void Invoke() { if (Action() is IInvokeProvider action) action.Invoke(); }
        Sample("initial");
        // Invoke the actual provider even for a disabled row. Core's raw semantics callback
        // does not guard Disabled; the native provider does. Keep this difference observable.
        Change(1000, "invoke-long-click", Invoke);
        At(16000); At(64000); At(160000);
        if (config.Action == "lifecycle")
        {
            Change(161000, "rename", () => item.LongClickLabel = "Archive"); At(176000);
            Change(177000, "remove-handler", () => item.LongClick -= callback); At(192000);
            Change(193000, "invoke-absent", Invoke); At(208000);
            Change(209000, "restore-handler", () => item.LongClick += callback); At(224000);
            Change(225000, "invoke-restored", Invoke); At(240000);
            Change(241000, "remove-label", () => item.LongClickLabel = null); At(256000);
        }
        At(400000);
    }

    private sealed class Marker : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(64, 24);
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}

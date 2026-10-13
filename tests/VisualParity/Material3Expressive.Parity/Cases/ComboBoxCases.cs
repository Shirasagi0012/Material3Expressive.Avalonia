using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ComboBoxConfig(bool Outlined = false, bool Slots = false, bool Label = true,
    bool Enabled = true, bool Error = false, bool Dark = false, bool Rtl = false,
    double? Width = null, double? Height = null, double LabelWidth = 40, double LabelHeight = 12,
    int Items = 0, int Selected = -1, string Action = "none");

[ParitySuite("combo-box")]
public sealed class ComboBoxCases
{
    public static IEnumerable<CaseData<ComboBoxConfig>> StaticConfigurations()
    {
        // Filled is Inside and outlined is Cutout; the theme owns that pairing. Above, label
        // alignment, prefix, and suffix are not part of this control. PlaceholderText is a string,
        // so it stays outside the current non-text coverage. The trailing arrow is real template
        // chrome, not a rectangle substitute.
        foreach (var outlined in new[] { false, true })
        foreach (var slots in new[] { false, true })
            yield return Data($"static.appearance.{Variant(outlined)}.{(slots ? "slots" : "plain")}",
                new(outlined, Slots: slots, Height: 96));
        foreach (var outlined in new[] { false, true })
        {
            foreach (var state in new[] { "dark", "error", "disabled", "disabled-error" })
                yield return Data($"static.state.{Variant(outlined)}.{state}", new(outlined, Slots: true,
                    Enabled: !state.Contains("disabled"), Error: state.Contains("error"), Dark: state == "dark", Height: 96));
            yield return Data($"static.rtl.{Variant(outlined)}", new(outlined, Slots: true, Rtl: true, Height: 96));
            yield return Data($"static.empty.{Variant(outlined)}", new(outlined, Label: false));
            yield return Data($"static.wide-label.{Variant(outlined)}", new(outlined, LabelWidth: 340, LabelHeight: 36));
            yield return Data($"static.narrow.{Variant(outlined)}", new(outlined, Slots: true, Width: 180, Height: 96));
            yield return Data($"static.tall.{Variant(outlined)}", new(outlined, Slots: true, Height: 100));
        }
        // One group, rectangle rows. Open/close motion is not implemented, so these stay settled.
        // Fixed height keeps the empty-line metric (56 vs 58.5) out of the menu comparison.
        // static.empty still records that metric.
        yield return Data("static.menu.filled.items-1", new(Items: 1, Height: 96));
        yield return Data("static.menu.filled.items-3", new(Items: 3, Height: 96));
        yield return Data("static.menu.outlined.items-3", new(true, Items: 3, Height: 96));
        yield return Data("static.menu.filled.items-3.rtl", new(Items: 3, Rtl: true, Height: 96));
        yield return Data("static.menu.filled.items-3.dark", new(Items: 3, Dark: true, Height: 96));
        yield return Data("static.menu.filled.items-3.selected", new(Items: 3, Selected: 1, Height: 96));
        yield return Data("static.menu.filled.items-3.slots", new(Slots: true, Items: 3, Height: 96));
    }

    public static IEnumerable<CaseData<ComboBoxConfig>> DynamicConfigurations()
    {
        // Closed-field color and label motion. Selection closes the native popup, and the exit
        // animation is not implemented, so the menu has no dynamic timeline here.
        foreach (var outlined in new[] { false, true })
        foreach (var action in new[] { "focus", "reverse", "disable", "error", "theme" })
            yield return Data($"dynamic.{action}.{Variant(outlined)}", new(outlined, Slots: true, Action: action, Height: 96));
    }

    static string Variant(bool outlined) => outlined ? "outlined" : "filled";
    static CaseData<ComboBoxConfig> Data(string id, ComboBoxConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ComboBoxConfig>();
        var menu = config.Items > 0;
        await using var host = new AvaloniaHost(writer, menu ? 560 : 400, menu ? 480 : 200);
        if (menu) InstallOverlay(host);
        var scene = new FieldScene(host, config);
        host.Mount(scene.Root);
        host.Frame(-1000);
        host.Frame(0);
        if (menu)
        {
            // Compose plays the menu enter transition from a closed state. Avalonia shows the
            // popup immediately. 800ms is the same settled point the dropdown suite uses.
            long next = 16000;
            while (next <= 800000) { host.Frame(next); next += 16000; }
            host.AdvanceTo(800000);
        }
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ComboBoxConfig>();
        await using var host = new AvaloniaHost(writer, 400, 200);
        var scene = new FieldScene(host, config);
        host.Mount(scene.Root);
        host.Frame(-1000);
        host.Frame(0);
        // Focus can move a platform caret on the editable path. These cases stay non-editable;
        // error and theme keep raster, and the others record geometry and label progress.
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: config.Action is "error" or "theme"));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action change)
        {
            Advance(time);
            change();
            writer.Record(time, "mutation", id);
            host.Draw();
            host.Frame(time);
            Sample(id);
        }
        Sample("initial");
        Change(1000, "start", () =>
        {
            if (config.Action == "error") MaterialComboBox.SetIsError(scene.Box, true);
            else if (config.Action == "theme") scene.Theme(true);
            else scene.Box.Focus();
        });
        foreach (var time in new long[] { 16000, 32000, 64000 }) { Advance(time); Sample($"time-{time}"); }
        if (config.Action == "reverse") Change(65000, "reverse", () => scene.FocusTarget.Focus());
        if (config.Action == "disable") Change(65000, "disable", () => scene.Box.IsEnabled = false);
        foreach (var time in new long[] { 96000, 160000, 240000, 400000, 800000 }) { Advance(time); Sample($"time-{time}"); }
    }

    static void InstallOverlay(AvaloniaHost host)
    {
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var presenter = new ContentPresenter { Name = "PART_ContentPresenter" };
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            names.Register(presenter.Name, presenter);
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            names.Register(layers.Name, layers);
            return layers;
        });
    }

    sealed class FieldScene
    {
        readonly AvaloniaHost host;
        readonly ComboBoxConfig config;
        readonly Dictionary<string, Control> slots = new();
        readonly List<Marker> markers = new();
        public ComboBox Box { get; } = new();
        public Control FocusTarget { get; } = new Border { Focusable = true, Width = 1, Height = 1 };
        public Control Root { get; }

        public FieldScene(AvaloniaHost host, ComboBoxConfig config)
        {
            this.host = host;
            this.config = config;
            host.Window.Styles.Insert(0, new FluentTheme());
            Theme(config.Dark);
            Box.Theme = (ControlTheme)host.Window.FindResource(config.Outlined ? "MaterialOutlinedComboBoxTheme" : "MaterialFilledComboBoxTheme")!;
            Box.IsEnabled = config.Enabled;
            if (config.Width is { } width) Box.Width = width;
            if (config.Height is { } height) Box.Height = height;
            Box.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            MaterialComboBox.SetIsError(Box, config.Error);
            Control Slot(string name, double width, double height)
            {
                var rectangle = new Marker(width, height)
                {
                    HorizontalAlignment = name == "leading" ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                slots.Add(name, rectangle);
                return rectangle;
            }
            if (config.Label) MaterialComboBox.SetLabel(Box, Slot("label", config.LabelWidth, config.LabelHeight));
            if (config.Slots)
            {
                MaterialComboBox.SetLeadingIcon(Box, Slot("leading", 24, 24));
                MaterialComboBox.SetSupportingText(Box, Slot("supporting", 60, 12));
            }
            if (config.Items > 0)
            {
                Box.ItemTemplate = new FuncDataTemplate<Row>((_, _) =>
                {
                    var marker = new Marker(48, 16)
                    {
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Center,
                    };
                    markers.Add(marker);
                    return new Panel { Children = { marker } };
                });
                for (var i = 0; i < config.Items; i++) Box.Items.Add(new Row(i));
                if (config.Selected >= 0) Box.SelectedIndex = config.Selected;
                Box.IsDropDownOpen = true;
            }
            if (config.Items > 0)
            {
                Box.HorizontalAlignment = HorizontalAlignment.Center;
                Box.VerticalAlignment = VerticalAlignment.Top;
                Box.Margin = new Thickness(0, 32, 0, 0);
                Root = new Grid { Background = Brushes.White, Children = { Box } };
            }
            else Root = new Grid { Children = { FocusTarget, CaseDefaults.Center(Box) } };
        }

        public void Theme(bool dark)
        {
            MenuCases.SetColors(host, dark);
            host.Window.Resources[Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
            host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse(dark ? "#36343B" : "#E6E0E9");
            host.Window.Resources[Sys.Color.Error] = Color.Parse(dark ? "#F2B8B5" : "#B3261E");
        }

        public ProbePoint[] Measure()
        {
            var menuItems = markers.Where(marker => marker.GetVisualAncestors().OfType<ComboBoxItem>().Any())
                .OrderBy(marker => marker.TranslatePoint(default, host.Window)!.Value.Y).ToList();
            if (config.Items > 0 && menuItems.Count != config.Items)
                throw new InvalidOperationException($"Expected {config.Items} menu markers, found {menuItems.Count}.");
            var result = new List<ProbePoint>
            {
                new("field", "field", null, AvaloniaMeasurements.Geometry(Box, host.Window).Where(measurement => measurement.Name is "box" or "size").ToArray()),
                new("state", "state", null,
                [
                    Measurement.Flag("expanded", Box.IsDropDownOpen, definition: "combo-box.menu-expanded"),
                    Measurement.Numeric("items", menuItems.Count, "count", definition: "combo-box.menu-items-presented"),
                ]),
            };
            foreach (var (name, slot) in slots.Where(pair => pair.Value.IsEffectivelyVisible && TopLevel.GetTopLevel(pair.Value) != null))
                result.Add(new(name, name, "field", AvaloniaMeasurements.Geometry(slot, host.Window).Where(measurement => measurement.Name is "box" or "size").ToArray()));
            if (config.Label)
            {
                var layout = Box.GetVisualDescendants().Single(visual => visual.GetType().Name == "ComboBoxField");
                var value = (float)layout.GetType().GetProperty("LabelProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(layout)!;
                result.Add(new("motion", "motion", "field", [Measurement.Numeric("labelProgress", value, "ratio", category: "motion", definition: "combo-box.label-minimized-progress")]));
            }
            for (var i = 0; i < menuItems.Count; i++)
                result.Add(new($"item-{i}", "content", "field", AvaloniaMeasurements.Geometry(menuItems[i], host.Window).Where(measurement => measurement.Name is "box" or "size").ToArray()));
            var valueMarker = markers.SingleOrDefault(marker => marker.IsEffectivelyVisible && marker.IsAttachedToVisualTree()
                && !marker.GetVisualAncestors().OfType<ComboBoxItem>().Any());
            if (valueMarker != null)
                result.Add(new("value", "value", "field", AvaloniaMeasurements.Geometry(valueMarker, host.Window).Where(measurement => measurement.Name is "box" or "size").ToArray()));
            return result.ToArray();
        }
    }

    sealed record Row(int Index);

    sealed class Marker(double width, double height) : Control
    {
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            if (TextElement.GetForeground(this) is { } brush) context.FillRectangle(brush, new Rect(Bounds.Size));
        }
    }
}

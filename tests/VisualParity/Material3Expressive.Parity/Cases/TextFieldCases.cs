using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record TextFieldConfig(bool Outlined = false, string Position = "Inside", bool Minimized = false,
    bool Label = true, bool Slots = false, bool Enabled = true, bool Error = false, bool Dark = false,
    bool Rtl = false, string Alignment = "Start", double? Width = null, double? Height = null,
    double LabelWidth = 40, double LabelHeight = 12, string Action = "none");

[ParitySuite("text-field")]
public sealed class TextFieldCases
{
    public static IEnumerable<CaseData<TextFieldConfig>> StaticConfigurations()
    {
        // Container style and label position are independent upstream. Fixed external height isolates
        // decoration/raster from native empty-line font metrics. Auto-size cases below retain those metrics.
        // Exercise every valid appearance combination.
        foreach (var outline in new[] { false, true })
        foreach (var position in new[] { "Inside", "Cutout", "Above" })
        foreach (var minimized in position == "Above" ? new[] { true } : new[] { false, true })
        foreach (var slots in new[] { false, true })
            yield return Data($"static.appearance.{outline}.{position}.{minimized}.{slots}", new(outline, position, minimized, Slots: slots, Height: 96));
        foreach (var outline in new[] { false, true })
        {
            var position = outline ? "Cutout" : "Inside";
            foreach (var state in new[] { "dark", "error", "disabled", "disabled-error" })
                yield return Data($"static.state.{outline}.{state}", new(outline, position, true, Slots: true,
                    Enabled: !state.Contains("disabled"), Error: state.Contains("error"), Dark: state == "dark", Height: 96));
            foreach (var position2 in new[] { "Inside", "Cutout", "Above" })
            foreach (var align in new[] { "Start", "Center", "End" })
                yield return Data($"static.rtl.{outline}.{position2}.{align}", new(outline, position2, true, Slots: true, Rtl: true, Alignment: align, Height: 96));
            yield return Data($"static.empty.{outline}", new(outline, position, Label: false));
            yield return Data($"static.wide-label.{outline}", new(outline, position, LabelWidth: 340, LabelHeight: 36));
            yield return Data($"static.narrow.{outline}", new(outline, position, true, Slots: true, Width: 180));
            yield return Data($"static.tall.{outline}", new(outline, position, true, Slots: true, Height: 100));
        }
    }

    public static IEnumerable<CaseData<TextFieldConfig>> DynamicConfigurations()
    {
        foreach (var outline in new[] { false, true })
        foreach (var action in new[] { "focus", "reverse", "disable", "error", "theme" })
            yield return Data($"dynamic.{action}.{outline}", new(outline, outline ? "Cutout" : "Inside", Slots: true, Action: action, Height: 96));
    }
    static CaseData<TextFieldConfig> Data(string id, TextFieldConfig config) => new(id.ToLowerInvariant(), id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        await using var host = new AvaloniaHost(writer, 400, 200);
        var scene = new FieldScene(host, writer.Metadata.ReadConfig<TextFieldConfig>());
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("changes", Data = nameof(DynamicConfigurations))]
    public async Task Changes(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<TextFieldConfig>();
        await using var host = new AvaloniaHost(writer, 400, 200);
        var scene = new FieldScene(host, config);
        host.Mount(scene.Root); host.Frame(-1000); host.Frame(0);
        // Native caret blink uses the platform timer, not the decoration clock. Focus cases observe
        // actual slot geometry/progress; raster is covered by unfocused appearance/state cases.
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: config.Action is "error" or "theme"));
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        Sample("initial");
        Change(1000, "start", () =>
        {
            if (config.Action == "error") MaterialTextField.SetIsError(scene.Box, true);
            else if (config.Action == "theme") scene.Theme(true);
            else scene.Box.Focus();
        });
        foreach (var time in new long[] { 16000, 32000, 64000 }) { Advance(time); Sample($"time-{time}"); }
        if (config.Action == "reverse") Change(65000, "reverse", () => scene.FocusTarget.Focus());
        if (config.Action == "disable") Change(65000, "disable", () => scene.Box.IsEnabled = false);
        foreach (var time in new long[] { 96000, 160000, 240000, 400000, 800000 }) { Advance(time); Sample($"time-{time}"); }
    }

    sealed class FieldScene
    {
        readonly AvaloniaHost host;
        readonly TextFieldConfig config;
        readonly Dictionary<string, Control> slots = new();
        public TextBox Box { get; } = new();
        public Control FocusTarget { get; } = new Border { Focusable = true, Width = 1, Height = 1 };
        public Control Root { get; }
        public FieldScene(AvaloniaHost host, TextFieldConfig config)
        {
            this.host = host; this.config = config;
            // Native ScrollViewer/Menu infrastructure, as in the consumer Catalog; the editor and
            // decoration use the product's explicit Material theme throughout.
            host.Window.Styles.Insert(0, new FluentTheme());
            Theme(config.Dark);
            Box.Theme = (ControlTheme)host.Window.FindResource(config.Outlined ? "MaterialOutlinedTextFieldTheme" : "MaterialFilledTextFieldTheme")!;
            Box.AcceptsReturn = false;
            Box.IsEnabled = config.Enabled;
            if (config.Width is { } w) Box.Width = w;
            if (config.Height is { } h) Box.Height = h;
            Box.FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            MaterialTextField.SetLabelPosition(Box, Enum.Parse<TextFieldLabelPosition>(config.Position));
            MaterialTextField.SetAlwaysMinimizeLabel(Box, config.Minimized);
            MaterialTextField.SetExpandedLabelAlignment(Box, Enum.Parse<TextFieldLabelAlignment>(config.Alignment));
            MaterialTextField.SetMinimizedLabelAlignment(Box, Enum.Parse<TextFieldLabelAlignment>(config.Alignment));
            MaterialTextField.SetIsError(Box, config.Error);
            Control Slot(string name, double width, double height)
            {
                // Compose Modifier.size is constrained preferred size. Avalonia's explicit Width
                // instead overflows tight constraints; intrinsic marker size preserves the same input.
                var rectangle = new FieldMarker(width, height)
                {
                    HorizontalAlignment = name is "leading" or "trailing" ? HorizontalAlignment.Center : HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                slots.Add(name, rectangle); return rectangle;
            }
            if (config.Label) MaterialTextField.SetLabel(Box, Slot("label", config.LabelWidth, config.LabelHeight));
            if (config.Slots)
            {
                Box.InnerLeftContent = Slot("leading", 24, 24); Box.InnerRightContent = Slot("trailing", 24, 24);
                MaterialTextField.SetSupportingText(Box, Slot("supporting", 60, 12));
                MaterialTextField.SetPrefix(Box, Slot("prefix", 16, 12)); MaterialTextField.SetSuffix(Box, Slot("suffix", 20, 12));
                MaterialTextField.SetPlaceholder(Box, Slot("placeholder", 48, 16));
            }
            Root = new Grid { Children = { FocusTarget, CaseDefaults.Center(Box) } };
        }
        public void Theme(bool dark)
        {
            CaseDefaults.SetThemeColors(host, dark);
            host.Window.Resources[Sys.Color.Outline] = Color.Parse(dark ? "#938F99" : "#79747E");
            host.Window.Resources[Sys.Color.SurfaceContainerHighest] = Color.Parse(dark ? "#36343B" : "#E6E0E9");
            host.Window.Resources[Sys.Color.Error] = Color.Parse(dark ? "#F2B8B5" : "#B3261E");
        }
        public ProbePoint[] Measure()
        {
            var result = new List<ProbePoint> { new("field", "field", null, AvaloniaMeasurements.Geometry(Box, host.Window).Where(m => m.Name is "box" or "size").ToArray()) };
            foreach (var (name, slot) in slots.Where(p => p.Value.IsEffectivelyVisible && TopLevel.GetTopLevel(p.Value) != null))
                result.Add(new(name, name, "field", AvaloniaMeasurements.Geometry(slot, host.Window).Where(m => m.Name is "box" or "size").ToArray()));
            if (config.Label)
            {
                var layout = Box.GetVisualDescendants().Single(v => v.GetType().Name == "TextFieldLayout");
                var value = (float)layout.GetType().GetProperty("LabelProgress", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(layout)!;
                result.Add(new("motion", "motion", "field", [Measurement.Numeric("labelProgress", value, "ratio", category: "motion", definition: "text-field.label-minimized-progress")]));
            }
            return result.ToArray();
        }
    }

    sealed class FieldMarker(double width, double height) : Control
    {
        static FieldMarker() => AffectsRender<FieldMarker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(TextElement.GetForeground(this)!, new Rect(Bounds.Size));
    }
}

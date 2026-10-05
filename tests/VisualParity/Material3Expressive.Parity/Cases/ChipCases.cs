using Avalonia;
using Avalonia.Styling;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ChipConfig(string Kind = "assist", bool Morph = false, bool Selected = false,
    bool Enabled = true, bool Dark = false, string Icons = "none", bool Rtl = false,
    double? Width = null, double? Height = null, double ContentWidth = 32, double ContentHeight = 16,
    string Custom = "none", string Action = "none");

[ParitySuite("chip")]
public sealed class ChipCases
{
    static readonly string[] Kinds = ["assist", "elevated-assist", "suggestion", "elevated-suggestion", "filter", "elevated-filter", "input"];
    static bool Selectable(string kind) => kind is "filter" or "elevated-filter" or "input";
    static IEnumerable<ChipConfig> Variants() => Kinds.SelectMany(kind => Selectable(kind)
        ? new[] { new ChipConfig(kind), new ChipConfig(kind, Morph: true) } : new[] { new ChipConfig(kind) });
    static CaseData<ChipConfig> Data(string id, ChipConfig config) => new(id, id, config);

    public static IEnumerable<CaseData<ChipConfig>> StaticConfigurations()
    {
        // All valid kind × fixed/morph × selected × enabled × light/dark combinations.
        foreach (var variant in Variants())
        foreach (var selected in Selectable(variant.Kind) ? new[] { false, true } : new[] { false })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.appearance.{variant.Kind}.{(variant.Morph ? "morph" : "fixed")}.{(selected ? "selected" : "unselected")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}",
                variant with { Selected = selected, Enabled = enabled, Dark = dark, Icons = "start" });

        // Layout is shared by elevated wrappers. Morph overloads use compact asymmetric spacing.
        foreach (var kind in new[] { "assist", "suggestion", "filter", "input" })
        foreach (var morph in Selectable(kind) ? new[] { false, true } : new[] { false })
        foreach (var icons in kind == "suggestion" ? new[] { "none", "start" } : new[] { "none", "start", "end", "both" })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.layout.{kind}.{(morph ? "morph" : "fixed")}.{icons}.{(rtl ? "rtl" : "ltr")}", new(kind, morph, Icons: icons, Rtl: rtl));
        foreach (var morph in new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var icons in new[] { "avatar", "avatar-both" })
            yield return Data($"static.avatar.{(morph ? "morph" : "fixed")}.{icons}.{(enabled ? "enabled" : "disabled")}", new("input", morph, Enabled: enabled, Icons: icons));
        foreach (var kind in new[] { "assist", "filter", "input" })
        {
            yield return Data($"static.layout.{kind}.tall", new(kind, ContentHeight: 64));
            yield return Data($"static.layout.{kind}.constrained-width", new(kind, Width: 40, Icons: "both"));
            yield return Data($"static.layout.{kind}.constrained-height", new(kind, Height: 20, Icons: "both"));
            yield return Data($"static.layout.{kind}.explicit-width", new(kind, Width: 160, Icons: "both"));
            yield return Data($"static.custom.{kind}.spacing-padding-rtl", new(kind, Rtl: true, Icons: "both", Custom: "spacing"));
            foreach (var enabled in new[] { false, true })
            foreach (var selected in Selectable(kind) ? new[] { false, true } : new[] { false })
                yield return Data($"static.custom.{kind}.colors.{enabled.ToString().ToLowerInvariant()}.{selected.ToString().ToLowerInvariant()}", new(kind, Selected: selected, Enabled: enabled, Icons: "both", Custom: "colors"));
        }
        yield return Data("static.custom.border", new(Custom: "border"));
        yield return Data("static.custom.elevation", new(Custom: "elevation"));
        yield return Data("static.layout.selectable-max-width", new("filter", ContentWidth: 1200));
    }

    public static IEnumerable<CaseData<ChipConfig>> DynamicConfigurations()
    {
        // Colors/elevation differ by kind; input feedback, cancellation and disabling share a controller.
        foreach (var kind in Kinds)
        foreach (var action in new[] { "press", "hover", "quick", "disabled-input" })
            yield return Data($"dynamic.{kind}.{action}", new(kind, Enabled: action != "disabled-input", Icons: "start", Action: action));
        foreach (var action in new[] { "cancel", "disable-press", "space", "enter", "theme", "request-only" })
            yield return Data($"dynamic.filter.{action}", new("filter", Icons: "start", Action: action));
        // Transparent flat Filter hover has an approved default elevation deviation; retain
        // the real Compose defaults so raster and elevation differences remain observable.
        foreach (var morph in new[] { false, true })
        foreach (var selected in new[] { false, true })
            yield return Data($"dynamic.filter.{(morph ? "morph" : "fixed")}.{(selected ? "selected" : "unselected")}.hover",
                new("filter", morph, Selected: selected, Action: "hover"));
        foreach (var kind in new[] { "filter", "input" })
        foreach (var morph in new[] { false, true })
        foreach (var action in new[] { "icons", "reverse-icons", "selection", "reverse-selection", "press" })
            yield return Data($"dynamic.{kind}.{(morph ? "morph" : "fixed")}.{action}", new(kind, morph, Icons: action.Contains("icons") ? "none" : "both", Action: action));
        yield return Data("dynamic.input.avatar-replacement", new("input", Icons: "start", Action: "avatar"));
        foreach (var kind in new[] { "filter", "input" })
        foreach (var action in new[] { "resize", "shapes" })
            yield return Data($"dynamic.{kind}.morph.{action}", new(kind, Morph: true, Icons: "both", Action: action));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ChipConfig>();
        await using var host = new AvaloniaHost(writer, config.ContentWidth > 1000 ? 1400 : 320, 160);
        var scene = new ChipScene(host, config);
        host.Mount(CaseDefaults.Center(scene.Chip)); host.Draw(); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ChipConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160);
        var scene = new ChipScene(host, config); var chip = scene.Chip;
        host.Mount(CaseDefaults.Center(chip)); host.Draw(); host.Frame(-1000); host.Frame(0);
        void Sample(string id) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image: id != "time-32000"));
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        // Apply each external property request through a composition frame before reading it.
        // Draw alone does not run Compose's frame recomposer; sampling there would mix stages.
        void Mutate(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Event(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        var center = chip.TranslatePoint(new Point(chip.Bounds.Width / 2, chip.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(8, 8);
        Sample("initial");
        switch (config.Action)
        {
            case "resize":
                Mutate(1000, "selected", () => ((SelectableChip)chip).IsSelected = true); Times(16000, 64000, 800000);
                Mutate(801000, "taller", () => chip.Height = 64); Times(816000, 960000);
                Mutate(961000, "shorter", () => chip.Height = 24); Times(976000, 1200000); break;
            case "shapes":
                Mutate(1000, "selected", () => ((SelectableChip)chip).IsSelected = true); Times(16000, 32000, 64000);
                Mutate(65000, "shapes", () => ((SelectableChip)chip).Shapes = new ChipShapes
                    { Shape = new CornerRadius(6), SelectedShape = new CornerRadius(14), PressedShape = new CornerRadius(4) });
                Times(80000, 160000);
                Mutate(161000, "unselected", () => ((SelectableChip)chip).IsSelected = false); Times(176000, 240000);
                Mutate(241000, "equal-shapes", () => ((SelectableChip)chip).Shapes = new ChipShapes
                    { Shape = new CornerRadius(6), SelectedShape = new CornerRadius(14), PressedShape = new CornerRadius(4) });
                Times(256000, 400000); break;
            case "icons": case "reverse-icons":
                Mutate(1000, "icons-enter", () => scene.SetIcons("both")); Times(16000, 32000, 64000);
                Mutate(config.Action == "reverse-icons" ? 65000 : 401000, "icons-exit", () => scene.SetIcons("none"));
                Times(config.Action == "reverse-icons" ? 80000 : 416000, 480000, 640000, 1000000); break;
            case "avatar":
                Mutate(1000, "avatar", () => scene.SetIcons("avatar")); Times(16000, 64000, 400000);
                Mutate(401000, "icon", () => scene.SetIcons("start")); Times(416000, 480000, 800000); break;
            case "selection": case "reverse-selection":
                Mutate(1000, "selected", () => ((SelectableChip)chip).IsSelected = true); Times(16000, 32000, 64000);
                if (config.Action == "reverse-selection") Mutate(65000, "unselected", () => ((SelectableChip)chip).IsSelected = false);
                Times(80000, 160000, 400000, 800000); break;
            case "theme":
                Mutate(1000, "dark", () => CaseDefaults.SetThemeColors(host, true)); Times(16000, 80000, 400000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 64000, 160000);
                Event(161000, "move", outside); Sample("exit"); Times(176000, 240000, 400000); break;
            case "space": case "enter":
                Advance(1000); chip.Focus(NavigationMethod.Tab); host.Draw(); Sample("focus"); Times(16000, 80000);
                var key = config.Action == "space" ? Key.Space : Key.Enter;
                Advance(81000); host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(81000); Sample("key-down"); Times(96000, 160000);
                Advance(161000); host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); host.Draw(); host.Frame(161000); Sample("key-up"); Times(176000, 400000); break;
            default:
                Event(1000, "move", center); Event(2000, "down", center); Sample("press");
                if (config.Action is "quick" or "disabled-input" or "request-only")
                { Event(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 400000); break; }
                Times(16000, 64000);
                if (config.Action == "cancel") Event(65000, "move", outside);
                if (config.Action == "disable-press") Mutate(65000, "disabled", () => chip.IsEnabled = false);
                Times(80000, 160000);
                Event(161000, "up", config.Action == "cancel" ? outside : center); Sample("release"); Times(176000, 240000, 400000, 800000); break;
        }
    }

    sealed class ChipScene
    {
        readonly AvaloniaHost host;
        readonly ChipConfig config;
        readonly Marker label;
        int clicks;
        public Chip Chip { get; }
        public ChipScene(AvaloniaHost host, ChipConfig config)
        {
            this.host = host; this.config = config;
            CaseDefaults.SetThemeColors(host, config.Dark);
            if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
            Chip = config.Kind switch
            {
                "assist" => new AssistChip(), "elevated-assist" => new AssistChip { IsElevated = true },
                "suggestion" => new SuggestionChip(), "elevated-suggestion" => new SuggestionChip { IsElevated = true },
                "filter" => new FilterChip(), "elevated-filter" => new FilterChip { IsElevated = true }, _ => new InputChip()
            };
            if (Chip is SelectableChip selectable) { selectable.IsSelected = config.Selected; if (!config.Morph) selectable.Shapes = null; }
            if (config.Morph)
            {
                // Compose's shapes overload also chooses tonal colors, compact arrangement and
                // different icon motion. Configure those independent inputs explicitly here.
                if (Chip is FilterChip) Chip.Resources[Comp.Chip.PrimaryIconColor] = new TokenRef(Comp.Chip.TonalIconColor);
                Chip.Resources[Comp.Chip.IconSizeExitAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast);
                Chip.Resources[Comp.Chip.IconAlphaEnterAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault);
                Chip.Resources[Comp.Chip.IconAlphaExitAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault);
            }
            Chip.IsEnabled = config.Enabled;
            Chip.Content = label = new Marker(new Size(config.ContentWidth, config.ContentHeight));
            if (config.Width is { } width) Chip.Width = width;
            if (config.Height is { } height) Chip.Height = height;
            SetIcons(config.Icons);
            if (config.Custom == "colors") ApplyCustomColors(Chip);
            if (config.Custom == "spacing") { Chip.Spacing = new ChipSpacing(6, 6); Chip.Padding = new Thickness(5, 2, 11, 4); }
            if (config.Custom == "border") { Chip.BorderBrush = Brushes.Red; Chip.BorderThickness = new Thickness(2); }
            if (config.Custom == "elevation") Chip.Elevation = new ChipElevation(6, 6, 6, 6, 6, 6);
            Chip.Click += (_, _) => { clicks++; if (Chip is SelectableChip c && config.Action != "request-only") c.IsSelected = !c.IsSelected; };
        }
        public void SetIcons(string icons)
        {
            Chip.LeadingIcon = icons is "start" or "both" or "avatar-both" ? new Marker(new Size(18, 18)) : null;
            Chip.TrailingIcon = icons is "end" or "both" or "avatar-both" ? new Marker(new Size(18, 18)) : null;
            if (Chip is InputChip input) input.Avatar = icons.StartsWith("avatar") ? new Marker(new Size(24, 24)) : null;
            if (config.Morph && config.Custom != "spacing")
                Chip.Spacing = new ChipSpacing(
                    Chip.LeadingIcon is not null || Chip is InputChip { Avatar: not null } ? 4 : 8,
                    Chip.TrailingIcon is not null ? 4 : 8);
        }
        public ProbePoint[] Measure() =>
        [
            new("chip", "chip", null, [.. AvaloniaMeasurements.Geometry(Chip, host.Window).Where(m => m.Name is "box" or "size"),
                Measurement.Flag("selected", Chip is SelectableChip { IsSelected: true }, definition: "chip.selected"),
                Measurement.Flag("enabled", Chip.IsEffectivelyEnabled, definition: "chip.enabled"),
                Measurement.Numeric("shadowElevation", Chip.GetVisualDescendants().OfType<MaterialSurface>().First().ShadowElevation,
                    "dip", category: "motion", definition: "chip.shadow-elevation", source: "Surface.ShadowElevation"),
                Measurement.Numeric("clicks", clicks, "count", definition: "chip.click-callback-count")]),
            new("label", "content", "chip", AvaloniaMeasurements.Geometry(label, host.Window).Where(m => m.Name is "box" or "size").ToArray())
        ];
    }
    private static void ApplyCustomColors(Chip chip)
    {
        void Add(Selector? selector, string background, string foreground, string leading, string trailing) => chip.Styles.Add(new Style
        {
            Selector = selector,
            Setters =
            {
                new Setter(Chip.BackgroundProperty, new SolidColorBrush(Color.Parse(background))),
                new Setter(Chip.ForegroundProperty, new SolidColorBrush(Color.Parse(foreground))),
                new Setter(Chip.LeadingIconColorProperty, new SolidColorBrush(Color.Parse(leading))),
                new Setter(Chip.TrailingIconColorProperty, new SolidColorBrush(Color.Parse(trailing))),
            },
        });
        Add(null, "#B0336699", "#126E60", "#925210", "#643399");
        Add(new Style(s => s.Class(":selected")).Selector, "#B0882255", "#336622", "#992255", "#225599");
        Add(new Style(s => s.Class(":disabled")).Selector, "#50336699", "#70556677", "#70925210", "#70643399");
        Add(new Style(s => s.Class(":disabled").Class(":selected")).Selector, "#50882255", "#70556677", "#70925210", "#70643399");
    }
    sealed class Marker(Size size) : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        static Marker() => AffectsRender<Marker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(size.Width, availableSize.Width), Math.Min(size.Height, availableSize.Height));
        public override void Render(DrawingContext context) => context.DrawRectangle(TextElement.GetForeground(this), null, new Rect(Bounds.Size));
    }
}

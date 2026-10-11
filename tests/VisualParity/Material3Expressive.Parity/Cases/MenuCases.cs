using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record MenuConfig(string Variant = "standard", bool Selected = false, bool Enabled = true,
    bool Dark = false, bool Rtl = false, bool Icon = true, double Width = 200,
    double ContentWidth = 48, double ContentHeight = 16, string Action = "none");

[ParitySuite("menu")]
public sealed class MenuCases
{
    public static IEnumerable<CaseData<MenuConfig>> StaticConfigurations()
    {
        // Baseline has no selectable Surface; Vertical has two palette families.
        foreach (var variant in new[] { "baseline", "standard", "vibrant" })
        foreach (var selected in variant == "baseline" ? new[] { false } : new[] { false, true })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return Data($"static.{variant}.{(selected ? "selected" : "normal")}.{(enabled ? "enabled" : "disabled")}.{(dark ? "dark" : "light")}", new(variant, selected, enabled, dark));
        foreach (var variant in new[] { "baseline", "standard", "vibrant" })
        {
            yield return Data($"layout.{variant}.rtl", new(variant, Rtl: true));
            yield return Data($"layout.{variant}.no-icon", new(variant, Icon: false));
            yield return Data($"layout.{variant}.tall", new(variant, ContentHeight: 40));
            yield return Data($"layout.{variant}.minimum", new(variant, Icon: false, Width: 120, ContentWidth: 8, ContentHeight: 8));
            yield return Data($"layout.{variant}.constrained", new(variant, Width: 80));
        }
    }
    public static IEnumerable<CaseData<MenuConfig>> DynamicConfigurations()
    {
        foreach (var variant in new[] { "baseline", "standard", "vibrant" })
        foreach (var action in new[] { "hover", "press", "disabled-input" })
            yield return Data($"dynamic.{variant}.{action}", new(variant, Enabled: action != "disabled-input", Action: action));
        foreach (var variant in new[] { "standard", "vibrant" })
        foreach (var action in new[] { "selection", "reverse", "theme" })
            yield return Data($"dynamic.{variant}.{action}", new(variant, Action: action));
    }
    private static CaseData<MenuConfig> Data(string id, MenuConfig config) => new(id, id, config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer) => await Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public async Task Dynamic(CaptureWriter writer) => await Capture(writer, true);

    internal static async Task Capture(CaptureWriter writer, bool dynamic, bool independent = false)
    {
        var config = writer.Metadata.ReadConfig<MenuConfig>();
        await using var host = new AvaloniaHost(writer, 320, 160);
        SetColors(host, config.Dark);
        if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var marker = new MenuMarker(new Size(config.ContentWidth, config.ContentHeight));
        var clicks = 0;
        Control item;
        Func<bool> selected;
        Action<bool> select;
        var iconSize = config.Variant == "baseline" ? 24 : 20;
        var icon = config.Icon ? new MenuMarker(new Size(iconSize, iconSize)) : null;
        if (independent)
        {
            DropdownMenuItem dropdown = config.Variant == "baseline" ? new DropdownMenuItem()
                : new SelectableDropdownMenuItem { IsSelected = config.Selected };
            dropdown.Content = marker; dropdown.LeadingIcon = icon;
            dropdown.Click += (_, _) => clicks++;
            selected = () => dropdown is SelectableDropdownMenuItem { IsSelected: true };
            select = value => ((SelectableDropdownMenuItem)dropdown).IsSelected = value;
            item = dropdown;
        }
        else
        {
            var native = new MenuItem { Header = marker, IsChecked = config.Selected, Icon = icon,
                Theme = (ControlTheme)host.Window.FindResource(config.Variant == "baseline" ? "MaterialBaselineMenuItemTheme" : "MaterialVerticalMenuItemTheme")! };
            native.Click += (_, _) => clicks++;
            selected = () => native.IsChecked; select = value => native.IsChecked = value;
            item = native;
        }
        item.IsEnabled = config.Enabled;
        MenuAppearance.SetColorStyle(item, config.Variant == "vibrant" ? MenuColorStyle.Vibrant : MenuColorStyle.Standard);
        var allocation = new Grid { Width = config.Width, Children = { item } };
        // The Compose fixture supplies DropdownMenu's ambient content color around the
        // isolated baseline item. Supply that same real theme environment here; standalone
        // DropdownMenuItem intentionally consumes its parent's foreground for the ripple.
        if (independent && config.Variant == "baseline")
            TextElement.SetForeground(allocation, new SolidColorBrush((Color)host.Window.FindResource(Sys.Color.OnSurface)!));
        host.Mount(CaseDefaults.Center(allocation)); host.Frame(-1000); host.Frame(0);
        ProbePoint[] Measure() =>
        [
            new("content", "content", null, AvaloniaMeasurements.Geometry(marker, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
            new("item", "item", null,
            [
                Measurement.Flag("selected", selected(), definition: "menu.selected", source: independent ? "SelectableDropdownMenuItem.IsSelected" : "native MenuItem.IsChecked"),
                Measurement.Numeric("clicks", clicks, "count", definition: "menu.click-count"),
                config.Variant == "baseline" ? Measurement.Numeric("cornerRadius", 0, "dip", category: "motion", definition: "menu.item.visible-rounded-corner") :
                    Measurement.Numeric("cornerRadius", ((CornerRadius)typeof(MaterialSurface).GetProperty("EffectiveCornerRadius", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(item.GetVisualDescendants().OfType<MaterialSurface>().Single(s => s.Name == "PART_ItemSurface"))!).TopLeft,
                        "dip", category: "motion", definition: "menu.item.visible-rounded-corner", source: "MaterialSurface.EffectiveCornerRadius cached by chrome"),
            ])
        ];
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, Measure(), image));
        Sample("initial"); if (!dynamic) return;
        long nextFrame = 16000;
        void Advance(long time) { while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; } host.AdvanceTo(time); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}", time != 32000); } }
        void Change(long time, string id, Action action) { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, item.Bounds.Height / 2), host.Window)!.Value;
        var outside = new Point(8, 8);
        void Event(long time, string action, Point point) { Advance(time); host.Input(action, point); host.Draw(); host.Frame(time); }
        switch (config.Action)
        {
            case "selection": case "reverse":
                Change(1000, "selected", () => select(true)); Times(16000, 32000, 64000);
                if (config.Action == "reverse") Change(65000, "reversed", () => select(false));
                Times(80000, 160000, 400000); break;
            case "theme":
                Change(1000, "dark", () => SetColors(host, true)); Times(16000, 64000, 400000);
                Change(401000, "light", () => SetColors(host, false)); Times(416000, 480000, 800000); break;
            case "hover":
                Event(1000, "move", center); Sample("hover"); Times(16000, 80000);
                Event(81000, "move", outside); Sample("exit"); Times(96000, 240000, 400000); break;
            default:
                Event(1000, "move", center); Event(2000, "down", center); Sample("press"); Times(16000, 32000, 64000, 160000);
                Event(161000, "up", center); Sample("release"); Times(176000, 240000, 400000); break;
        }
    }
    internal static void SetColors(AvaloniaHost host, bool dark)
    {
        CaseDefaults.SetThemeColors(host, dark);
        host.Window.Resources[Sys.Color.Tertiary] = Color.Parse(dark ? "#EFB8C8" : "#7D5260");
        host.Window.Resources[Sys.Color.OnTertiary] = Color.Parse(dark ? "#492532" : "#FFFFFF");
        host.Window.Resources[Sys.Color.TertiaryContainer] = Color.Parse(dark ? "#633B48" : "#FFD8E4");
        host.Window.Resources[Sys.Color.OnTertiaryContainer] = Color.Parse(dark ? "#FFD8E4" : "#31111D");
        host.Window.Resources[Sys.Color.SurfaceContainer] = Color.Parse(dark ? "#211F26" : "#F3EDF7");
    }
    private sealed class MenuMarker(Size desiredSize) : Control
    {
        static MenuMarker() => AffectsRender<MenuMarker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(desiredSize.Width, availableSize.Width), Math.Min(desiredSize.Height, availableSize.Height));
        public override void Render(DrawingContext context)
        {
            if (TextElement.GetForeground(this) is { } brush) context.FillRectangle(brush, new Rect(Bounds.Size));
        }
    }
}


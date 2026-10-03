using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using SkiaSharp;

namespace Material3Expressive.Parity;

public sealed record IconButtonConfig(string Style = "filled", string Size = "small", string Shape = "round",
    bool Enabled = true, bool Dark = false, bool Rtl = false, double? Width = null, double? Height = null,
    double? PressedRadius = null, string Action = "none", string Custom = "none", bool Empty = false,
    string? AmbientColor = null);

[ParitySuite("icon-button")]
public sealed class IconButtonCases
{
    static readonly string[] Styles = ["standard", "filled", "tonal", "outlined"];
    static readonly string[] Sizes = ["xsmall", "small", "medium", "large", "xlarge"];

    public static IEnumerable<CaseData<IconButtonConfig>> StaticConfigurations()
    {
        // The current local API implements uniform width; narrow/wide and toggle are not exposed.
        foreach (var style in Styles)
        foreach (var size in Sizes)
        foreach (var shape in new[] { "round", "square" })
            yield return Data($"static.appearance.{style}.{size}.{shape}", new(style, size, shape));
        // Colors branch by style/state, while outlined stroke width also branches by size.
        foreach (var style in Styles)
        {
            yield return Data($"static.disabled.{style}", new(style, Enabled: false));
            yield return Data($"static.dark.{style}", new(style, Dark: true));
            yield return Data($"static.dark-disabled.{style}", new(style, Enabled: false, Dark: true));
        }
        foreach (var size in Sizes.Where(s => s != "small"))
            yield return Data($"static.disabled.outlined.{size}", new("outlined", size, Enabled: false));
        // Plain standard/outlined upstream defaults consume LocalContentColor, unlike vibrant.
        foreach (var style in new[] { "standard", "outlined" })
        foreach (var enabled in new[] { true, false })
            yield return Data($"static.ambient.{style}.{(enabled ? "enabled" : "disabled")}",
                new(style, Enabled: enabled, AmbientColor: "#208070"));
        foreach (var size in Sizes)
            yield return Data($"static.layout.empty.{size}", new(Size: size, Empty: true));
        yield return Data("static.layout.constrained-width", new(Width: 20));
        yield return Data("static.layout.constrained-height", new(Height: 20));
        yield return Data("static.layout.expanded", new(Width: 80, Height: 64));
        yield return Data("static.layout.rtl", new(Rtl: true));
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.custom.asymmetric.{(rtl ? "rtl" : "ltr")}", new(Shape: "asymmetric", Rtl: rtl));
        yield return Data("static.custom.border", new("outlined", Custom: "border"));
    }

    public static IEnumerable<CaseData<IconButtonConfig>> DynamicConfigurations()
    {
        foreach (var style in Styles)
        {
            yield return Data($"dynamic.fixed-press.{style}", new(style, Action: "press"));
            yield return Data($"dynamic.morph-press.{style}.small", new(style, PressedRadius: 8, Action: "press"));
            foreach (var action in new[] { "hover", "focus", "disable", "theme", "disabled" })
                yield return Data($"dynamic.{action}.{style}", new(style, Enabled: action != "disabled", Action: action));
        }
        // Shape clamping and content dimensions have independent size branches.
        foreach (var size in Sizes.Where(s => s != "small"))
            yield return Data($"dynamic.morph-press.filled.{size}", new(Size: size, PressedRadius: 8, Action: "press"));
        foreach (var action in new[] { "quick", "cancel", "drag-back", "reverse", "clear-shape", "change-shape", "size" })
            yield return Data($"dynamic.{action}", new(PressedRadius: 8, Action: action));
        foreach (var key in new[] { "space", "enter" })
        {
            yield return Data($"dynamic.keyboard.{key}", new(Action: key));
            yield return Data($"dynamic.disabled-keyboard.{key}", new(Enabled: false, Action: key));
        }
    }

    static CaseData<IconButtonConfig> Data(string id, IconButtonConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<IconButtonConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        var scene = new IconScene(host, config);
        host.Mount(CaseDefaults.Center(scene.Button));
        scene.ConfigureVector();
        host.Draw(); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<IconButtonConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        var scene = new IconScene(host, config);
        host.Mount(CaseDefaults.Center(scene.Button)); scene.ConfigureVector();
        host.Draw(); host.Frame(-1000); host.Frame(0);
        Sample("initial");
        var center = new Point(160, 120); var outside = new Point(8, 8);
        long nextFrameUs = 16000;
        void AdvanceTo(long time)
        {
            while (nextFrameUs <= time) { host.Frame(nextFrameUs); nextFrameUs += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        void SamplesAt(params long[] times)
        {
            foreach (var time in times) { AdvanceTo(time); host.Draw(); Sample("time-" + time, time != 32000); }
        }
        void Event(long time, string action, Point position) { AdvanceTo(time); host.Input(action, position); host.Draw(); }
        void Focus(long time, bool focused)
        {
            AdvanceTo(time);
            if (focused) scene.Button.Focus(NavigationMethod.Tab); else host.Window.FocusManager?.Focus(null);
            writer.Record(time, "input", focused ? "IconButton.Focus(Tab)" : "FocusManager.Focus(null)"); host.Draw();
        }
        void Key(long time, bool down)
        {
            AdvanceTo(time);
            var value = config.Action == "space" ? Avalonia.Input.Key.Space : Avalonia.Input.Key.Enter;
            var physical = config.Action == "space" ? PhysicalKey.Space : PhysicalKey.Enter;
            if (down) host.Window.KeyPress(value, RawInputModifiers.None, physical, value == Avalonia.Input.Key.Space ? " " : null);
            else host.Window.KeyRelease(value, RawInputModifiers.None, physical, value == Avalonia.Input.Key.Space ? " " : null);
            writer.Record(time, "input", $"key {config.Action} {(down ? "down" : "up")}"); host.Draw();
        }
        switch (config.Action)
        {
            case "hover":
                Event(1000, "move", center); Sample("hover-event"); SamplesAt(16000, 75000, 180000);
                Event(181000, "move", outside); Sample("exit-event"); SamplesAt(196000, 400000); break;
            case "focus":
                Focus(1000, true); Sample("focus-event"); SamplesAt(16000, 45000, 180000);
                Focus(181000, false); Sample("blur-event"); SamplesAt(196000, 400000); break;
            case "space": case "enter":
                Focus(1000, true); SamplesAt(16000, 121000);
                Key(122000, true); Sample("key-down"); SamplesAt(138000, 197000, 347000);
                Key(348000, false); Sample("key-up"); SamplesAt(364000, 498000, 700000); break;
            case "theme": case "size":
                AdvanceTo(1000);
                if (config.Action == "theme") CaseDefaults.SetThemeColors(host, true); else scene.SetSize("large");
                writer.Record(1000, "mutation", config.Action); host.Draw(); Sample("mutation"); SamplesAt(16000, 400000);
                AdvanceTo(401000);
                if (config.Action == "theme") CaseDefaults.SetThemeColors(host, config.Dark); else scene.SetSize(config.Size);
                writer.Record(401000, "mutation", config.Action + " restored"); host.Draw(); Sample("restored"); SamplesAt(416000, 800000); break;
            default:
                Event(1000, "move", center); Sample("pointer-enter");
                Event(2000, "down", center); Sample("down-event");
                if (config.Action is "quick" or "disabled")
                {
                    Event(5000, "up", center); Sample("up-before-first-frame"); SamplesAt(16000, 75000, 225000, 600000); break;
                }
                if (config.Action == "reverse")
                {
                    SamplesAt(16000, 32000); Event(33000, "up", center); Sample("reverse-event");
                    SamplesAt(48000); Event(49000, "down", center); Sample("repress-event"); SamplesAt(64000, 96000);
                    Event(97000, "up", center); Sample("final-up"); SamplesAt(113000, 225000, 600000); break;
                }
                SamplesAt(16000, 32000, 75000);
                if (config.Action == "cancel")
                {
                    Event(76000, "move", outside); Sample("drag-out"); Event(77000, "up", outside); Sample("outside-release"); SamplesAt(92000, 225000, 600000); break;
                }
                if (config.Action == "drag-back")
                {
                    Event(76000, "move", outside); Sample("drag-out"); SamplesAt(92000, 150000);
                    Event(151000, "move", center); Sample("drag-back"); SamplesAt(167000, 225000);
                    Event(226000, "up", center); Sample("inside-release"); SamplesAt(242000, 600000); break;
                }
                if (config.Action == "disable")
                {
                    AdvanceTo(76000); scene.Button.IsEnabled = false; writer.Record(76000, "mutation", "enabled=false"); host.Draw(); Sample("disabled-during-press"); SamplesAt(80000, 96000, 225000, 400000);
                    Event(401000, "up", center); Sample("disabled-release"); AdvanceTo(402000); scene.Button.IsEnabled = true;
                    writer.Record(402000, "mutation", "enabled=true"); host.Draw(); Sample("reenabled"); SamplesAt(418000, 600000); break;
                }
                if (config.Action is "clear-shape" or "change-shape")
                {
                    AdvanceTo(76000); MaterialButton.SetPressedCornerRadius(scene.Button, config.Action == "clear-shape" ? null : new CornerRadius(14));
                    writer.Record(76000, "mutation", config.Action); host.Draw(); Sample("shape-mutation");
                }
                SamplesAt(120000, 225000, 400000); Event(401000, "up", center); Sample("release-event"); SamplesAt(416000, 551000, 700000); break;
        }
    }

    sealed class IconScene
    {
        readonly AvaloniaHost host;
        readonly IconButtonConfig config;
        int clicks;
        public IconButton Button { get; }
        public MaterialSurface Surface => Button.GetVisualDescendants().OfType<MaterialSurface>().Single();
        VectorSymbol? vector;
        public IconScene(AvaloniaHost host, IconButtonConfig config)
        {
            this.host = host; this.config = config;
            CaseDefaults.SetThemeColors(host, config.Dark);
            if (config.AmbientColor is { } ambient) TextElement.SetForeground(host.Window, new SolidColorBrush(Color.Parse(ambient)));
            Button = new IconButton { IsEnabled = config.Enabled, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
                Symbol = config.Empty ? MaterialSymbols.MaterialSymbolCodepoint.None : MaterialSymbols.MaterialSymbolCodepoint.Home };
            Button.Theme = (ControlTheme)host.Window.FindResource(config.Style switch
            {
                "standard" => "MaterialStandardIconButtonTheme", "tonal" => "MaterialTonalIconButtonTheme",
                "outlined" => "MaterialOutlinedIconButtonTheme", _ => "MaterialFilledIconButtonTheme"
            })!;
            if (config.Width is { } width) Button.Width = width;
            if (config.Height is { } height) Button.Height = height;
            MaterialButton.SetPressedCornerRadius(Button, config.PressedRadius is { } radius ? new CornerRadius(radius) : null);
            if (config.Custom == "border") { Button.BorderThickness = new Thickness(3); Button.BorderBrush = Brushes.Red; }
            Button.Click += (_, _) => clicks++;
        }

        public void ConfigureVector()
        {
            // The product only exposes a Symbol slot. Replace its font painter with a test
            // MaterialSymbol subclass, keeping the product template, base measurement, style key,
            // actual small-size template values and foreground inheritance. Size-class selectors
            // still target this part; no expected icon dimensions or positional corrections here.
            var symbol = Button.GetVisualDescendants().OfType<MaterialSymbol>().Single();
            vector = new VectorSymbol { HorizontalAlignment = symbol.HorizontalAlignment, VerticalAlignment = symbol.VerticalAlignment,
                Symbol = symbol.Symbol };
            // Retain the original template association so /template/ selectors keep applying
            // to the replacement painter. Avalonia 12.1.2 exposes an internal setter here.
            typeof(StyledElement).GetProperty(nameof(StyledElement.TemplatedParent))!.SetValue(vector, symbol.TemplatedParent);
            vector.SetValue(Control.WidthProperty, symbol.Width, BindingPriority.Template);
            vector.SetValue(Control.HeightProperty, symbol.Height, BindingPriority.Template);
            Surface.Child = vector;
            SetSize(config.Size);
        }

        public void SetSize(string size)
        {
            foreach (var value in Sizes) Button.Classes.Remove(value);
            if (size != "small") Button.Classes.Add(size);
            if (config.Shape != "round") Button.CornerRadius = config.Shape == "asymmetric"
                ? new CornerRadius(4, 16, 10, 2)
                : new CornerRadius(size switch { "xsmall" or "small" => 12, "medium" => 16, _ => 28 });
        }

        public ProbePoint[] Measure()
        {
            var surface = Surface;
            var fields = Geometry(surface).ToList();
            var chrome = (Border)typeof(MaterialSurface).GetField("_chrome", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(surface)!;
            var radius = chrome.CornerRadius;
            using var round = new SKRoundRect();
            round.SetRectRadii(new SKRect(0, 0, (float)surface.Bounds.Width, (float)surface.Bounds.Height),
                new[] { radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft }.Select(r => new SKPoint((float)r, (float)r)).ToArray());
            var radii = new[] { SKRoundRectCorner.UpperLeft, SKRoundRectCorner.UpperRight, SKRoundRectCorner.LowerRight, SKRoundRectCorner.LowerLeft }
                .Select(round.GetRadii).SelectMany(r => new[] { (double)r.X, r.Y }).ToArray();
            var origin = surface.TranslatePoint(default, host.Window)!.Value;
            if (surface.TranslatePoint(new Point(surface.Bounds.Width, 0), host.Window)!.Value.X < origin.X)
                radii = [radii[2], radii[3], radii[0], radii[1], radii[6], radii[7], radii[4], radii[5]];
            fields.Add(Measurement.Vector("cornerRadii", "quad", radii, "dip", "container", "motion", "icon-button.container.normalized-visual-rounded-outline",
                "derived: cached MaterialSurface chrome Border.CornerRadius; SKRoundRect normalization; actual x-axis reflection", CornerComponents));
            var buttonFields = Geometry(Button).ToList();
            if (config.Action != "none")
            {
                buttonFields.Add(Measurement.Flag("enabled", Button.IsEffectivelyEnabled, definition: "icon-button.enabled", source: "IconButton.IsEffectivelyEnabled"));
                buttonFields.Add(Measurement.Flag("pressed", Interaction("Pressed"), definition: "icon-button.pressed", source: "Material interaction controller active Pressed flag"));
                buttonFields.Add(Measurement.Flag("hovered", Interaction("Hovered"), definition: "icon-button.hovered", source: "Material interaction controller active Hovered flag"));
                buttonFields.Add(Measurement.Flag("focused", Button.IsFocused, definition: "icon-button.focused", source: "IconButton.IsFocused"));
                buttonFields.Add(Measurement.Numeric("clickCount", clicks, "count", definition: "icon-button.on-click-count", source: "real IconButton.Click callback"));
            }
            var points = new List<ProbePoint> { new("button", "button", null, buttonFields.ToArray()), new("container", "container", "button", fields.ToArray()) };
            if (vector is not null && !config.Empty)
            {
                var contentFields = Geometry(vector).ToList();
                contentFields.Add(vector.Paint is ISolidColorBrush brush
                    ? Measurement.Vector("color", "color", [brush.Color.R, brush.Color.G, brush.Color.B, Math.Round(brush.Color.A * brush.Opacity, MidpointRounding.AwayFromZero)],
                        "srgba8", "none", "properties", "icon-button.icon.painted-solid-srgba8", "derived: VectorSymbol.Render inherited MaterialSymbol.Foreground; effective alpha rounded to nearest byte")
                    : Measurement.Unavailable("color", "properties", "color", "srgba8", "none", "Vector painter has not drawn a solid brush.", "icon-button.icon.painted-solid-srgba8"));
                points.Add(new("icon", "icon", "container", contentFields.ToArray()));
            }
            return points.ToArray();
            Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
            bool Interaction(string name)
            {
                var controller = typeof(MaterialSurface).GetField("_interactionController", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(surface)!;
                var value = (Enum)controller.GetType().GetProperty("Interaction", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(controller)!;
                return value.HasFlag((Enum)Enum.Parse(value.GetType(), name));
            }
        }
    }

    sealed class VectorSymbol : MaterialSymbol
    {
        protected override Type StyleKeyOverride => typeof(MaterialSymbol);
        protected override bool BypassFlowDirectionPolicies => true;
        public IBrush? Paint { get; private set; }
        static readonly Geometry Arrow = Geometry.Parse("M 2,3 L 11,3 L 11,1 L 18,8 L 11,15 L 11,12 L 2,12 Z");
        public override void Render(DrawingContext context)
        {
            if (Symbol == MaterialSymbols.MaterialSymbolCodepoint.None) return;
            Paint = Foreground;
            using (context.PushTransform(Matrix.CreateScale(Bounds.Width / 20, Bounds.Height / 20)))
                context.DrawGeometry(Paint, null, Arrow);
        }
    }
    static readonly string[] CornerComponents = ["topLeft.x", "topLeft.y", "topRight.x", "topRight.y", "bottomRight.x", "bottomRight.y", "bottomLeft.x", "bottomLeft.y"];
}

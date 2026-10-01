using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using SkiaSharp;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Parity;

public sealed record ButtonConfig(string Style = "filled", string Size = "small", string Shape = "round",
    bool Enabled = true, bool Dark = false, string Icon = "none", bool Rtl = false, string Label = "Save",
    double? Width = null, string Custom = "none", bool Baseline = false, string Action = "none");

[ParitySuite("button")]
public sealed class ButtonCases
{
    static readonly string[] Styles = ["filled", "elevated", "tonal", "outlined", "text"];
    static readonly string[] Sizes = ["xsmall", "small", "medium", "large", "xlarge"];

    public static IEnumerable<CaseData<ButtonConfig>> StaticConfigurations()
    {
        // Button.kt: five wrappers share size defaults; round and rounded-square are valid shapes.
        // Local square is expressed through its public shape token, not an invented square class.
        foreach (var style in Styles)
        foreach (var size in Sizes)
        foreach (var shape in new[] { "round", "square" })
            yield return Data($"static.{style}.{size}.{shape}", new(style, size, shape));

        // Disabled colors depend on style, not typography/height. Outlined width has size branches.
        foreach (var style in Styles)
        {
            yield return Data($"disabled.{style}.small", new(style, Enabled: false));
            yield return Data($"dark.{style}.medium", new(style, "medium", Dark: true));
            yield return Data($"dark-disabled.{style}.small", new(style, Enabled: false, Dark: true));
            yield return Data($"baseline.{style}", new(style, Baseline: true));
        }
        foreach (var size in Sizes.Where(s => s != "small"))
            yield return Data($"disabled.outlined.{size}", new("outlined", size, Enabled: false));

        // Icon size, padding and spacing branch by height; leading/trailing also depend on direction.
        foreach (var size in Sizes)
        {
            foreach (var icon in new[] { "start", "end", "both" })
                yield return Data($"icon.filled.{size}.{icon}", new(Size: size, Icon: icon));
            yield return Data($"rtl.filled.{size}.start", new(Size: size, Icon: "start", Rtl: true));
            yield return Data($"wrap.filled.{size}", new(Size: size, Label: "Save all pending changes", Width: 150));
        }
        // Other styles share content layout, but distinct foreground must reach arbitrary icon content.
        foreach (var style in Styles.Where(s => s != "filled"))
            yield return Data($"icon.{style}.small.start", new(style, Icon: "start"));
        yield return Data("edge.empty", new(Label: ""));
        yield return Data("edge.short", new(Label: "I"));
        yield return Data("edge.spaces", new(Label: "Save  "));
        yield return Data("edge.multiline", new(Label: "Save\nchanges"));
        yield return Data("edge.rtl-asymmetric", new(Shape: "asymmetric", Rtl: true));
        foreach (var custom in new[] { "colors", "padding", "border", "elevation", "no-elevation", "typography" })
            yield return Data($"custom.{custom}", new(Custom: custom));
        yield return Data("custom.colors-disabled", new(Enabled: false, Custom: "colors"));
    }

    public static IEnumerable<CaseData<ButtonConfig>> DynamicConfigurations()
    {
        // Press shape differs at every size. Wrappers share the shape algorithm, colors do not.
        foreach (var size in Sizes)
            yield return Data($"press.filled.{size}", new(Size: size, Action: "press"));
        foreach (var style in Styles.Where(s => s != "filled"))
            yield return Data($"press.{style}.small", new(style, Action: "press"));
        // A fixed rounded-square start and asymmetric logical corners exercise morphing without
        // pill normalization and the changing horizontal optical offset in AnimatedShape.kt.
        yield return Data("press.filled.small.square", new(Shape: "square", Action: "press"));
        yield return Data("press.filled.small.rtl-asymmetric", new(Shape: "asymmetric", Rtl: true, Action: "press"));
        foreach (var style in Styles)
        {
            yield return Data($"hover.{style}", new(style, Action: "hover"));
            yield return Data($"focus.{style}", new(style, Action: "focus"));
            yield return Data($"disabled-input.{style}", new(style, Enabled: false, Action: "disabled"));
        }
        foreach (var action in new[] { "quick", "cancel", "reverse", "disable", "space", "enter", "theme", "size", "content", "shape-change" })
            yield return Data($"behavior.{action}", new(Action: action));
        foreach (var action in new[] { "disable", "space", "focus-hover", "hover-focus" })
            yield return Data($"behavior.elevated.{action}", new("elevated", Action: action));
        yield return Data("behavior.custom-elevation", new(Custom: "elevation", Action: "hover"));
        // Default elevations collapse several states to the same value. Distinct custom values
        // make pressed/focused/disabled target selection and their transitions observable.
        foreach (var action in new[] { "press", "focus", "disable" })
            yield return Data($"behavior.custom-elevation.{action}", new(Custom: "elevation", Action: action));
        yield return Data("behavior.no-elevation-hover", new(Custom: "no-elevation", Action: "hover"));
        yield return Data("behavior.baseline-press", new(Baseline: true, Action: "press"));
        yield return Data("behavior.disabled-space", new(Enabled: false, Action: "space"));
    }

    static CaseData<ButtonConfig> Data(string id, ButtonConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        var scene = new ButtonScene(host, config);
        host.Mount(CaseDefaults.Center(scene.Button));
        scene.ConfigureContent();
        host.Draw();
        host.Frame(-1000);
        host.Frame(0);
        // Initial colors/elevation snap to target; no input and no autonomous Button animation.
        writer.Submit(host.Snapshot("initial", "initial", scene.Measure()));
    }

    [ParityScenario("behavior", Data = nameof(DynamicConfigurations))]
    public async Task Behavior(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        var scene = new ButtonScene(host, config);
        host.Mount(CaseDefaults.Center(scene.Button));
        scene.ConfigureContent();
        host.Draw();
        host.Frame(-1000);
        host.Frame(0);
        Sample("initial");
        var center = new Point(160, 120);
        var outside = new Point(8, 8);

        void Sample(string id, bool image = true) => writer.Submit(host.Snapshot(id, id, scene.Measure(), image));
        void Event(long time, string action, Point position)
        {
            host.AdvanceTo(time); host.Input(action, position); host.Draw();
        }
        void Frames(params long[] times)
        {
            foreach (var time in times) { host.Frame(time); Sample("frame-" + time, time is not (32000 or 450000 or 92000)); }
        }
        void Focus(long time, bool focused)
        {
            host.AdvanceTo(time);
            if (focused) scene.Button.Focus(NavigationMethod.Tab);
            else host.Window.FocusManager?.Focus(null);
            writer.Record(time, "input", focused ? "Button.Focus(Tab)" : "FocusManager.Focus(null)");
            host.Draw();
        }
        void Key(long time, string key, bool down)
        {
            host.AdvanceTo(time);
            var value = key == "space" ? Avalonia.Input.Key.Space : Avalonia.Input.Key.Enter;
            var physical = key == "space" ? PhysicalKey.Space : PhysicalKey.Enter;
            if (down) host.Window.KeyPress(value, RawInputModifiers.None, physical, key == "space" ? " " : null);
            else host.Window.KeyRelease(value, RawInputModifiers.None, physical, key == "space" ? " " : null);
            writer.Record(time, "input", $"key {key} {(down ? "down" : "up")}");
            host.Draw();
        }

        switch (config.Action)
        {
            case "hover":
                Event(1000, "move", center); Sample("hover-event");
                Frames(16000, 75000, 121000, 180000);
                Event(181000, "move", outside); Sample("exit-event");
                Frames(196000, 301000, 400000); break;
            case "focus":
                Focus(1000, true); Sample("focus-event"); Frames(16000, 45000, 121000, 180000);
                Focus(181000, false); Sample("blur-event"); Frames(196000, 331000, 400000); break;
            case "focus-hover":
            case "hover-focus":
                if (config.Action == "focus-hover") Focus(1000, true); else Event(1000, "move", center);
                Frames(16000, 121000);
                if (config.Action == "focus-hover") Event(122000, "move", center); else Focus(122000, true);
                Sample("second-interaction"); Frames(138000, 242000);
                if (config.Action == "focus-hover") Event(243000, "move", outside); else Focus(243000, false);
                Sample("remove-last"); Frames(259000, 400000); break;
            case "space":
            case "enter":
                Focus(1000, true); Frames(16000, 121000);
                Key(122000, config.Action, true); Sample("key-down"); Frames(138000, 197000, 347000);
                Key(348000, config.Action, false); Sample("key-up"); Frames(364000, 498000, 700000); break;
            case "theme":
            case "size":
            case "content":
                host.AdvanceTo(1000);
                if (config.Action == "theme") scene.SetTheme(true);
                if (config.Action == "size") { scene.Button.Classes.Add("large"); scene.ConfigureContent("large"); }
                if (config.Action == "content") scene.Button.Content = "Save all changes";
                writer.Record(1000, "mutation", config.Action);
                host.Draw(); Sample("mutation"); Frames(16000, 400000); break;
            default:
                // A mouse reaches the target before pressing it. Explicit Move avoids comparing
                // the adapters' different implicit hover updates on a teleported Press event.
                Event(1000, "move", center); Sample("pointer-enter");
                Event(2000, "down", center); Sample("down-event");
                if (config.Action is "quick" or "disabled")
                {
                    Event(5000, "up", center); Sample("up-before-first-frame"); Frames(16000, 75000, 225000, 375000, 600000); break;
                }
                if (config.Action == "reverse")
                {
                    Frames(16000, 32000); Event(33000, "up", center); Sample("reverse-event");
                    Frames(48000); Event(49000, "down", center); Sample("repress-event");
                    Frames(64000, 96000); Event(97000, "up", center); Sample("final-up");
                    Frames(113000, 225000, 375000, 600000); break;
                }
                Frames(16000, 32000, 75000);
                if (config.Action == "cancel")
                {
                    Event(76000, "move", outside); Sample("drag-out");
                    Event(77000, "up", outside); Sample("outside-release"); Frames(92000, 225000, 375000, 600000); break;
                }
                if (config.Action == "disable")
                {
                    host.AdvanceTo(76000); scene.Button.IsEnabled = false; writer.Record(76000, "mutation", "enabled=false");
                    host.Draw(); Sample("disabled-during-press"); Frames(92000, 225000, 400000);
                    Event(401000, "up", center);
                    host.AdvanceTo(402000); scene.Button.IsEnabled = true; writer.Record(402000, "mutation", "enabled=true");
                    host.Draw(); Sample("reenabled"); Frames(418000, 600000); break;
                }
                if (config.Action == "shape-change")
                {
                    host.AdvanceTo(76000); scene.Surface.PressedCornerRadius = new CornerRadius(14);
                    writer.Record(76000, "mutation", "pressedShape=14dip"); host.Draw(); Sample("third-shape");
                }
                Frames(120000, 225000, 400000);
                Event(401000, "up", center); Sample("release-event"); Frames(416000, 450000, 551000, 700000); break;
        }
    }

    sealed class ButtonScene
    {
        readonly AvaloniaHost host;
        readonly ButtonConfig config;
        int clicks;
        public Button Button { get; }
        public MaterialSurface Surface => Button.GetVisualDescendants().OfType<MaterialSurface>().Single();

        public ButtonScene(AvaloniaHost host, ButtonConfig config)
        {
            this.host = host; this.config = config;
            SetTheme(config.Dark);
            Button = new Button { Content = config.Label, IsEnabled = config.Enabled, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            Button.Theme = (ControlTheme)host.Window.FindResource(config.Style switch
            {
                "filled" => "MaterialFilledButtonTheme", "elevated" => "MaterialElevatedButtonTheme",
                "tonal" => "MaterialTonalButtonTheme", "outlined" => "MaterialOutlinedButtonTheme", _ => "MaterialTextButtonTheme"
            })!;
            if (config.Size != "small") Button.Classes.Add(config.Size);
            if (config.Width is { } width) Button.Width = width;
            if (config.Shape != "round") host.Window.Resources[Sys.Shape.Full] = config.Shape == "asymmetric"
                // Compose corners are start/end; Avalonia CornerRadius uses physical corners.
                ? config.Rtl ? new CornerRadius(16, 4, 2, 10) : new CornerRadius(4, 16, 10, 2)
                : new CornerRadius(config.Size switch { "xsmall" or "small" => 12, "medium" => 16, _ => 28 });
            if (config.Custom == "padding") { Button.Padding = new Thickness(5, 3, 27, 9); Button.Width = 220; }
            if (config.Custom == "border") { Button.BorderThickness = new Thickness(3); Button.BorderBrush = Brushes.Red; }
            if (config.Custom == "typography") { Button.FontSize = 19; Button.FontWeight = FontWeight.Bold; }
            if (config.Custom == "colors") { Button.Background = new SolidColorBrush(Color.Parse(config.Enabled ? "#208070" : "#90A0B0")); Button.Foreground = new SolidColorBrush(Color.Parse(config.Enabled ? "#FFEEDD" : "#203040")); }
            if (config.Custom == "elevation") MaterialButton.SetElevation(Button, new() { Default = 8, Hovered = 12, Focused = 10, Pressed = 4, Disabled = 0 });
            if (config.Custom == "no-elevation") MaterialButton.SetElevation(Button, null);
            Button.Click += (_, _) => clicks++;
        }

        public void SetTheme(bool dark)
        {
            host.Window.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
            // Same fixed Compose ColorScheme inputs on both ends. This isolates Button from MCU.
            host.Window.Resources[Sys.Color.Primary] = Color.Parse(dark ? "#D0BCFF" : "#6750A4");
            host.Window.Resources[Sys.Color.OnPrimary] = Color.Parse(dark ? "#381E72" : "#FFFFFF");
            host.Window.Resources[Sys.Color.SecondaryContainer] = Color.Parse(dark ? "#4A4458" : "#E8DEF8");
            host.Window.Resources[Sys.Color.OnSecondaryContainer] = Color.Parse(dark ? "#E8DEF8" : "#1D192B");
            host.Window.Resources[Sys.Color.OnSurface] = Color.Parse(dark ? "#E6E0E9" : "#1D1B20");
            host.Window.Resources[Sys.Color.OnSurfaceVariant] = Color.Parse(dark ? "#CAC4D0" : "#49454F");
            host.Window.Resources[Sys.Color.OutlineVariant] = Color.Parse(dark ? "#49454F" : "#CAC4D0");
            host.Window.Resources[Sys.Color.SurfaceContainerLow] = Color.Parse(dark ? "#1D1B20" : "#F7F2FA");
        }

        public void ConfigureContent(string? size = null)
        {
            if (config.Icon == "none") return;
            size ??= config.Size;
            var iconKey = size switch { "xsmall" => Comp.ButtonXSmall.IconSize, "medium" => Comp.ButtonMedium.IconSize, "large" => Comp.ButtonLarge.IconSize, "xlarge" => Comp.ButtonXLarge.IconSize, _ => Comp.ButtonSmall.IconSize };
            var spaceKey = size switch { "xsmall" => Comp.ButtonXSmall.IconLabelSpace, "medium" => Comp.ButtonMedium.IconLabelSpace, "large" => Comp.ButtonLarge.IconLabelSpace, "xlarge" => Comp.ButtonXLarge.IconLabelSpace, _ => Comp.ButtonSmall.IconLabelSpace };
            if (!Button.TryFindResource(iconKey, out var iconValue) || iconValue is not double iconSize
                || !Button.TryFindResource(spaceKey, out var spaceValue) || spaceValue is not double spacing)
                throw new InvalidOperationException("Button icon token unavailable.");
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing, VerticalAlignment = VerticalAlignment.Center };
            ButtonIcon Icon(string name)
            {
                return new ButtonIcon(Button) { Name = name, Width = iconSize, Height = iconSize };
            }
            if (config.Icon is "start" or "both") panel.Children.Add(Icon("start-icon"));
            panel.Children.Add(new TextBlock { Text = config.Label, VerticalAlignment = VerticalAlignment.Center });
            if (config.Icon is "end" or "both") panel.Children.Add(Icon("end-icon"));
            Button.Content = panel;
        }

        public ProbePoint[] Measure()
        {
            var surface = Surface;
            var fields = AvaloniaMeasurements.Geometry(surface, host.Window).ToList();
            fields.Add(Measurement.Numeric("shadowElevation", surface.ShadowElevation, category: "motion", definition: "button.shadow-elevation", source: "MaterialSurface.ShadowElevation consumed by DrawShadow"));
            var chrome = (Border)typeof(MaterialSurface).GetField("_chrome", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)!;
            var radius = chrome.CornerRadius;
            // Native Skia normalization used by the renderer; raw cached corners remain searchable.
            using var round = new SKRoundRect();
            round.SetRectRadii(new SKRect(0, 0, (float)surface.Bounds.Width, (float)surface.Bounds.Height),
                new[] { radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft }.Select(r => new SKPoint((float)r, (float)r)).ToArray());
            var normalized = new[] { SKRoundRectCorner.UpperLeft, SKRoundRectCorner.UpperRight, SKRoundRectCorner.LowerRight, SKRoundRectCorner.LowerLeft }.Select(round.GetRadii).SelectMany(r => new[] { (double)r.X, r.Y }).ToArray();
            fields.Add(Measurement.Vector("cornerRadii", "quad", normalized, "dip", "container", "motion", "button.container.normalized-rounded-outline", "derived: Border.CornerRadius cached at chrome; SKRoundRect.SetRectRadii native normalization", CornerComponents));
            fields.Add(Measurement.Vector("nativeCornerRadii", "quad", new[] { radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft }.SelectMany(r => new[] { r, r }).ToArray(), "dip", "container", "motion", "avalonia.button.border.cached-corner-radius", "MaterialSurface._chrome.CornerRadius", CornerComponents));
            var points = new List<ProbePoint>
            {
                new("button", "button", null, AvaloniaMeasurements.Geometry(Button, host.Window).Concat(new[] {
                    Measurement.Flag("enabled", Button.IsEffectivelyEnabled, definition: "button.enabled", source: "Button.IsEffectivelyEnabled"),
                    Measurement.Flag("pressed", Interaction("Pressed"), definition: "button.pressed", source: "MaterialSurface._interactionController.Interaction active Pressed feedback flag"),
                    Measurement.Flag("hovered", Interaction("Hovered"), definition: "button.hovered", source: "MaterialSurface._interactionController.Interaction active Hovered feedback flag"),
                    Measurement.Flag("focused", Button.IsFocused, definition: "button.focused", source: "Button.IsFocused"),
                    Measurement.Numeric("clickCount", clicks, "count", definition: "button.on-click-count", source: "Button.Click handler") }).ToArray()),
                new("container", "container", "button", fields.ToArray())
            };
            var label = Button.GetVisualDescendants().OfType<TextBlock>().Single();
            var text = AvaloniaMeasurements.Text("label", "container", label, host.Window);
            points.Add(text with { Measurements = text.Measurements.Where(ComparableText).ToArray() });
            foreach (var icon in Button.GetVisualDescendants().OfType<ButtonIcon>())
                points.Add(new(icon.Name!, "icon", "container", AvaloniaMeasurements.Geometry(icon, host.Window)));
            return points.ToArray();

            bool Interaction(string name)
            {
                var controller = typeof(MaterialSurface).GetField("_interactionController", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)
                    ?? throw new InvalidOperationException("Button has no Material interaction controller.");
                var current = (Enum)controller.GetType().GetProperty("Interaction", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller)!;
                return current.HasFlag((Enum)Enum.Parse(current.GetType(), name));
            }
        }
    }

    // Arbitrary vector content with a fixed 20x20 viewport, matching the Compose ImageVector.
    // This avoids accidentally testing an icon font or Path.Stretch's bounds normalization.
    sealed class ButtonIcon(Button button) : Control
    {
        static readonly Geometry Glyph = Geometry.Parse("M 8,2 L 12,2 L 12,8 L 18,8 L 18,12 L 12,12 L 12,18 L 8,18 L 8,12 L 2,12 L 2,8 L 8,8 Z");
        public override void Render(DrawingContext context)
        {
            using (context.PushTransform(Matrix.CreateScale(Bounds.Width / 20, Bounds.Height / 20)))
                context.DrawGeometry(button.Foreground, null, Glyph);
        }
    }

    static bool ComparableText(Measurement m) => m.Name is not ("resolvedFonts" or "drawOrigin" or "glyphInk")
        && !m.Name.EndsWith(".width");
    static readonly string[] CornerComponents = ["topLeft.x", "topLeft.y", "topRight.x", "topRight.y", "bottomRight.x", "bottomRight.y", "bottomLeft.x", "bottomLeft.y"];
}

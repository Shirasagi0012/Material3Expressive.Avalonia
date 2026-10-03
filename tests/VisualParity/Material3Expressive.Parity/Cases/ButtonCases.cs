using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
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
    bool Enabled = true, bool Dark = false, string Icon = "none", bool Rtl = false,
    double? Width = 240, string Custom = "none", bool Baseline = false, string Action = "none",
    double? Height = null, bool Empty = false, double ContentWidth = 32, double ContentHeight = 16,
    bool RequiredContentSize = false);

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
            yield return Data($"static.appearance.{style}.{size}.{shape}", new(style, size, shape));

        // Disabled colors depend on style, not typography/height. Outlined width has size branches.
        foreach (var style in Styles)
        {
            yield return Data($"static.disabled.{style}", new(style, Enabled: false));
            yield return Data($"static.dark.{style}", new(style, Dark: true));
            yield return Data($"static.dark-disabled.{style}", new(style, Enabled: false, Dark: true));
            yield return Data($"static.baseline.{style}", new(style, Baseline: true));
        }
        foreach (var size in Sizes.Where(s => s != "small"))
            yield return Data($"static.disabled.outlined.{size}", new("outlined", size, Enabled: false));

        // Icon size, padding and spacing branch by height; leading/trailing also depend on direction.
        foreach (var size in Sizes)
        {
            foreach (var icon in new[] { "start", "end", "both" })
                yield return Data($"static.layout.icons.{size}.{icon}", new(Size: size, Icon: icon, Width: null));
            yield return Data($"static.layout.intrinsic.{size}", new(Size: size, Width: null));
            yield return Data($"static.layout.empty.{size}", new(Size: size, Empty: true, Width: null));
        }
        // Other styles share content layout, but distinct foreground must reach arbitrary icon content.
        foreach (var style in Styles.Where(s => s != "filled"))
            yield return Data($"static.layout.style-icon.{style}", new(style, Icon: "start", Width: null));
        foreach (var icon in new[] { "start", "end", "both" })
            yield return Data($"static.layout.rtl-icons.{icon}", new(Icon: icon, Rtl: true, Width: null));
        yield return Data("static.layout.tall-content", new(Width: null, ContentHeight: 80));
        yield return Data("static.layout.constrained-width", new(Width: 30));
        yield return Data("static.layout.constrained-height", new(Height: 35));
        // Required dimensions may overflow; preferred dimensions obey the incoming constraints.
        yield return Data("static.layout.required-constrained-width", new(Width: 30, RequiredContentSize: true));
        yield return Data("static.layout.required-constrained-height", new(Height: 35, RequiredContentSize: true));
        // Preserve the previous non-text empty-container regressions under real empty content.
        yield return Data("static.layout.empty-constrained-width", new(Empty: true, Width: 30));
        yield return Data("static.layout.empty-constrained-height", new(Empty: true, Height: 35));
        foreach (var rtl in new[] { false, true })
        {
            var direction = rtl ? "rtl" : "ltr";
            yield return Data($"static.layout.asymmetric-shape.{direction}", new(Shape: "asymmetric", Rtl: rtl));
            yield return Data($"static.custom.padding.{direction}", new(Custom: "padding", Rtl: rtl));
        }
        foreach (var custom in new[] { "border", "elevation", "no-elevation" })
            yield return Data($"static.custom.{custom}", new(Custom: custom));
        yield return Data("static.custom.colors.enabled", new(Custom: "colors"));
        yield return Data("static.custom.colors.disabled", new(Enabled: false, Custom: "colors"));
    }

    public static IEnumerable<CaseData<ButtonConfig>> DynamicConfigurations()
    {
        // Press shape differs at every size. Wrappers share the shape algorithm, colors do not.
        foreach (var size in Sizes)
        foreach (var shape in new[] { "round", "square" })
            yield return Data($"dynamic.press.filled.{size}.{shape}", new(Size: size, Shape: shape, Action: "press"));
        foreach (var style in Styles.Where(s => s != "filled"))
            yield return Data($"dynamic.press.{style}.small.round", new(style, Action: "press"));
        // A fixed rounded-square start and asymmetric logical corners exercise morphing without
        // pill normalization and the changing horizontal optical offset in AnimatedShape.kt.
        foreach (var rtl in new[] { false, true })
            yield return Data($"dynamic.press-asymmetric.{(rtl ? "rtl" : "ltr")}", new(Shape: "asymmetric", Rtl: rtl, Action: "press"));
        foreach (var style in Styles)
        {
            yield return Data($"dynamic.hover.{style}", new(style, Action: "hover"));
            yield return Data($"dynamic.focus.{style}", new(style, Action: "focus"));
            yield return Data($"dynamic.disabled-pointer.{style}", new(style, Enabled: false, Action: "disabled"));
            yield return Data($"dynamic.disable-during-press.{style}", new(style, Action: "disable"));
            yield return Data($"dynamic.change-theme.{style}", new(style, Action: "theme"));
        }
        yield return Data("dynamic.quick-click", new(Action: "quick"));
        yield return Data("dynamic.release-outside", new(Action: "cancel"));
        yield return Data("dynamic.drag-out-back", new(Action: "drag-back"));
        yield return Data("dynamic.repress", new(Action: "reverse"));
        yield return Data("dynamic.change-size", new(Action: "size"));
        yield return Data("dynamic.change-content-size", new(Action: "content", Width: null));
        yield return Data("dynamic.change-pressed-shape", new(Action: "shape-change"));
        foreach (var key in new[] { "space", "enter" })
        {
            yield return Data($"dynamic.keyboard.{key}", new(Action: key));
            yield return Data($"dynamic.disabled-keyboard.{key}", new(Enabled: false, Action: key));
        }
        // Retain the elevated keyboard path, whose elevation changes accompany key feedback.
        yield return Data("dynamic.keyboard.elevated-space", new("elevated", Action: "space"));
        foreach (var action in new[] { "focus-hover", "hover-focus" })
            yield return Data($"dynamic.elevation.{action}", new("elevated", Custom: "elevation", Action: action));
        // Default elevations collapse several states to the same value. Distinct custom values
        // make pressed/focused/disabled target selection and their transitions observable.
        foreach (var action in new[] { "hover", "press", "focus", "disable" })
            yield return Data($"dynamic.custom-elevation.{action}", new(Custom: "elevation", Action: action));
        yield return Data("dynamic.no-elevation-hover", new(Custom: "no-elevation", Action: "hover"));
        yield return Data("dynamic.baseline-press", new(Baseline: true, Action: "press"));
    }

    static CaseData<ButtonConfig> Data(string id, ButtonConfig config) => new(id, id.Replace('.', ' '), config);

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<ButtonConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        var scene = new ButtonScene(host, config, writer);
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
        var scene = new ButtonScene(host, config, writer);
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
            AdvanceTo(time); host.Input(action, position); host.Draw();
        }
        // A fixed 16ms frame grid is independent of the observation and input times.
        // At a grid-time input, deliver that frame first; Snapshot never adds a frame.
        long nextFrameUs = 16_000;
        void AdvanceTo(long time)
        {
            while (nextFrameUs <= time)
            {
                host.Frame(nextFrameUs);
                nextFrameUs += 16_000;
            }
            host.AdvanceTo(time);
        }
        void SamplesAt(params long[] times)
        {
            foreach (var time in times)
            {
                AdvanceTo(time);
                host.Draw();
                Sample("time-" + time, time is not (32000 or 450000 or 92000));
            }
        }
        void Focus(long time, bool focused)
        {
            AdvanceTo(time);
            if (focused) scene.Button.Focus(NavigationMethod.Tab);
            else host.Window.FocusManager?.Focus(null);
            writer.Record(time, "input", focused ? "Button.Focus(Tab)" : "FocusManager.Focus(null)");
            host.Draw();
        }
        void Key(long time, string key, bool down)
        {
            AdvanceTo(time);
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
                SamplesAt(16000, 75000, 121000, 180000);
                Event(181000, "move", outside); Sample("exit-event");
                SamplesAt(196000, 301000, 400000); break;
            case "focus":
                Focus(1000, true); Sample("focus-event"); SamplesAt(16000, 45000, 121000, 180000);
                Focus(181000, false); Sample("blur-event"); SamplesAt(196000, 331000, 400000); break;
            case "focus-hover":
            case "hover-focus":
                if (config.Action == "focus-hover") Focus(1000, true); else Event(1000, "move", center);
                SamplesAt(16000, 121000);
                if (config.Action == "focus-hover") Event(122000, "move", center); else Focus(122000, true);
                Sample("second-interaction"); SamplesAt(138000, 242000);
                if (config.Action == "focus-hover") Event(243000, "move", outside); else Focus(243000, false);
                Sample("remove-last"); SamplesAt(259000, 400000);
                if (config.Action == "focus-hover") Focus(401000, false); else Event(401000, "move", outside);
                Sample("remove-first"); SamplesAt(417000, 560000); break;
            case "space":
            case "enter":
                Focus(1000, true); SamplesAt(16000, 121000);
                Key(122000, config.Action, true); Sample("key-down"); SamplesAt(138000, 197000, 347000);
                Key(348000, config.Action, false); Sample("key-up"); SamplesAt(364000, 498000, 700000); break;
            case "theme":
            case "size":
            case "content":
                AdvanceTo(1000);
                if (config.Action == "theme") scene.SetTheme(true);
                if (config.Action == "size") scene.SetSize("large");
                if (config.Action == "content") scene.SetContentWidth(64);
                writer.Record(1000, "mutation", config.Action);
                host.Draw(); Sample("mutation"); SamplesAt(16000, 400000);
                AdvanceTo(401000);
                if (config.Action == "theme") scene.SetTheme(false);
                if (config.Action == "size") scene.SetSize("small");
                if (config.Action == "content") scene.SetContentWidth(32);
                writer.Record(401000, "mutation", config.Action + " restored");
                host.Draw(); Sample("restored"); SamplesAt(416000, 800000); break;
            default:
                // A mouse reaches the target before pressing it. Explicit Move avoids comparing
                // the adapters' different implicit hover updates on a teleported Press event.
                Event(1000, "move", center); Sample("pointer-enter");
                Event(2000, "down", center); Sample("down-event");
                if (config.Action == "quick")
                {
                    Event(5000, "up", center); Sample("up-before-first-frame"); SamplesAt(16000, 75000, 225000, 375000, 600000); break;
                }
                if (config.Action == "disabled")
                {
                    Event(5000, "up", center); Sample("up-before-first-frame"); SamplesAt(16000, 75000);
                    Event(76000, "move", outside); Sample("disabled-pointer-exit"); SamplesAt(92000, 225000, 375000, 600000); break;
                }
                if (config.Action == "reverse")
                {
                    SamplesAt(16000, 32000); Event(33000, "up", center); Sample("reverse-event");
                    SamplesAt(48000); Event(49000, "down", center); Sample("repress-event");
                    SamplesAt(64000, 96000); Event(97000, "up", center); Sample("final-up");
                    SamplesAt(113000, 225000, 375000, 600000); break;
                }
                SamplesAt(16000, 32000, 75000);
                if (config.Action == "cancel")
                {
                    Event(76000, "move", outside); Sample("drag-out");
                    Event(77000, "up", outside); Sample("outside-release"); SamplesAt(92000, 225000, 375000, 600000); break;
                }
                if (config.Action == "drag-back")
                {
                    Event(76000, "move", outside); Sample("drag-out"); SamplesAt(92000, 150000);
                    Event(151000, "move", center); Sample("drag-back"); SamplesAt(167000, 225000);
                    Event(226000, "up", center); Sample("inside-release"); SamplesAt(242000, 376000, 600000); break;
                }
                if (config.Action == "disable")
                {
                    AdvanceTo(76000); scene.Button.IsEnabled = false; writer.Record(76000, "mutation", "enabled=false");
                    host.Draw(); Sample("disabled-during-press"); SamplesAt(80000, 92000, 96000, 112000, 128000, 225000, 400000);
                    Event(401000, "up", center); Sample("disabled-release");
                    AdvanceTo(402000); scene.Button.IsEnabled = true; writer.Record(402000, "mutation", "enabled=true");
                    host.Draw(); Sample("reenabled"); SamplesAt(418000, 600000); break;
                }
                if (config.Action == "shape-change")
                {
                    AdvanceTo(76000); scene.Surface.PressedCornerRadius = new CornerRadius(14);
                    writer.Record(76000, "mutation", "pressedShape=14dip"); host.Draw(); Sample("third-shape");
                }
                SamplesAt(120000, 225000, 400000);
                Event(401000, "up", center); Sample("release-event"); SamplesAt(416000, 450000, 551000, 700000); break;
        }
    }

    sealed class ButtonScene
    {
        readonly AvaloniaHost host;
        readonly ButtonConfig config;
        readonly CaptureWriter writer;
        int clicks;
        ButtonMarker? marker;
        public Button Button { get; }
        public MaterialSurface Surface => Button.GetVisualDescendants().OfType<MaterialSurface>().Single();

        public ButtonScene(AvaloniaHost host, ButtonConfig config, CaptureWriter writer)
        {
            this.host = host; this.config = config; this.writer = writer;
            SetTheme(config.Dark);
            Button = new Button { IsEnabled = config.Enabled, FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            Button.Theme = (ControlTheme)host.Window.FindResource(config.Style switch
            {
                "filled" => "MaterialFilledButtonTheme", "elevated" => "MaterialElevatedButtonTheme",
                "tonal" => "MaterialTonalButtonTheme", "outlined" => "MaterialOutlinedButtonTheme", _ => "MaterialTextButtonTheme"
            })!;
            if (config.Size != "small") Button.Classes.Add(config.Size);
            if (config.Width is { } width) Button.Width = width;
            if (config.Height is { } height) Button.Height = height;
            if (config.Shape != "round") host.Window.Resources[Sys.Shape.Full] = config.Shape == "asymmetric"
                // Avalonia mirrors the Button's local x-axis in RTL. The same local corners
                // therefore supply Compose's start/end shape in both layout directions.
                ? new CornerRadius(4, 16, 10, 2)
                : new CornerRadius(config.Size switch { "xsmall" or "small" => 12, "medium" => 16, _ => 28 });
            // The fixed Button.kt shape overload uses ContentPadding, not contentPaddingFor:
            // BaselineButtonTokens.Leading/TrailingSpace = 24, vertical = 8; TextButton = 12,8.
            if (config.Baseline) Button.Padding = new Thickness(config.Style == "text" ? 12 : 24, 8);
            // The mirrored local axis also makes Left/Right consume logical start/end padding.
            if (config.Custom == "padding") Button.Padding = new Thickness(5, 3, 27, 9);
            if (config.Custom == "border") { Button.BorderThickness = new Thickness(3); Button.BorderBrush = Brushes.Red; }
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
            if (config.Baseline)
            {
                // Configure the real surface after mounting. The older shape overload retains
                // its supplied shape during press; the expressive ButtonShapes overload morphs.
                Surface.PressedCornerRadius = null;
                var padding = Button.Padding;
                writer.Record(host.TimeUs, "configuration", "baseline shape overload", new
                {
                    paddingDip = new[] { padding.Left, padding.Top, padding.Right, padding.Bottom },
                    pressedCornerRadius = Surface.PressedCornerRadius,
                });
            }
            if (config.Empty) { Button.Content = null; return; }
            marker = new ButtonMarker { Name = "content", PreferredSize = new Size(config.ContentWidth, config.ContentHeight),
                VerticalAlignment = VerticalAlignment.Center };
            if (config.RequiredContentSize) { marker.Width = config.ContentWidth; marker.Height = config.ContentHeight; }
            if (config.Icon == "none") { Button.Content = marker; return; }
            size ??= config.Size;
            var iconKey = size switch { "xsmall" => Comp.ButtonXSmall.IconSize, "medium" => Comp.ButtonMedium.IconSize, "large" => Comp.ButtonLarge.IconSize, "xlarge" => Comp.ButtonXLarge.IconSize, _ => Comp.ButtonSmall.IconSize };
            var spaceKey = size switch { "xsmall" => Comp.ButtonXSmall.IconLabelSpace, "medium" => Comp.ButtonMedium.IconLabelSpace, "large" => Comp.ButtonLarge.IconLabelSpace, "xlarge" => Comp.ButtonXLarge.IconLabelSpace, _ => Comp.ButtonSmall.IconLabelSpace };
            if (!Button.TryFindResource(iconKey, out var iconValue) || iconValue is not double iconSize
                || !Button.TryFindResource(spaceKey, out var spaceValue) || spaceValue is not double spacing)
                throw new InvalidOperationException("Button icon token unavailable.");
            // At the pinned baseline ButtonDefaults.iconSpacingFor uses ExtraSmallIconSpacing=4dp
            // to correct the raw XS IconLabelSpace=8dp. Match that composable default in this scene.
            if (size == "xsmall") spacing = 4;
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = spacing, VerticalAlignment = VerticalAlignment.Center };
            ButtonMarker Icon(string name)
            {
                return new ButtonMarker { Name = name, IsIcon = true, Width = iconSize, Height = iconSize };
            }
            if (config.Icon is "start" or "both") panel.Children.Add(Icon("start-icon"));
            panel.Children.Add(marker);
            if (config.Icon is "end" or "both") panel.Children.Add(Icon("end-icon"));
            Button.Content = panel;
        }

        public void SetContentWidth(double width) { marker!.PreferredSize = new Size(width, marker.PreferredSize.Height); marker.InvalidateMeasure(); }

        public void SetSize(string size)
        {
            foreach (var value in Sizes) Button.Classes.Remove(value);
            if (size != "small") Button.Classes.Add(size);
        }

        public ProbePoint[] Measure()
        {
            var surface = Surface;
            var fields = LayoutBounds(surface).ToList();
            if (config.Action != "none" || config.Style == "elevated" || config.Custom is "elevation" or "no-elevation")
                fields.Add(Measurement.Numeric("shadowElevation", surface.ShadowElevation, category: "motion", definition: "button.shadow-elevation", source: "MaterialSurface.ShadowElevation consumed by DrawShadow"));
            var chrome = (Border)typeof(MaterialSurface).GetField("_chrome", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)!;
            var radius = chrome.CornerRadius;
            // Native Skia normalization used by the renderer, in container DIP.
            using var round = new SKRoundRect();
            round.SetRectRadii(new SKRect(0, 0, (float)surface.Bounds.Width, (float)surface.Bounds.Height),
                new[] { radius.TopLeft, radius.TopRight, radius.BottomRight, radius.BottomLeft }.Select(r => new SKPoint((float)r, (float)r)).ToArray());
            var normalized = new[] { SKRoundRectCorner.UpperLeft, SKRoundRectCorner.UpperRight, SKRoundRectCorner.LowerRight, SKRoundRectCorner.LowerLeft }.Select(round.GetRadii).SelectMany(r => new[] { (double)r.X, r.Y }).ToArray();
            // Report visible physical TL/TR/BR/BL, using the actual surface transform rather
            // than the requested direction. An RTL boundary can reflect the local x-axis.
            var origin = surface.TranslatePoint(default, host.Window) ?? throw new InvalidOperationException("Surface is not attached to the capture window.");
            var xEnd = surface.TranslatePoint(new Point(surface.Bounds.Width, 0), host.Window) ?? throw new InvalidOperationException("Surface is not attached to the capture window.");
            if (xEnd.X < origin.X)
                normalized = [normalized[2], normalized[3], normalized[0], normalized[1], normalized[6], normalized[7], normalized[4], normalized[5]];
            fields.Add(Measurement.Vector("cornerRadii", "quad", normalized, "dip", "container", "motion", "button.container.normalized-visual-rounded-outline", "derived: Border.CornerRadius cached at chrome; SKRoundRect.SetRectRadii normalization; actual Surface.TranslatePoint x-axis reflection mapped to visible physical TL/TR/BR/BL", CornerComponents));
            if (!config.Baseline && config.Action is "press" or "quick" or "cancel" or "drag-back" or "reverse" or "disable" or "shape-change" or "space" or "enter" or "size")
            {
                // Passive diagnostics: distinguish target registration from the first animation
                // frame. An uncreated morph has no progress/spec; do not synthesize its values.
                var morph = typeof(MaterialSurface).GetField("_shapeMorph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)
                    as Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;
                fields.Add(Measurement.Flag("shapeRunning", morph?.IsRunning == true, category: "motion",
                    definition: "button.shape-animation.running", source: "MaterialSurface._shapeMorph?.IsRunning == true"));
                foreach (var (name, unit, definition) in new[]
                {
                    ("shapeProgress", "fraction", "button.shape-animation.interpolation-progress"),
                    ("shapeVelocity", "fraction/s", "button.shape-animation.interpolation-velocity"),
                    ("shapeDampingRatio", "ratio", "button.shape-animation.spring-damping-ratio"),
                    ("shapeStiffness", "stiffness", "button.shape-animation.spring-stiffness")
                })
                {
                    if (morph is null)
                        fields.Add(Measurement.Unavailable(name, "motion", "number", unit, "none",
                            "MaterialSurface has not created a shape morph yet.", definition));
                    else
                    {
                        var spec = (Material3Expressive.Foundation.Motion.SpringSpec)typeof(Material3Expressive.Foundation.Interaction.AnimatedCornerRadius)
                            .GetField("_spec", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(morph)!;
                        fields.Add(Measurement.Numeric(name, name switch
                        {
                            "shapeProgress" => morph.Progress,
                            "shapeVelocity" => ((ComposeAnimation.Runtime.Animatable<float>)typeof(Material3Expressive.Foundation.Interaction.AnimatedCornerRadius)
                                .GetField("_progress", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(morph)!).Velocity,
                            "shapeDampingRatio" => spec.DampingRatio,
                            _ => spec.Stiffness
                        }, unit, category: "motion", definition: definition,
                            source: name switch { "shapeProgress" => "AnimatedCornerRadius.Progress", "shapeVelocity" => "AnimatedCornerRadius._progress.Velocity", _ => "AnimatedCornerRadius._spec captured for this morph" }));
                    }
                }
            }
            var buttonFields = LayoutBounds(Button).ToList();
            if (config.Action is "disable" or "disabled" || !config.Enabled && config.Action != "none")
                buttonFields.Add(Measurement.Flag("enabled", Button.IsEffectivelyEnabled, definition: "button.enabled", source: "Button.IsEffectivelyEnabled"));
            if (config.Action is "press" or "quick" or "cancel" or "drag-back" or "reverse" or "disable" or "disabled" or "shape-change" or "space" or "enter")
            {
                buttonFields.Add(Measurement.Flag("pressed", Interaction("Pressed"), definition: "button.pressed", source: "MaterialSurface._interactionController.Interaction active Pressed feedback flag"));
                buttonFields.Add(Measurement.Numeric("clickCount", clicks, "count", definition: "button.on-click-count", source: "Button.Click handler"));
            }
            if (config.Action is "hover" or "focus-hover" or "hover-focus" or "press" or "quick" or "cancel" or "drag-back" or "reverse" or "disable" or "disabled" or "shape-change")
                buttonFields.Add(Measurement.Flag("hovered", Interaction("Hovered"), definition: "button.hovered", source: "MaterialSurface._interactionController.Interaction active Hovered feedback flag"));
            if (config.Action is "focus" or "focus-hover" or "hover-focus" or "space" or "enter")
                buttonFields.Add(Measurement.Flag("focused", Button.IsFocused, definition: "button.focused", source: "Button.IsFocused"));
            var points = new List<ProbePoint>
            {
                new("button", "button", null, buttonFields.ToArray()),
                new("container", "container", "button", fields.ToArray())
            };
            var contentGeometry = config.Width == null || config.Width < 64 || config.Height != null || config.Icon != "none"
                || config.Shape == "asymmetric" || config.Custom == "padding";
            foreach (var content in Button.GetVisualDescendants().OfType<ButtonMarker>())
            {
                var contentFields = contentGeometry ? LayoutBounds(content).ToList() : [];
                contentFields.Add(content.Paint is ISolidColorBrush brush
                    ? Measurement.Vector("color", "color", [brush.Color.R, brush.Color.G, brush.Color.B, Math.Round(brush.Color.A * brush.Opacity, MidpointRounding.AwayFromZero)],
                        "srgba8", "none", "properties", "button.content.painted-solid-srgba8", "derived: ButtonMarker.Render inherited TextElement.Foreground; byte RGB and effective alpha rounded to nearest byte")
                    : Measurement.Unavailable("color", "properties", "color", "srgba8", "none",
                        content.Paint == null ? "Content Render has not run (it can be fully constrained or clipped)." : "Content painted with a non-solid brush.",
                        "button.content.painted-solid-srgba8"));
                points.Add(new(content.Name!, content.IsIcon ? "icon" : "content", "container", contentFields.ToArray()));
            }
            return points.ToArray();

            // These scenes have no rotation: their layout evidence is final position/size.
            // Framework-local axis orientation is not the Button layout contract.
            Measurement[] LayoutBounds(Control control) => AvaloniaMeasurements.Geometry(control, host.Window)
                .Where(m => m.Name is "box" or "size").ToArray();

            bool Interaction(string name)
            {
                var controller = typeof(MaterialSurface).GetField("_interactionController", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)
                    ?? throw new InvalidOperationException("Button has no Material interaction controller.");
                var current = (Enum)controller.GetType().GetProperty("Interaction", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(controller)!;
                return current.HasFlag((Enum)Enum.Parse(current.GetType(), name));
            }
        }
    }

    // Normal Button content: a fixed-size rectangle or an asymmetric vector in a 20x20 viewport.
    // Consume the actual inherited foreground, including subsequent theme/state updates.
    sealed class ButtonMarker : Control
    {
        // Match Compose Canvas/non-AutoMirrored vector content: RTL changes slot order and
        // alignment, but does not reverse the marker's local drawing coordinate system.
        protected override bool BypassFlowDirectionPolicies => true;
        public bool IsIcon { get; init; }
        public Size PreferredSize { get; set; }
        protected override Size MeasureOverride(Size availableSize) => new(
            Math.Min(availableSize.Width, PreferredSize.Width), Math.Min(availableSize.Height, PreferredSize.Height));
        public IBrush? Paint { get; private set; }
        static ButtonMarker() => AffectsRender<ButtonMarker>(TextElement.ForegroundProperty);
        static readonly Geometry Arrow = Geometry.Parse("M 2,3 L 11,3 L 11,1 L 18,8 L 11,15 L 11,12 L 2,12 Z");
        public override void Render(DrawingContext context)
        {
            Paint = TextElement.GetForeground(this);
            if (IsIcon)
            {
                using (context.PushTransform(Matrix.CreateScale(Bounds.Width / 20, Bounds.Height / 20)))
                    context.DrawGeometry(Paint, null, Arrow);
            }
            else context.DrawRectangle(Paint, null, new Rect(Bounds.Size));
        }
    }
    static readonly string[] CornerComponents = ["topLeft.x", "topLeft.y", "topRight.x", "topRight.y", "bottomRight.x", "bottomRight.y", "bottomLeft.x", "bottomLeft.y"];
}

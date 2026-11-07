using Avalonia;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record MaterialShapeConfig(string Kind = "surface", string Shape = "cut", bool Rtl = false,
    bool Content = false, double Border = 1.1, double Elevation = 0, bool Clickable = false, string Action = "none", string PressedShape = "none", bool Gradient = false, string GradientType = "linear");

[ParitySuite("material-shape")]
public sealed class MaterialShapeCases
{
    private static readonly string[] IconKinds = ["icon-standard", "icon-filled", "icon-tonal", "icon-outlined", "icon-vibrant", "icon-outlined-vibrant",
        "icon-toggle-standard", "icon-toggle-filled", "icon-toggle-tonal", "icon-toggle-outlined"];
    public static IEnumerable<CaseData<MaterialShapeConfig>> Configurations()
    {
        foreach (var shape in new[] { "cut", "offset" })
        foreach (var paint in new[] { "radial", "conic" })
            yield return new($"static.path.{shape}.{paint}", "Gradient coordinate preservation", new(Shape: shape, Border: 4.25, Gradient: true, GradientType: paint));
        foreach (var shape in new[] { "cut", "hole", "offset" })
            yield return new($"static.path.{shape}.gradient", "Gradient path border paint", new(Shape: shape, Border: 4.25, Gradient: true));
        foreach (var kind in new[] { "toggle-filled", "toggle-tonal", "toggle-elevated", "toggle-outlined" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{(rtl ? "rtl" : "ltr")}", "Native toggle fixed cut shape", new(kind, "asymmetric", rtl));
        foreach (var kind in IconKinds)
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{(rtl ? "rtl" : "ltr")}", "Icon button static cut outline",
                new(kind, "asymmetric", rtl, Border: kind.Contains("outlined") ? 1.1 : 0));
        foreach (var kind in new[] { "chip-assist", "chip-suggestion", "chip-filter", "chip-input", "chip-elevated-assist", "chip-elevated-suggestion", "chip-elevated-filter" })
        foreach (var shape in new[] { "asymmetric", "percent" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{shape}.{(rtl ? "rtl" : "ltr")}", "Chip fixed shape overload",
                new(kind, shape, rtl, Elevation: kind.Contains("elevated") ? 6 : 0));
        foreach (var kind in new[] { "surface", "click", "card", "elevated", "outlined", "button-filled", "button-elevated", "button-tonal", "button-outlined", "button-text" })
        foreach (var shape in new[] { "cut", "asymmetric", "percent", "zero" })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{kind}.{shape}.{(rtl ? "rtl" : "ltr")}", "Cut corner container and uniform path border",
                new(kind, shape, rtl, Elevation: kind == "elevated" ? 6 : 0));
        foreach (var shape in new[] { "cut", "asymmetric", "percent", "concave", "offset", "curve", "hole" })
        foreach (var layer in new[] { "fill", "content", "shadow", "thick" })
            yield return new($"static.path.{shape}.{layer}", "Generic fill, clip, shadow and border branches",
                new(Shape: shape, Content: layer == "content", Border: layer == "thick" ? 40 : layer == "fill" ? 0 : 1.1,
                    Elevation: layer == "shadow" ? 6 : 0));
        // Clickable Card adds minimum sizing and an independent elevation collector.
        foreach (var kind in new[] { "card", "elevated", "outlined" })
            yield return new($"static.{kind}.clickable", "Clickable card path", new(kind, "asymmetric", Clickable: true));
    }

    public static IEnumerable<CaseData<MaterialShapeConfig>> DynamicConfigurations()
    {
        foreach (var kind in new[] { "toggle-filled", "toggle-tonal", "toggle-elevated", "toggle-outlined" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "press", "toggle-morph", "morph-resize" })
            yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}", "Native toggle cut press and selection",
                new(kind, "asymmetric", rtl, Action: action, PressedShape: "percent"));
        foreach (var kind in IconKinds)
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "press", kind.Contains("toggle") ? "toggle-morph" : "morph-resize" })
            yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}", "Icon path press and checked morph",
                new(kind, "asymmetric", rtl, Border: kind.Contains("outlined") ? 1.1 : 0, Action: action, PressedShape: "percent"));
        foreach (var kind in new[] { "chip-assist", "chip-suggestion", "chip-filter", "chip-input" })
        foreach (var action in new[] { "press", "focus", "selection" })
        {
            if (action == "selection" && kind is not ("chip-filter" or "chip-input")) continue;
            yield return new($"dynamic.{kind}.{action}", "Chip path feedback and fixed selection outline",
                new(kind, "asymmetric", Action: action));
        }
        foreach (var kind in new[] { "button-filled", "button-elevated", "button-tonal", "button-outlined", "button-text" })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "press", "reverse", "morph-resize" })
            yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}", "Cut corner morph and cancellation",
                new(kind, "asymmetric", rtl, Action: action, PressedShape: "percent"));
        yield return new("dynamic.button-filled.snap", "Non-corner paths switch without interpolation",
            new("button-filled", "cut", Action: "press", PressedShape: "concave"));
        foreach (var shape in new[] { "cut", "hole", "zero" })
        {
            yield return new($"dynamic.click.hit.{shape}", "Real mouse hit testing follows the outline", new("click", shape, Action: "hit"));
            yield return new($"dynamic.click.focus.{shape}", "Generic and rectangular focus outline branches", new("click", shape, Action: "focus"));
        }
        foreach (var kind in new[] { "click", "card", "elevated", "outlined" })
        foreach (var action in new[] { "press", "focus", "resize" })
        foreach (var rtl in new[] { false, true })
            yield return new($"dynamic.{kind}.{action}.{(rtl ? "rtl" : "ltr")}", "Path indication, interruption and live geometry",
                new(kind, "asymmetric", rtl, Clickable: true, Action: action));
    }

    [ParityScenario("static", Data = nameof(Configurations))]
    public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer);

    private static async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<MaterialShapeConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        CaseDefaults.SetThemeColors(host, false);
        var source = new MaterialInteractionSource();
        var shape = CreateShape(c.Shape);
        Control? content = c.Content ? new Border { Background = Brushes.Blue } : null;
        var color = Color.Parse(c.Kind.StartsWith("chip-", StringComparison.Ordinal) ? "#BBDDCC" : "#EADDFF");
        Control control;
        var clicks = 0;
        if (c.Kind == "surface") control = new MaterialSurface { Shape = shape, Background = new SolidColorBrush(color),
            BorderBrush = Brushes.Red, BorderThickness = new(c.Border), ShadowElevation = c.Elevation, Child = content };
        else if (c.Kind == "click")
        {
            var clickable = new ClickableSurface { Shape = shape, Background = new SolidColorBrush(color), Foreground = Brushes.Black,
                BorderBrush = Brushes.Red, BorderThickness = new(c.Border), ShadowElevation = c.Elevation, Content = content, InteractionSource = source };
            clickable.Click += (_, _) => clicks++; control = clickable;
        }
        else if (c.Kind.StartsWith("toggle-", StringComparison.Ordinal))
        {
            var button = new ToggleButton { Content = content, Padding = new(0), Background = new SolidColorBrush(color),
                Foreground = Brushes.Black, BorderBrush = Brushes.Red, BorderThickness = new(c.Border) };
            var theme = c.Kind[7..] switch { "tonal" => "Tonal", "elevated" => "Elevated", "outlined" => "Outlined", _ => "Filled" };
            button.Theme = (ControlTheme)host.Window.FindResource($"Material{theme}ToggleButtonTheme")!;
            MaterialButton.SetShape(button, shape);
            MaterialButton.SetPressedShape(button, c.PressedShape == "none" ? null : CreateShape(c.PressedShape));
            ToggleButtonAppearance.SetCheckedShape(button, c.PressedShape == "none" ? null : new CutCornerShape(8));
            MaterialButton.SetInteractionSource(button, source); MaterialButton.SetElevation(button, new());
            button.Click += (_, _) => clicks++; control = button;
        }
        else if (c.Kind.StartsWith("icon-", StringComparison.Ordinal))
        {
            var toggle = c.Kind.Contains("toggle");
            IconButton button = toggle ? new IconToggleButton() : new IconButton();
            var theme = c.Kind.Replace("icon-toggle-", "", StringComparison.Ordinal).Replace("icon-", "", StringComparison.Ordinal) switch {
                "filled" => "Filled", "tonal" => "Tonal", "outlined" => "Outlined", "vibrant" => "Vibrant", "outlined-vibrant" => "OutlinedVibrant", _ => "Standard" };
            button.Theme = (ControlTheme)host.Window.FindResource($"Material{theme}Icon{(toggle ? "Toggle" : "")}ButtonTheme")!;
            button.Background = new SolidColorBrush(color); button.Foreground = Brushes.Black;
            button.BorderBrush = Brushes.Red; button.BorderThickness = new(c.Border); button.InteractionSource = source;
            MaterialButton.SetShape(button, shape);
            MaterialButton.SetPressedShape(button, c.PressedShape == "none" ? null : CreateShape(c.PressedShape));
            if (button is IconToggleButton selected) selected.CheckedShape = c.PressedShape == "none" ? null : new CutCornerShape(8);
            button.Click += (_, _) => clicks++; control = button;
        }
        else if (c.Kind.StartsWith("chip-", StringComparison.Ordinal))
        {
            Chip chip = c.Kind switch {
                "chip-suggestion" => new SuggestionChip(), "chip-filter" => new FilterChip(), "chip-input" => new InputChip(),
                "chip-elevated-assist" => new AssistChip { IsElevated = true },
                "chip-elevated-suggestion" => new SuggestionChip { IsElevated = true },
                "chip-elevated-filter" => new FilterChip { IsElevated = true }, _ => new AssistChip() };
            chip.Shape = shape; chip.Content = content; chip.Padding = new(0);
            chip.Background = new SolidColorBrush(color); chip.Foreground = Brushes.Black;
            chip.BorderBrush = Brushes.Red; chip.BorderThickness = new(c.Border);
            chip.Elevation = new(c.Elevation, c.Elevation, c.Elevation, c.Elevation, c.Elevation, c.Elevation);
            chip.InteractionSource = source;
            chip.Click += (_, _) => clicks++; control = chip;
        }
        else if (c.Kind.StartsWith("button-", StringComparison.Ordinal))
        {
            var button = new Button { Content = content, Padding = new(0), Background = new SolidColorBrush(color),
                Foreground = Brushes.Black, BorderBrush = Brushes.Red, BorderThickness = new(c.Border) };
            var theme = c.Kind[7..] switch { "elevated" => "Elevated", "tonal" => "Tonal", "outlined" => "Outlined", "text" => "Text", _ => "Filled" };
            button.Theme = (ControlTheme)host.Window.FindResource($"Material{theme}ButtonTheme")!;
            MaterialButton.SetShape(button, shape);
            MaterialButton.SetPressedShape(button, c.PressedShape == "none" ? null : CreateShape(c.PressedShape));
            MaterialButton.SetInteractionSource(button, source);
            MaterialButton.SetElevation(button, new() { Default = c.Elevation, Pressed = c.Elevation, Focused = c.Elevation,
                Hovered = c.Elevation, Disabled = c.Elevation });
            button.Click += (_, _) => clicks++; control = button;
        }
        else
        {
            Card card = c.Kind switch { "elevated" => new ElevatedCard(), "outlined" => new OutlinedCard(), _ => new Card() };
            card.Shape = shape; card.ContainerColor = color; card.ContentColor = Brushes.Black;
            card.BorderBrush = Brushes.Red; card.BorderThickness = new(c.Border); card.Content = content;
            card.Elevation = new() { Default = c.Elevation, Pressed = c.Elevation, Focused = c.Elevation,
                Hovered = c.Elevation, Dragged = c.Elevation, Disabled = c.Elevation };
            card.IsClickable = c.Clickable; card.InteractionSource = source;
            card.Click += (_, _) => clicks++; control = card;
        }
        control.Width = 120; control.Height = 64;
        if (c.Gradient) ((MaterialSurface)control).BorderBrush = new LinearGradientBrush {
            StartPoint = new(0, 0, RelativeUnit.Relative), EndPoint = new(1, 0, RelativeUnit.Relative),
            GradientStops = [new GradientStop(Colors.Red, 0), new GradientStop(Colors.Blue, 1)] };
        if (c.Gradient && c.GradientType == "radial") ((MaterialSurface)control).BorderBrush = new RadialGradientBrush {
            Center = new(16, 12, RelativeUnit.Absolute), GradientOrigin = new(16, 12, RelativeUnit.Absolute),
            RadiusX = new(20, RelativeUnit.Absolute), RadiusY = new(20, RelativeUnit.Absolute),
            GradientStops = [new GradientStop(Colors.Red, 0), new GradientStop(Colors.Blue, 1)] };
        if (c.Gradient && c.GradientType == "conic") ((MaterialSurface)control).BorderBrush = new ConicGradientBrush {
            Center = new(16, 12, RelativeUnit.Absolute), Angle = 90,
            GradientStops = [new GradientStop(Colors.Red, 0), new GradientStop(Colors.Blue, 1)] };
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        control.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        if (c.Action == "focus")
        {
            MaterialInteraction.SetFocusFeedback(control, FocusFeedback.InsetRing);
            MaterialInteraction.SetFocusRingOuterColor(control, Colors.Red);
            MaterialInteraction.SetFocusRingInnerColor(control, Colors.Blue);
        }
        host.Mount(new Grid { Background = Brushes.White, [TextElement.ForegroundProperty] = Brushes.Black, Children = { control } });
        host.Frame(-1000); host.Frame(0);
        var next = 16000L;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var state = new List<Measurement> { Measurement.Numeric("clicks", clicks, "count", definition: "shape.activation-count") };
            if (control is ToggleButton or IconToggleButton)
                state.Add(Measurement.Flag("checked", control is ToggleButton native ? native.IsChecked == true : ((IconToggleButton)control).IsChecked,
                    definition: "shape.checked-state"));
            if (c.PressedShape == "percent")
            {
                var surface = control.GetVisualDescendants().OfType<MaterialSurface>().Single();
                var morph = (AnimatedCornerRadius)typeof(MaterialSurface).GetField("_shapeMorph", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(surface)!;
                state.Add(Measurement.Numeric("progress", morph.Progress, "fraction", category: "motion", definition: "shape.morph.progress"));
            }
            writer.Submit(host.Snapshot(id, id,
            [new ProbePoint("container", "container", null,
                AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
             new ProbePoint("state", "state", null, state.ToArray())]));
        }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Change(long time, string id, Action change) { Advance(time); change(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void SetChecked(bool value) { if (control is ToggleButton native) native.IsChecked = value; else ((IconToggleButton)control).IsChecked = value; }
        Sample("initial");
        if (c.Action == "toggle-morph")
        {
            Change(1000, "checked", () => SetChecked(true)); Times(16000, 32000);
            Advance(33000); host.Input("move", new(120, 80)); host.Input("down", new(120, 80)); host.Draw(); Times(48000, 64000);
            Advance(65000); host.Input("up", new(120, 80)); host.Input("move", new(8, 8)); host.Draw(); Times(80000);
            Change(97000, "unchecked", () => SetChecked(false)); Times(112000, 160000, 640000);
        }
        else if (c.Action == "selection")
        {
            Change(1000, "select", () => { if (control is SelectableChip chip) chip.IsSelected = true; });
            Times(16000, 160000);
            Change(161000, "deselect", () => { if (control is SelectableChip chip) chip.IsSelected = false; });
            Times(176000, 640000);
        }
        else if (c.Action == "hit")
        {
            Advance(1000); host.Input("move", new(61, 49)); host.Input("down", new(61, 49)); host.Input("up", new(61, 49)); host.Draw();
            Times(16000, 160000);
            Advance(161000); host.Input("move", new(120, 80)); host.Input("down", new(120, 80)); host.Input("up", new(120, 80)); host.Draw();
            Times(176000, 640000);
        }
        else if (c.Action is "press" or "reverse" or "morph-resize")
        {
            Advance(1000); host.Input("move", new(120, 80)); host.Input("down", new(120, 80)); host.Draw();
            Times(16000, 32000);
            if (c.Action == "morph-resize") Change(33000, "morph-resize", () => { control.Width = 88; control.Height = 56; });
            if (c.Action == "reverse")
            {
                Advance(33000); host.Input("up", new(120, 80)); host.Draw(); Times(48000, 64000);
                Advance(65000); host.Input("down", new(120, 80)); host.Draw();
            }
            Times(80000, 160000);
            Advance(161000); host.Input("up", new(120, 80)); host.Input("move", new(8, 8)); host.Draw();
            Times(176000, 240000, 640000);
        }
        else if (c.Action == "focus")
        {
            Change(1000, "focus", () => control.Focus(NavigationMethod.Tab));
            Times(16000, 80000, 160000, 320000);
            Change(321000, "blur", () => host.Window.FocusManager!.Focus(null));
            Times(336000, 400000, 640000);
        }
        else if (c.Action == "resize")
        {
            Change(1000, "resize", () => { control.Width = 80; control.Height = 48; });
            Change(17000, "mutate", () => ((CutCornerShape)shape).TopStart = new(.75, RelativeUnit.Relative));
            Change(33000, "direction", () => control.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
            Change(49000, "restore", () => { control.Width = 120; control.Height = 64; });
            Times(64000, 160000);
        }
    }

    internal static MaterialShape CreateShape(string kind) => kind switch
    {
        "zero" => new CutCornerShape(),
        "asymmetric" => new CutCornerShape { TopStart = new(40, RelativeUnit.Absolute), TopEnd = new(8, RelativeUnit.Absolute),
            BottomEnd = new(24, RelativeUnit.Absolute), BottomStart = new(48, RelativeUnit.Absolute) },
        "percent" => new CutCornerShape(new RelativeScalar(.5, RelativeUnit.Relative)),
        "concave" or "offset" or "curve" or "hole" => new CustomShape(kind),
        _ => new CutCornerShape(20)
    };

    private sealed class CustomShape(string kind) : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density) => PathGeometry.Parse(kind switch
        {
            "offset" => "M8,4 L108,4 L108,52 L8,52 Z",
            "curve" => "M0,32 C0,0 120,0 120,32 C120,64 0,64 0,32 Z",
            "hole" => "F0 M0,0 L120,0 L120,64 L0,64 Z M40,20 L80,20 L80,44 L40,44 Z",
            _ => "M0,0 L120,0 L120,64 L60,40 L0,64 Z"
        });
    }
}

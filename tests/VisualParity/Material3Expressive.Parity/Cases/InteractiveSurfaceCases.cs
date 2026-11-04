using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;

namespace Material3Expressive.Parity;

public sealed record InteractiveSurfaceConfig(string Kind = "click", string Shape = "rectangle", bool State = false,
    bool Enabled = true, bool Dark = false, bool Rtl = false, double? Width = 120, double? Height = 64,
    double ContentWidth = 32, double ContentHeight = 16, double Minimum = 48, double Tonal = 0,
    double Shadow = 0, bool Border = false, string Color = "surface", string Action = "none");

[ParitySuite("interactive-surface")]
public sealed class InteractiveSurfaceCases
{
    public static IEnumerable<CaseData<InteractiveSurfaceConfig>> StaticConfigurations()
    {
        foreach (var kind in new[] { "click", "select", "toggle" })
        foreach (var state in kind == "click" ? new[] { false } : new[] { false, true })
        foreach (var shape in new[] { "rectangle", "round", "asymmetric" })
        foreach (var enabled in new[] { false, true })
        foreach (var dark in new[] { false, true })
            yield return new($"static.{kind}.{state}.{shape}.{enabled}.{dark}".ToLowerInvariant(), "Surface appearance and controlled state",
                new(kind, shape, state, enabled, dark));
        // All overloads use the same Box/minimum-size layout and paint pipeline.
        yield return new("static.layout.auto", "Natural content size", new(Width: null, Height: null, ContentWidth: 80, ContentHeight: 60));
        yield return new("static.layout.small", "Minimum interaction allocation", new(Width: null, Height: null));
        yield return new("static.layout.empty", "Empty surface minimum allocation", new(Width: null, Height: null, ContentWidth: 0, ContentHeight: 0));
        yield return new("static.layout.minimum-zero", "Disabled minimum interaction allocation", new(Width: null, Height: null, Minimum: 0));
        yield return new("static.layout.minimum-64", "Inherited minimum interaction allocation", new(Width: null, Height: null, Minimum: 64));
        yield return new("static.layout.constrained", "Explicit size below interaction minimum", new(Width: 20, Height: 12));
        yield return new("static.layout.rtl", "Logical direction", new(Rtl: true));
        foreach (var dark in new[] { false, true })
        {
            yield return new($"static.paint.tonal.{dark}".ToLowerInvariant(), "Tonal elevation", new(Dark: dark, Tonal: 6));
            yield return new($"static.paint.shadow.{dark}".ToLowerInvariant(), "Independent shadow elevation", new(Shape: "round", Dark: dark, Shadow: 6));
            yield return new($"static.paint.border.{dark}".ToLowerInvariant(), "Border does not inset content", new(Shape: "asymmetric", Dark: dark, Border: true));
            yield return new($"static.paint.role.{dark}".ToLowerInvariant(), "Content color follows role", new(Dark: dark, Color: "primary"));
        }
    }

    public static IEnumerable<CaseData<InteractiveSurfaceConfig>> DynamicConfigurations()
    {
        foreach (var kind in new[] { "click", "select", "toggle" })
        foreach (var action in new[] { "press", "quick", "cancel", "disable", "space", "enter", "keys", "keys-reverse", "key-source", "source", "replace", "detach" })
            yield return new($"dynamic.{kind}.{action}", "Surface input and feedback lifecycle", new(kind, "round", Action: action));
    }

    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var c = writer.Metadata.ReadConfig<InteractiveSurfaceConfig>();
        await using var host = new AvaloniaHost(writer, 240, 160);
        CaseDefaults.SetThemeColors(host, c.Dark);
        host.Window.Resources[MaterialTokens.Sys.Color.Surface] = Color.Parse(c.Dark ? "#141218" : "#FEF7FF");
        host.Window.Resources[MaterialTokens.Sys.Color.SurfaceTint] = Color.Parse(c.Dark ? "#D0BCFF" : "#6750A4");
        ClickableSurface surface = c.Kind switch { "select" => new SelectableSurface { IsSelected = c.State },
            "toggle" => new ToggleableSurface { IsChecked = c.State }, _ => new ClickableSurface() };
        surface.IsEnabled = c.Enabled;
        surface.CornerRadius = c.Shape switch { "round" => new(16), "asymmetric" => new(24, 4, 12, 0), _ => default };
        surface.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        surface.Width = c.Width ?? double.NaN; surface.Height = c.Height ?? double.NaN;
        surface.TonalElevation = c.Tonal; surface.ShadowElevation = c.Shadow;
        MaterialInteractiveSize.SetMinimum(surface, c.Minimum);
        if (c.Border) { surface.BorderBrush = Brushes.Red; surface.BorderThickness = new(1.25); }
        if (c.Color == "primary") surface.Background = new SolidColorBrush(Color.Parse(c.Dark ? "#D0BCFF" : "#6750A4"));
        // Natural-size marker: its arranged size remains free to receive Surface's content constraints.
        var marker = new Marker(c.ContentWidth, c.ContentHeight);
        surface.Content = marker;
        var clicks = 0; var requests = 0;
        surface.Click += (_, e) => { if (ReferenceEquals(e.Source, surface)) clicks++; };
        if (surface is ToggleableSurface toggle) toggle.CheckedChangeRequested += (_, e) => { requests++; toggle.IsChecked = e.Value; };
        if (surface is SelectableSurface selected) selected.Click += (_, _) => selected.IsSelected = true;
        var source = new MaterialInteractionSource(); surface.InteractionSource = source;
        var parent = new Grid { Background = Brushes.White, Children = { surface } };
        surface.HorizontalAlignment = HorizontalAlignment.Center; surface.VerticalAlignment = VerticalAlignment.Center;
        host.Mount(parent); host.Frame(-1000); host.Frame(0);
        long next = 16000;
        void Advance(long time) { while (next <= time) { host.Frame(next); next += 16000; } host.AdvanceTo(time); }
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("state", "state", null,
                [Measurement.Numeric("clicks", clicks, "count", definition: "surface.activation-count"),
                 Measurement.Numeric("requests", requests, "count", definition: "surface.toggle-request-count"),
                 Measurement.Flag("value", surface is SelectableSurface s ? s.IsSelected : surface is ToggleableSurface t && t.IsChecked, definition: "surface.controlled-state")]) };
            if (parent.Children.Contains(surface))
            {
                points.Add(new("surface", "container", null, AvaloniaMeasurements.Geometry(surface, host.Window).Where(m => m.Name is "box" or "size").ToArray()));
                points.Add(new("content", "content", "surface", AvaloniaMeasurements.Geometry(marker, host.Window).Where(m => m.Name is "box" or "size").ToArray()));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray(), image: id != "time-32000"));
        }
        void Change(long time, string id, Action action)
        { Advance(time); action(); writer.Record(time, "mutation", id); host.Draw(); host.Frame(time); Sample(id); }
        void Times(params long[] times) { foreach (var time in times) { Advance(time); host.Draw(); Sample($"time-{time}"); } }
        void Input(long time, string operation, Point position) { Advance(time); host.Input(operation, position); host.Draw(); }
        var center = surface.TranslatePoint(new Rect(surface.Bounds.Size).Center, host.Window)!.Value;
        var outside = new Point(8, 8);
        Sample("initial"); if (!dynamic) return;
        if (c.Action is "keys" or "keys-reverse" or "key-source")
        {
            Change(1000, "focus", () => surface.Focus(NavigationMethod.Tab)); Times(16000, 160000);
            void KeyChange(long time, string id, Key key, bool down) => Change(time, id, () =>
            {
                if (down) host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
                else host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            });
            KeyChange(161000, "space-down", Key.Space, true); Times(176000);
            if (c.Action == "key-source")
            {
                Change(177000, "replace", () => surface.InteractionSource = new());
                KeyChange(193000, "cancelled-key-up", Key.Space, false);
                KeyChange(209000, "new-down", Key.Space, true); Times(224000);
                KeyChange(241000, "new-up", Key.Space, false);
            }
            else
            {
                KeyChange(177000, "enter-down", Key.Enter, true);
                KeyChange(193000, "space-repeat", Key.Space, true); Times(208000, 240000);
                KeyChange(241000, "first-up", c.Action == "keys-reverse" ? Key.Enter : Key.Space, false); Times(256000, 320000);
                KeyChange(321000, "second-up", c.Action == "keys-reverse" ? Key.Space : Key.Enter, false);
                KeyChange(337000, "unmatched-up", Key.Space, false);
            }
            Times(400000, 800000); return;
        }
        if (c.Action is "space" or "enter")
        {
            Change(1000, "focus", () => surface.Focus(NavigationMethod.Tab)); Times(16000, 64000, 160000);
            var key = c.Action == "space" ? Key.Space : Key.Enter;
            Change(161000, "key-down", () => host.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null)); Times(176000, 240000);
            Change(241000, "key-up", () => host.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null)); Times(256000, 400000, 800000);
            return;
        }
        if (c.Action is "source" or "replace" or "detach")
        {
            var first = new MaterialInteractionEvent.Press(new Point(40, 24));
            var second = new MaterialInteractionEvent.Press(new Point(80, 40));
            Change(1000, "first", () => source.Emit(first)); Times(16000, 32000, 64000);
            if (c.Action == "source")
            {
                Change(65000, "second", () => source.Emit(second));
                Change(81000, "release-first", () => source.Emit(new MaterialInteractionEvent.Release(first)));
                Times(96000, 160000);
                Change(161000, "cancel-second", () => source.Emit(new MaterialInteractionEvent.CancelPress(second)));
            }
            else if (c.Action == "replace")
            {
                var old = source;
                Change(65000, "replace", () => surface.InteractionSource = source = new());
                Change(81000, "old-release", () => old.Emit(new MaterialInteractionEvent.Release(first)));
                Change(97000, "new-press", () => source.Emit(second)); Times(112000, 160000);
                Change(161000, "new-release", () => source.Emit(new MaterialInteractionEvent.Release(second)));
            }
            else
            {
                Change(65000, "detach", () => parent.Children.Remove(surface));
                Change(81000, "detached-press", () => source.Emit(second));
                Change(97000, "reattach", () => parent.Children.Add(surface)); Times(112000, 160000);
            }
            Times(176000, 240000, 400000, 800000); return;
        }
        Input(1000, "move", center); Input(2000, "down", center); Sample("press");
        if (c.Action == "quick") { Input(5000, "up", center); Sample("release"); Times(16000, 80000, 240000, 800000); return; }
        Times(16000, 32000, 64000);
        if (c.Action == "cancel") Input(65000, "move", outside);
        if (c.Action == "disable") Change(65000, "disable", () => surface.IsEnabled = false);
        Times(80000, 160000);
        Input(161000, "up", c.Action == "cancel" ? outside : center); Sample("release"); Times(176000, 240000, 400000, 800000);
    }

    private sealed class Marker(double width, double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
        public override void Render(DrawingContext context)
        {
            // A small centered mark leaves the actual surface fill/ripple visible even under tight constraints.
            var size = new Size(Math.Min(12, Bounds.Width), Math.Min(8, Bounds.Height));
            context.FillRectangle(Avalonia.Controls.Documents.TextElement.GetForeground(this)!,
                new Rect((Bounds.Width - size.Width) / 2, (Bounds.Height - size.Height) / 2, size.Width, size.Height));
        }
    }
}

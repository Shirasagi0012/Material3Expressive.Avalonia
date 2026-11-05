using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record AnchoredChildInputConfig(string Kind, bool Rtl, bool NativeThumb = false);

[ParitySuite("anchored-child-input")]
public sealed class AnchoredChildInputCases
{
    public static IEnumerable<CaseData<AnchoredChildInputConfig>> Configurations()
    {
        foreach (var kind in new[] { "swipe", "sheet" })
        foreach (var rtl in new[] { false, true })
        {
            yield return new($"dynamic.{kind}.{(rtl ? "rtl" : "ltr")}", "Disable parent gestures during child press", new(kind, rtl));
            yield return new($"native.{kind}.{(rtl ? "rtl" : "ltr")}", "Native thumb drag keeps child capture", new(kind, rtl, true));
        }
    }

    [ParityScenario("disable-parent", Data = nameof(Configurations))]
    public async Task DisableParent(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<AnchoredChildInputConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        CaseDefaults.SetThemeColors(host, false);
        var clicks = 0;
        // A plain native button isolates capture ownership from Material button animation.
        // It remains a real input owner and invokes Click through normal mouse events.
        Control button = c.NativeThumb ? new Thumb
        {
            Width = 80, Height = 48, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Template = new FuncControlTemplate<Thumb>((_, _) => new Border { Background = Brush.Parse("#FFD166") })
        } : new Button
        {
            Width = 80, Height = 48, MinHeight = 0, MinWidth = 0, Padding = default,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Template = new FuncControlTemplate<Button>((_, _) => new Border { Background = Brush.Parse("#FFD166") })
        };
        if (button is Button clickable) clickable.Click += (_, _) => clicks++;
        var content = new Border { Height = 128, Background = Brush.Parse("#336699"), Child = button };
        var swipe = new SwipeToDismissBox { Width = 240, Height = 128, Content = c.Kind == "swipe" ? content : null, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var sheet = new BottomSheet { State = new(SheetValue.Expanded), Content = c.Kind == "sheet" ? content : null, DragHandle = null, DragHandleTemplate = null };
        var root = new Border { Background = Brushes.White, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Child = c.Kind == "swipe" ? swipe : sheet };
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        long nextFrame = 16000;
        void Advance(long time)
        {
            while (nextFrame <= time) { host.Frame(nextFrame); nextFrame += 16000; }
            host.AdvanceTo(time);
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [new("child", "child", null, [.. AvaloniaMeasurements.Geometry(button, host.Window).Where(m => m.Name is "box" or "size"),
            Measurement.Numeric("clicks", clicks, "count", definition: "child.clicks"),
            Measurement.Numeric("offset", c.Kind == "swipe" ? swipe.State.CurrentOffset : sheet.State.CurrentOffset,
                "dip", "local", "motion", "parent.offset", "Mounted parent state offset")])]));
        var point = button.TranslatePoint(new Point(20, 20), host.Window)!.Value;
        var end = c.NativeThumb ? point + (c.Kind == "sheet" ? new Vector(0, 60) : new Vector(c.Rtl ? -60 : 60, 0)) : point;
        Sample("initial");
        Advance(1000); host.Input("move", point); host.Input("down", point); host.Draw(); Sample("pressed");
        Advance(17000);
        if (c.NativeThumb) host.Input("move", end);
        else
        {
            if (c.Kind == "swipe") swipe.GesturesEnabled = false; else sheet.GesturesEnabled = false;
            writer.Record(17000, "mutation", "disable-parent");
        }
        host.Draw(); Sample("changed");
        Advance(32000); host.Draw(); Sample("committed");
        Advance(49000); host.Input("up", end); host.Draw(); Sample("released");
        Advance(80000); host.Draw(); Sample("settled");
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record WindowInsetsPaddingConfig(bool Rtl = false, bool Consumed = false, bool Union = false,
    bool Fractional = false, bool Excess = false, bool Dynamic = false);

[ParitySuite("window-insets-padding")]
public sealed class WindowInsetsPaddingCases
{
    public static IEnumerable<CaseData<WindowInsetsPaddingConfig>> Cases()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var consumed in new[] { false, true })
        foreach (var union in new[] { false, true })
        foreach (var fractional in new[] { false, true })
            yield return new($"static.{rtl}.{consumed}.{union}.{fractional}".ToLowerInvariant(), "Nested physical inset padding and consumption", new(rtl, consumed, union, fractional));
        foreach (var rtl in new[] { false, true })
        {
            yield return new($"excess.{rtl}".ToLowerInvariant(), "Insets larger than the viewport", new(rtl, Excess: true));
            yield return new($"dynamic.{rtl}".ToLowerInvariant(), "Insets, ancestor consumption and layout direction changes", new(rtl, true, true, Dynamic: true));
        }
    }
    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<WindowInsetsPaddingConfig>();
        await using var host = new AvaloniaHost(writer, 280, 240);
        var screen = new Scaffold { ContentWindowInsets = new Thickness(24, 36, 32, 48), Background = Brushes.Cyan };
        var marker = new Border { Background = Brushes.Red };
        var body = new FillBorder { Child = marker };
        body.Bind(Border.PaddingProperty, screen.GetObservable(Scaffold.ContentPaddingProperty)); screen.Content = body;
        var inner = new MaterialWindowInsetsPadding { Insets = new Thickness(16, 30, 20, 40), Child = screen };
        var outer = new MaterialWindowInsetsPadding { Insets = c.Excess ? new Thickness(180, 150) : c.Fractional
            ? new Thickness(8.375, 24.125, 12.625, 16.375) : new Thickness(8, 24, 12, 16), Child = inner };
        var root = new Border { Child = outer, Background = Brushes.White, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Consumed) MaterialWindowInsets.SetConsumePadding(root, new Thickness(4, 12, 8, 16));
        if (c.Union) MaterialWindowInsets.SetConsumeInsets(root, new Thickness(6, 18, 10, 24));
        host.Mount(root); host.Frame(0);
        void Sample(string id)
        {
            ProbePoint Point(string name, Control control) => new(name, name, null,
                AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray());
            writer.Submit(host.Snapshot(id, id, [Point("outer", outer), Point("inner", inner), Point("screen", screen), Point("marker", marker),
                new("padding", "padding", null, [Measurement.Numeric("start", screen.ContentPadding.Left, "dip", definition: "insets.scaffold-start"),
                    Measurement.Numeric("top", screen.ContentPadding.Top, "dip", definition: "insets.scaffold-top"),
                    Measurement.Numeric("end", screen.ContentPadding.Right, "dip", definition: "insets.scaffold-end"),
                    Measurement.Numeric("bottom", screen.ContentPadding.Bottom, "dip", definition: "insets.scaffold-bottom")])]));
        }
        Sample("initial"); if (!c.Dynamic) return;
        void Change(long time, string id, Action action) { host.AdvanceTo(time); action(); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "ancestor", () => MaterialWindowInsets.SetConsumeInsets(root, new Thickness(20, 4, 2, 50)));
        Change(17000, "outer", () => outer.Insets = new Thickness(30, 20, 40, 10));
        Change(33000, "inner", () => inner.Insets = new Thickness(5, 8, 12, 16));
        Change(49000, "direction", () => root.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
        Change(65000, "clear-consumption", () => { root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty); root.ClearValue(MaterialWindowInsets.ConsumePaddingProperty); });
    }
    private sealed class FillBorder : Border
    {
        protected override Size MeasureOverride(Size availableSize)
        { base.MeasureOverride(availableSize); return availableSize; }
    }
}

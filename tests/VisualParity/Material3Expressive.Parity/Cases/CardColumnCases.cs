using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record CardColumnConfig(double Height = 101, double Width = 101, double FirstWeight = 1,
    double SecondWeight = 2, double ThirdWeight = 3, bool Fill = true, bool AllUnfilled = false,
    double FixedHeight = 13, bool Rtl = false, bool Unbounded = false, double Minimum = 0, string Action = "none");

[ParitySuite("card-column")]
public sealed class CardColumnCases
{
    public static IEnumerable<CaseData<CardColumnConfig>> StaticCases()
    {
        foreach (var rtl in new[] { false, true })
        {
            var direction = rtl ? "rtl" : "ltr";
            yield return new($"static.ratio.{direction}", "Unequal weights and logical alignment", new(Rtl: rtl));
            yield return new($"static.fractional-pixel.{direction}", "Odd physical-pixel width and center rounding", new(Width: 101.25, Height: 100.25, Rtl: rtl));
            yield return new($"static.remainder-positive.{direction}", "Positive remainder goes to first weighted host", new(Height: 100, FirstWeight: 1, SecondWeight: 1, ThirdWeight: 1, FixedHeight: 0, Rtl: rtl));
            yield return new($"static.remainder-negative.{direction}", "Negative remainder removed from first weighted host", new(Height: 2, FirstWeight: 1, SecondWeight: 1, ThirdWeight: 1, FixedHeight: 0, Rtl: rtl));
            yield return new($"static.unfilled.{direction}", "Fill false leaves space unused", new(Fill: false, Rtl: rtl));
            yield return new($"static.all-unfilled.{direction}", "Column shrinks around unfilled weighted hosts", new(Fill: false, AllUnfilled: true, Rtl: rtl));
            yield return new($"static.unweighted-host.{direction}", "Optional weight uses preferred host size", new(FirstWeight: 0, Rtl: rtl));
            yield return new($"static.exhausted.{direction}", "Fixed children exhaust bounded height", new(FixedHeight: 130, Rtl: rtl));
            yield return new($"static.unbounded.{direction}", "No weighted space under unbounded zero-minimum constraints", new(Unbounded: true, Rtl: rtl));
            yield return new($"static.unbounded-minimum.{direction}", "Unbounded height uses explicit column minimum", new(Unbounded: true, Minimum: 91, Rtl: rtl));
            yield return new($"static.fractional-weight.{direction}", "Float weight arithmetic and remainder", new(FirstWeight: .2, SecondWeight: .3, ThirdWeight: .7, Rtl: rtl));
        }
    }
    public static IEnumerable<CaseData<CardColumnConfig>> DynamicCases()
    {
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "weights", "fill", "constraints", "direction" })
            yield return new($"dynamic.{action}.{(rtl ? "rtl" : "ltr")}", "Live weighted column updates", new(Rtl: rtl, Action: action));
    }
    [ParityScenario("static", Data = nameof(StaticCases))]
    public Task Static(CaptureWriter writer) => Capture(writer, false);
    [ParityScenario("dynamic", Data = nameof(DynamicCases))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer, true);

    private static async Task Capture(CaptureWriter writer, bool dynamic)
    {
        var c = writer.Metadata.ReadConfig<CardColumnConfig>();
        await using var host = new AvaloniaHost(writer, 320, 240);
        host.Window.FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        var column = new CardColumn { Width = c.Width, MinHeight = c.Minimum };
        if (!c.Unbounded) column.MaxHeight = c.Height;
        var colors = new[] { Color.Parse("#336699"), Color.Parse("#996633"), Color.Parse("#339966") };
        var weights = new[] { c.FirstWeight, c.SecondWeight, c.ThirdWeight };
        var content = Enumerable.Range(0, 3).Select(i => new Marker(20 + i * 10, 10 + i, colors[i])).ToArray();
        var items = Enumerable.Range(0, 3).Select(i => new CardColumnItem
        {
            Weight = weights[i] > 0 ? weights[i] : double.NaN,
            Fill = i == 0 ? c.Fill : !c.AllUnfilled, Alignment = (CardColumnAlignment)i, Child = content[i]
        }).ToArray();
        var fixedChild = new Marker(51, c.FixedHeight, Color.Parse("#888888"));
        column.Children.AddRange([items[0], fixedChild, items[1], items[2]]);
        var card = new Card { Content = column, CornerRadius = default, Background = Brushes.White };
        Control root = c.Unbounded ? new UnboundedHost { Child = card } : card;
        host.Mount(CaseDefaults.Center(root)); host.Frame(-1000); host.Frame(0);
        ProbePoint Geometry(string id, Control control) => new(id, id, null,
            AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray());
        void Sample(string id)
        {
            var points = new List<ProbePoint> { Geometry("card", card), Geometry("column", column), Geometry("fixed", fixedChild) };
            for (var i = 0; i < 3; i++) { points.Add(Geometry($"item-{i}", items[i])); points.Add(Geometry($"content-{i}", content[i])); }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial"); if (!dynamic) return;
        for (var step = 1; step <= 3; step++)
        {
            host.AdvanceTo(step * 16000 - 1000);
            switch (c.Action)
            {
                case "weights": items[0].Weight = step == 2 ? double.NaN : step * 2; break;
                case "fill": items[0].Fill = step == 2; items[1].Fill = step != 3; break;
                case "constraints": column.MaxHeight = step == 1 ? 77 : step == 2 ? 5 : 123; column.Width = step == 2 ? 73 : 101; break;
                case "direction": host.Window.FlowDirection = (step % 2 == 1) != c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight; break;
            }
            host.Frame(step * 16000); Sample($"frame-{step}");
        }
    }
    private sealed class Marker(double width, double height, Color color) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(width, availableSize.Width), Math.Min(height, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(new SolidColorBrush(color), new Rect(Bounds.Size));
    }
    private sealed class UnboundedHost : Decorator
    {
        protected override Size MeasureOverride(Size availableSize)
        {
            Child!.Measure(new Size(availableSize.Width, double.PositiveInfinity)); return Child.DesiredSize;
        }
    }
}

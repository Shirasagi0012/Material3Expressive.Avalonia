using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record TooltipContainerShapeConfig(bool Rich = false, string Shape = "cut", bool Caret = false,
    bool Rtl = false, string Position = "Above", bool Dynamic = false, bool SameWindingCaret = false);

[ParitySuite("tooltip-container-shape")]
public sealed class TooltipContainerShapeCases
{
    public static IEnumerable<CaseData<TooltipContainerShapeConfig>> Configurations()
    {
        foreach (var rich in new[] { false, true })
        foreach (var shape in new[] { "cut", "round", "hole", "winding-hole", "curve" })
        foreach (var caret in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"static.{rich}.{shape}.{caret}.{rtl}", new(rich, shape, caret, rtl));
        foreach (var rich in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var position in new[] { "Below", "Start", "End" })
            yield return Data($"direction.{rich}.{rtl}.{position}", new(rich, Caret: true, Rtl: rtl, Position: position));
        foreach (var rich in new[] { false, true })
        foreach (var caret in new[] { false, true })
            yield return Data($"dynamic.{rich}.{caret}", new(rich, Caret: caret, Dynamic: true));
        foreach (var rich in new[] { false, true })
            yield return Data($"caret-fill-rule.{rich}", new(rich, Caret: true, SameWindingCaret: true));
    }
    private static CaseData<TooltipContainerShapeConfig> Data(string id, TooltipContainerShapeConfig c) => new(id.ToLowerInvariant(), id, c);

    [ParityScenario("render", Data = nameof(Configurations))]
    public async Task Render(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TooltipContainerShapeConfig>();
        await using var host = new AvaloniaHost(writer, 256, 192);
        host.Window.Template = new FuncControlTemplate<Window>((owner, names) =>
        {
            var presenter = new ContentPresenter();
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            presenter.Bind(ContentPresenter.BackgroundProperty, owner.GetObservable(TemplatedControl.BackgroundProperty));
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            names.Register(layers.Name, layers); return layers;
        });
        CaseDefaults.SetThemeColors(host, false);
        var cut = new CutCornerShape { TopStart = new(18, RelativeUnit.Absolute), TopEnd = new(4, RelativeUnit.Absolute),
            BottomEnd = new(14, RelativeUnit.Absolute), BottomStart = new(2, RelativeUnit.Absolute) };
        MaterialShape shape = c.Shape switch
        {
            "round" => new RoundedCornerShape { TopStart = new(.35, RelativeUnit.Relative), TopEnd = new(4, RelativeUnit.Absolute),
                BottomEnd = new(.25, RelativeUnit.Relative), BottomStart = new(2, RelativeUnit.Absolute) },
            "hole" or "winding-hole" or "curve" => new CustomShape(c.Shape), _ => cut
        };
        ContentControl tooltip = c.Rich ? new RichTooltip() : new PlainTooltip();
        tooltip.Width = 96; tooltip.Height = 64;
        tooltip.Background = new SolidColorBrush(Color.Parse("#6750A4"));
        tooltip.Content = new OverflowInk();
        tooltip.SetValue(PlainTooltip.ShadowElevationProperty, 6d);
        tooltip.SetValue(PlainTooltip.ShapeProperty, shape);
        if (c.Caret) tooltip.SetValue(PlainTooltip.CaretSizeProperty, new Size(16, 8));
        if (c.SameWindingCaret) tooltip.SetValue(PlainTooltip.CaretShapeProperty, new TooltipCaretShape((_, _) =>
        {
            var path = PathGeometry.Parse("M -12,0 L 12,0 L 12,12 L -12,12 Z M -4,3 L 4,3 L 4,8 L -4,8 Z");
            path.FillRule = FillRule.EvenOdd;
            return path;
        }));
        using var state = new TooltipState(initialIsVisible: true, mutatorMutex: new());
        var box = new TooltipBox { Tooltip = tooltip, State = state, EnableUserInput = false,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            AnchorPosition = Enum.Parse<TooltipAnchorPosition>(c.Position), Content = new Border { Width = 24, Height = 24, Background = Brushes.Blue } };
        var canvas = new Canvas { Background = Brushes.White, Children = { box } };
        Canvas.SetLeft(box, 116); Canvas.SetTop(box, 80);
        host.Mount(canvas); host.Frame(-1000); host.Frame(0); Sample("initial");
        if (c.Dynamic)
        {
            Change(1000, "mutate", () => { cut.TopStart = new(30, RelativeUnit.Absolute); cut.BottomEnd = new(0, RelativeUnit.Absolute); });
            Change(2000, "resize", () => tooltip.Width = 120);
            Change(3000, "rtl", () => box.FlowDirection = FlowDirection.RightToLeft);
            Change(4000, "caret", () => tooltip.SetValue(PlainTooltip.CaretSizeProperty, c.Caret ? null : new Size(16, 8)));
            Change(5000, "replace", () => tooltip.SetValue(PlainTooltip.ShapeProperty, new CustomShape("curve")));
            Change(6000, "clear", () => tooltip.ClearValue(PlainTooltip.ShapeProperty));
        }
        void Change(long time, string id, Action change) { host.AdvanceTo(time); change(); host.Frame(time); Sample(id); }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
            [new("anchor", "anchor", null, AvaloniaMeasurements.Geometry(box, host.Window).Where(m => m.Name is "box" or "size").ToArray()),
             new("tooltip", "tooltip", null, AvaloniaMeasurements.Geometry(tooltip, host.Window).Where(m => m.Name is "box" or "size").ToArray())]));
    }
    private sealed class OverflowInk : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(64, availableSize.Width), Math.Min(16, availableSize.Height));
        public override void Render(DrawingContext context) => context.FillRectangle(new SolidColorBrush(Color.Parse("#FFD600")), new Rect(-200, -200, 500, 500));
    }
    private sealed class CustomShape(string kind) : MaterialShape
    {
        public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
        {
            var w = size.Width; var h = size.Height;
            var geometry = PathGeometry.Parse(kind switch
            {
                "hole" => FormattableString.Invariant($"M 0,0 L {w},0 L {w},{h} L 0,{h} Z M {w / 3},{h / 3} L {w * 2 / 3},{h / 3} L {w * 2 / 3},{h * 2 / 3} L {w / 3},{h * 2 / 3} Z"),
                "winding-hole" => FormattableString.Invariant($"M 0,0 L {w},0 L {w},{h} L 0,{h} Z M {w / 3},{h / 3} L {w / 3},{h * 2 / 3} L {w * 2 / 3},{h * 2 / 3} L {w * 2 / 3},{h / 3} Z"),
                _ => FormattableString.Invariant($"M 0,0 L {w},0 L {w},{h / 2} C {w},{h} {w / 2},{h} 0,{h / 2} Z")
            });
            geometry.FillRule = FillRule.EvenOdd;
            return geometry;
        }
    }
}

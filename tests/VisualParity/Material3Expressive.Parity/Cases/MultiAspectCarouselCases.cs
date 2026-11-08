using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record MultiAspectCarouselConfig(bool Vertical = false, bool Rtl = false,
    double ItemSize = 120, double CrossSize = 90, double Offset = 40, double Radius = 16,
    double Border = 0, string Action = "none", bool Grid = false, string Shape = "none");

[ParitySuite("multi-aspect-carousel")]
public sealed class MultiAspectCarouselCases
{
    public static IEnumerable<CaseData<MultiAspectCarouselConfig>> StaticConfigurations()
    {
        // Both axes and directions use different physical placement, including the upstream RTL mask behavior.
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var size in new[] { 45d, 75d, 90d, 120d, 160d, 200d })
            yield return Data($"static.{(vertical ? "vertical" : "horizontal")}.{(rtl ? "rtl" : "ltr")}.size-{size}", new(vertical, rtl, size));
        foreach (var offset in new[] { 0d, 20d, 119d, 128d, 250d })
            yield return Data($"static.offset-{offset}", new(Offset: offset));
        yield return Data("static.square", new(Radius: 0));
        yield return Data("static.border", new(Border: 2));
        yield return Data("static.oversized", new(ItemSize: 400));
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var size in new[] { 75d, 120d, 200d })
            yield return Data($"static.grid.{(vertical ? "vertical" : "horizontal")}.{(rtl ? "rtl" : "ltr")}.size-{size}", new(vertical, rtl, size, Grid: true));
    }

    public static IEnumerable<CaseData<MultiAspectCarouselConfig>> DynamicConfigurations()
    {
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "scroll", "resize", "shape" })
            yield return Data($"dynamic.{(vertical ? "vertical" : "horizontal")}.{(rtl ? "rtl" : "ltr")}.{action}", new(vertical, rtl, Action: action));
        foreach (var vertical in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return Data($"dynamic.grid.{(vertical ? "vertical" : "horizontal")}.{(rtl ? "rtl" : "ltr")}.scroll", new(vertical, rtl, Action: "scroll", Grid: true));
        yield return Data("dynamic.border.resize", new(Border: 2, Action: "resize"));
        yield return Data("dynamic.border.shape", new(Border: 2, Action: "shape"));
    }
    private static CaseData<MultiAspectCarouselConfig> Data(string id, MultiAspectCarouselConfig config) => new(id, id, config);
    [ParityScenario("static", Data = nameof(StaticConfigurations))]
    public Task Static(CaptureWriter writer) => Capture(writer);
    [ParityScenario("dynamic", Data = nameof(DynamicConfigurations))]
    public Task Dynamic(CaptureWriter writer) => Capture(writer);

    internal static async Task Capture(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<MultiAspectCarouselConfig>();
        await using var host = new AvaloniaHost(writer, 380, 380, new ManualTimeProvider());
        var items = Enumerable.Range(0, 8).Select(index => new MultiAspectCarouselItem
        {
            Width = config.Vertical ? config.CrossSize : config.ItemSize,
            Height = config.Vertical ? config.ItemSize : config.CrossSize,
            Orientation = config.Vertical ? Orientation.Vertical : Orientation.Horizontal,
            UseItemCrossAxisSize = config.Grid,
            CornerRadius = new(config.Radius), BorderBrush = Brushes.Black, BorderThickness = new(config.Border),
            Shape = config.Shape == "none" ? null : MaterialShapeCases.CreateShape(config.Shape),
            Content = new Marker(index),
        }).ToArray();
        var stack = new StackPanel { Orientation = config.Vertical ? Orientation.Vertical : Orientation.Horizontal, Spacing = 8 };
        var grid = new Grid { RowSpacing = 8, ColumnSpacing = 8 };
        if (config.Grid)
        {
            for (var row = 0; row < (config.Vertical ? 4 : 2); row++) grid.RowDefinitions.Add(new(new GridLength(config.Vertical ? config.ItemSize : config.CrossSize)));
            for (var col = 0; col < (config.Vertical ? 2 : 4); col++) grid.ColumnDefinitions.Add(new(new GridLength(config.Vertical ? config.CrossSize : config.ItemSize)));
            for (var i = 0; i < items.Length; i++)
            {
                Grid.SetRow(items[i], config.Vertical ? i / 2 : i % 2);
                Grid.SetColumn(items[i], config.Vertical ? i % 2 : i / 2);
                grid.Children.Add(items[i]);
            }
        }
        else foreach (var item in items) stack.Children.Add(item);
        var cross = config.Grid ? config.CrossSize * 2 + 8 : config.CrossSize;
        var viewer = new ScrollViewer
        {
            ClipToBounds = false,
            Width = config.Vertical ? cross : 300, Height = config.Vertical ? 300 : cross,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            FlowDirection = config.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Content = config.Grid ? grid : stack,
            HorizontalScrollBarVisibility = config.Vertical ? ScrollBarVisibility.Disabled : ScrollBarVisibility.Hidden,
            VerticalScrollBarVisibility = config.Vertical ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled,
            // Native physical presenter: the component observes actual UI layout, not compositor-only offsets.
            Template = new FuncControlTemplate<ScrollViewer>((owner, scope) => new ScrollContentPresenter
            {
                // Foundation ClipScrollableContainer clips the scrolling axis and allows 30dp
                // cross-axis elevation overflow. A rectangular clip hid oversized Shape paths.
                Name = "PART_ContentPresenter", Content = owner.Content, ClipToBounds = false,
                Clip = new RectangleGeometry(config.Vertical ? new Rect(-30, 0, cross + 60, 300) : new Rect(0, -30, 300, cross + 60)),
                CanHorizontallyScroll = !config.Vertical, CanVerticallyScroll = config.Vertical,
                [!ScrollContentPresenter.OffsetProperty] = owner[!ScrollViewer.OffsetProperty],
            }.RegisterInNameScope(scope)),
        };
        host.Mount(new Grid { Margin = new Thickness(24), Children = { viewer } }); host.Frame(0);
        void Scroll(double offset) => viewer.Offset = config.Vertical ? new(0, offset) : new(offset, 0);
        Scroll(config.Offset); host.Draw();
        void Sample(string id)
        {
            var points = new List<ProbePoint> { new("viewport", "viewport", null,
                AvaloniaMeasurements.Geometry(viewer, host.Window).Where(x => x.Name is "box" or "size").ToArray()) };
            for (var i = 0; i < items.Length; i++)
            {
                var info = items[i].ActualDrawInfo;
                // The lazy reference has no item info for an item fully outside its viewport.
                if (info.Size == 0) continue;
                points.Add(new($"item-{i}", "item", "viewport", [
                    Measurement.Numeric("maskStart", info.MaskStart, "dip", definition: "carousel.mask.start"),
                    Measurement.Numeric("maskEnd", info.MaskEnd, "dip", definition: "carousel.mask.end"),
                    Measurement.Numeric("parallax", info.Parallax, "dip", definition: "carousel.content.parallax"),
                    Measurement.Numeric("minSize", info.MinSize, "dip", definition: "carousel.mask.minimum"),
                ]));
            }
            writer.Submit(host.Snapshot(id, id, points.ToArray()));
        }
        Sample("initial");
        if (config.Action == "none") return;
        foreach (var (time, value) in new[] { (16000L, 20d), (32000L, 80d), (48000L, 140d), (64000L, 40d), (80000L, 0d) })
        {
            host.AdvanceTo(time);
            if (config.Action == "scroll") Scroll(value);
            if (config.Action == "resize") foreach (var item in items)
            { if (config.Vertical) item.Height = 100 + value; else item.Width = 100 + value; }
            if (config.Action == "shape") foreach (var item in items) item.CornerRadius = new(value / 2);
            if (config.Action == "replace-shape") foreach (var item in items) item.Shape = MaterialShapeCases.CreateShape(value > 40 ? "round-percent" : "percent");
            host.Draw(); host.Frame(time); Sample($"time-{time}");
        }
    }

    private sealed class Marker(int index) : Control
    {
        // Compose Canvas drawing coordinates are physical, independent of LocalLayoutDirection.
        protected override bool BypassFlowDirectionPolicies => true;
        public override void Render(DrawingContext context)
        {
            var color = new[] { Brushes.CornflowerBlue, Brushes.Coral, Brushes.DarkSeaGreen }[index % 3];
            context.FillRectangle(color, new Rect(Bounds.Size));
            context.FillRectangle(Brushes.Navy, new Rect(0, 0, Bounds.Width / 3, Bounds.Height / 2));
            context.FillRectangle(Brushes.Gold, new Rect(Bounds.Width * .6, Bounds.Height * .5, Bounds.Width * .2, Bounds.Height * .4));
        }
    }
}

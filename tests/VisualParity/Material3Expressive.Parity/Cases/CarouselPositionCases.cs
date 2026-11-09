using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;

public sealed record CarouselPositionConfig(string Variant, bool Rtl, string Action, int Item = 3,
    float Fraction = .25f, double Width = 320, double ItemWidth = 160, double Spacing = 8);

[ParitySuite("carousel-position")]
public sealed class CarouselPositionCases
{
    public static IEnumerable<CaseData<CarouselPositionConfig>> Cases()
    {
        foreach (var variant in Enum.GetNames<CarouselVariant>())
        foreach (var rtl in new[] { false, true })
        foreach (var action in new[] { "same-positive", "same-negative", "viewport", "item-width", "shrink", "shrink-negative", "empty", "interrupt", "start-bound", "end-bound", "fractional" })
        {
            var config = new CarouselPositionConfig(variant, rtl, action,
                Item: action.StartsWith("shrink") ? 6 : action == "start-bound" ? 0 : action == "end-bound" ? 7 : action == "interrupt" ? 2 : 3,
                Fraction: action is "same-negative" or "shrink-negative" ? -.25f : action == "start-bound" ? -.5f : action is "end-bound" or "fractional" ? .5f : .25f,
                Width: action == "fractional" ? 319 : 320, ItemWidth: action == "fractional" ? 159.3 : 160, Spacing: action == "fractional" ? 7.5 : 8);
            yield return new($"{variant.ToLowerInvariant()}.{(rtl ? "rtl" : "ltr")}.{action}", "Save and restore the logical carousel position", config);
        }
    }

    [ParityScenario("restore", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<CarouselPositionConfig>();
        await using var host = new AvaloniaHost(writer, 400, 180, new ManualTimeProvider());
        HorizontalCarousel Create(double width, double itemWidth, int count)
        {
            var result = new HorizontalCarousel { Width = width, Height = 120, ItemWidth = itemWidth,
                Variant = Enum.Parse<CarouselVariant>(c.Variant), ItemSpacing = c.Spacing,
                HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
                FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
            Populate(result, count);
            return result;
        }
        static void Populate(HorizontalCarousel target, int count)
        {
            for (var i = 0; i < count; i++) target.Items.Add(new CarouselItem { CornerRadius = new(16), Content = new Marker(i) });
        }
        var carousel = Create(c.Width, c.ItemWidth, 8);
        carousel.RestorePosition(new(c.Item, c.Fraction));
        var root = new Grid { Margin = new Thickness(24), Children = { carousel } };
        host.Mount(root); host.Frame(0);
        // Exercise the public serializable value, not a reconstructed offset from the reference.
        var saved = JsonSerializer.Deserialize<CarouselPosition>(JsonSerializer.Serialize(carousel.CapturePosition()));
        void Sample(string id)
        {
            var captured = carousel.CapturePosition();
            writer.Submit(host.Snapshot(id, id,
            [
                new("position", "carousel", null,
                [
                    Measurement.Numeric("item", carousel.CurrentItem, "index", definition: "carousel.currentItem"),
                    Measurement.Numeric("fraction", carousel.CurrentItemOffsetFraction, "ratio", definition: "carousel.currentItemOffsetFraction"),
                    Measurement.Flag("scrolling", carousel.IsScrollInProgress, definition: "carousel.isScrollInProgress")
                ]),
                new("captured", "position", null,
                [
                    Measurement.Numeric("item", captured.Item, "index", definition: "carousel.savedItem"),
                    Measurement.Numeric("fraction", captured.OffsetFraction, "ratio", definition: "carousel.savedOffsetFraction")
                ])
            ], true));
        }
        Sample("initial");
        if (c.Action == "interrupt")
        {
            Task? animation = null;
            for (long time = 16000; time <= 320000; time += 16000)
            {
                host.AdvanceTo(time);
                if (time == 16000) animation = carousel.AnimateScrollToItemAsync(7);
                if (time == 80000) { carousel.RestorePosition(saved); host.Draw(); }
                host.Frame(time);
                if (time <= 96000 || time is 160000 or 320000) Sample($"time-{time}");
            }
            if (animation is not null) await animation;
            return;
        }
        host.AdvanceTo(16000);
        carousel = Create(c.Action == "viewport" ? 240 : c.Action == "fractional" ? 273 : c.Width,
            c.Action == "item-width" ? 190 : c.Action == "fractional" ? 137.7 : c.ItemWidth,
            c.Action == "empty" ? 0 : c.Action.StartsWith("shrink") ? 3 : 8);
        carousel.RestorePosition(saved);
        root.Children.Clear(); root.Children.Add(carousel);
        host.Draw(); host.Frame(16000); Sample("restored");
        if (c.Action == "empty")
        {
            host.AdvanceTo(48000); Populate(carousel, 8); host.Draw(); host.Frame(48000); Sample("populated");
        }
        host.Frame(160000); Sample("settled");
    }

    private sealed class Marker(int index) : Control
    {
        protected override bool BypassFlowDirectionPolicies => true;
        public override void Render(DrawingContext context)
        {
            context.FillRectangle(new[] { Brushes.CornflowerBlue, Brushes.Coral, Brushes.DarkSeaGreen }[index % 3], new Rect(Bounds.Size));
            context.FillRectangle(Brushes.Navy, new Rect(0, 0, Bounds.Width / 3, Bounds.Height / 2));
            context.FillRectangle(Brushes.Gold, new Rect(Bounds.Width * .6, Bounds.Height * .5, Bounds.Width * .2, Bounds.Height * .4));
        }
    }
}

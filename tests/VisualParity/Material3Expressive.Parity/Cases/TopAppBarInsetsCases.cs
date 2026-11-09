using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record TopAppBarInsetsConfig(bool TwoRows, bool Rtl = false, bool Consumed = false, bool Nested = true, bool Dynamic = false, bool Search = false);

[ParitySuite("top-app-bar-insets")]
public sealed class TopAppBarInsetsCases
{
    public static IEnumerable<CaseData<TopAppBarInsetsConfig>> Cases()
    {
        foreach (var twoRows in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        {
            foreach (var consumed in new[] { false, true })
            foreach (var nested in new[] { false, true })
                yield return new($"static.{twoRows}.{rtl}.{consumed}.{nested}".ToLowerInvariant(), "Top app bar independent row inset consumption", new(twoRows, rtl, consumed, nested));
            yield return new($"dynamic.{twoRows}.{rtl}".ToLowerInvariant(), "Insets update through actual title and action slots", new(twoRows, rtl, true, true, true));
        }
        foreach (var rtl in new[] { false, true })
        {
            foreach (var consumed in new[] { false, true })
            foreach (var nested in new[] { false, true })
                yield return new($"search.{rtl}.{consumed}.{nested}".ToLowerInvariant(), "Search top bar inset consumption", new(false, rtl, consumed, nested, Search: true));
            yield return new($"search.dynamic.{rtl}".ToLowerInvariant(), "Search slots observe changing inset consumption", new(false, rtl, true, true, true, true));
        }
    }

    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<TopAppBarInsetsConfig>();
        await using var host = new AvaloniaHost(writer, 400, 280);
        CaseDefaults.SetThemeColors(host, false);
        var insets = new Thickness(13, 7, 5, 11);
        var markers = new Dictionary<string, Control>();
        Control Slot(string name, IBrush color)
        {
            var marker = new Border { Width = 24, Height = 24, Background = color }; markers[name] = marker;
            // The reference slot is a Box(propagateMinConstraints=false), including
            // when no Insets modifier is applied. Keep its TopStart placement when
            // SearchBar's minimum width makes the slot wider than its marker.
            var box = new SlotBox { Child = marker };
            return c.Nested ? new MaterialWindowInsetsPadding { Insets = insets, Child = box } : box;
        }
        ItemsControl bar;
        if (c.Search)
        {
            var search = new AppBarWithSearch { WindowInsets = insets, NavigationIcon = Slot("navigation", Brushes.Black),
                InputField = Slot("title", Brushes.Magenta) };
            bar = search;
        }
        else
        {
            TopAppBar top = c.TwoRows ? new TwoRowsTopAppBar() : new TopAppBar();
            top.WindowInsets = insets; top.NavigationIcon = Slot("navigation", Brushes.Black); top.Title = Slot("title", Brushes.Magenta);
            if (c.TwoRows) top.CollapsedTitle = Slot("collapsed", Brushes.Blue);
            bar = top;
        }
        bar.Background = Brushes.Cyan; bar.Items.Add(Slot("action", Brushes.Red));
        var root = new Border { Child = bar, Background = Brushes.White, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Consumed) MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            ProbePoint Point(string name, Control control) => new(name, name, null, AvaloniaMeasurements.Geometry(control, host.Window)
                .Where(m => m.Name is "box" or "size").ToArray());
            writer.Submit(host.Snapshot(id, id, [Point("bar", bar), .. markers.Select(p => Point(p.Key, p.Value))]));
        }
        Sample("initial"); if (!c.Dynamic) return;
        void Change(long time, string id, Action action) { host.AdvanceTo(time); action(); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "fully-consumed", () => MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)));
        Change(17000, "larger-insets", () => {
            var value = new Thickness(40, 35, 32, 31);
            if (bar is AppBarWithSearch search) search.WindowInsets = value; else ((TopAppBar)bar).WindowInsets = value;
        });
        Change(33000, "clear-parent", () => root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty));
        Change(49000, "direction", () => root.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
    }

    private sealed class SlotBox : Decorator
    {
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is { } child) child.Arrange(new Rect(default(Point), child.DesiredSize.Constrain(finalSize)));
            return finalSize;
        }
    }
}

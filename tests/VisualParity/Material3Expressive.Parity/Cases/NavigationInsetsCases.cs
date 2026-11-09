using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record NavigationInsetsConfig(string Kind = "bar", bool Rtl = false, bool Consumed = false, bool Nested = false, bool Dynamic = false);

[ParitySuite("navigation-insets")]
public sealed class NavigationInsetsCases
{
    public static IEnumerable<CaseData<NavigationInsetsConfig>> Cases()
    {
        foreach (var kind in new[] { "bar", "short", "rail", "wide", "expanded", "bottom", "flexible" })
        foreach (var rtl in new[] { false, true })
        {
            foreach (var consumed in new[] { false, true })
            foreach (var nested in new[] { false, true })
                yield return new($"static.{kind}.{rtl}.{consumed}.{nested}".ToLowerInvariant(), "Navigation inset exclusion and logical content consumption", new(kind, rtl, consumed, nested));
            yield return new($"dynamic.{kind}.{rtl}".ToLowerInvariant(), "Change parent consumption and component insets", new(kind, rtl, true, true, true));
        }
    }
    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<NavigationInsetsConfig>();
        await using var host = new AvaloniaHost(writer, 360, 280);
        CaseDefaults.SetThemeColors(host, false);
        var insets = new Thickness(13, 7, 5, 11);
        var marker = new Border { Width = 24, Height = 24, Background = Brushes.Black };
        Control icon = c.Nested ? new MaterialWindowInsetsPadding { Insets = insets, Child = marker } : marker;
        Control item;
        ItemsControl nav;
        if (c.Kind is "bottom" or "flexible") {
            item = icon; var bar = c.Kind == "bottom" ? new BottomAppBar() : new FlexibleBottomAppBar();
            bar.WindowInsets = insets; nav = bar;
        }
        else if (c.Kind is "wide" or "expanded") {
            item = new WideNavigationRailItem { Icon = icon };
            nav = new WideNavigationRail { WindowInsets = insets,
                State = new WideNavigationRailState(c.Kind == "expanded" ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed) };
        }
        else if (c.Kind == "rail") { item = new NavigationRailItem { Icon = icon }; nav = new NavigationRail { WindowInsets = insets }; }
        else if (c.Kind == "short") { item = new ShortNavigationBarItem { Icon = icon }; nav = new ShortNavigationBar { WindowInsets = insets }; }
        else { item = new NavigationBarItem { Icon = icon }; nav = new NavigationBar { WindowInsets = insets }; }
        nav.Items.Add(item); nav.Background = Brushes.Cyan;
        if (c.Kind is "bar" or "short" or "bottom" or "flexible") nav.VerticalAlignment = VerticalAlignment.Top;
        var root = new Border { Child = nav, Background = Brushes.White, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Consumed) MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            ProbePoint Point(string name, Control control) => new(name, name, null, AvaloniaMeasurements.Geometry(control, host.Window)
                .Where(m => m.Name is "box" or "size").ToArray());
            writer.Submit(host.Snapshot(id, id, [Point("container", nav), Point("item", item), Point("icon", marker)]));
        }
        Sample("initial"); if (!c.Dynamic) return;
        void Change(long time, string id, Action action) { host.AdvanceTo(time); action(); host.Draw(); host.Frame(time); Sample(id); }
        Change(1000, "fully-consumed", () => MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)));
        Change(17000, "larger-insets", () => {
            var value = new Thickness(40, 35, 32, 31);
            if (nav is BottomAppBar bottom) bottom.WindowInsets = value;
            else if (nav is WideNavigationRail wide) wide.WindowInsets = value;
            else if (nav is NavigationRail rail) rail.WindowInsets = value;
            else if (nav is ShortNavigationBar shortBar) shortBar.WindowInsets = value;
            else ((NavigationBar)nav).WindowInsets = value;
        });
        Change(33000, "clear-parent", () => root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty));
        Change(49000, "direction", () => root.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
    }
}

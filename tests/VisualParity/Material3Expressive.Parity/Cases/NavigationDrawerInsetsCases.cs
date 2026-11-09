using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Parity;

public sealed record NavigationDrawerInsetsConfig(string Kind = "permanent", bool Rtl = false,
    string Consumption = "parent", bool Nested = true, bool Dynamic = false);

[ParitySuite("navigation-drawer-insets")]
public sealed class NavigationDrawerInsetsCases
{
    public static IEnumerable<CaseData<NavigationDrawerInsetsConfig>> Cases()
    {
        foreach (var kind in new[] { "permanent", "dismissible", "modal" })
        foreach (var rtl in new[] { false, true })
        {
            var direction = rtl ? "rtl" : "ltr";
            foreach (var consumption in new[] { "none", "parent", "mixed", "self" })
                yield return new($"static.{kind}.{consumption}.{direction}", "Sheet excludes consumed insets and publishes them to nested content",
                    new(kind, rtl, consumption));
            yield return new($"static.{kind}.plain.{direction}", "Sheet excludes ancestor insets without a nested inset consumer",
                new(kind, rtl, Nested: false));
            yield return new($"dynamic.{kind}.{direction}", "Change ancestor, requested insets, direction and actual content",
                new(kind, rtl, Dynamic: true));
        }
    }

    [ParityScenario("capture", Data = nameof(Cases))]
    public async Task Capture(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<NavigationDrawerInsetsConfig>();
        await using var host = new AvaloniaHost(writer, 520, 320);
        CaseDefaults.SetThemeColors(host, false);
        var requested = new Thickness(13, 7, 5, 11);
        var state = new DrawerState(DrawerValue.Open);
        DrawerSheet sheet = c.Kind switch
        {
            "modal" => new ModalDrawerSheet { State = state },
            "dismissible" => new DismissibleDrawerSheet { State = state },
            _ => new PermanentDrawerSheet()
        };
        sheet.Width = 280; sheet.WindowInsets = requested; sheet.Background = Brushes.Cyan; sheet.TonalElevation = 0;
        if (c.Consumption == "self")
        {
            MaterialWindowInsets.SetConsumePadding(sheet, new Thickness(3, 5, 9, 7));
            MaterialWindowInsets.SetConsumeInsets(sheet, new Thickness(12, 8, 15, 10));
        }
        var marker = new Border { Height = 56, Background = Brushes.Black };
        Control content = c.Nested ? new MaterialWindowInsetsPadding { Insets = requested, Child = marker } : marker;
        sheet.Content = content;
        NavigationDrawer drawer = c.Kind switch
        {
            "modal" => new ModalNavigationDrawer { State = state },
            "dismissible" => new DismissibleNavigationDrawer { State = state },
            _ => new PermanentNavigationDrawer()
        };
        drawer.DrawerContent = sheet; drawer.Content = new Border();
        var root = new Border { Background = Brushes.White, Child = drawer,
            FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        if (c.Consumption != "none") MaterialWindowInsets.SetConsumeInsets(root, new Thickness(4, 3, 2, 6));
        if (c.Consumption == "mixed") MaterialWindowInsets.SetConsumePadding(root, new Thickness(3, 2, 7, 1));
        host.Mount(root); host.Frame(-1000); host.Frame(0);
        void Sample(string id)
        {
            ProbePoint Point(string name, Control control) => new(name, name, null,
                AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray());
            writer.Submit(host.Snapshot(id, id, [Point("sheet", sheet), Point("content", content), Point("marker", marker)]));
        }
        Sample("initial"); if (!c.Dynamic) return;
        void Change(long time, string id, Action action)
        {
            host.AdvanceTo(time); action(); host.Draw(); host.Frame(time); Sample(id);
        }
        Change(1000, "fully-consumed", () => MaterialWindowInsets.SetConsumeInsets(root, new Thickness(30)));
        Change(17000, "larger-insets", () => sheet.WindowInsets = new Thickness(40, 35, 32, 31));
        Change(33000, "clear-parent", () => root.ClearValue(MaterialWindowInsets.ConsumeInsetsProperty));
        Change(49000, "direction", () => root.FlowDirection = c.Rtl ? FlowDirection.LeftToRight : FlowDirection.RightToLeft);
        Change(65000, "replace-content", () =>
        {
            marker = new Border { Height = 56, Background = Brushes.Black };
            content = new MaterialWindowInsetsPadding { Insets = new Thickness(48, 38, 50, 42), Child = marker };
            sheet.Content = content;
        });
    }
}

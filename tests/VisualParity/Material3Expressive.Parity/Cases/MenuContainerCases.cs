using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;

namespace Material3Expressive.Parity;
public sealed record MenuContainerConfig(bool Vibrant = false, bool Dark = false, bool Rtl = false, bool Divider = false, double Width = 200, bool LegacySeparator = false, bool AutoWidth = false);

[ParitySuite("menu-container")]
public sealed class MenuContainerCases
{
    public static IEnumerable<CaseData<MenuContainerConfig>> Configurations()
    {
        foreach (var vibrant in new[] { false, true })
        foreach (var dark in new[] { false, true })
        foreach (var rtl in new[] { false, true })
            yield return new($"static.{(vibrant ? "vibrant" : "standard")}.{(dark ? "dark" : "light")}.{(rtl ? "rtl" : "ltr")}", "Native Vertical presenter", new(vibrant, dark, rtl));
        yield return new("layout.divider", "Native separator", new(Divider: true));
        yield return new("layout.legacy-divider", "Native MenuItem separator", new(Divider: true, LegacySeparator: true));
        yield return new("layout.minimum", "Automatic item minimum", new(Width: 120));
        yield return new("layout.intrinsic-divider", "Separator in an automatic width menu", new(Divider: true, AutoWidth: true));
        yield return new("layout.intrinsic-legacy-divider", "MenuItem separator in an automatic width menu", new(Divider: true, LegacySeparator: true, AutoWidth: true));
    }

    [ParityScenario("static", Data = nameof(Configurations))]
    public async Task Static(CaptureWriter writer)
    {
        var config = writer.Metadata.ReadConfig<MenuContainerConfig>();
        await using var host = new AvaloniaHost(writer, 320, 200);
        MenuCases.SetColors(host, config.Dark);
        if (config.Rtl) host.Window.FlowDirection = FlowDirection.RightToLeft;
        var markers = new[] { new Marker(), new Marker() };
        var presenter = new MenuFlyoutPresenter();
        if (!config.AutoWidth) presenter.Width = config.Width;
        MenuAppearance.SetColorStyle(presenter, config.Vibrant ? MenuColorStyle.Vibrant : MenuColorStyle.Standard);
        presenter.Items.Add(new MenuItem { Header = markers[0] });
        if (config.Divider) presenter.Items.Add(config.LegacySeparator ? new MenuItem { Header = "-" } : new Separator());
        presenter.Items.Add(new MenuItem { Header = markers[1], IsChecked = true });
        host.Mount(CaseDefaults.Center(presenter)); host.Frame(-1000); host.Frame(0);
        writer.Submit(host.Snapshot("initial", "initial",
            markers.Select((marker, i) => new ProbePoint($"content-{i}", "content", null,
                AvaloniaMeasurements.Geometry(marker, host.Window).Where(m => m.Name is "box" or "size").ToArray())).ToArray()));
    }
    private sealed class Marker : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new(Math.Min(32, availableSize.Width), Math.Min(16, availableSize.Height));
        public override void Render(DrawingContext context) { if (TextElement.GetForeground(this) is { } brush) context.FillRectangle(brush, new Rect(Bounds.Size)); }
    }
}


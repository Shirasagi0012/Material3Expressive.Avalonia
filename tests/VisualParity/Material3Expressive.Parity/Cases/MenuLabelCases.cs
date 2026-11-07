using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Parity;

public sealed record MenuLabelConfig(bool Group = true, bool Rtl = false, bool Precision = false,
    double ContentWidth = 24, double ContentHeight = 12, double AvailableWidth = 360,
    double Width = 0, double Height = 0, bool CustomPadding = false, bool Dynamic = false, bool InMenu = false);

[ParitySuite("menu-label")]
public sealed class MenuLabelCases
{
    public static IEnumerable<CaseData<MenuLabelConfig>> Configurations()
    {
        foreach (var group in new[] { false, true })
        foreach (var rtl in new[] { false, true })
        {
            var prefix = $"{(group ? "group" : "trailing")}.{(rtl ? "rtl" : "ltr")}";
            yield return new(prefix, prefix, new(group, rtl));
            yield return new(prefix + ".precision", prefix, new(group, rtl, Precision: true));
            yield return new(prefix + ".custom", prefix, new(group, rtl, Precision: true, CustomPadding: true));
            yield return new(prefix + ".narrow", prefix, new(group, rtl, AvailableWidth: 28));
            yield return new(prefix + ".wide-content", prefix, new(group, rtl, ContentWidth: 320));
            yield return new(prefix + ".explicit", prefix, new(group, rtl, Width: 320, Height: 20));
            yield return new(prefix + ".tall", prefix, new(group, rtl, ContentHeight: 48));
            if (!group)
            {
                yield return new(prefix + ".dynamic", prefix, new(group, rtl, Dynamic: true));
                yield return new(prefix + ".menu", prefix, new(group, rtl, Dynamic: true, InMenu: true));
            }
        }
    }

    [ParityScenario("layout", Data = nameof(Configurations))]
    public async Task Layout(CaptureWriter writer)
    {
        var c = writer.Metadata.ReadConfig<MenuLabelConfig>();
        await using var host = new AvaloniaHost(writer, 400, 200);
        CaseDefaults.SetThemeColors(host, false);
        var content = new MenuMarker(new Size(c.ContentWidth, c.ContentHeight));
        ContentControl label = c.Group ? new DropdownMenuGroupLabel() : new DropdownMenuItemTrailingLabel();
        label.Content = content;
        if (c.Width > 0) label.Width = c.Width;
        if (c.Height > 0) label.Height = c.Height;
        if (c.CustomPadding) label.Padding = new(3, 5, 9, 7);
        Control component = label;
        if (c.InMenu)
            component = new SelectableDropdownMenuItem { Content = new MenuMarker(new Size(48, 16)), TrailingContent = label,
                Width = 220, HorizontalAlignment = HorizontalAlignment.Left };
        var allocation = new Grid { Width = c.AvailableWidth, HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center, FlowDirection = c.Rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
            Children = { component } };
        TextElement.SetForeground(allocation, Brushes.Navy);
        MaterialPrecisionPointer.SetIsEnabled(allocation, c.Precision);
        host.Mount(CaseDefaults.Center(allocation)); host.Frame(-1000); host.Frame(0);
        Sample("initial");
        if (c.Dynamic)
        {
            host.AdvanceTo(1000); MaterialPrecisionPointer.SetIsEnabled(allocation, true); host.Frame(1000); Sample("precision-on");
            host.AdvanceTo(2000); MaterialPrecisionPointer.SetIsEnabled(allocation, false); host.Frame(2000); Sample("precision-off");
        }
        void Sample(string id) => writer.Submit(host.Snapshot(id, id,
        [new("label", "label", null, Geometry(label)), new("content", "content", "label", Geometry(content))]));
        Measurement[] Geometry(Control control) => AvaloniaMeasurements.Geometry(control, host.Window).Where(m => m.Name is "box" or "size").ToArray();
    }

    private sealed class MenuMarker(Size desiredSize) : Control
    {
        static MenuMarker() => AffectsRender<MenuMarker>(TextElement.ForegroundProperty);
        protected override Size MeasureOverride(Size availableSize) => desiredSize.Constrain(availableSize);
        public override void Render(DrawingContext context)
        {
            if (TextElement.GetForeground(this) is { } brush) context.FillRectangle(brush, new Rect(Bounds.Size));
        }
    }
}

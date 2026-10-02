using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace Material3Expressive.Components;

// NavigationRail.kt's fillMaxHeight -> widthIn(min=80) -> vertical padding -> Column.
internal sealed class NavigationRailLayout : Panel
{
    public static readonly StyledProperty<double> ContainerMinWidthProperty =
        AvaloniaProperty.Register<NavigationRailLayout, double>(nameof(ContainerMinWidth), 80);
    public double ContainerMinWidth { get => GetValue(ContainerMinWidthProperty); set => SetValue(ContainerMinWidthProperty, value); }
    static NavigationRailLayout() => AffectsMeasure<NavigationRailLayout>(ContainerMinWidthProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var header = (ContentPresenter)Children[0];
        var content = Children[1];
        var gap = header.Content is null ? 0 : 16; // Column gaps 4 + Spacer(8) + gap 4.
        var space = new Size(availableSize.Width, Math.Max(0, availableSize.Height - 8));
        header.Measure(space);
        content.Measure(new Size(space.Width, Math.Max(0, space.Height - header.DesiredSize.Height - gap)));
        var width = Math.Max(Math.Min(ContainerMinWidth, availableSize.Width), Math.Max(header.DesiredSize.Width, content.DesiredSize.Width));
        return new Size(width, double.IsFinite(availableSize.Height) ? availableSize.Height : header.DesiredSize.Height + gap + content.DesiredSize.Height + 8);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var header = (ContentPresenter)Children[0];
        var content = Children[1];
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double width) => Math.Truncate((finalSize.Width - width) * scale / 2) / scale;
        header.Arrange(new Rect(Center(header.DesiredSize.Width), 4, header.DesiredSize.Width, header.DesiredSize.Height));
        var top = header.Content is null ? 4 : 4 + header.DesiredSize.Height + 16;
        content.Arrange(new Rect(Center(content.DesiredSize.Width), top, content.DesiredSize.Width, Math.Max(0, finalSize.Height - top - 4)));
        return finalSize;
    }
}

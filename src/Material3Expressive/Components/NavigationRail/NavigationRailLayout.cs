using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace Material3Expressive.Components;

// NavigationRail.kt's fillMaxHeight -> widthIn(min=80) -> vertical padding -> Column.
internal sealed class NavigationRailLayout : Panel
{
    public static readonly StyledProperty<double> ContainerMinWidthProperty =
        AvaloniaProperty.Register<NavigationRailLayout, double>(nameof(ContainerMinWidth));
    public double ContainerMinWidth { get => GetValue(ContainerMinWidthProperty); set => SetValue(ContainerMinWidthProperty, value); }
    public static readonly StyledProperty<double> VerticalPaddingProperty = AvaloniaProperty.Register<NavigationRailLayout, double>(nameof(VerticalPadding), 4d);
    public double VerticalPadding { get => GetValue(VerticalPaddingProperty); set => SetValue(VerticalPaddingProperty, value); }
    public static readonly StyledProperty<double> HeaderItemSpacingProperty = AvaloniaProperty.Register<NavigationRailLayout, double>(nameof(HeaderItemSpacing), 16d);
    public double HeaderItemSpacing { get => GetValue(HeaderItemSpacingProperty); set => SetValue(HeaderItemSpacingProperty, value); }
    static NavigationRailLayout() => AffectsMeasure<NavigationRailLayout>(ContainerMinWidthProperty, VerticalPaddingProperty, HeaderItemSpacingProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        var header = (ContentPresenter)Children[0];
        var content = Children[1];
        var gap = header.Content is null ? 0 : HeaderItemSpacing;
        var space = new Size(availableSize.Width, Math.Max(0, availableSize.Height - VerticalPadding * 2));
        header.Measure(space);
        content.Measure(new Size(space.Width, Math.Max(0, space.Height - header.DesiredSize.Height - gap)));
        var width = Math.Max(Math.Min(ContainerMinWidth, availableSize.Width), Math.Max(header.DesiredSize.Width, content.DesiredSize.Width));
        return new Size(width, double.IsFinite(availableSize.Height) ? availableSize.Height : header.DesiredSize.Height + gap + content.DesiredSize.Height + VerticalPadding * 2);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var header = (ContentPresenter)Children[0];
        var content = Children[1];
        var scale = LayoutHelper.GetLayoutScale(this);
        double Center(double width) => Math.Truncate((finalSize.Width - width) * scale / 2) / scale;
        header.Arrange(new Rect(Center(header.DesiredSize.Width), VerticalPadding, header.DesiredSize.Width, header.DesiredSize.Height));
        var top = header.Content is null ? VerticalPadding : VerticalPadding + header.DesiredSize.Height + HeaderItemSpacing;
        content.Arrange(new Rect(Center(content.DesiredSize.Width), top, content.DesiredSize.Width, Math.Max(0, finalSize.Height - top - VerticalPadding)));
        return finalSize;
    }
}

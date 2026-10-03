using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Row's optional fixed icon box, spacing, and remaining-width content measurement.
internal sealed class ButtonContentRow : Panel
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<ButtonContentRow, double>(nameof(IconSize));
    public static readonly StyledProperty<double> IconSpacingProperty = AvaloniaProperty.Register<ButtonContentRow, double>(nameof(IconSpacing));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public double IconSpacing { get => GetValue(IconSpacingProperty); set => SetValue(IconSpacingProperty, value); }
    private Size _icon, _content;
    static ButtonContentRow() => AffectsMeasure<ButtonContentRow>(IconSizeProperty, IconSpacingProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        var icon = (Avalonia.Controls.Presenters.ContentPresenter)Children[0];
        var hasIcon = icon.Content is not null;
        _icon = hasIcon ? new Size(Math.Min(IconSize, availableSize.Width), Math.Min(IconSize, availableSize.Height)) : default;
        icon.IsVisible = hasIcon;
        icon.Measure(_icon);
        Children[1].Measure(new Size(Math.Max(0, availableSize.Width - _icon.Width - (hasIcon ? IconSpacing : 0)), availableSize.Height));
        _content = Children[1].DesiredSize;
        return new Size(_icon.Width + (hasIcon ? IconSpacing : 0) + _content.Width, Math.Max(_icon.Height, _content.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 2) return finalSize;
        var hasIcon = Children[0].IsVisible;
        var spacing = hasIcon ? Math.Min(IconSpacing, Math.Max(0, finalSize.Width - _icon.Width)) : 0;
        var width = _icon.Width + spacing + _content.Width;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Round(double value) => MathUtils.DipToPx(value, scale) / scale;
        var x = Round((finalSize.Width - width) / 2);
        Children[0].Arrange(new Rect(x, Round((finalSize.Height - _icon.Height) / 2), _icon.Width, _icon.Height));
        x += _icon.Width + spacing;
        Children[1].Arrange(new Rect(x, Round((finalSize.Height - _content.Height) / 2), _content.Width, _content.Height));
        return finalSize;
    }
}

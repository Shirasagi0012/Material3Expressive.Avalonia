using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// The optional fixed icon box and remaining-width label measurement shared by Button families.
internal sealed class ButtonContentRow : Panel
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<ButtonContentRow, double>(nameof(IconSize));
    public static readonly StyledProperty<double> IconSpacingProperty = AvaloniaProperty.Register<ButtonContentRow, double>(nameof(IconSpacing));
    public static readonly StyledProperty<bool> FillContentWhenNoIconProperty = AvaloniaProperty.Register<ButtonContentRow, bool>(nameof(FillContentWhenNoIcon));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public double IconSpacing { get => GetValue(IconSpacingProperty); set => SetValue(IconSpacingProperty, value); }
    public bool FillContentWhenNoIcon { get => GetValue(FillContentWhenNoIconProperty); set => SetValue(FillContentWhenNoIconProperty, value); }
    private Size _icon, _content;
    private bool _hasIcon, _hasLabel;

    static ButtonContentRow()
    {
        AffectsMeasure<ButtonContentRow>(IconSizeProperty, IconSpacingProperty);
        AffectsArrange<ButtonContentRow>(FillContentWhenNoIconProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!TryGetParts(out var icon, out var content)) return default;

        _hasIcon = icon.Content is not null || icon.ContentTemplate is not null;
        _hasLabel = content.Content is not null || content.ContentTemplate is not null;
        _icon = _hasIcon
            ? new Size(Math.Min(IconSize, availableSize.Width), Math.Min(IconSize, availableSize.Height))
            : default;
        icon.IsVisible = _hasIcon;
        icon.Measure(_icon);

        var spacing = _hasIcon && _hasLabel ? IconSpacing : 0;
        var contentSize = _hasIcon
            ? new Size(Math.Max(0, availableSize.Width - _icon.Width - spacing), availableSize.Height)
            : availableSize;
        content.Measure(contentSize);
        _content = content.DesiredSize;
        return _hasIcon
            ? new Size(_icon.Width + spacing + _content.Width, Math.Max(_icon.Height, _content.Height))
            : _content;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!TryGetParts(out var icon, out var content)) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Round(double value) => MathUtils.DipToPx(value, scale) / scale;

        if (!_hasIcon)
        {
            if (FillContentWhenNoIcon)
            {
                // Button and SplitButton previously arranged their presenter to the full area.
                content.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
            }
            else
            {
                // ToggleButton's pre-existing row centers its natural-size label as a group.
                content.Arrange(new Rect(Round((finalSize.Width - _content.Width) / 2),
                    Round((finalSize.Height - _content.Height) / 2), _content.Width, _content.Height));
            }
            return finalSize;
        }

        var spacing = _hasLabel ? Math.Min(IconSpacing, Math.Max(0, finalSize.Width - _icon.Width)) : 0;
        var width = _icon.Width + spacing + _content.Width;
        var x = Round((finalSize.Width - width) / 2);
        var contentY = Round((finalSize.Height - _content.Height) / 2);
        var availableContentHeight = _hasLabel
            ? Math.Max(0, _content.Height - content.Padding.Top - content.Padding.Bottom)
            : Math.Max(0, finalSize.Height - content.Padding.Top - content.Padding.Bottom);
        var iconY = _hasLabel
            ? contentY + content.Padding.Top + Round((availableContentHeight - _icon.Height) / 2)
            : content.Padding.Top + Round((availableContentHeight - _icon.Height) / 2);
        icon.Arrange(new Rect(x + content.Padding.Left, iconY, _icon.Width, _icon.Height));
        x += _icon.Width + spacing;
        content.Arrange(new Rect(x, contentY, _content.Width, _content.Height));
        return finalSize;
    }

    private bool TryGetParts(out ContentPresenter icon, out ButtonContentPresenter content)
    {
        icon = null!;
        content = null!;
        if (Children.Count != 2) return false;

        var contentIndex = Children[0] is ButtonContentPresenter ? 0 : Children[1] is ButtonContentPresenter ? 1 : -1;
        if (contentIndex < 0) return false;
        content = (ButtonContentPresenter)Children[contentIndex];
        var iconIndex = 1 - contentIndex;
        if (Children[iconIndex] is not ContentPresenter iconPresenter) return false;
        icon = iconPresenter;
        return true;
    }
}

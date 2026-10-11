using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// The optional fixed icon box and remaining-width label measurement shared by Button families.
// An optional third ContentPresenter child is a trailing icon box (used for DropDownButton's
// chevron); templates with two children lay out exactly as before.
internal sealed class ButtonContentRow : Panel
{
    public static readonly StyledProperty<double> IconSizeProperty = AvaloniaProperty.Register<ButtonContentRow, double>(nameof(IconSize));
    public static readonly StyledProperty<double> IconSpacingProperty = AvaloniaProperty.Register<ButtonContentRow, double>(nameof(IconSpacing));
    public static readonly StyledProperty<bool> FillContentWhenNoIconProperty = AvaloniaProperty.Register<ButtonContentRow, bool>(nameof(FillContentWhenNoIcon));
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }
    public double IconSpacing { get => GetValue(IconSpacingProperty); set => SetValue(IconSpacingProperty, value); }
    public bool FillContentWhenNoIcon { get => GetValue(FillContentWhenNoIconProperty); set => SetValue(FillContentWhenNoIconProperty, value); }
    private Size _icon, _trailing, _content;
    private bool _hasIcon, _hasTrailing, _hasLabel;

    static ButtonContentRow()
    {
        AffectsMeasure<ButtonContentRow>(IconSizeProperty, IconSpacingProperty);
        AffectsArrange<ButtonContentRow>(FillContentWhenNoIconProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (!TryGetParts(out var icon, out var content, out var trailing)) return default;

        _hasIcon = HasContent(icon);
        // The trailing box's content comes from owner-type styles, not a TemplateBinding. After a
        // runtime Theme switch its re-styling waits for its own Measure, which an invisible
        // control skips, so re-style it before reading whether it has content.
        trailing?.ApplyStyling();
        _hasTrailing = HasContent(trailing);
        _hasLabel = content.Content is not null || content.ContentTemplate is not null;
        _icon = MeasureIcon(icon, _hasIcon, availableSize);
        _trailing = MeasureIcon(trailing, _hasTrailing, availableSize);

        var spacing = _hasIcon && _hasLabel ? IconSpacing : 0;
        var trailingSpacing = _hasTrailing && (_hasLabel || _hasIcon) ? IconSpacing : 0;
        var reserved = _icon.Width + spacing + _trailing.Width + trailingSpacing;
        var contentSize = _hasIcon || _hasTrailing
            ? new Size(Math.Max(0, availableSize.Width - reserved), availableSize.Height)
            : availableSize;
        content.Measure(contentSize);
        _content = content.DesiredSize;
        return _hasIcon || _hasTrailing
            ? new Size(reserved + _content.Width, Math.Max(Math.Max(_icon.Height, _trailing.Height), _content.Height))
            : _content;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!TryGetParts(out var icon, out var content, out var trailing)) return finalSize;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Round(double value) => MathUtils.DipToPx(value, scale) / scale;

        if (!_hasIcon && !_hasTrailing)
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

        var spacing = _hasIcon && _hasLabel ? Math.Min(IconSpacing, Math.Max(0, finalSize.Width - _icon.Width)) : 0;
        var trailingSpacing = _hasTrailing && (_hasLabel || _hasIcon) ? IconSpacing : 0;
        var width = _icon.Width + spacing + _content.Width + trailingSpacing + _trailing.Width;
        var x = Round((finalSize.Width - width) / 2);
        var contentY = Round((finalSize.Height - _content.Height) / 2);
        var availableContentHeight = _hasLabel
            ? Math.Max(0, _content.Height - content.Padding.Top - content.Padding.Bottom)
            : Math.Max(0, finalSize.Height - content.Padding.Top - content.Padding.Bottom);
        double IconY(Size size) => _hasLabel
            ? contentY + content.Padding.Top + Round((availableContentHeight - size.Height) / 2)
            : content.Padding.Top + Round((availableContentHeight - size.Height) / 2);
        // Both icons sit inside the label presenter's padding, so the outer padding stays the
        // owner's Padding: [left | icon | gap | label | gap | trailing | right].
        if (_hasIcon) icon.Arrange(new Rect(x + content.Padding.Left, IconY(_icon), _icon.Width, _icon.Height));
        x += _icon.Width + spacing;
        content.Arrange(new Rect(x, contentY, _content.Width, _content.Height));
        if (_hasTrailing)
        {
            var trailingX = x + _content.Width - content.Padding.Right + trailingSpacing;
            trailing!.Arrange(new Rect(trailingX, IconY(_trailing), _trailing.Width, _trailing.Height));
        }
        return finalSize;
    }

    private Size MeasureIcon(ContentPresenter? presenter, bool hasContent, Size availableSize)
    {
        if (presenter is null) return default;
        var size = hasContent
            ? new Size(Math.Min(IconSize, availableSize.Width), Math.Min(IconSize, availableSize.Height))
            : default;
        presenter.IsVisible = hasContent;
        presenter.Measure(size);
        return size;
    }

    private static bool HasContent(ContentPresenter? presenter) =>
        presenter is not null && (presenter.Content is not null || presenter.ContentTemplate is not null);

    private bool TryGetParts(out ContentPresenter icon, out ButtonContentPresenter content, out ContentPresenter? trailing)
    {
        icon = null!;
        content = null!;
        trailing = null;
        if (Children.Count is not (2 or 3)) return false;

        var contentIndex = Children[0] is ButtonContentPresenter ? 0 : Children[1] is ButtonContentPresenter ? 1 : -1;
        if (contentIndex < 0) return false;
        content = (ButtonContentPresenter)Children[contentIndex];
        var iconIndex = 1 - contentIndex;
        if (Children[iconIndex] is not ContentPresenter iconPresenter) return false;
        icon = iconPresenter;
        if (Children.Count == 3)
        {
            if (Children[2] is not ContentPresenter trailingPresenter) return false;
            trailing = trailingPresenter;
        }
        return true;
    }
}

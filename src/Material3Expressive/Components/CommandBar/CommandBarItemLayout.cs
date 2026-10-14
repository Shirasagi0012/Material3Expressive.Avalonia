using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Components;

/// <summary>Template layout for native command buttons, with a separate indicator and ripple area.</summary>
internal sealed partial class CommandBarItemLayout : Panel
{
    public static readonly FuncValueConverter<Color, IBrush> ContentBrushConverter =
        new(static color => new ImmutableSolidColorBrush(color));

    private Button? _owner;
    private SelectionIndicatorAnimation? _selectionAnimation;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is not Button owner)
            return;

        _owner = owner;
        owner.PropertyChanged += OwnerPropertyChanged;
        InitializeSelection();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is { })
            _owner.PropertyChanged -= OwnerPropertyChanged;
        _selectionAnimation?.Dispose();
        _selectionAnimation = null;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }

    private bool IsSelected => _owner is ToggleButton { IsChecked: true };
    private bool IsEnabledForAnimation => _owner?.IsEffectivelyEnabled == true;
    private Color IconTarget => !IsEnabledForAnimation ? DisabledIconColor : IsSelected ? SelectedIconColor : UnselectedIconColor;
    private Color TextTarget => !IsEnabledForAnimation ? DisabledTextColor : IsSelected ? SelectedTextColor : UnselectedTextColor;

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ToggleButton.IsCheckedProperty || change.Property == IsEffectivelyEnabledProperty)
        {
            UpdateSelection();
            InvalidateArrange();
        }
    }

    private void InitializeSelection()
    {
        if (_owner is null)
            return;

        if (_owner is not CommandBarToggleButton)
        {
            // Plain commands have no selection tracks or indicator background.
            SetCurrentValue(AnimatedIconColorProperty, IconTarget);
            SetCurrentValue(AnimatedTextColorProperty, TextTarget);
            return;
        }

        var animation = new SelectionIndicatorAnimation(_owner, IsSelected, IconTarget, TextTarget);
        _selectionAnimation = animation;
        animation.IndicatorOpacity.Bind(this, IndicatorOpacityProperty);
        animation.IndicatorSize.Bind(this, IndicatorSizeProperty);
        animation.IconColor.Bind(this, AnimatedIconColorProperty);
        animation.TextColor.Bind(this, AnimatedTextColorProperty);
    }

    private void UpdateSelection()
    {
        if (_selectionAnimation is { } animation)
            animation.UpdateTargets(IsSelected, IconTarget, TextTarget, EffectsAnimation, SizeAnimation);
        else if (_owner is { })
        {
            SetCurrentValue(AnimatedIconColorProperty, IconTarget);
            SetCurrentValue(AnimatedTextColorProperty, TextTarget);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EffectsAnimationProperty || change.Property == SizeAnimationProperty
         || change.Property == SelectedIconColorProperty || change.Property == SelectedTextColorProperty
         || change.Property == UnselectedIconColorProperty || change.Property == UnselectedTextColorProperty
         || change.Property == DisabledIconColorProperty || change.Property == DisabledTextColorProperty)
            UpdateSelection();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        Control icon = Children[1];
        Control label = Children[2];
        if (IsInOverflow)
        {
            double widthLimit = Math.Min(availableSize.Width, MenuMaximumWidth);
            double horizontalPadding = MenuContentPadding.Left + MenuContentPadding.Right;
            double verticalPadding = MenuContentPadding.Top + MenuContentPadding.Bottom;
            double contentWidthLimit = Math.Max(0, widthLimit - horizontalPadding);
            icon.Measure(new Size(contentWidthLimit, availableSize.Height));
            double spacing = icon.DesiredSize.Width > 0 ? IconLabelSpacing : 0;
            label.Measure(new Size(Math.Max(0, contentWidthLimit - icon.DesiredSize.Width - spacing), availableSize.Height));
            double menuWidth = Math.Min(widthLimit, Math.Max(MenuMinimumWidth,
                icon.DesiredSize.Width + spacing + label.DesiredSize.Width + horizontalPadding));
            double containerHeight = Math.Max(MenuContainerHeight,
                Math.Max(icon.DesiredSize.Height, label.DesiredSize.Height) + verticalPadding);
            var containerSize = new Size(menuWidth, containerHeight);
            Children[0].Measure(containerSize);
            Children[3].Measure(containerSize);
            return new Size(menuWidth, Math.Max(containerHeight, MenuMinimumInteractiveHeight)).Constrain(availableSize);
        }

        icon.Measure(availableSize);
        label.Measure(availableSize);
        Size iconSize = icon.DesiredSize;
        Size labelSize = label.IsVisible ? label.DesiredSize : default;
        double contentWidth = IsStacked
            ? Math.Max(IndicatorWidth, labelSize.Width)
            : iconSize.Width + (labelSize.Width > 0 && iconSize.Width > 0 ? IconLabelSpacing : 0) + labelSize.Width;
        double contentHeight = IsStacked
            ? IndicatorHeight + (labelSize.Height > 0 ? IconLabelSpacing + labelSize.Height : 0)
            : Math.Max(IndicatorHeight, labelSize.Height);
        double width = contentWidth + 16;
        if (HasInlineLabel)
        {
            width = contentWidth + InlineContentPadding.Left + InlineContentPadding.Right + 8;
            contentHeight = Math.Max(iconSize.Height, labelSize.Height)
              + InlineContentPadding.Top + InlineContentPadding.Bottom;
        }

        var size = new Size(Math.Max(width, 48), Math.Max(contentHeight + 8, IsStacked ? 60 : 48));
        Children[0].Measure(size);
        Children[3].Measure(size);
        return size;
    }

    private bool IsStacked => LabelPosition == CommandBarDefaultLabelPosition.Bottom;

    private bool HasInlineLabel => LabelPosition == CommandBarDefaultLabelPosition.Right
     && !IsInOverflow && Children[2].IsVisible && Children[2].DesiredSize.Width > 0;

    protected override Size ArrangeOverride(Size finalSize)
    {
        Control icon = Children[1];
        Control label = Children[2];
        Size iconSize = icon.DesiredSize;
        Size labelSize = label.IsVisible ? label.DesiredSize : default;
        Rect rippleBounds;

        if (IsInOverflow)
        {
            double contentHeight = Math.Max(iconSize.Height, labelSize.Height);
            double containerHeight = Math.Max(MenuContainerHeight,
                contentHeight + MenuContentPadding.Top + MenuContentPadding.Bottom);
            rippleBounds = new Rect(0, (finalSize.Height - containerHeight) / 2, finalSize.Width, containerHeight);
            double contentTop = rippleBounds.Y + MenuContentPadding.Top
              + (rippleBounds.Height - MenuContentPadding.Top - MenuContentPadding.Bottom - contentHeight) / 2;
            double spacing = iconSize.Width > 0 ? IconLabelSpacing : 0;
            icon.Arrange(new Rect(MenuContentPadding.Left, contentTop + (contentHeight - iconSize.Height) / 2,
                iconSize.Width, iconSize.Height));
            label.Arrange(new Rect(MenuContentPadding.Left + iconSize.Width + spacing,
                contentTop + (contentHeight - labelSize.Height) / 2, labelSize.Width, labelSize.Height));
        }
        else if (IsStacked)
        {
            double totalHeight = IndicatorHeight + (labelSize.Height > 0 ? IconLabelSpacing + labelSize.Height : 0);
            double top = (finalSize.Height - totalHeight) / 2;
            double rippleWidth = Math.Min(IndicatorWidth, Math.Max(0, finalSize.Width - 8));
            rippleBounds = new Rect((finalSize.Width - rippleWidth) / 2, top, rippleWidth, IndicatorHeight);
            icon.Arrange(new Rect((finalSize.Width - iconSize.Width) / 2,
                top + (IndicatorHeight - iconSize.Height) / 2, iconSize.Width, iconSize.Height));
            label.Arrange(new Rect((finalSize.Width - labelSize.Width) / 2,
                top + IndicatorHeight + IconLabelSpacing, labelSize.Width, labelSize.Height));
        }
        else
        {
            double spacing = labelSize.Width > 0 && iconSize.Width > 0 ? IconLabelSpacing : 0;
            double contentWidth = iconSize.Width + spacing + labelSize.Width;
            double rippleHeight = HasInlineLabel
                ? Math.Max(iconSize.Height, labelSize.Height) + InlineContentPadding.Top + InlineContentPadding.Bottom
                : IndicatorHeight;
            rippleBounds = new Rect(4, (finalSize.Height - rippleHeight) / 2,
                Math.Max(0, finalSize.Width - 8), rippleHeight);
            double left = HasInlineLabel
                ? rippleBounds.X + InlineContentPadding.Left : (finalSize.Width - contentWidth) / 2;
            double contentTop = HasInlineLabel ? rippleBounds.Y + InlineContentPadding.Top : 0;
            double contentHeight = HasInlineLabel ? Math.Max(iconSize.Height, labelSize.Height) : finalSize.Height;
            icon.Arrange(new Rect(left, contentTop + (contentHeight - iconSize.Height) / 2, iconSize.Width, iconSize.Height));
            label.Arrange(new Rect(left + iconSize.Width + spacing,
                contentTop + (contentHeight - labelSize.Height) / 2, labelSize.Width, labelSize.Height));
        }

        // The background expands independently; the ripple always keeps its full geometry.
        double indicatorWidth = rippleBounds.Width * Math.Max(0, IndicatorSize);
        Children[0]
           .Arrange(new Rect(rippleBounds.Center.X - indicatorWidth / 2,
                rippleBounds.Y, indicatorWidth, rippleBounds.Height));
        Children[3].Arrange(rippleBounds);
        if (Children[0] is Border { Child: Border indicator })
        {
            indicator.CornerRadius = IsInOverflow ? MenuSelectedItemShape
                : new CornerRadius(Math.Min(indicatorWidth, rippleBounds.Height) / 2);
        }

        if (Children[3] is MaterialSurface ripple)
        {
            if (IsInOverflow)
                ripple.CornerRadius = _owner is CommandBarToggleButton ? MenuSelectedItemShape : MenuItemShape;
            else
                ripple.CornerRadius = new CornerRadius(rippleBounds.Height / 2);
        }

        return finalSize;
    }
}

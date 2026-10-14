using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Components;

internal sealed partial class CommandBarItemLayout
{
    public static readonly StyledProperty<CommandBarDefaultLabelPosition> LabelPositionProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, CommandBarDefaultLabelPosition>(nameof(LabelPosition), CommandBarDefaultLabelPosition.Bottom);
    public CommandBarDefaultLabelPosition LabelPosition
    {
        get => GetValue(LabelPositionProperty);
        set => SetValue(LabelPositionProperty, value);
    }

    public static readonly StyledProperty<bool> IsInOverflowProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, bool>(nameof(IsInOverflow), false);
    public bool IsInOverflow
    {
        get => GetValue(IsInOverflowProperty);
        set => SetValue(IsInOverflowProperty, value);
    }

    public static readonly StyledProperty<double> IndicatorWidthProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(IndicatorWidth), 56);
    public double IndicatorWidth
    {
        get => GetValue(IndicatorWidthProperty);
        set => SetValue(IndicatorWidthProperty, value);
    }

    public static readonly StyledProperty<double> IndicatorHeightProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(IndicatorHeight), 32);
    public double IndicatorHeight
    {
        get => GetValue(IndicatorHeightProperty);
        set => SetValue(IndicatorHeightProperty, value);
    }

    public static readonly StyledProperty<double> IconLabelSpacingProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(IconLabelSpacing), 4);
    public double IconLabelSpacing
    {
        get => GetValue(IconLabelSpacingProperty);
        set => SetValue(IconLabelSpacingProperty, value);
    }

    public static readonly StyledProperty<Thickness> InlineContentPaddingProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Thickness>(nameof(InlineContentPadding), new Thickness(12, 6));
    public Thickness InlineContentPadding
    {
        get => GetValue(InlineContentPaddingProperty);
        set => SetValue(InlineContentPaddingProperty, value);
    }

    public static readonly StyledProperty<float> IndicatorOpacityProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, float>(nameof(IndicatorOpacity), 0);
    public float IndicatorOpacity
    {
        get => GetValue(IndicatorOpacityProperty);
        set => SetValue(IndicatorOpacityProperty, value);
    }

    public static readonly StyledProperty<float> IndicatorSizeProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, float>(nameof(IndicatorSize), 0);
    public float IndicatorSize
    {
        get => GetValue(IndicatorSizeProperty);
        set => SetValue(IndicatorSizeProperty, value);
    }

    public static readonly StyledProperty<SpringSpec> EffectsAnimationProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, SpringSpec>(nameof(EffectsAnimation), default);
    public SpringSpec EffectsAnimation
    {
        get => GetValue(EffectsAnimationProperty);
        set => SetValue(EffectsAnimationProperty, value);
    }

    public static readonly StyledProperty<SpringSpec> SizeAnimationProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, SpringSpec>(nameof(SizeAnimation), default);
    public SpringSpec SizeAnimation
    {
        get => GetValue(SizeAnimationProperty);
        set => SetValue(SizeAnimationProperty, value);
    }

    public static readonly StyledProperty<Color> SelectedIconColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(SelectedIconColor), default);
    public Color SelectedIconColor
    {
        get => GetValue(SelectedIconColorProperty);
        set => SetValue(SelectedIconColorProperty, value);
    }

    public static readonly StyledProperty<Color> SelectedTextColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(SelectedTextColor), default);
    public Color SelectedTextColor
    {
        get => GetValue(SelectedTextColorProperty);
        set => SetValue(SelectedTextColorProperty, value);
    }

    public static readonly StyledProperty<Color> UnselectedIconColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(UnselectedIconColor), default);
    public Color UnselectedIconColor
    {
        get => GetValue(UnselectedIconColorProperty);
        set => SetValue(UnselectedIconColorProperty, value);
    }

    public static readonly StyledProperty<Color> UnselectedTextColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(UnselectedTextColor), default);
    public Color UnselectedTextColor
    {
        get => GetValue(UnselectedTextColorProperty);
        set => SetValue(UnselectedTextColorProperty, value);
    }

    public static readonly StyledProperty<Color> DisabledIconColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(DisabledIconColor), default);
    public Color DisabledIconColor
    {
        get => GetValue(DisabledIconColorProperty);
        set => SetValue(DisabledIconColorProperty, value);
    }

    public static readonly StyledProperty<Color> DisabledTextColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(DisabledTextColor), default);
    public Color DisabledTextColor
    {
        get => GetValue(DisabledTextColorProperty);
        set => SetValue(DisabledTextColorProperty, value);
    }

    public static readonly StyledProperty<Color> AnimatedIconColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(AnimatedIconColor), default);
    public Color AnimatedIconColor
    {
        get => GetValue(AnimatedIconColorProperty);
        set => SetValue(AnimatedIconColorProperty, value);
    }

    public static readonly StyledProperty<Color> AnimatedTextColorProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Color>(nameof(AnimatedTextColor), default);
    public Color AnimatedTextColor
    {
        get => GetValue(AnimatedTextColorProperty);
        set => SetValue(AnimatedTextColorProperty, value);
    }

    public static readonly StyledProperty<Thickness> MenuContentPaddingProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, Thickness>(nameof(MenuContentPadding), default);
    public Thickness MenuContentPadding
    {
        get => GetValue(MenuContentPaddingProperty);
        set => SetValue(MenuContentPaddingProperty, value);
    }

    public static readonly StyledProperty<double> MenuMinimumWidthProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(MenuMinimumWidth), 0);
    public double MenuMinimumWidth
    {
        get => GetValue(MenuMinimumWidthProperty);
        set => SetValue(MenuMinimumWidthProperty, value);
    }

    public static readonly StyledProperty<double> MenuMaximumWidthProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(MenuMaximumWidth), double.PositiveInfinity);
    public double MenuMaximumWidth
    {
        get => GetValue(MenuMaximumWidthProperty);
        set => SetValue(MenuMaximumWidthProperty, value);
    }

    public static readonly StyledProperty<double> MenuContainerHeightProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(MenuContainerHeight), 0);
    public double MenuContainerHeight
    {
        get => GetValue(MenuContainerHeightProperty);
        set => SetValue(MenuContainerHeightProperty, value);
    }

    public static readonly StyledProperty<double> MenuMinimumInteractiveHeightProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, double>(nameof(MenuMinimumInteractiveHeight), 0);
    public double MenuMinimumInteractiveHeight
    {
        get => GetValue(MenuMinimumInteractiveHeightProperty);
        set => SetValue(MenuMinimumInteractiveHeightProperty, value);
    }

    public static readonly StyledProperty<CornerRadius> MenuItemShapeProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, CornerRadius>(nameof(MenuItemShape), default);
    public CornerRadius MenuItemShape
    {
        get => GetValue(MenuItemShapeProperty);
        set => SetValue(MenuItemShapeProperty, value);
    }

    public static readonly StyledProperty<CornerRadius> MenuSelectedItemShapeProperty =
        AvaloniaProperty.Register<CommandBarItemLayout, CornerRadius>(nameof(MenuSelectedItemShape), default);
    public CornerRadius MenuSelectedItemShape
    {
        get => GetValue(MenuSelectedItemShapeProperty);
        set => SetValue(MenuSelectedItemShapeProperty, value);
    }

    static CommandBarItemLayout()
    {
        AffectsMeasure<CommandBarItemLayout>(LabelPositionProperty, IndicatorWidthProperty, IndicatorHeightProperty,
            IconLabelSpacingProperty, InlineContentPaddingProperty, IsInOverflowProperty,
            MenuContentPaddingProperty, MenuMinimumWidthProperty, MenuMaximumWidthProperty,
            MenuContainerHeightProperty, MenuMinimumInteractiveHeightProperty);
        AffectsArrange<CommandBarItemLayout>(IndicatorSizeProperty, MenuItemShapeProperty, MenuSelectedItemShapeProperty);
    }
}

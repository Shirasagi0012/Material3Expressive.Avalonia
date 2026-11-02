using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Typography;
using Material3Expressive.Foundation.Motion;
namespace Material3Expressive.Components;
internal sealed partial class TextFieldLayout
{
    public static readonly StyledProperty<Thickness> SupportingPaddingProperty = AvaloniaProperty.Register<TextFieldLayout, Thickness>(nameof(SupportingPadding), new Thickness(16, 4, 16, 0));
    public Thickness SupportingPadding { get => GetValue(SupportingPaddingProperty); set => SetValue(SupportingPaddingProperty, value); }
    public static readonly StyledProperty<double> IconAdjacentPaddingReductionProperty = AvaloniaProperty.Register<TextFieldLayout, double>(nameof(IconAdjacentPaddingReduction), 12d);
    public double IconAdjacentPaddingReduction { get => GetValue(IconAdjacentPaddingReductionProperty); set => SetValue(IconAdjacentPaddingReductionProperty, value); }
    public static readonly StyledProperty<double> AffixSpacingProperty = AvaloniaProperty.Register<TextFieldLayout, double>(nameof(AffixSpacing), 2d);
    public double AffixSpacing { get => GetValue(AffixSpacingProperty); set => SetValue(AffixSpacingProperty, value); }
    public static readonly StyledProperty<Thickness> AboveLabelPaddingProperty = AvaloniaProperty.Register<TextFieldLayout, Thickness>(nameof(AboveLabelPadding), new Thickness(4, 0, 4, 4));
    public Thickness AboveLabelPadding { get => GetValue(AboveLabelPaddingProperty); set => SetValue(AboveLabelPaddingProperty, value); }
    public static readonly StyledProperty<double> ExpandedLabelMinHeightProperty = AvaloniaProperty.Register<TextFieldLayout, double>(nameof(ExpandedLabelMinHeight), 24d);
    public double ExpandedLabelMinHeight { get => GetValue(ExpandedLabelMinHeightProperty); set => SetValue(ExpandedLabelMinHeightProperty, value); }
    public static readonly StyledProperty<double> CollapsedLabelMinHeightProperty = AvaloniaProperty.Register<TextFieldLayout, double>(nameof(CollapsedLabelMinHeight), 16d);
    public double CollapsedLabelMinHeight { get => GetValue(CollapsedLabelMinHeightProperty); set => SetValue(CollapsedLabelMinHeightProperty, value); }

    public static readonly StyledProperty<double> DefaultMinimumWidthProperty = AvaloniaProperty.Register<TextFieldLayout, double>(nameof(DefaultMinimumWidth));
    public double DefaultMinimumWidth { get => GetValue(DefaultMinimumWidthProperty); set => SetValue(DefaultMinimumWidthProperty, value); }
    public static readonly StyledProperty<double> DefaultMinimumHeightProperty = AvaloniaProperty.Register<TextFieldLayout, double>(nameof(DefaultMinimumHeight));
    public double DefaultMinimumHeight { get => GetValue(DefaultMinimumHeightProperty); set => SetValue(DefaultMinimumHeightProperty, value); }
    public static readonly StyledProperty<double?> DefaultMinimizedLabelLineHeightProperty = AvaloniaProperty.Register<TextFieldLayout, double?>(nameof(DefaultMinimizedLabelLineHeight));
    public double? DefaultMinimizedLabelLineHeight { get => GetValue(DefaultMinimizedLabelLineHeightProperty); set => SetValue(DefaultMinimizedLabelLineHeightProperty, value); }
    public static readonly StyledProperty<SpringSpec?> SpatialAnimationProperty = AvaloniaProperty.Register<TextFieldLayout, SpringSpec?>(nameof(SpatialAnimation));
    public SpringSpec? SpatialAnimation { get => GetValue(SpatialAnimationProperty); set => SetValue(SpatialAnimationProperty, value); }
    public static readonly StyledProperty<SpringSpec?> FastEffectsAnimationProperty = AvaloniaProperty.Register<TextFieldLayout, SpringSpec?>(nameof(FastEffectsAnimation));
    public SpringSpec? FastEffectsAnimation { get => GetValue(FastEffectsAnimationProperty); set => SetValue(FastEffectsAnimationProperty, value); }
    public static readonly StyledProperty<SpringSpec?> SlowEffectsAnimationProperty = AvaloniaProperty.Register<TextFieldLayout, SpringSpec?>(nameof(SlowEffectsAnimation));
    public SpringSpec? SlowEffectsAnimation { get => GetValue(SlowEffectsAnimationProperty); set => SetValue(SlowEffectsAnimationProperty, value); }

    public static readonly StyledProperty<IBrush?> ContainerBrushProperty = AvaloniaProperty.Register<TextFieldLayout, IBrush?>(nameof(ContainerBrush));
    public IBrush? ContainerBrush { get => GetValue(ContainerBrushProperty); set => SetValue(ContainerBrushProperty, value); }
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty = AvaloniaProperty.Register<TextFieldLayout, IBrush?>(nameof(IndicatorBrush));
    public IBrush? IndicatorBrush { get => GetValue(IndicatorBrushProperty); set => SetValue(IndicatorBrushProperty, value); }
    public static readonly StyledProperty<IBrush?> LabelBrushProperty = AvaloniaProperty.Register<TextFieldLayout, IBrush?>(nameof(LabelBrush));
    public IBrush? LabelBrush { get => GetValue(LabelBrushProperty); set => SetValue(LabelBrushProperty, value); }
    public static readonly StyledProperty<TextStyle?> LargeTextStyleProperty = AvaloniaProperty.Register<TextFieldLayout, TextStyle?>(nameof(LargeTextStyle));
    public TextStyle? LargeTextStyle { get => GetValue(LargeTextStyleProperty); set => SetValue(LargeTextStyleProperty, value); }
    public static readonly StyledProperty<TextStyle?> SmallTextStyleProperty = AvaloniaProperty.Register<TextFieldLayout, TextStyle?>(nameof(SmallTextStyle));
    public TextStyle? SmallTextStyle { get => GetValue(SmallTextStyleProperty); set => SetValue(SmallTextStyleProperty, value); }
}

// Tooltip.kt RichTooltip / textVerticalPadding, AndroidX f3ed195f0c9e; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

internal sealed class RichTooltipLayout : Panel
{
    public static readonly StyledProperty<double> SubheadFirstBaselineOffsetProperty =
        AvaloniaProperty.Register<RichTooltipLayout, double>(nameof(SubheadFirstBaselineOffset), 28d);
    public double SubheadFirstBaselineOffset { get => GetValue(SubheadFirstBaselineOffsetProperty); set => SetValue(SubheadFirstBaselineOffsetProperty, value); }

    public static readonly StyledProperty<double> TextFirstBaselineOffsetProperty =
        AvaloniaProperty.Register<RichTooltipLayout, double>(nameof(TextFirstBaselineOffset), 24d);
    public double TextFirstBaselineOffset { get => GetValue(TextFirstBaselineOffsetProperty); set => SetValue(TextFirstBaselineOffsetProperty, value); }

    public static readonly StyledProperty<double> TextBottomPaddingProperty =
        AvaloniaProperty.Register<RichTooltipLayout, double>(nameof(TextBottomPadding), 16d);
    public double TextBottomPadding { get => GetValue(TextBottomPaddingProperty); set => SetValue(TextBottomPaddingProperty, value); }

    public static readonly StyledProperty<double> TextVerticalPaddingProperty =
        AvaloniaProperty.Register<RichTooltipLayout, double>(nameof(TextVerticalPadding), 4d);
    public double TextVerticalPadding { get => GetValue(TextVerticalPaddingProperty); set => SetValue(TextVerticalPaddingProperty, value); }

    public static readonly StyledProperty<double> ActionMinHeightProperty =
        AvaloniaProperty.Register<RichTooltipLayout, double>(nameof(ActionMinHeight), 36d);
    public double ActionMinHeight { get => GetValue(ActionMinHeightProperty); set => SetValue(ActionMinHeightProperty, value); }

    public static readonly StyledProperty<double> ActionBottomPaddingProperty =
        AvaloniaProperty.Register<RichTooltipLayout, double>(nameof(ActionBottomPadding), 8d);
    public double ActionBottomPadding { get => GetValue(ActionBottomPaddingProperty); set => SetValue(ActionBottomPaddingProperty, value); }

    static RichTooltipLayout() => AffectsMeasure<RichTooltipLayout>(SubheadFirstBaselineOffsetProperty,
        TextFirstBaselineOffsetProperty, TextBottomPaddingProperty, TextVerticalPaddingProperty,
        ActionMinHeightProperty, ActionBottomPaddingProperty);

    private readonly Rect[] _slots = new Rect[3];
    protected override bool BypassFlowDirectionPolicies => true;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3) return default;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx(value, scale);
        var width = double.IsFinite(availableSize.Width) ? Px(availableSize.Width) : int.MaxValue;
        var height = double.IsFinite(availableSize.Height) ? Px(availableSize.Height) : int.MaxValue;
        var title = (ContentPresenter)Children[0];
        var action = (ContentPresenter)Children[2];
        double y = 0;
        var measuredWidth = 0;
        for (var i = 0; i < 3; i++)
        {
            var slot = (ContentPresenter)Children[i];
            if (!slot.IsVisible) { _slots[i] = default; continue; }
            slot.UpdateChild();
            var remaining = Math.Max(0, height - (int)y);
            var simpleText = i == 1 && !title.IsVisible && !action.IsVisible;
            var bottom = Px(i == 2 ? ActionBottomPadding : i == 1 ? simpleText ? TextVerticalPadding : TextBottomPadding : 0);
            // requiredHeightIn(min=36) may exceed the parent's remaining maximum.
            // Compose centers that overflow in the apparent, constrained placeable.
            var maximum = i == 2 ? Math.Max(Px(ActionMinHeight), remaining) : remaining;
            var top = simpleText ? Px(TextVerticalPadding) : 0;
            slot.Measure(new Size(width / scale, Math.Max(0, maximum - top - bottom) / scale));
            var size = slot.DesiredSize;
            var childHeight = Px(size.Height);
            if (i < 2 && !simpleText)
            {
                var first = MaterialTextBaseline.Read(slot.Child, scale).First;
                var baseline = first == int.MinValue ? 0 : first;
                top = Math.Clamp(Px(i == 0 ? SubheadFirstBaselineOffset : TextFirstBaselineOffset) - baseline, 0,
                    Math.Max(0, maximum - childHeight - bottom));
                // Even an unspecified "after" uses zero in alignmentLineOffsetMeasure.
                // A baseline below the padded child therefore contributes trailing space.
                bottom += Math.Clamp(baseline - childHeight - bottom, 0,
                    Math.Max(0, maximum - childHeight - bottom - top));
            }
            var actualHeight = Math.Min(maximum, top + childHeight + bottom);
            if (i == 2) actualHeight = Math.Max(Px(ActionMinHeight), actualHeight);
            var apparentHeight = Math.Min(remaining, actualHeight);
            _slots[i] = new Rect(0, (y + (apparentHeight - actualHeight) / 2 + top) / scale,
                size.Width, size.Height);
            y += apparentHeight;
            measuredWidth = Math.Max(measuredWidth, Px(size.Width));
        }
        return new Size(measuredWidth / scale, y / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return finalSize;
        for (var i = 0; i < 3; i++)
        {
            var rect = _slots[i];
            var x = FlowDirection == FlowDirection.RightToLeft ? finalSize.Width - rect.Width : 0;
            Children[i].Arrange(rect.WithX(x));
        }
        return finalSize;
    }

}

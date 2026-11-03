// Tooltip.kt RichTooltip / textVerticalPadding, AndroidX f3ed195f0c9e; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class RichTooltipLayout : Panel
{
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
            var bottom = Px(i == 2 ? 8 : i == 1 ? simpleText ? 4 : 16 : 0);
            // requiredHeightIn(min=36) may exceed the parent's remaining maximum.
            // Compose centers that overflow in the apparent, constrained placeable.
            var maximum = i == 2 ? Math.Max(Px(36), remaining) : remaining;
            var top = simpleText ? Px(4) : 0;
            slot.Measure(new Size(width / scale, Math.Max(0, maximum - top - bottom) / scale));
            var size = slot.DesiredSize;
            var childHeight = Px(size.Height);
            if (i < 2 && !simpleText)
            {
                var baseline = FirstBaseline(slot.Child, scale);
                top = Math.Clamp(Px(i == 0 ? 28 : 24) - baseline, 0,
                    Math.Max(0, maximum - childHeight - bottom));
                // Even an unspecified "after" uses zero in alignmentLineOffsetMeasure.
                // A baseline below the padded child therefore contributes trailing space.
                bottom += Math.Clamp(baseline - childHeight - bottom, 0,
                    Math.Max(0, maximum - childHeight - bottom - top));
            }
            var actualHeight = Math.Min(maximum, top + childHeight + bottom);
            if (i == 2) actualHeight = Math.Max(Px(36), actualHeight);
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

    // Native text and explicit native baselines are available without a shared
    // alignment-line framework. Missing baselines follow Compose's zero fallback.
    private static int FirstBaseline(Control? child, double scale)
    {
        if (child is null) return 0;
        if (child.IsSet(TextBlock.BaselineOffsetProperty))
            return MathUtils.DipToPx(child.Margin.Top + TextBlock.GetBaselineOffset(child), scale);
        if (child is TextBlock text && text.TextLayout.TextLines is { Count: > 0 } lines)
            return MathUtils.DipToPx(text.Margin.Top + text.Padding.Top + lines[0].Baseline, scale);
        if (child is ContentPresenter presenter)
            return MathUtils.DipToPx(presenter.Margin.Top + presenter.Padding.Top, scale) + FirstBaseline(presenter.Child, scale);
        return 0;
    }
}

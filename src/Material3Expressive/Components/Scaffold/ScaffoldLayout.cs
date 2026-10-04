using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Fixed Scaffold.kt: ScaffoldLayout. All calculations use physical integer pixels; RTL is
// converted to Avalonia's mirrored local coordinates only when placing a slot.
internal sealed class ScaffoldLayout : Panel
{
    private int _leftInset, _rightInset, _fabLeft, _fabOffset, _snackbarOffset;
    private bool _hasFab;

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = (Scaffold)TemplatedParent!;
        // Like SubcomposeLayout using constraints.maxWidth/maxHeight, Scaffold requires bounded
        // screen constraints. It has no intrinsic/wrap-content measurement in Compose.
        if (!double.IsFinite(availableSize.Width) || !double.IsFinite(availableSize.Height))
            throw new InvalidOperationException("Scaffold requires bounded width and height. Set Height when hosting it in a vertical ScrollViewer.");
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        var width = Px(availableSize.Width);
        var height = Px(availableSize.Height);
        var loose = new Size(width / scale, height / scale);
        var insets = owner.ContentWindowInsets;
        var consumed = owner.GetValue(MaterialWindowInsets.ConsumedPaddingProperty);
        _leftInset = Math.Max(0, Px(insets.Left) - Px(consumed.Left));
        _rightInset = Math.Max(0, Px(insets.Right) - Px(consumed.Right));
        var topInset = Math.Max(0, Px(insets.Top) - Px(consumed.Top));
        var bottomInset = Math.Max(0, Px(insets.Bottom) - Px(consumed.Bottom));
        var insetConstraints = new Size(Math.Max(0, width - _leftInset - _rightInset) / scale,
            Math.Max(0, height - bottomInset) / scale);
        var top = (ContentPresenter)Children[0];
        var body = Children[1];
        var snackbar = Children[2];
        var fab = Children[3];
        var bottom = (ContentPresenter)Children[4];
        top.Measure(loose);
        snackbar.Measure(insetConstraints);
        fab.Measure(insetConstraints);
        var fabWidth = Px(fab.DesiredSize.Width);
        var fabHeight = Px(fab.DesiredSize.Height);
        _hasFab = fabWidth != 0 && fabHeight != 0;
        var spacing = Px(16);
        var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
        _fabLeft = owner.FloatingActionButtonPosition switch
        {
            FabPosition.Start => rtl ? width - spacing - fabWidth - _rightInset : spacing + _leftInset,
            FabPosition.End or FabPosition.EndOverlay => rtl ? spacing + _leftInset : width - spacing - fabWidth - _rightInset,
            _ => (width - fabWidth + _leftInset - _rightInset) / 2,
        };
        bottom.Measure(loose);
        // A present zero-height bar still replaces the corresponding inset, matching the
        // presence of a zero-height placeable rather than an empty composable lambda.
        var bottomHeight = bottom.Child is not null ? Px(bottom.DesiredSize.Height) : (int?)null;
        _fabOffset = !_hasFab ? 0 : bottomHeight is null || owner.FloatingActionButtonPosition == FabPosition.EndOverlay
            ? fabHeight + spacing + bottomInset : bottomHeight.Value + fabHeight + spacing;
        var snackbarHeight = Px(snackbar.DesiredSize.Height);
        _snackbarOffset = snackbarHeight == 0 ? 0 : snackbarHeight + (_hasFab ? _fabOffset : bottomHeight ?? bottomInset);
        owner.UpdateContentPadding(new Thickness((rtl ? _rightInset : _leftInset) / scale,
            (top.Child is null ? topInset : Px(top.DesiredSize.Height)) / scale,
            (rtl ? _leftInset : _rightInset) / scale, (bottomHeight ?? bottomInset) / scale));
        body.Measure(loose);
        return loose;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = MathUtils.DipToPx(finalSize.Width, scale);
        var height = MathUtils.DipToPx(finalSize.Height, scale);
        void Place(Control child, int x, int y)
        {
            var childWidth = MathUtils.DipToPx(child.DesiredSize.Width, scale);
            var childHeight = MathUtils.DipToPx(child.DesiredSize.Height, scale);
            if (FlowDirection == FlowDirection.RightToLeft) x = width - x - childWidth;
            child.Arrange(new Rect(x / scale, y / scale, childWidth / scale, childHeight / scale));
        }
        Place(Children[0], 0, 0);
        Place(Children[1], 0, 0);
        var snackbarWidth = MathUtils.DipToPx(Children[2].DesiredSize.Width, scale);
        Place(Children[2], (width - snackbarWidth + _leftInset - _rightInset) / 2, height - _snackbarOffset);
        if (_hasFab) Place(Children[3], _fabLeft, height - _fabOffset);
        else Children[3].Arrange(default);
        Place(Children[4], 0, height - MathUtils.DipToPx(Children[4].DesiredSize.Height, scale));
        return finalSize;
    }
}

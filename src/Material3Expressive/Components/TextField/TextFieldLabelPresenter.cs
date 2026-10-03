using Avalonia;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Compose Alignment.CenterVertically rounds half pixels upward. Avalonia's ordinary
// presenter uses midpoint-to-even, observable while the label's minimum line height animates.
internal sealed class TextFieldLabelPresenter : ContentPresenter
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        var result = base.ArrangeOverride(finalSize);
        if (Child is { } child && VerticalContentAlignment == VerticalAlignment.Center)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            var y = Padding.Top + MathUtils.RoundToInt((finalSize.Height - Padding.Top - Padding.Bottom - child.Bounds.Height) * scale / 2) / scale;
            child.Arrange(new Rect(child.Bounds.X, y, child.Bounds.Width, child.Bounds.Height));
        }
        return result;
    }
}

using System;
using Avalonia;
using Avalonia.Controls.Presenters;

namespace Material3Expressive.Components;

// Keep the Material interactive slot at 48 DIP while arranging visual content at its
// measured size. ContentPresenter otherwise stretches a small icon child to the slot.
internal sealed class TextFieldIconPresenter : ContentPresenter
{
    protected override Type StyleKeyOverride => typeof(ContentPresenter);

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is not { } child) return base.ArrangeOverride(finalSize);

        var size = child.DesiredSize;
        child.Arrange(new Rect(
            (finalSize.Width - size.Width) / 2,
            (finalSize.Height - size.Height) / 2,
            size.Width,
            size.Height));
        return finalSize;
    }
}

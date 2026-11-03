// Translated from AndroidX TimePicker.kt: SideControlColumn (f3ed195f0c9e).
// Copyright 2026 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class TimeSideControlLayout : Panel
{
    // These are SideControlColumn's fixed tap-target and three-item placement rules,
    // independent of the configurable height of the adjacent time fields.
    internal const double TapTargetSize = 48;

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var size = MathUtils.DipToPx(TapTargetSize, scale) / scale;
        foreach (var child in Children) child.Measure(new Size(size, size));
        return new Size(size, double.IsFinite(availableSize.Height) ? availableSize.Height : Children.Count * size);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var size = MathUtils.DipToPx(TapTargetSize, scale);
        var width = MathUtils.DipToPx(finalSize.Width, scale);
        var height = MathUtils.DipToPx(finalSize.Height, scale);
        var y = (height - Children.Count * size) / 2;
        if (Children.Count == 3) y += MathUtils.DipToPx(3, scale);
        foreach (var child in Children)
        {
            child.Arrange(new Rect((width - size) / 2 / scale, y / scale, size / scale, size / scale));
            y += size;
        }
        return finalSize;
    }
}

using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// HorizontalCenterOptically.kt: correction uses the animated shape, clamped by logical padding.
internal sealed class SplitButtonOpticalLayout : Decorator
{
    protected override Size ArrangeOverride(Size finalSize)
    {
        var surface = this.GetVisualAncestors().OfType<MaterialSurface>().FirstOrDefault();
        var corners = surface?.SplitOpticalCornerRadius ?? default;
        var scale = LayoutHelper.GetLayoutScale(this);
        var correction = 0.11f * (((float)corners.TopLeft + (float)corners.BottomLeft) / 2f
            - ((float)corners.TopRight + (float)corners.BottomRight) / 2f);
        var pixels = MathUtils.RoundToInt(Math.Clamp(correction * (float)scale, -(float)(Padding.Left * scale), (float)(Padding.Right * scale)));
        Child?.Arrange(new Rect(Padding.Left + pixels / scale, Padding.Top,
            Math.Max(0, finalSize.Width - Padding.Left - Padding.Right), Math.Max(0, finalSize.Height - Padding.Top - Padding.Bottom)));
        return finalSize;
    }
}

internal sealed class SplitButtonCornerConverter : IMultiValueConverter
{
    public bool Trailing { get; set; }
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [CornerRadius outer, CornerRadius inner]
            ? Trailing ? new CornerRadius(inner.TopLeft, outer.TopRight, outer.BottomRight, inner.BottomLeft)
                : new CornerRadius(outer.TopLeft, inner.TopRight, inner.BottomRight, outer.BottomLeft)
            : AvaloniaProperty.UnsetValue;
}

// LeadingButton/TrailingButton retain SmallContainerHeight inside defaultMinSize,
// even when the caller's heightIn minimum (for example ExtraSmall) is smaller.
internal sealed class SplitButtonMinimumHeightConverter : IMultiValueConverter
{
    public object Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values is [double requested, double minimum] ? Math.Max(requested, minimum) : AvaloniaProperty.UnsetValue;
}

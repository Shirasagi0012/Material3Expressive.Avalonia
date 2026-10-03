using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

// Internal analogue of Badge.kt's badgeBounds() ruler provider. Only the upstream
// navigation item layouts provide these bounds; it does not clip or change measure.
internal sealed class BadgeBounds : AvaloniaObject
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<BadgeBounds, Control, bool>("IsEnabled");
    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);
}

internal sealed class BadgedBoxLayout : Panel
{
    private double _end = double.PositiveInfinity;
    private double _top = double.NegativeInfinity;
    private Size _badgeSize;

    public BadgedBoxLayout() => LayoutUpdated += (_, _) =>
    {
        // Avalonia assigns each ancestor's Bounds after ArrangeOverride. Resolve
        // rulers when the layout pass has completed, then re-arrange only on change.
        var bounds = this.GetVisualAncestors().OfType<Control>().FirstOrDefault(BadgeBounds.GetIsEnabled);
        var end = double.PositiveInfinity;
        var top = double.NegativeInfinity;
        if (bounds is not null && TopLevel.GetTopLevel(this) is { } root)
        {
            var origin = bounds.TranslatePoint(default, root)!.Value;
            var endPoint = bounds.TranslatePoint(new Point(bounds.Bounds.Width, 0), root)!.Value;
            var localOrigin = this.TranslatePoint(default, root)!.Value;
            var localEnd = this.TranslatePoint(new Point(Bounds.Width, 0), root)!.Value;
            // badgeBounds provides a physical ruler (not providesRelative). Compose
            // reads physical coordinates *before* placeRelative's RTL conversion.
            // Undo Avalonia's mirrored local space when reading that same ruler.
            end = Math.Max(origin.X, endPoint.X) - Math.Min(localOrigin.X, localEnd.X);
            top = origin.Y - localOrigin.Y;
        }
        if (end == _end && top == _top) return;
        _end = end;
        _top = top;
        InvalidateArrange();
    };

    protected override Size MeasureOverride(Size availableSize)
    {
        // Measure the badge first; its height never inherits the owner's minimum.
        Children[1].Measure(availableSize);
        var minimumWidth = double.IsNaN(Width) ? MinWidth : availableSize.Width;
        _badgeSize = new Size(Math.Max(Children[1].DesiredSize.Width, Math.Min(minimumWidth, availableSize.Width)), Children[1].DesiredSize.Height);
        Children[0].Measure(availableSize);
        return Children[0].DesiredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = MathUtils.DipToPx(finalSize.Width, scale);
        var badge = Children[1];
        var badgeWidth = MathUtils.DipToPx(_badgeSize.Width, scale);
        var badgeHeight = MathUtils.DipToPx(_badgeSize.Height, scale);
        var hasContent = badgeWidth > MathUtils.DipToPx(6, scale);
        // No explicit RTL flip: Avalonia mirrors this panel's local coordinates,
        // the equivalent of Compose placeRelative, without mirroring text glyphs.
        var x = width - MathUtils.DipToPx(hasContent ? 12 : 6, scale);
        if (double.IsFinite(_end)) x = Math.Min(x, (int)(_end * scale) - badgeWidth);
        var y = -badgeHeight + MathUtils.DipToPx(hasContent ? 14 : 6, scale);
        if (double.IsFinite(_top)) y = Math.Max(y, (int)(_top * scale));
        Children[0].Arrange(new Rect(finalSize));
        badge.Arrange(new Rect(x / scale, y / scale, badgeWidth / scale, badgeHeight / scale));
        return finalSize;
    }
}

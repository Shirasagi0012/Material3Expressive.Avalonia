using Avalonia;
using Avalonia.Controls;

namespace Material3Expressive.Foundation.Overlays;

// Fixed Core ComposeSceneLayerMeasurePolicy.kt: the shortest client dimension selects
// the platform width cap, before explicit content constraints are applied.
internal class DialogLayout : Grid
{
    public static readonly StyledProperty<bool> UsePlatformDefaultWidthProperty =
        AvaloniaProperty.Register<DialogLayout, bool>(nameof(UsePlatformDefaultWidth), true);
    public bool UsePlatformDefaultWidth { get => GetValue(UsePlatformDefaultWidthProperty); set => SetValue(UsePlatformDefaultWidthProperty, value); }

    static DialogLayout() => AffectsMeasure<DialogLayout>(UsePlatformDefaultWidthProperty);

    protected Size PlatformSize(Size availableSize)
    {
        if (!UsePlatformDefaultWidth) return availableSize;
        var shortest = Math.Min(availableSize.Width, availableSize.Height);
        var preferred = shortest >= 600 ? 580 : shortest >= 480 ? 440 : 320;
        return new Size(Math.Min(availableSize.Width, preferred), availableSize.Height);
    }

    protected override Size MeasureCore(Size availableSize) => base.MeasureCore(PlatformSize(availableSize));

    protected override Size ArrangeOverride(Size finalSize) => base.ArrangeOverride(finalSize.Constrain(DesiredSize));
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class NavigationDrawerLayout : Panel
{
    internal NavigationDrawer? Owner { get; set; }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 3 || Owner is null) return default;
        var page = Children[0]; var sheet = Children[2];
        sheet.Measure(availableSize);
        var remaining = Owner is PermanentNavigationDrawer ? Math.Max(0, availableSize.Width - sheet.DesiredSize.Width) : availableSize.Width;
        page.Measure(new Size(remaining, availableSize.Height));
        Children[1].Measure(availableSize);
        if (Owner is DismissibleNavigationDrawer and not ModalNavigationDrawer) return page.DesiredSize;
        var naturalWidth = Owner is PermanentNavigationDrawer ? sheet.DesiredSize.Width + page.DesiredSize.Width : Math.Max(sheet.DesiredSize.Width, page.DesiredSize.Width);
        return new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : naturalWidth,
            double.IsFinite(availableSize.Height) ? availableSize.Height : Math.Max(sheet.DesiredSize.Height, page.DesiredSize.Height));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3 || Owner is null) return finalSize;
        var page = Children[0]; var scrim = Children[1]; var sheet = Children[2];
        if (scrim is Border border) border.Background = (Owner as ModalNavigationDrawer)?.ScrimBrush;
        var width = sheet.DesiredSize.Width; var scale = LayoutHelper.GetLayoutScale(this);
        Owner.AnimatedState?.UpdateAnchors(width, scale);
        var state = Owner.AnimatedState;
        var offset = state is null ? 0 : MathUtils.RoundToInt(state.OffsetPx) / scale;
        var contentX = Owner is ModalNavigationDrawer ? 0 : width + offset;
        page.Arrange(new Rect(contentX, 0, Owner is PermanentNavigationDrawer ? Math.Max(0, finalSize.Width - width) : finalSize.Width, finalSize.Height));
        sheet.Arrange(new Rect(offset, 0, width, sheet.DesiredSize.Height));
        var fraction = state is null ? 1 : Math.Clamp((state.OffsetPx - state.ClosedPx) / -state.ClosedPx, 0, 1);
        var visible = state is null || (Owner is ModalNavigationDrawer ? fraction != 0 : contentX != 0);
        sheet.Opacity = visible ? 1 : 0;
        sheet.IsHitTestVisible = visible;
        KeyboardNavigation.SetTabNavigation(sheet, visible ? KeyboardNavigationMode.Continue : KeyboardNavigationMode.None);
        scrim.Arrange(new Rect(finalSize));
        scrim.Opacity = Owner is ModalNavigationDrawer ? fraction : 0;
        scrim.IsHitTestVisible = Owner is ModalNavigationDrawer && state?.IsOpen == true;
        return finalSize;
    }
}

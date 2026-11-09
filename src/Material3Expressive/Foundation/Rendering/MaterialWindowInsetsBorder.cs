using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Material3Expressive.Foundation.Rendering;

// Template bridge: the public control owns the consumption so its logical item/header content
// inherits it too. The border only applies that control's remaining physical padding.
internal sealed class MaterialWindowInsetsBorder : Border
{
    private Control? _owner;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = TemplatedParent as Control;
        if (_owner is not null) _owner.PropertyChanged += OwnerChanged;
        UpdatePadding();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FlowDirectionProperty) UpdatePadding();
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    { if (e.Property == MaterialWindowInsets.UnconsumedInsetsProperty) UpdatePadding(); }
    private void UpdatePadding()
    {
        var insets = _owner?.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty) ?? default;
        Padding = FlowDirection == FlowDirection.RightToLeft
            ? new Thickness(insets.Right, insets.Top, insets.Left, insets.Bottom) : insets;
    }
}

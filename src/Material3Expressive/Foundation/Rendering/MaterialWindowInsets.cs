using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>Propagates padding already applied by a content/host container to nested Scaffolds.</summary>
public sealed class MaterialWindowInsets : AvaloniaObject
{
    private MaterialWindowInsets() { }
    /// <summary>Logical start/top/end/bottom padding to add to ancestor consumption; applies no layout padding.</summary>
    public static readonly AttachedProperty<Thickness> ConsumePaddingProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("ConsumePadding");
    internal static readonly AttachedProperty<Thickness> ConsumedPaddingProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("ConsumedPadding", inherits: true);
    private static readonly AttachedProperty<PaddingScope?> ScopeProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, PaddingScope?>("Scope");

    static MaterialWindowInsets() => ConsumePaddingProperty.Changed.AddClassHandler<Control>((owner, _) =>
    {
        if (owner.GetValue(ScopeProperty) is not null) return;
        var scope = new PaddingScope(owner);
        owner.SetValue(ScopeProperty, scope);
        scope.Initialize();
    });
    public static Thickness GetConsumePadding(Control element) => element.GetValue(ConsumePaddingProperty);
    public static void SetConsumePadding(Control element, Thickness value) => element.SetValue(ConsumePaddingProperty, value);

    // A provider's local total must not hide its parent's updates. Non-providers simply inherit
    // the total. Scope subscriptions are severed on detach and rebuilt after reparenting.
    private sealed class PaddingScope(Control owner)
    {
        private StyledElement? _parent;
        private bool _detached;
        public void Initialize()
        {
            owner.PropertyChanged += Changed;
            owner.AttachedToLogicalTree += Attached;
            owner.DetachedFromLogicalTree += Detached;
            owner.AttachedToVisualTree += VisualAttached;
            owner.DetachedFromVisualTree += VisualDetached;
            Reparent();
        }
        private void Attached(object? sender, LogicalTreeAttachmentEventArgs e) { _detached = false; Reparent(); }
        private void Detached(object? sender, LogicalTreeAttachmentEventArgs e) { _detached = true; Reparent(); }
        private void VisualAttached(object? sender, VisualTreeAttachmentEventArgs e) { _detached = false; Reparent(); }
        private void VisualDetached(object? sender, VisualTreeAttachmentEventArgs e) { _detached = true; Reparent(); }
        private void Reparent()
        {
            var parent = _detached ? null : owner.Parent ?? owner.GetVisualParent() as StyledElement ?? owner.TemplatedParent as StyledElement;
            if (!ReferenceEquals(parent, _parent))
            {
                if (_parent is not null) _parent.PropertyChanged -= ParentChanged;
                _parent = parent;
                if (_parent is not null) _parent.PropertyChanged += ParentChanged;
            }
            Update();
        }
        private void ParentChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == ConsumedPaddingProperty) Update();
        }
        private void Changed(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StyledElement.ParentProperty || e.Property == StyledElement.TemplatedParentProperty) Reparent();
            else if (e.Property == ConsumePaddingProperty || e.Property == Visual.FlowDirectionProperty) Update();
        }
        private void Update()
        {
            var inherited = _parent?.GetValue(ConsumedPaddingProperty) ?? default;
            var padding = GetConsumePadding(owner);
            var scale = (float)LayoutHelper.GetLayoutScale(owner);
            double Rounded(double value) => MathUtils.DipToPx((float)value, scale) / (double)scale;
            var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            owner.SetValue(ConsumedPaddingProperty, inherited + new Thickness(
                Rounded(rtl ? padding.Right : padding.Left), Rounded(padding.Top),
                Rounded(rtl ? padding.Left : padding.Right), Rounded(padding.Bottom)));
        }
    }
}

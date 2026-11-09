using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Foundation.Rendering;

/// <summary>Propagates consumed padding and physical window insets to participating descendant layouts.</summary>
public sealed class MaterialWindowInsets : AvaloniaObject
{
    private MaterialWindowInsets() { }
    /// <summary>Logical start/top/end/bottom padding to add to ancestor consumption; applies no layout padding.</summary>
    public static readonly AttachedProperty<Thickness> ConsumePaddingProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("ConsumePadding");
    /// <summary>Physical left/top/right/bottom insets already handled by the host; unions with ancestor consumption and applies no padding.</summary>
    public static readonly AttachedProperty<Thickness> ConsumeInsetsProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("ConsumeInsets");
    // Insets applied by a participating control's own layout, after caller-supplied consumption.
    internal static readonly AttachedProperty<Thickness> AppliedInsetsProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("AppliedInsets");
    internal static readonly AttachedProperty<Thickness> UnconsumedInsetsProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("UnconsumedInsets");
    internal static readonly AttachedProperty<Thickness> ConsumedPaddingProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, Thickness>("ConsumedPadding", inherits: true);
    private static readonly AttachedProperty<PaddingScope?> ScopeProperty =
        AvaloniaProperty.RegisterAttached<MaterialWindowInsets, Control, PaddingScope?>("Scope");

    static MaterialWindowInsets()
    {
        ConsumePaddingProperty.Changed.AddClassHandler<Control>((owner, _) => EnsureScope(owner));
        ConsumeInsetsProperty.Changed.AddClassHandler<Control>((owner, _) => EnsureScope(owner));
        AppliedInsetsProperty.Changed.AddClassHandler<Control>((owner, _) => EnsureScope(owner));
    }
    internal static void EnsureScope(Control owner)
    {
        if (owner.GetValue(ScopeProperty) is not null) return;
        var scope = new PaddingScope(owner);
        owner.SetValue(ScopeProperty, scope);
        scope.Initialize();
    }
    internal static void Refresh(Control owner) => owner.GetValue(ScopeProperty)?.Update();
    public static Thickness GetConsumeInsets(Control element) => element.GetValue(ConsumeInsetsProperty);
    public static void SetConsumeInsets(Control element, Thickness value) => element.SetValue(ConsumeInsetsProperty, value);
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
            else if (e.Property == ConsumePaddingProperty || e.Property == ConsumeInsetsProperty || e.Property == AppliedInsetsProperty || e.Property == Visual.FlowDirectionProperty) Update();
        }
        public void Update()
        {
            var inherited = _parent?.GetValue(ConsumedPaddingProperty) ?? default;
            var padding = GetConsumePadding(owner);
            var scale = (float)LayoutHelper.GetLayoutScale(owner);
            double Rounded(double value) => MathUtils.DipToPx((float)value, scale) / (double)scale;
            var rtl = owner.FlowDirection == FlowDirection.RightToLeft;
            var added = inherited + new Thickness(
                Rounded(rtl ? padding.Right : padding.Left), Rounded(padding.Top),
                Rounded(rtl ? padding.Left : padding.Right), Rounded(padding.Bottom));
            var insets = GetConsumeInsets(owner);
            var consumed = new Thickness(
                Math.Max(Rounded(insets.Left), added.Left), Math.Max(Rounded(insets.Top), added.Top),
                Math.Max(Rounded(insets.Right), added.Right), Math.Max(Rounded(insets.Bottom), added.Bottom));
            var applied = owner.GetValue(AppliedInsetsProperty);
            var remaining = new Thickness(
                Math.Max(0, Rounded(applied.Left) - consumed.Left), Math.Max(0, Rounded(applied.Top) - consumed.Top),
                Math.Max(0, Rounded(applied.Right) - consumed.Right), Math.Max(0, Rounded(applied.Bottom) - consumed.Bottom));
            owner.SetValue(UnconsumedInsetsProperty, remaining);
            owner.SetValue(ConsumedPaddingProperty, consumed + remaining);
        }
    }
}

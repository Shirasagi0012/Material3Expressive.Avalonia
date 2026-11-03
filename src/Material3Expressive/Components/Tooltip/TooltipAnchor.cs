using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed class TooltipAnchor(Control owner, Action positionChanged)
{
    private readonly List<Visual> _ancestors = [];

    public void Attach()
    {
        Detach();
        foreach (var ancestor in owner.GetVisualAncestors())
        {
            _ancestors.Add(ancestor);
            ancestor.PropertyChanged += OnAncestorChanged;
            if (ancestor.RenderTransform is IMutableTransform transform) transform.Changed += OnTransformChanged;
        }
    }

    public void Detach()
    {
        foreach (var ancestor in _ancestors)
        {
            ancestor.PropertyChanged -= OnAncestorChanged;
            if (ancestor.RenderTransform is IMutableTransform transform) transform.Changed -= OnTransformChanged;
        }
        _ancestors.Clear();
    }

    private void OnAncestorChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Visual.RenderTransformProperty)
        {
            if (change.OldValue is IMutableTransform old) old.Changed -= OnTransformChanged;
            if (change.NewValue is IMutableTransform next) next.Changed += OnTransformChanged;
        }
        if (change.Property == Visual.RenderTransformProperty || change.Property == Visual.RenderTransformOriginProperty
            || change.Property == Visual.BoundsProperty || change.Property == Visual.HasMirrorTransformProperty)
            positionChanged();
    }

    private void OnTransformChanged(object? sender, EventArgs e) => positionChanged();

    public static Rect BoundsInWindow(Control owner, TopLevel topLevel)
    {
        // BasicTooltipBox puts Popup and WrappedAnchor in an unmodified outer Box. The
        // modifier belongs to WrappedAnchor: its render transform does not move the Popup's
        // anchor. Ancestor transforms do participate in Core Popup.positionInWindow().
        var parent = owner.GetVisualParent()!;
        var mirrored = false;
        for (Visual? visual = parent; visual is not null && visual != topLevel; visual = visual.GetVisualParent())
            mirrored ^= visual.HasMirrorTransform;
        var origin = mirrored ? owner.Bounds.TopRight : owner.Bounds.TopLeft;
        return new Rect(parent.TranslatePoint(origin, topLevel)!.Value, owner.Bounds.Size);
    }
}

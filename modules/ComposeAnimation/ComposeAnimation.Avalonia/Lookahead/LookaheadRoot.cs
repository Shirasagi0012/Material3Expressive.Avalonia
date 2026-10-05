using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimation;

/// <summary>
/// Explicit finite-viewport entry into cooperative lookahead. The domain coordinator is independent
/// of this control; the root remains available for applications needing a fixed viewport.
/// </summary>
public sealed class LookaheadRoot : Decorator, ILookaheadPlacementHost
{
    internal LookaheadContext Context { get; }

    public LookaheadRoot()
    {
        Context = new LookaheadContext(this, () => Child);
        LookaheadEnvironment.SetContext(this, Context);
        Context.TargetLayoutUpdated += (_, args) => TargetLayoutUpdated?.Invoke(this, args);
    }

    /// <summary>Last complete target layout; null before layout or after target invalidation.</summary>
    public LookaheadLayoutSnapshot? Snapshot => Context.Snapshot;
    /// <summary>Raised after a complete target measure/placement and before actual child layout.</summary>
    public event EventHandler? TargetLayoutUpdated;
    internal long ApproachInvalidationPassCount => Context.ApproachInvalidationPassCount;

    /// <summary>Returns a participant's target in its current domain, or null outside a target tree.</summary>
    public static LookaheadLayoutResult? TargetFor(Control control)
        => LookaheadScope.FindContext(control)?.Snapshot?.Results.GetValueOrDefault(control);

    /// <summary>Invalidates the target after external layout-policy inputs change.</summary>
    public void InvalidateTargetLayout() => Context.InvalidateTargetLayout();
    /// <summary>Invalidates actual placement without invalidating target layout.</summary>
    public void InvalidateApproachPlacement() => Context.InvalidateApproachPlacement();
    /// <summary>Synchronously moves existing content within this attached domain, preserving its motion.</summary>
    public void MoveContent(Control content, Action move) => Context.MoveContent(content, move);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (Context is not null && (change.Property == ChildProperty || change.Property == PaddingProperty))
            Context.InvalidateTargetLayout();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Context.Attach();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Context.Detach();
        base.OnDetachedFromVisualTree(e);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Padding != default)
            throw new NotSupportedException("LookaheadRoot does not yet support Padding. Use Margin on its child.");
        return Context.MeasureLegacy(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize) => Context.ArrangeLegacy(finalSize);
}

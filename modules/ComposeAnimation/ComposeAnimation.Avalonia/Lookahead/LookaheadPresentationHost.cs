using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;

namespace ComposeAnimation;

/// <summary>
/// Private visual and inheritance bridge for a locally presented lookahead child. The owner keeps
/// the user's logical child; this host supplies the actual layout slot without breaking inherited
/// properties or the current lookahead context.
/// </summary>
internal sealed class LookaheadPresentationHost : Control, ILookaheadPlacementHost
{
    private readonly Func<Size, Size> _measure;
    private readonly Func<Size, Size> _arrange;
    private readonly Action _contentInvalidated;
    private Control? _child;
    private int _suppressInvalidationDepth;
    private int _measureDepth;

    internal LookaheadPresentationHost(Control owner, Func<Size, Size> measure,
        Func<Size, Size> arrange, Action contentInvalidated)
    {
        ArgumentNullException.ThrowIfNull(owner);
        _measure = measure ?? throw new ArgumentNullException(nameof(measure));
        _arrange = arrange ?? throw new ArgumentNullException(nameof(arrange));
        _contentInvalidated = contentInvalidated ?? throw new ArgumentNullException(nameof(contentInvalidated));

        // The host is an implementation detail rather than user content. A logical parent gives
        // it ordinary property/resource/context inheritance without adding it to the public
        // logical-children collection owned by AnimatedBounds.
        ((ISetLogicalParent)this).SetParent(owner);
    }

    internal Control? Child => _child;

    /// <summary>
    /// Changes only visual and inheritance ownership. The caller establishes/removes the user's
    /// logical parent before/after this operation.
    /// </summary>
    internal void SetChild(Control? child)
    {
        VerifyAccess();
        if (ReferenceEquals(_child, child)) return;

        if (_child is { } oldChild)
        {
            VisualChildren.Remove(oldChild);
            ((ISetInheritanceParent)oldChild).SetParent(oldChild.Parent);
        }

        _child = child;
        if (child is not null)
        {
            ((ISetInheritanceParent)child).SetParent(this);
            VisualChildren.Add(child);
        }

        // SetChild is a real target input even when an earlier approach request already left this
        // host measure-invalid. Publish it explicitly and suppress the invalidation callback so it
        // is reported exactly once.
        _suppressInvalidationDepth++;
        try
        {
            InvalidateMeasure();
            InvalidateArrange();
        }
        finally { _suppressInvalidationDepth--; }
        _contentInvalidated();
    }

    /// <summary>
    /// Requests another approach measurement without classifying that request as a target-content
    /// change. If real child invalidation was merged into this dirty state, MeasureOverride reports
    /// it after the child has been sampled by the owner delegate.
    /// </summary>
    internal void InvalidateApproachMeasure()
    {
        VerifyAccess();
        _suppressInvalidationDepth++;
        try { InvalidateMeasure(); }
        finally { _suppressInvalidationDepth--; }
    }

    internal void InvalidateApproachArrange()
    {
        VerifyAccess();
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // A child can become dirty after this host was already invalidated for an approach frame.
        // In that case Avalonia has no second host invalidation to observe. Remember the source
        // before the delegate measures it, then notify the owner after the sample is coherent.
        var contentWasInvalid = _child is { IsMeasureValid: false };
        _measureDepth++;
        Size result;
        try { result = _measure(availableSize); }
        finally { _measureDepth--; }
        if (contentWasInvalid) _contentInvalidated();
        return result;
    }

    protected override Size ArrangeOverride(Size finalSize) => _arrange(finalSize);

    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
        if (_suppressInvalidationDepth == 0 && _measureDepth == 0)
            _contentInvalidated();
    }
}

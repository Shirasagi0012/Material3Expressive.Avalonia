using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimation;

/// <summary>A private visual bridge; effects never replace the user's transform or opacity.</summary>
internal sealed class EnterExitSurface : Control, ILookaheadLayoutPolicy
{
    internal bool OwnsLogicalChild { get; set; } = true;
    private Control? _child;

    public Control? Child
    {
        get => _child;
        set
        {
            if (ReferenceEquals(_child, value)) return;
            if (_child is { })
            {
                VisualChildren.Remove(_child);
                if (OwnsLogicalChild) LogicalChildren.Remove(_child);
            }

            _child = value;
            if (value is { })
            {
                VisualChildren.Add(value);
                if (OwnsLogicalChild) LogicalChildren.Add(value);
            }

            InvalidateMeasure();
        }
    }

    internal EnterExitSurface()
    {
        LookaheadLayout.RegisterInherited(this);
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];

    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        if (Child is not { } child) return context.Layout(default);
        LookaheadPlaceable measured = context.Measure(child, constraints);
        return context.Layout(measured.DesiredSize,
            placement => placement.Arrange(measured, new Rect(placement.FinalSize)));
    }

    protected override Size MeasureOverride(Size availableSize)
        => LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
        => LookaheadLayout.TryMeasureCore(this, availableSize, out Size desired) ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out Rect bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty) LookaheadLayout.VisualParentChanged(this);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}

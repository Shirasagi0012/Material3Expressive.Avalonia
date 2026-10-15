using Avalonia;
using Avalonia.Controls;

namespace ComposeAnimation;

public partial class AnimatedVisibility
{
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren
        => IsContentPresent
         && (_enterExitLayout is { } layout) ? [layout] : [];

    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties => [TargetVisibleProperty, IsContentPresentProperty];

    void ILookaheadPreparationParticipant.PrepareLookahead() => PreparePresentation();

    private void PreparePresentation()
    {
        if (!IsContentPresent) return;

        ApplyTemplate();
        _contentPresenter?.PrepareContent();
    }

    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        if (!IsContentPresent || _enterExitLayout is not { } layout)
            return context.LayoutWithoutFootprint();

        LookaheadPlaceable content = context.Measure(layout, constraints);
        Size contentSize = content.DesiredSize;

        if (context.Phase == LookaheadLayoutPhase.Target && !TargetVisible)
            return context.LayoutWithoutFootprint(placement => placement.Arrange(content, new Rect(contentSize)));

        if (context.Phase != LookaheadLayoutPhase.Target)
            Scope?.MarkInitialLayoutReady();

        return context.Layout(
            contentSize,
            placement => placement.Arrange(content, new Rect(placement.FinalSize)));
    }

    protected override Size MeasureCore(Size availableSize)
    {
        try
        {
            PreparePresentation();
            if (LookaheadLayout.TryMeasureCore(this, availableSize, out Size desired))
                return desired;

            return IsContentPresent ? base.MeasureCore(availableSize) : default;
        }
        catch (Exception error)
        {
            _failed = true;
            SynchronizePresentation(true);
            AnimationScope.ReportError(this, error);
            throw;
        }
    }

    protected override Size MeasureOverride(Size availableSize)
        => LookaheadPolicyRuntime.MeasureActual(this, this, LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));

    protected override Size ArrangeOverride(Size finalSize)
        => IsContentPresent ? LookaheadPolicyRuntime.ArrangeActual(this, finalSize) : finalSize;

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out Rect bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }
}

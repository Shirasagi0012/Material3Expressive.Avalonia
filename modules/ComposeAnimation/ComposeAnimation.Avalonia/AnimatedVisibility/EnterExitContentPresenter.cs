using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace ComposeAnimation;

/// <summary>ContentPresenter's decoration geometry with independent target measurement.</summary>
internal sealed class EnterExitContentPresenter : ContentPresenter, ILookaheadLayoutPolicy,
    ILookaheadPreparationParticipant, ILookaheadTargetInvalidationSource
{
    internal EnterExitContentPresenter()
    {
        LookaheadLayout.RegisterInherited(this);
    }

    public event EventHandler? TargetMeasureInvalidated;
    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];

    IEnumerable<AvaloniaProperty> ILookaheadLayoutNode.LookaheadProperties =>
        [PaddingProperty, BorderThicknessProperty, HorizontalContentAlignmentProperty, VerticalContentAlignmentProperty];

    internal void PrepareContent()
    {
        ApplyTemplate();
        Child?.ApplyStyling();
    }

    void ILookaheadPreparationParticipant.PrepareLookahead() => PrepareContent();

    LookaheadMeasureResult ILookaheadLayoutPolicy.MeasureLayout(LookaheadMeasureContext context, LookaheadConstraints constraints)
    {
        double scale = LayoutHelper.GetLayoutScale(this);

        Thickness padding = UseLayoutRounding
            ? LayoutHelper.RoundLayoutThickness(Padding, scale)
            : Padding;
        Thickness border = UseLayoutRounding
            ? LayoutHelper.RoundLayoutThickness(BorderThickness, scale)
            : BorderThickness;
        Thickness decoration = padding + border;

        if (Child is not { } child)
            return context.Layout(default(Size).Inflate(decoration));

        LookaheadPlaceable content = context.Measure(child, constraints.Deflate(decoration));
        Size desired = content.DesiredSize.Inflate(decoration);

        return context.Layout(desired, placement => {
            // ContentPresenter.ArrangeOverrideImpl: align its decorated desired size first,
            // then deflate padding/border to obtain the actual child slot.
            Size available = placement.FinalSize;
            var size = new Size(
                HorizontalContentAlignment == HorizontalAlignment.Stretch
                    ? available.Width
                    : Math.Min(available.Width, desired.Width),
                VerticalContentAlignment == VerticalAlignment.Stretch
                    ? available.Height
                    : Math.Min(available.Height, desired.Height));

            if (UseLayoutRounding)
            {
                size = LayoutHelper.RoundLayoutSizeUp(size, scale);
                available = LayoutHelper.RoundLayoutSizeUp(available, scale);
            }

            var origin = new Point(
                HorizontalContentAlignment switch {
                    HorizontalAlignment.Center => (available.Width - size.Width) / 2,
                    HorizontalAlignment.Right => available.Width - size.Width,
                    _ => 0
                },
                VerticalContentAlignment switch {
                    VerticalAlignment.Center => (available.Height - size.Height) / 2,
                    VerticalAlignment.Bottom => available.Height - size.Height,
                    _ => 0
                });
            if (UseLayoutRounding) origin = LayoutHelper.RoundLayoutPoint(origin, scale);
            placement.Arrange(content, new Rect(origin, size).Deflate(decoration));
        });
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        PrepareContent();
        return LookaheadPolicyRuntime.MeasureActual(this, this,
            LookaheadPolicyRuntime.ActualPhase(this),
            LookaheadConstraints.AtMost(availableSize));
    }

    protected override Size ArrangeOverride(Size finalSize) => LookaheadPolicyRuntime.ArrangeActual(this, finalSize);

    protected override Size MeasureCore(Size availableSize)
    {
        PrepareContent();
        return LookaheadLayout.TryMeasureCore(this, availableSize, out Size desired)
            ? desired
            : base.MeasureCore(availableSize);
    }

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadLayout.TryArrangeCore(this, finalRect, out Rect bounds))
            Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VisualParentProperty)
            LookaheadLayout.VisualParentChanged(this);
        if (change.Property == ChildProperty)
            TargetMeasureInvalidated?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadLayout.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }
}

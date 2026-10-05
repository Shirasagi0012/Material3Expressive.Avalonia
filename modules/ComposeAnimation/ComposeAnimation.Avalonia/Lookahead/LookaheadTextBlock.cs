using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace ComposeAnimation;

/// <summary>
/// Experimental plain-text TextBlock with independent target measurement. Actual rendering
/// remains TextBlock rendering. Inlines are not supported inside a LookaheadRoot yet.
/// </summary>
public class LookaheadTextBlock : TextBlock, ILookaheadConstrainedLayoutNode, ILookaheadTargetInvalidationSource
{
    private event EventHandler? TargetMeasureInvalidated;

    event EventHandler? ILookaheadTargetInvalidationSource.TargetMeasureInvalidated
    {
        add => TargetMeasureInvalidated += value;
        remove => TargetMeasureInvalidated -= value;
    }

    /// <summary>
    /// Refreshes text measurement after an external dependency changes (for example a font
    /// collection or an in-place FontFeatures edit). Also notifies the target root even if
    /// this control is already measure-invalid. Ordinary animation samples must not call this.
    /// </summary>
    public void InvalidateTargetMeasure()
    {
        VerifyAccess();
        InvalidateTextLayout();
        TargetMeasureInvalidated?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == InlinesProperty)
        {
            if (change.OldValue is InlineCollection oldInlines) oldInlines.Invalidated -= InlinesInvalidated;
            if (change.NewValue is InlineCollection newInlines) newInlines.Invalidated += InlinesInvalidated;
        }
        if (p == TextProperty || p == FontFamilyProperty || p == FontSizeProperty ||
            p == FontStyleProperty || p == FontWeightProperty || p == FontStretchProperty ||
            p == FontFeaturesProperty || p == TextDecorationsProperty || p == ForegroundProperty ||
            p == FlowDirectionProperty || p == TextAlignmentProperty || p == TextWrappingProperty ||
            p == TextTrimmingProperty || p == LineHeightProperty || p == LineSpacingProperty ||
            p == LetterSpacingProperty || p == MaxLinesProperty || p == PaddingProperty || p == InlinesProperty)
            InvalidateTargetMeasure();
    }

    private void InlinesInvalidated(object? sender, EventArgs e) => InvalidateTargetMeasure();

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Array.Empty<Control>();

    Size ILookaheadConstrainedLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, LookaheadConstraints constraints)
        => ((ILookaheadLayoutNode)this).MeasureLookahead(builder, constraints.MaxSize);
    Size ILookaheadConstrainedLayoutNode.MeasureApproach(LookaheadConstraints constraints)
        => MeasureOverride(constraints.MaxSize);

    protected override Size MeasureCore(Size availableSize)
        => LookaheadMeasure.TryMeasureCore(this, availableSize, out var desired) ? desired : base.MeasureCore(availableSize);

    protected override void ArrangeCore(Rect finalRect)
    {
        if (LookaheadMeasure.TryArrangeCore(this, finalRect, ArrangeOverride, out var bounds)) Bounds = bounds;
        else base.ArrangeCore(finalRect);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LookaheadMeasure.Forget(this);
        base.OnDetachedFromVisualTree(e);
    }

    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
    {
        if (Inlines is { Count: > 0 })
            throw new NotSupportedException("LookaheadTextBlock currently supports Text only. Run/Span and InlineUIContainer target measurement are not implemented.");

        var padding = UseLayoutRounding
            ? LayoutHelper.RoundLayoutThickness(Padding, LayoutHelper.GetLayoutScale(this)) : Padding;
        var constraint = available.Deflate(padding);
        var properties = new GenericTextRunProperties(
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize,
            TextDecorations, Foreground, fontFeatures: FontFeatures);
        var paragraph = new GenericTextParagraphProperties(FlowDirection, TextAlignment, true, false,
            properties, TextWrapping, LineHeight, 0, LetterSpacing);
        AvaloniaTextLayoutInterop.SetLineSpacing(paragraph, LineSpacing);

        // Do not call base.CreateTextLayout: it consumes the live _constraint/_textRuns and
        // cache. This temporary layout owns its runs; the published snapshot stores geometry
        // only. No target resource depends on subsequent actual Measure/Arrange or disposal.
        using var layout = new TextLayout(new SimpleTextSource(Text ?? "", properties), paragraph,
            TextTrimming, constraint.Width, constraint.Height, MaxLines);
        return new Size(layout.WidthIncludingTrailingWhitespace, layout.Height).Inflate(padding);
    }

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds) { }
}

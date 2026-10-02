using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Material3Expressive.Components;

internal sealed class ButtonContentPresenter : ContentPresenter
{
    private IDataTemplate? _automaticLabelTemplate;
    private IDataTemplate AutomaticLabelTemplate => _automaticLabelTemplate ??= new FuncDataTemplate(
        // Remain a fallback even when templates are replaced before UpdateChild().
        data => data is string && this.FindDataTemplate(data) is null,
        (_, _) =>
        {
            var label = new ButtonLabel { ClipToBounds = true };
            label.Bind(TextBlock.TextProperty, new Binding(nameof(DataContext)) { Source = label });
            return label;
        }, supportsRecycling: true);

    static ButtonContentPresenter() => ContentTemplateProperty.OverrideMetadata<ButtonContentPresenter>(
        new StyledPropertyMetadata<IDataTemplate?>(coerce: (control, template) =>
        {
            var presenter = (ButtonContentPresenter)control;
            return template ?? (presenter.Content is string && !presenter.RecognizesAccessKey
                ? presenter.AutomaticLabelTemplate : null);
        }));

    protected override Type StyleKeyOverride => typeof(ContentPresenter);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ContentProperty || change.Property == RecognizesAccessKeyProperty)
            CoerceValue(ContentTemplateProperty);
        base.OnPropertyChanged(change);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        CoerceValue(ContentTemplateProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = base.MeasureOverride(availableSize);
        // Compose's constrained paragraph keeps the available width after soft wrapping.
        // TextBlock instead reports its longest formatted line, which would re-center
        // the paragraph. Apply this only to the automatic string label, preserving
        // caller-supplied controls/templates and intrinsic width for hard line breaks.
        if (ReferenceEquals(ContentTemplate, AutomaticLabelTemplate) && Child is ButtonLabel label
            && double.IsFinite(availableSize.Width)
            && label.TextLayout.TextLines.Any(line => line.TextLineBreak?.IsSplit == true))
            return desired.WithWidth(availableSize.Width);
        return desired;
    }

    private sealed class ButtonLabel : TextBlock
    {
        protected override Type StyleKeyOverride => typeof(TextBlock);

        // ParagraphLayoutCache/SkikoParagraph keep every line for overflow=Clip;
        // only ellipsis limits paragraph height. Avalonia's outer MeasureCore
        // still constrains DesiredSize; Bounds/ClipToBounds constrain the draw.
        protected override Size MeasureOverride(Size availableSize) => base.MeasureOverride(
            TextTrimming == TextTrimming.None ? availableSize.WithHeight(double.PositiveInfinity) : availableSize);

        protected override Size ArrangeOverride(Size finalSize)
        {
            base.ArrangeOverride(TextTrimming == TextTrimming.None ? finalSize.WithHeight(double.PositiveInfinity) : finalSize);
            return finalSize;
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

public abstract class ButtonGroupItem : ContentControl
{
    public static readonly StyledProperty<double> WeightProperty =
        AvaloniaProperty.Register<ButtonGroupItem, double>(nameof(Weight), double.NaN,
            validate: weight => double.IsNaN(weight) || weight > 0);

    public static readonly StyledProperty<double> CompressionLimitProperty =
        AvaloniaProperty.Register<ButtonGroupItem, double>(nameof(CompressionLimit));
    public static readonly StyledProperty<Control?> PressSourceProperty =
        AvaloniaProperty.Register<ButtonGroupItem, Control?>(nameof(PressSource));
    public static readonly StyledProperty<VerticalAlignment?> AlignmentProperty =
        AvaloniaProperty.Register<ButtonGroupItem, VerticalAlignment?>(nameof(Alignment));

    private ButtonGroupWidthMotion? _widthMotion;

    public double Weight
    {
        get => GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    public double CompressionLimit
    {
        get => GetValue(CompressionLimitProperty);
        set => SetValue(CompressionLimitProperty, value);
    }

    public Control? PressSource
    {
        get => GetValue(PressSourceProperty);
        set => SetValue(PressSourceProperty, value);
    }

    public VerticalAlignment? Alignment
    {
        get => GetValue(AlignmentProperty);
        set => SetValue(AlignmentProperty, value);
    }

    internal Button? ActionButton { get; set; }
    internal float PressProgress => _widthMotion?.Progress ?? 0;
    internal virtual MaterialInteractionSource? WidthInteractionSource => null;
    internal virtual double ResolvedCompressionLimit => PressSource is null && WidthInteractionSource is null ? 0 : CompressionLimit;
    internal abstract Control CreateMenuContent(ButtonGroupMenuState state);

    internal void ReconnectPress()
    {
        var source = WidthInteractionSource;
        var control = source is null ? PressSource ?? ActionButton : null;
        if (this.IsAttachedToVisualTree() && (source is not null || control is not null))
        {
            if (_widthMotion is null)
                _widthMotion = new ButtonGroupWidthMotion(this, control, source);
            else
                _widthMotion.SetSource(control, source);
        }
        else
        {
            _widthMotion?.Dispose();
            _widthMotion = null;
        }
        this.FindAncestorOfType<ButtonGroupLayout>()?.InvalidateItemsMeasure();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ReconnectPress();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _widthMotion?.Dispose();
        _widthMotion = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == PressSourceProperty)
            ReconnectPress();

        if (change.Property == WeightProperty || change.Property == CompressionLimitProperty || change.Property == AlignmentProperty)
            this.FindAncestorOfType<ButtonGroupLayout>()?.InvalidateItemsMeasure();
    }
}

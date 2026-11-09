using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>Logical horizontal placement of a <see cref="CardColumnItem"/> in its column.</summary>
public enum CardColumnAlignment { Start, Center, End }

/// <summary>
/// An explicit Box-like child of CardColumn. Weight constrains this host, while its
/// content is measured without the host's minimum height and placed at TopStart.
/// </summary>
public sealed class CardColumnItem : Decorator
{
    public static readonly StyledProperty<double> WeightProperty =
        AvaloniaProperty.Register<CardColumnItem, double>(nameof(Weight), double.NaN,
            validate: value => double.IsNaN(value) || (float)value > 0);
    public static readonly StyledProperty<bool> FillProperty =
        AvaloniaProperty.Register<CardColumnItem, bool>(nameof(Fill), true);
    public static readonly StyledProperty<CardColumnAlignment> AlignmentProperty =
        AvaloniaProperty.Register<CardColumnItem, CardColumnAlignment>(nameof(Alignment));

    private double _minimumHeight;

    /// <summary>Positive relative weight; NaN leaves this host unweighted.</summary>
    public double Weight { get => GetValue(WeightProperty); set => SetValue(WeightProperty, value); }
    /// <summary>Whether this host occupies its full weighted height. Does not stretch its child.</summary>
    public bool Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }
    public CardColumnAlignment Alignment { get => GetValue(AlignmentProperty); set => SetValue(AlignmentProperty, value); }

    internal void MeasureInColumn(Size available, double minimumHeight)
    {
        if (_minimumHeight != minimumHeight)
        {
            _minimumHeight = minimumHeight;
            InvalidateMeasure();
        }
        Measure(available);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);
        return new Size(content.Width, Math.Max(content.Height, Parent is CardColumn ? _minimumHeight : 0));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            // Box(propagateMinConstraints=false): a larger weighted host never turns
            // the child's default Stretch into fillMaxHeight. Native RTL mirrors Start.
            var inner = new Rect(finalSize).Deflate(Padding);
            child.Arrange(new Rect(inner.Position, child.DesiredSize.Constrain(inner.Size)));
        }
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == VisualParentProperty)
        {
            _minimumHeight = 0;
            InvalidateMeasure();
        }
        if (change.Property == WeightProperty || change.Property == FillProperty || change.Property == AlignmentProperty)
            (Parent as CardColumn)?.InvalidateMeasure();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _minimumHeight = 0;
        InvalidateMeasure();
        base.OnDetachedFromVisualTree(e);
    }
}

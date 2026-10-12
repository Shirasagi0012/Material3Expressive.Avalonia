using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace ComposeAnimation.Test.Probes;

// Test fixture for width-only lookahead animation; not a public animation control.
// Only width animates; height follows the actual wrapped text (line counts change discretely).
internal sealed class TextWidthApproach : Decorator, ILookaheadLayoutNode, ILookaheadTargetInvalidationSource, ILookaheadPlacementHost
{
    public static readonly StyledProperty<double> TargetWidthProperty =
        AvaloniaProperty.Register<TextWidthApproach, double>(nameof(TargetWidth), 160,
            validate: value => double.IsFinite(value) && value > 0);

    public double TargetWidth { get => GetValue(TargetWidthProperty); set => SetValue(TargetWidthProperty, value); }
    public IFiniteAnimationSpec<double> AnimationSpec { get; set; } =
        new SpringSpec<double>(stiffness: Spring.StiffnessMediumLow, visibilityThreshold: 0.5);

    private event EventHandler? TargetMeasureInvalidated;
    private Animatable<double>? _width;
    private double _target;

    event EventHandler? ILookaheadTargetInvalidationSource.TargetMeasureInvalidated
    {
        add => TargetMeasureInvalidated += value;
        remove => TargetMeasureInvalidated -= value;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TargetWidthProperty)
        {
            InvalidateMeasure();
            TargetMeasureInvalidated?.Invoke(this, EventArgs.Empty);
        }
    }

    private LookaheadTextBlock TextChild()
    {
        if (Child is not LookaheadTextBlock text || !double.IsNaN(text.Width) ||
            !double.IsNaN(text.Height) || Padding != default)
            throw new NotSupportedException("TextWidthApproach requires an Auto-sized LookaheadTextBlock and no wrapper Padding.");
        return text;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var text = TextChild();
        var target = LookaheadRoot.TargetFor(this)?.MeasuredSize.Width ?? Math.Min(TargetWidth, availableSize.Width);
        if (_width is null)
        {
            var clock = TopLevel.GetTopLevel(this) is null ? null : AnimationScope.Get(this).Clock;
            if (clock is null) return MeasureText(text, target, availableSize);
            _target = target;
            _width = new Animatable<double>(new ComposeAnimation.Runtime.DoubleConverter(), target, clock, 0.5, "Text width approach");
            _width.PropertyChanged += WidthChanged;
        }
        if (_target != target)
        {
            _target = target;
            _ = AnimateWidth(_width, target);
        }
        return MeasureText(text, Math.Clamp(_width.Value, 0, availableSize.Width), availableSize);
    }

    private static Size MeasureText(LookaheadTextBlock text, double width, Size available)
    {
        text.Measure(new Size(width, available.Height));
        return new Size(width, text.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child) LookaheadPlacement.ArrangeChild(this, child, new Rect(finalSize));
        return finalSize;
    }

    private void WidthChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(Animatable<double>.Value) or nameof(Animatable<double>.IsRunning))
            InvalidateMeasure();
    }

    private async Task AnimateWidth(Animatable<double> width, double target)
    {
        try { await width.AnimateToAsync(target, AnimationSpec); }
        catch (OperationCanceledException) { }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_width is { } width)
        {
            width.PropertyChanged -= WidthChanged;
            width.Stop();
            _width = null;
        }
        base.OnDetachedFromVisualTree(e);
    }

    IEnumerable<Control> ILookaheadLayoutNode.LookaheadChildren => Child is { } child ? [child] : [];

    Size ILookaheadLayoutNode.MeasureLookahead(LookaheadLayoutBuilder builder, Size available)
    {
        var width = Math.Min(TargetWidth, available.Width);
        var measured = builder.Measure(TextChild(), new Size(width, available.Height));
        return new Size(width, measured.Height);
    }

    void ILookaheadLayoutNode.ArrangeLookahead(LookaheadLayoutBuilder builder, Rect bounds)
        => builder.Place(TextChild(), bounds);
}

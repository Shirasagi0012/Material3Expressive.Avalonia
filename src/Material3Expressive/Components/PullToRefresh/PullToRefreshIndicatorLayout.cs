using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed class PullToRefreshIndicatorLayout : Decorator
{
    public static readonly StyledProperty<double> ContainerWidthProperty = AvaloniaProperty.Register<PullToRefreshIndicatorLayout, double>(nameof(ContainerWidth));
    public static readonly StyledProperty<double> ContainerHeightProperty = AvaloniaProperty.Register<PullToRefreshIndicatorLayout, double>(nameof(ContainerHeight));
    public double ContainerWidth { get => GetValue(ContainerWidthProperty); set => SetValue(ContainerWidthProperty, value); }
    public double ContainerHeight { get => GetValue(ContainerHeightProperty); set => SetValue(ContainerHeightProperty, value); }
    private readonly TranslateTransform _translation = new();
    private PullToRefreshIndicatorBox? _owner;
    private IDisposable? _transformContribution, _elevationContribution;
    private static readonly DirectProperty<PullToRefreshIndicatorLayout, double> OffsetProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshIndicatorLayout, double>("Offset", layout => layout._offset);
    private static readonly DirectProperty<PullToRefreshIndicatorLayout, double> CurrentElevationProperty =
        AvaloniaProperty.RegisterDirect<PullToRefreshIndicatorLayout, double>("CurrentElevation", layout => layout._currentElevation);
    private double _offset, _currentElevation;
    internal double TranslationY => _translation.Y;
    static PullToRefreshIndicatorLayout() => AffectsMeasure<PullToRefreshIndicatorLayout>(ContainerWidthProperty, ContainerHeightProperty);
    public PullToRefreshIndicatorLayout()
    {
        // Upstream clips only above the layout origin. The translated indicator and
        // its shadow may extend below and to either side of the allocated 40/48dp box.
        // Avalonia represents clips as x/y/width/height, unlike Compose's LTRB.
        // Leave headroom for the width and transformed bounds to remain finite in Skia floats.
        Clip = new RectangleGeometry(new Rect(-float.MaxValue / 4, 0, float.MaxValue / 2, float.MaxValue / 4));
        _translation.Bind(TranslateTransform.YProperty, this.GetObservable(OffsetProperty), BindingPriority.Animation);
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<PullToRefreshIndicatorBox>();
        if (_owner is not null) _owner.PresentationChanged += Update;
        if (Child is MaterialSurface surface)
        {
            surface.ApplyTonalElevation = false;
            _transformContribution = surface.SetValue(RenderTransformProperty, _translation, BindingPriority.Animation);
            _elevationContribution = surface.Bind(MaterialSurface.ShadowElevationProperty,
                this.GetObservable(CurrentElevationProperty), BindingPriority.Animation);
        }
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PresentationChanged -= Update;
        _owner = null;
        _transformContribution?.Dispose(); _transformContribution = null;
        _elevationContribution?.Dispose(); _elevationContribution = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        double Pixel(double value) => Math.Floor(value * scale + .5) / scale;
        var size = new Size(Math.Min(Pixel(ContainerWidth), availableSize.Width), Math.Min(Pixel(ContainerHeight), availableSize.Height));
        Child?.Measure(size);
        return size;
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        Child?.Arrange(new Rect(finalSize));
        Update();
        return finalSize;
    }
    private void Update()
    {
        if (_owner is not { } owner || Child is not MaterialSurface surface) return;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var maxDistancePx = MathF.Floor((float)owner.MaxDistance * scale + .5f);
        var fraction = owner.EffectiveState.DistanceFraction;
        SetAndRaise(OffsetProperty, ref _offset, (fraction * maxDistancePx - (float)surface.Bounds.Height * scale) / scale);
        SetAndRaise(CurrentElevationProperty, ref _currentElevation, fraction > 0 || owner.IsRefreshing ? owner.Elevation : 0);
    }
}

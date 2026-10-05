using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using MaterialShapes;

namespace MaterialShapes.Controls;

public class MaterialShapeView : Decorator
{
    public static readonly StyledProperty<RoundedPolygon?> ShapeProperty =
        AvaloniaProperty.Register<MaterialShapeView, RoundedPolygon?>(nameof(Shape));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<MaterialShapeView, IBrush?>(nameof(Background));

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<MaterialShapeView, Stretch>(nameof(Stretch), Stretch.Uniform);

    public static readonly StyledProperty<bool> CenterProperty =
        AvaloniaProperty.Register<MaterialShapeView, bool>(nameof(Center), true);

    public static readonly StyledProperty<bool> IsShapeNormalizedProperty =
        AvaloniaProperty.Register<MaterialShapeView, bool>(nameof(IsShapeNormalized), true);

    private StreamGeometry? _geometryCache;
    private Size _lastSize;

    static MaterialShapeView()
    {
        AffectsRender<MaterialShapeView>(BackgroundProperty, ShapeProperty, StretchProperty, CenterProperty,
            IsShapeNormalizedProperty);
        ShapeProperty.Changed.AddClassHandler<MaterialShapeView>((x, _) => x.UpdateGeometry());
        StretchProperty.Changed.AddClassHandler<MaterialShapeView>((x, _) => x.UpdateGeometry());
        CenterProperty.Changed.AddClassHandler<MaterialShapeView>((x, _) => x.UpdateGeometry());
        IsShapeNormalizedProperty.Changed.AddClassHandler<MaterialShapeView>((x, _) => x.UpdateGeometry());
    }

    public RoundedPolygon? Shape
    {
        get => GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    public bool Center
    {
        get => GetValue(CenterProperty);
        set => SetValue(CenterProperty, value);
    }

    /// <summary>
    /// When true (default), <see cref="Shape"/> is assumed to be normalized to a 1×1 box (0..1 coordinates).
    /// When false, the shape's actual geometry bounds are used for fitting.
    /// </summary>
    public bool IsShapeNormalized
    {
        get => GetValue(IsShapeNormalizedProperty);
        set => SetValue(IsShapeNormalizedProperty, value);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!finalSize.Equals(_lastSize))
        {
            _lastSize = finalSize;
            UpdateGeometry();
        }

        return base.ArrangeOverride(finalSize);
    }

    public override void Render(DrawingContext context)
    {
        if (Background is { } background && _geometryCache is { } geometry)
            context.DrawGeometry(background, null, geometry);

        base.Render(context);
    }

    private void UpdateGeometry()
    {
        if (Shape is null || _lastSize.Width <= 0 || _lastSize.Height <= 0)
        {
            _geometryCache = null;
            Clip = null;
            InvalidateVisual();
            return;
        }

        var geometry = Shape.ToGeometry();

        var sourceBounds = IsShapeNormalized
            ? new Rect(0, 0, 1, 1)
            : geometry.Bounds;

        if (sourceBounds.Width <= 0 || sourceBounds.Height <= 0)
        {
            _geometryCache = null;
            Clip = null;
            InvalidateVisual();
            return;
        }

        // TODO (Perf): This will copy the inner SKPath of geometry. Not sure if it will cause a larger amount of unmanaged memory usage.
        // It might be better to do transform when doing conversion to StreamGeometry.
        geometry.Transform = Utils.CreateFitTransform(sourceBounds, _lastSize, Stretch, Center);

        _geometryCache = geometry;
        Clip = geometry;
        InvalidateVisual();
    }
}
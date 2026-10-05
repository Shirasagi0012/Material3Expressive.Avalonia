using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace MaterialShapes.Controls;

public class MorphShapeView : Decorator
{
    public static readonly StyledProperty<RoundedPolygon?> FromProperty =
        AvaloniaProperty.Register<MorphShapeView, RoundedPolygon?>(nameof(From));

    public static readonly StyledProperty<RoundedPolygon?> ToProperty =
        AvaloniaProperty.Register<MorphShapeView, RoundedPolygon?>(nameof(To));

    public static readonly StyledProperty<double> ProgressProperty =
        AvaloniaProperty.Register<MorphShapeView, double>(nameof(Progress), 0d);

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<MorphShapeView, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<IBrush?> StrokeProperty =
        AvaloniaProperty.Register<MorphShapeView, IBrush?>(nameof(Stroke));

    public static readonly StyledProperty<double> StrokeThicknessProperty =
        AvaloniaProperty.Register<MorphShapeView, double>(nameof(StrokeThickness), 1d);

    public static readonly StyledProperty<Stretch> StretchProperty =
        AvaloniaProperty.Register<MorphShapeView, Stretch>(nameof(Stretch), Stretch.Uniform);

    public static readonly StyledProperty<bool> CenterProperty =
        AvaloniaProperty.Register<MorphShapeView, bool>(nameof(Center), false);

    public static readonly StyledProperty<bool> IsShapeNormalizedProperty =
        AvaloniaProperty.Register<MorphShapeView, bool>(nameof(IsShapeNormalized), true);

    public static readonly DirectProperty<MorphShapeView, Morph?> MorphProperty =
        AvaloniaProperty.RegisterDirect<MorphShapeView, Morph?>(nameof(Morph), (v) => v.Morph);

    private StreamGeometry? _geometryCache;
    private IPen? _penCache;
    private Size _lastSize;

    static MorphShapeView()
    {
        AffectsRender<MorphShapeView>(FromProperty, ToProperty, ProgressProperty, FillProperty, StrokeProperty,
            StrokeThicknessProperty, StretchProperty, CenterProperty, IsShapeNormalizedProperty);
        ProgressProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x.UpdateGeometry());
        StretchProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x.UpdateGeometry());
        CenterProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x.UpdateGeometry());
        IsShapeNormalizedProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x.UpdateGeometry());
        StrokeProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x._penCache = null);
        StrokeThicknessProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x._penCache = null);
        FromProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x.UpdateMorph());
        ToProperty.Changed.AddClassHandler<MorphShapeView>((x, _) => x.UpdateMorph());
    }

    public RoundedPolygon? From
    {
        get => GetValue(FromProperty);
        set => SetValue(FromProperty, value);
    }

    public RoundedPolygon? To
    {
        get => GetValue(ToProperty);
        set => SetValue(ToProperty, value);
    }

    public double Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public IBrush? Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
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
    /// When true (default), <see cref="From"/> and <see cref="To"/> are assumed to be normalized to a 1×1 box (0..1 coordinates).
    /// When false, the morph's actual geometry bounds are used for fitting.
    /// </summary>
    public bool IsShapeNormalized
    {
        get => GetValue(IsShapeNormalizedProperty);
        set => SetValue(IsShapeNormalizedProperty, value);
    }

    public Morph? Morph
    {
        get;
        private set => SetAndRaise(MorphProperty, ref field, value);
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
        if (_geometryCache is null) return;

        _penCache ??= Stroke is { } ? new Pen(Stroke, StrokeThickness) : null;
        context.DrawGeometry(Fill, _penCache, _geometryCache);
        base.Render(context);
    }

    private void UpdateGeometry()
    {
        var morph = Morph;
        if (morph is null || _lastSize.Width <= 0 || _lastSize.Height <= 0)
        {
            _geometryCache = null;
            Clip = null;
            InvalidateVisual();
            return;
        }

        var geometry = morph.ToGeometry(Progress);

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

        geometry.Transform = Utils.CreateFitTransform(sourceBounds, _lastSize, Stretch, Center);

        _geometryCache = geometry;
        Clip = geometry;
        InvalidateVisual();
    }

    private void UpdateMorph()
    {
        if (From is null || To is null)
        {
            Morph = null;
            return;
        }

        Morph = new Morph(From, To);
        UpdateGeometry();
    }
}

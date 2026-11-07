using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Border.kt drawRoundRectBorder / createRoundRectPath. Nonuniform thickness retains
// Avalonia's native drawing path; neither branch contributes padding to Surface content.
internal sealed class SurfaceOutline : Control
{
    public static readonly StyledProperty<IBrush?> BorderBrushProperty = Border.BorderBrushProperty.AddOwner<SurfaceOutline>();
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public static readonly StyledProperty<Thickness> BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<SurfaceOutline>();
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<SurfaceOutline>();
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    internal static readonly StyledProperty<bool> UseHairlineProperty = AvaloniaProperty.Register<SurfaceOutline, bool>(nameof(UseHairline));
    internal bool UseHairline { get => GetValue(UseHairlineProperty); set => SetValue(UseHairlineProperty, value); }
    internal static readonly StyledProperty<bool> UsePathOutlineProperty = AvaloniaProperty.Register<SurfaceOutline, bool>(nameof(UsePathOutline));
    internal bool UsePathOutline { get => GetValue(UsePathOutlineProperty); set => SetValue(UsePathOutlineProperty, value); }
    internal static readonly StyledProperty<Size> PathLayoutSizeProperty = AvaloniaProperty.Register<SurfaceOutline, Size>(nameof(PathLayoutSize));
    internal Size PathLayoutSize { get => GetValue(PathLayoutSizeProperty); set => SetValue(PathLayoutSizeProperty, value); }
    internal static readonly StyledProperty<Rect?> PathBoundsProperty = AvaloniaProperty.Register<SurfaceOutline, Rect?>(nameof(PathBounds));
    internal Rect? PathBounds { get => GetValue(PathBoundsProperty); set => SetValue(PathBoundsProperty, value); }
    private WriteableBitmap? _pathMask;
    private PathGeometry? _shapePath;
    internal PathGeometry? ShapePath
    {
        get => _shapePath;
        set
        {
            if (ReferenceEquals(_shapePath, value)) return;
            _shapePath = value;
            _pathMask?.Dispose(); _pathMask = null;
            InvalidateVisual();
        }
    }
    private readonly PathBorderMask _pathMaskCache = new();
    private (Size Size, CornerRadius Corners, float Stroke, float Scale, bool Fill, Point Origin, Color Color, Size Layout) _pathMaskKey;
    private readonly Border _fallback;

    static SurfaceOutline() => AffectsRender<SurfaceOutline>(BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, UseHairlineProperty, UsePathOutlineProperty, PathLayoutSizeProperty, PathBoundsProperty);
    public SurfaceOutline()
    {
        RenderOptions.SetBitmapInterpolationMode(this, BitmapInterpolationMode.LowQuality);
        _fallback = new Border
        {
            [!FlowDirectionProperty] = this[!FlowDirectionProperty],
            [!Border.BorderBrushProperty] = this[!BorderBrushProperty],
            [!Border.BorderThicknessProperty] = this[!BorderThicknessProperty],
            [!Border.CornerRadiusProperty] = this[!CornerRadiusProperty],
            IsHitTestVisible = false
        };
        VisualChildren.Add(_fallback);
        _fallback.IsVisible = !BorderThickness.IsUniform;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_fallback is not null) _fallback.IsVisible = !BorderThickness.IsUniform;
    }
    protected override Size MeasureOverride(Size availableSize) { _fallback.Measure(availableSize); return default; }
    protected override Size ArrangeOverride(Size finalSize) { _fallback.Arrange(new Rect(finalSize)); return finalSize; }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _pathMask?.Dispose(); _pathMask = null;
        _pathMaskCache.Dispose();
        base.OnDetachedFromVisualTree(e);
    }

    public override void Render(DrawingContext context)
    {
        if (!BorderThickness.IsUniform || BorderBrush is null || BorderThickness.Top < 0
            || (BorderThickness.Top == 0 && !UseHairline) || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = (float)Bounds.Width * scale;
        var height = (float)Bounds.Height * scale;
        var stroke = MathF.Min(BorderThickness.Top == 0 ? 1 : MathF.Ceiling((float)BorderThickness.Top * scale),
            MathF.Ceiling(MathF.Min(width, height) / 2));
        if (ShapePath is { } shape)
        {
            if (stroke * 2 > MathF.Min(width, height))
            {
                context.DrawGeometry(BorderBrush, null, shape);
                return;
            }
            using var path = ShapeGeometry.ToSkiaPath(shape, scale);
            if (path.Bounds.Width <= 0 || path.Bounds.Height <= 0) return;
            var solid = BorderBrush as ISolidColorBrush;
            var color = solid is null ? Colors.White : Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
            var bounds = path.Bounds;
            var key = (new Size(bounds.Width / scale, bounds.Height / scale), default(CornerRadius), stroke, scale,
                false, new Point(bounds.Left / scale, bounds.Top / scale), color, Bounds.Size);
            if (_pathMask is null || _pathMaskKey != key)
            {
                _pathMask?.Dispose();
                _pathMask = _pathMaskCache.Update(path, stroke, scale, false, Bounds.Size, color);
                _pathMaskKey = key;
            }
            var destination = new Rect(bounds.Left / scale, bounds.Top / scale,
                MathF.Ceiling(bounds.Width) / scale, MathF.Ceiling(bounds.Height) / scale);
            if (solid is not null)
                context.DrawImage(_pathMask, new Rect(0, 0, MathF.Ceiling(bounds.Width), MathF.Ceiling(bounds.Height)), destination);
            else
                using (context.PushTransform(Matrix.CreateTranslation(destination.X, destination.Y)))
                using (context.PushOpacityMask(new ImageBrush(_pathMask) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }, new Rect(destination.Size)))
                {
                    // Compose sizes the shader to the ceil-sized cache, then translates the
                    // canvas by -pathBounds.origin. Avalonia normally anchors it to the filled
                    // rectangle instead. Preserve the shader's original coordinate system.
                    var brush = BorderBrush;
                    if (brush is IGradientBrush gradient)
                    {
                        var origin = gradient.TransformOrigin.ToPixels(destination.Size);
                        var transform = new ImmutableTransform(Matrix.CreateTranslation(-origin.X, -origin.Y)
                            * (gradient.Transform?.Value ?? Matrix.Identity)
                            * Matrix.CreateTranslation(origin.X - destination.X, origin.Y - destination.Y));
                        var stops = gradient.GradientStops.Select(s => new ImmutableGradientStop(s.Offset, s.Color)).ToArray();
                        brush = gradient switch
                        {
                            ILinearGradientBrush linear => new ImmutableLinearGradientBrush(stops, gradient.Opacity, transform,
                                RelativePoint.TopLeft, gradient.SpreadMethod, linear.StartPoint, linear.EndPoint),
                            IRadialGradientBrush radial => new RadialGradientBrush
                            {
                                GradientStops = [.. gradient.GradientStops.Select(s => new GradientStop(s.Color, s.Offset))],
                                Opacity = gradient.Opacity, Transform = transform, TransformOrigin = RelativePoint.TopLeft,
                                SpreadMethod = gradient.SpreadMethod, Center = radial.Center, GradientOrigin = radial.GradientOrigin,
                                RadiusX = radial.RadiusX, RadiusY = radial.RadiusY
                            },
                            IConicGradientBrush conic => new ImmutableConicGradientBrush(stops, gradient.Opacity, transform,
                                RelativePoint.TopLeft, gradient.SpreadMethod, conic.Center, conic.Angle),
                            _ => brush
                        };
                    }
                    context.FillRectangle(brush, new Rect(destination.Size));
                }
            return;
        }
        if (UsePathOutline)
        {
            var layout = PathLayoutSize == default ? Bounds.Size : PathLayoutSize;
            var minimum = MathF.Min((float)layout.Width * scale, (float)layout.Height * scale);
            stroke = MathF.Min(BorderThickness.Top == 0 ? 1 : MathF.Ceiling((float)BorderThickness.Top * scale), MathF.Ceiling(minimum / 2));
            var path = PathBounds ?? new Rect(Bounds.Size);
            var solid = BorderBrush as ISolidColorBrush;
            var color = solid is null ? Colors.White : Color.FromArgb((byte)Math.Round(solid.Color.A * solid.Opacity), solid.Color.R, solid.Color.G, solid.Color.B);
            var key = (path.Size, CornerRadius, stroke, scale, stroke * 2 > minimum, path.Position, color, layout);
            if (_pathMask is null || _pathMaskKey != key)
            {
                _pathMask?.Dispose();
                _pathMask = _pathMaskCache.Update(path.Size, path.Position, CornerRadius, stroke, scale, key.Item5, layout, color);
                _pathMaskKey = key;
            }
            // Preserve the ceil-sized raster instead of stretching it to the fractional path bounds.
            var maskBounds = new Rect(0, 0, Math.Ceiling(path.Width * scale) / scale, Math.Ceiling(path.Height * scale) / scale);
            if (solid is not null)
                context.DrawImage(_pathMask, new Rect(0, 0, Math.Ceiling(path.Width * scale), Math.Ceiling(path.Height * scale)), maskBounds);
            else
                using (context.PushOpacityMask(new ImageBrush(_pathMask) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top }, maskBounds))
                    context.FillRectangle(BorderBrush, maskBounds);
            return;
        }
        if (!CornerRadius.IsUniform)
        {
            // Compose creates the outer rounded path, then subtracts a full-stroke inset
            // with each corner independently shrunk. A centered Border is not equivalent.
            CornerRadius InsetCorners(float inset) => new(
                MathF.Max(0, (float)CornerRadius.TopLeft * scale - inset) / scale,
                MathF.Max(0, (float)CornerRadius.TopRight * scale - inset) / scale,
                MathF.Max(0, (float)CornerRadius.BottomRight * scale - inset) / scale,
                MathF.Max(0, (float)CornerRadius.BottomLeft * scale - inset) / scale);

            var outer = RoundedRectangleGeometry.Create(new Rect(0, 0, width / scale, height / scale), InsetCorners(0));
            if (stroke * 2 >= MathF.Min(width, height))
                context.DrawGeometry(BorderBrush, null, outer);
            else
            {
                var inner = RoundedRectangleGeometry.Create(new Rect(stroke / scale, stroke / scale,
                    (width - stroke * 2) / scale, (height - stroke * 2) / scale), InsetCorners(stroke));
                context.DrawGeometry(BorderBrush, null, new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner));
            }
            return;
        }

        var radius = MathF.Min((float)CornerRadius.TopLeft * scale, MathF.Min(width, height) / 2);
        if (stroke * 2 > MathF.Min(width, height))
            context.DrawRectangle(BorderBrush, null, new RoundedRect(new Rect(Bounds.Size), radius / scale));
        else if (radius < stroke / 2)
        {
            // Small outer radius with a sharp inner rectangle, as in Border.kt's clip difference.
            var outer = new RectangleGeometry(new Rect(Bounds.Size), radius / scale, radius / scale);
            var inner = new RectangleGeometry(new Rect(stroke / scale, stroke / scale,
                (width - stroke * 2) / scale, (height - stroke * 2) / scale));
            context.DrawGeometry(BorderBrush, null, new CombinedGeometry(GeometryCombineMode.Exclude, outer, inner));
        }
        else
        {
            var half = stroke / 2;
            context.DrawRectangle(null, new Pen(BorderBrush, stroke / scale), new RoundedRect(
                new Rect(half / scale, half / scale, (width - stroke) / scale, (height - stroke) / scale),
                (radius - half) / scale));
        }

    }
}

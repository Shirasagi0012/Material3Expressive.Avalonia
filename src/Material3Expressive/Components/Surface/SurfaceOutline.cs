using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

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
    private readonly Border _fallback;

    static SurfaceOutline() => AffectsRender<SurfaceOutline>(BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, UseHairlineProperty);
    public SurfaceOutline()
    {
        _fallback = new Border
        {
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

    public override void Render(DrawingContext context)
    {
        if (!BorderThickness.IsUniform || BorderBrush is null || BorderThickness.Top < 0
            || (BorderThickness.Top == 0 && !UseHairline) || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = (float)Bounds.Width * scale;
        var height = (float)Bounds.Height * scale;
        var stroke = MathF.Min(BorderThickness.Top == 0 ? 1 : MathF.Ceiling((float)BorderThickness.Top * scale),
            MathF.Ceiling(MathF.Min(width, height) / 2));
        if (!CornerRadius.IsUniform)
        {
            // Compose creates the outer rounded path, then subtracts a full-stroke inset
            // with each corner independently shrunk. A centered Border is not equivalent.
            CornerRadius InsetCorners(float inset) => new(
                MathF.Max(0, (float)CornerRadius.TopLeft * scale - inset) / scale,
                MathF.Max(0, (float)CornerRadius.TopRight * scale - inset) / scale,
                MathF.Max(0, (float)CornerRadius.BottomRight * scale - inset) / scale,
                MathF.Max(0, (float)CornerRadius.BottomLeft * scale - inset) / scale);

            var outer = RoundRectPath(new Rect(0, 0, width / scale, height / scale), InsetCorners(0));
            if (stroke * 2 >= MathF.Min(width, height))
                context.DrawGeometry(BorderBrush, null, outer);
            else
            {
                var inner = RoundRectPath(new Rect(stroke / scale, stroke / scale,
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

        static StreamGeometry RoundRectPath(Rect bounds, CornerRadius corners)
        {
            // Skia's addRoundRect/SetRectRadii scales overlapping radii together before
            // adding arcs. StreamGeometry requires that fit to be explicit on each path.
            double fit = 1;
            void Limit(double first, double second, double extent)
            {
                if (first + second > extent) fit = Math.Min(fit, extent / (first + second));
            }
            Limit(corners.TopLeft, corners.TopRight, bounds.Width);
            Limit(corners.TopRight, corners.BottomRight, bounds.Height);
            Limit(corners.BottomRight, corners.BottomLeft, bounds.Width);
            Limit(corners.BottomLeft, corners.TopLeft, bounds.Height);
            if (fit < 1)
                corners = new((float)(corners.TopLeft * fit), (float)(corners.TopRight * fit),
                    (float)(corners.BottomRight * fit), (float)(corners.BottomLeft * fit));
            var geometry = new StreamGeometry();
            using var path = geometry.Open();
            path.BeginFigure(new Point(bounds.Left + corners.TopLeft, bounds.Top), true);
            path.LineTo(new Point(bounds.Right - corners.TopRight, bounds.Top));
            path.ArcTo(new Point(bounds.Right, bounds.Top + corners.TopRight), new Size(corners.TopRight, corners.TopRight), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(bounds.Right, bounds.Bottom - corners.BottomRight));
            path.ArcTo(new Point(bounds.Right - corners.BottomRight, bounds.Bottom), new Size(corners.BottomRight, corners.BottomRight), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(bounds.Left + corners.BottomLeft, bounds.Bottom));
            path.ArcTo(new Point(bounds.Left, bounds.Bottom - corners.BottomLeft), new Size(corners.BottomLeft, corners.BottomLeft), 0, false, SweepDirection.Clockwise);
            path.LineTo(new Point(bounds.Left, bounds.Top + corners.TopLeft));
            path.ArcTo(new Point(bounds.Left + corners.TopLeft, bounds.Top), new Size(corners.TopLeft, corners.TopLeft), 0, false, SweepDirection.Clockwise);
            path.EndFigure(true);
            return geometry;
        }
    }
}

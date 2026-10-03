using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Material3Expressive.Components;

// Border.kt drawRoundRectBorder's simple round-rect branch. Nonuniform Avalonia borders
// retain their native drawing path; neither branch contributes padding to Surface content.
internal sealed class SurfaceOutline : Control
{
    public static readonly StyledProperty<IBrush?> BorderBrushProperty = Border.BorderBrushProperty.AddOwner<SurfaceOutline>();
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public static readonly StyledProperty<Thickness> BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<SurfaceOutline>();
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<SurfaceOutline>();
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    private readonly Border _fallback;
    private bool Simple => BorderThickness.IsUniform && CornerRadius.IsUniform;

    static SurfaceOutline() => AffectsRender<SurfaceOutline>(BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty);
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
        _fallback.IsVisible = !Simple;
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_fallback is not null) _fallback.IsVisible = !Simple;
    }
    protected override Size MeasureOverride(Size availableSize) { _fallback.Measure(availableSize); return default; }
    protected override Size ArrangeOverride(Size finalSize) { _fallback.Arrange(new Rect(finalSize)); return finalSize; }

    public override void Render(DrawingContext context)
    {
        if (!Simple || BorderBrush is null || BorderThickness.Top <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0) return;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = (float)Bounds.Width * scale;
        var height = (float)Bounds.Height * scale;
        var stroke = MathF.Min(MathF.Ceiling((float)BorderThickness.Top * scale), MathF.Ceiling(MathF.Min(width, height) / 2));
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

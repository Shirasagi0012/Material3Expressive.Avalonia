using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
namespace Material3Expressive.Components;

// Kept separate because Avalonia Panel seals Render. Shares its parent's coordinate space.
internal sealed class TextFieldDecoration : Control
{
    private readonly SurfaceOutline _outline = new() { IsHitTestVisible = false };
    private Rect _container;

    public TextFieldDecoration() => VisualChildren.Add(_outline);

    internal void Update(Rect container, Rect cutout, Size size, bool outlined, IBrush? indicator,
        double thickness, CornerRadius radius)
    {
        if (_container != container) { _container = container; InvalidateArrange(); }
        _outline.IsVisible = outlined;
        _outline.BorderBrush = indicator;
        _outline.BorderThickness = new Thickness(thickness);
        _outline.CornerRadius = radius;
        var clip = new GeometryGroup { FillRule = FillRule.EvenOdd };
        clip.Children.Add(new RectangleGeometry(new Rect(size)));
        if (cutout.Width > 0) clip.Children.Add(new RectangleGeometry(cutout));
        Clip = clip;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) { _outline.Measure(availableSize); return default; }
    protected override Size ArrangeOverride(Size finalSize) { _outline.Arrange(_container); return finalSize; }
    public override void Render(DrawingContext context) => (Parent as TextFieldLayout)?.Draw(context);
}

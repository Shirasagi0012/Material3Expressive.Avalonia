using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;
namespace Material3Expressive.Components;

// Kept separate because Avalonia Panel seals Render. Shares its parent's coordinate space.
internal sealed class TextFieldDecoration : Control
{
    private readonly SurfaceOutline _outline = new() { IsHitTestVisible = false };
    private Rect _container;
    private InsetFocusRingPresenter? _searchFocusRing;
    private CornerRadius _corners;
    private PathGeometry? _shape;
    private bool _logicalShape;

    public TextFieldDecoration()
    {
        VisualChildren.Add(_outline);
    }

    internal void SetSearchFocusRing(SearchBarInputField? owner, MaterialInteractionSource? source)
    {
        if (owner is null || source is null)
        {
            if (_searchFocusRing is null) return;
            _searchFocusRing.Disconnect();
            VisualChildren.Remove(_searchFocusRing);
            _searchFocusRing = null;
        }
        else
        {
            if (_searchFocusRing is null)
            {
                _searchFocusRing = new InsetFocusRingPresenter();
                VisualChildren.Add(_searchFocusRing);
                InvalidateMeasure();
            }
            _searchFocusRing.Connect(owner, source);
            _searchFocusRing.Corners = _corners;
            _searchFocusRing.ShapePath = _shape;
            _searchFocusRing.FlowDirection = _logicalShape ? FlowDirection : FlowDirection.LeftToRight;
        }
    }

    internal void Update(Rect container, Rect cutout, Size size, bool outlined, IBrush? indicator,
        double thickness, CornerRadius radius, PathGeometry? shape = null, bool logicalShape = false)
    {
        if (_container != container) { _container = container; InvalidateArrange(); }
        _outline.IsVisible = outlined;
        _outline.BorderBrush = indicator;
        _outline.BorderThickness = new Thickness(thickness);
        _outline.CornerRadius = radius;
        _outline.ShapePath = shape;
        // Visual-only children do not inherit direction. Explicit Shape uses logical
        // coordinates; legacy CornerRadius retains this component's physical corners.
        _logicalShape = logicalShape;
        _outline.FlowDirection = logicalShape ? FlowDirection : FlowDirection.LeftToRight;
        _corners = radius; _shape = shape;
        if (_searchFocusRing is not null)
        {
            _searchFocusRing.Corners = radius;
            _searchFocusRing.ShapePath = shape;
            _searchFocusRing.FlowDirection = _outline.FlowDirection;
        }
        var clip = new GeometryGroup { FillRule = FillRule.EvenOdd };
        // A Generic outline can extend beyond the container. The label cutout must
        // not turn that drawing into a layout-bounds clip (notably for inset rings).
        var paintBounds = new Rect(size);
        if (shape is not null)
            paintBounds = paintBounds.Union(shape.Bounds.Translate(container.Position));
        clip.Children.Add(new RectangleGeometry(paintBounds));
        if (cutout.Width > 0) clip.Children.Add(new RectangleGeometry(cutout));
        Clip = clip;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) { _outline.Measure(availableSize); _searchFocusRing?.Measure(availableSize); return default; }
    protected override Size ArrangeOverride(Size finalSize) { _outline.Arrange(_container); _searchFocusRing?.Arrange(_container); return finalSize; }
    public override void Render(DrawingContext context) => (Parent as TextFieldLayout)?.Draw(context);
}

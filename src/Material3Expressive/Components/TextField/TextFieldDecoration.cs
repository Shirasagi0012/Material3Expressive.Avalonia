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

    public TextFieldDecoration() => VisualChildren.Add(_outline);

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
            _searchFocusRing.Corners = owner.CornerRadius;
        }
    }

    internal void Update(Rect container, Rect cutout, Size size, bool outlined, IBrush? indicator,
        double thickness, CornerRadius radius)
    {
        if (_container != container) { _container = container; InvalidateArrange(); }
        _outline.IsVisible = outlined;
        _outline.BorderBrush = indicator;
        _outline.BorderThickness = new Thickness(thickness);
        _outline.CornerRadius = radius;
        if (_searchFocusRing is not null) _searchFocusRing.Corners = radius;
        var clip = new GeometryGroup { FillRule = FillRule.EvenOdd };
        clip.Children.Add(new RectangleGeometry(new Rect(size)));
        if (cutout.Width > 0) clip.Children.Add(new RectangleGeometry(cutout));
        Clip = clip;
        InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize) { _outline.Measure(availableSize); _searchFocusRing?.Measure(availableSize); return default; }
    protected override Size ArrangeOverride(Size finalSize) { _outline.Arrange(_container); _searchFocusRing?.Arrange(_container); return finalSize; }
    public override void Render(DrawingContext context) => (Parent as TextFieldLayout)?.Draw(context);
}

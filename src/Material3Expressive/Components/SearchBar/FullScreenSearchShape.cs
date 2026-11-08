using Avalonia;
using Avalonia.Media;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// FullScreenSearchBarLayout's remembered GenericShape. Its Path.addOutline uses NonZero,
// and even an explicit RoundedCornerShape(50%) takes the custom (non-Circle singleton) branch.
internal sealed class FullScreenSearchShape : MaterialShape, IDisposable
{
    private readonly MaterialShape _collapsed;
    private static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<FullScreenSearchShape, bool>(nameof(Expanded));
    private static readonly StyledProperty<int> RevisionProperty = AvaloniaProperty.Register<FullScreenSearchShape, int>("Revision");
    internal bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
    internal FullScreenSearchShape(MaterialShape collapsed)
    {
        _collapsed = collapsed;
        _collapsed.PropertyChanged += ShapeChanged;
    }
    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => SetValue(RevisionProperty, GetValue(RevisionProperty) + 1);
    public override PathGeometry CreateOutline(Size size, FlowDirection layoutDirection, double density)
    {
        if (Expanded) return RoundedRectangleGeometry.Create(new Rect(size), default);
        var source = _collapsed.CreateOutline(size, layoutDirection, density);
        var result = source.Figures is { Count: > 0 } ? PathGeometry.Parse(source.ToString()) : new PathGeometry();
        result.FillRule = FillRule.NonZero;
        result.Transform = new MatrixTransform(source.Transform?.Value ?? Matrix.Identity);
        return result;
    }
    public void Dispose() => _collapsed.PropertyChanged -= ShapeChanged;
}

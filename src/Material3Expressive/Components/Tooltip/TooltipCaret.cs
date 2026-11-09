// Tooltip.kt: layoutCaret, caretX, DefaultTooltipCaretShape and TooltipCaretShape.
// Copyright The Android Open Source Project; Apache-2.0. Fixed baseline in docs/upstream-baseline.json.
using Avalonia;
using Avalonia.Media;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Rendering;
using MaterialSkiaShadow;
using SkiaSharp;

namespace Material3Expressive.Components;

internal sealed record TooltipCaretPlacement(PixelRect Anchor, PixelSize Window, PixelPoint Tooltip,
    TooltipAnchorPosition? Position, bool Rtl, double Density);

internal sealed class TooltipCaret : AvaloniaObject
{
    internal static readonly AttachedProperty<TooltipCaretPlacement?> PlacementProperty =
        AvaloniaProperty.RegisterAttached<TooltipCaret, AvaloniaObject, TooltipCaretPlacement?>("Placement", inherits: true);
    public static readonly AttachedProperty<Size?> SizeProperty =
        AvaloniaProperty.RegisterAttached<TooltipCaret, MaterialSurface, Size?>("Size");
    public static readonly AttachedProperty<TooltipCaretShape?> ShapeProperty =
        AvaloniaProperty.RegisterAttached<TooltipCaret, MaterialSurface, TooltipCaretShape?>("Shape");
    public static readonly AttachedProperty<MaterialShape?> ContainerShapeProperty =
        AvaloniaProperty.RegisterAttached<TooltipCaret, MaterialSurface, MaterialShape?>("ContainerShape");
    private static readonly AttachedProperty<ContainerShapeObserver?> ShapeObserverProperty =
        AvaloniaProperty.RegisterAttached<TooltipCaret, MaterialSurface, ContainerShapeObserver?>("ShapeObserver");
    public static MaterialShape? GetContainerShape(MaterialSurface surface) => surface.GetValue(ContainerShapeProperty);
    public static void SetContainerShape(MaterialSurface surface, MaterialShape? value) => surface.SetValue(ContainerShapeProperty, value);
    public static TooltipCaretShape? GetShape(MaterialSurface surface) => surface.GetValue(ShapeProperty);
    public static void SetShape(MaterialSurface surface, TooltipCaretShape? value) => surface.SetValue(ShapeProperty, value);
    public static Size? GetSize(MaterialSurface surface) => surface.GetValue(SizeProperty);
    public static void SetSize(MaterialSurface surface, Size? value) => surface.SetValue(SizeProperty, value);

    static TooltipCaret()
    {
        SizeProperty.Changed.AddClassHandler<MaterialSurface>((surface, _) => Changed(surface));
        ShapeProperty.Changed.AddClassHandler<MaterialSurface>((surface, _) => Changed(surface));
        ContainerShapeProperty.Changed.AddClassHandler<MaterialSurface>((surface, _) => Changed(surface));
        static void Changed(MaterialSurface surface)
        {
            surface.PropertyChanged -= SurfaceChanged;
            if (GetSize(surface) is not null || GetShape(surface) is not null || GetContainerShape(surface) is not null)
                surface.PropertyChanged += SurfaceChanged;
            var observer = surface.GetValue(ShapeObserverProperty);
            if (observer is null && GetContainerShape(surface) is not null)
            {
                observer = new ContainerShapeObserver(surface);
                surface.SetValue(ShapeObserverProperty, observer);
            }
            observer?.Refresh();
            Update(surface);
        }
    }

    private static void SurfaceChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Visual.BoundsProperty || change.Property == MaterialSurface.CornerRadiusProperty
            || change.Property == PlacementProperty || change.Property == Visual.FlowDirectionProperty) Update((MaterialSurface)sender!);
    }

    private static void Update(MaterialSurface surface)
    {
        var shape = GetShape(surface);
        var size = GetSize(surface);
        var containerShape = GetContainerShape(surface);
        var hasCaret = shape is not null || size is not null;
        // A caret composes the two outlines. Without one, keep Surface's regular shape
        // path, including its rounded-outline fast path and its own mutable-shape observer.
        surface.Shape = hasCaret ? null : containerShape;
        if (!hasCaret || surface.Bounds.Width <= 0 || surface.Bounds.Height <= 0)
        {
            surface.SetContainerGeometry(null, null);
            return;
        }
        var placement = surface.GetValue(PlacementProperty);
        var transform = CalculateTransform(surface.Bounds.Size, placement);
        // Popup content participates in Avalonia's RTL mirror. Caret coordinates are
        // physical, unlike content alignment, so undo that mirror on the outline alone.
        if (placement?.Rtl == true) transform *= new Matrix(-1, 0, 0, 1, surface.Bounds.Width, 0);
        Geometry caret;
        using var caretBuilder = new SKPathBuilder();
        if (shape is { })
        {
            var outline = shape(surface.Bounds.Size, surface.FlowDirection)
                ?? throw new InvalidOperationException("A tooltip caret shape must return a path.");
            // PathGeometry's invariant path data is SVG-compatible apart from its fill-rule
            // prefix. Use its public representation rather than backend/private geometry APIs.
            var data = outline.ToString();
            if (data.StartsWith("F0 ", StringComparison.Ordinal) || data.StartsWith("F1 ", StringComparison.Ordinal)) data = data[3..];
            using var path = string.IsNullOrWhiteSpace(data) ? new SKPath() : SKPath.ParseSvgPathData(data)
                ?? throw new InvalidOperationException("Unable to read the tooltip caret path.");
            // TooltipCaretShape.addPath copies contours into a default NonZero path;
            // it does not copy the source outline's fill type.
            path.FillType = SKPathFillType.Winding;
            var matrix = (outline.Transform?.Value ?? Matrix.Identity) * transform;
            caretBuilder.AddPath(path, new SKMatrix((float)matrix.M11, (float)matrix.M21, (float)matrix.M31,
                (float)matrix.M12, (float)matrix.M22, (float)matrix.M32, 0, 0, 1));
            var caretSnapshot = string.IsNullOrWhiteSpace(data) ? new PathGeometry() : PathGeometry.Parse(data);
            caretSnapshot.FillRule = FillRule.NonZero;
            caret = caretSnapshot;
            caret.Transform = new MatrixTransform(matrix);
        }
        else
        {
            Point Transform(double x, double y) => new Point(x, y).Transform(transform);
            Point[] points = [Transform(0, 0), Transform(size!.Value.Width / 2, 0),
                Transform(0, size.Value.Height), Transform(-size.Value.Width / 2, 0)];
            var triangle = new StreamGeometry();
            using (var path = triangle.Open())
            {
                path.BeginFigure(points[0], true);
                foreach (var point in points.Skip(1)) path.LineTo(point);
                path.EndFigure(true);
            }
            caret = triangle;
            var builder = caretBuilder;
            builder.MoveTo((float)points[0].X, (float)points[0].Y);
            foreach (var point in points.Skip(1)) builder.LineTo((float)point.X, (float)point.Y);
            builder.Close();

        }
        using var caretPath = caretBuilder.Detach();
        var bounds = new Rect(surface.Bounds.Size);
        Geometry containerGeometry;
        using var containerBuilder = new SKPathBuilder();
        if (containerShape is not null)
        {
            var outline = containerShape.CreateOutline(bounds.Size, surface.FlowDirection, LayoutHelper.GetLayoutScale(surface));
            var snapshot = PathGeometry.Parse(outline.ToString());
            snapshot.FillRule = FillRule.NonZero;
            var matrix = outline.Transform?.Value ?? Matrix.Identity;
            if (surface.FlowDirection == FlowDirection.RightToLeft)
                matrix *= new Matrix(-1, 0, 0, 1, bounds.Width, 0);
            snapshot.Transform = new MatrixTransform(matrix);
            containerGeometry = snapshot;
            using var path = ShapeGeometry.ToSkiaPath(snapshot);
            containerBuilder.AddPath(path);
        }
        else
        {
            var corners = surface.EffectiveCornerRadius;
            containerGeometry = RoundedRectangleGeometry.Create(bounds, corners);
            // Retain Skia's conic round-rect arcs for the themed corner path.
            using var rounded = new SKRoundRect();
            rounded.SetRectRadii(new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height),
                [new((float)corners.TopLeft, (float)corners.TopLeft), new((float)corners.TopRight, (float)corners.TopRight),
                 new((float)corners.BottomRight, (float)corners.BottomRight), new((float)corners.BottomLeft, (float)corners.BottomLeft)]);
            containerBuilder.AddRoundRect(rounded, SKPathDirection.Clockwise);
        }
        var geometry = new CombinedGeometry(GeometryCombineMode.Union, containerGeometry, caret);
        using var containerPath = containerBuilder.Detach();
        using var union = containerPath.Op(caretPath, SKPathOp.Union)
            ?? throw new InvalidOperationException("Unable to combine tooltip container and caret outlines.");
        surface.SetContainerGeometry(geometry, ComposeShadowShape.FromPath(union));
    }

    // Only the caret branch owns a separate shape observation. The source may be shared
    // by a view model, so detach must remove its reference back to the tooltip surface.
    private sealed class ContainerShapeObserver
    {
        private readonly MaterialSurface _surface;
        private MaterialShape? _observed;
        public ContainerShapeObserver(MaterialSurface surface)
        {
            _surface = surface;
            surface.AttachedToVisualTree += Attached;
            surface.DetachedFromVisualTree += Detached;
        }
        public void Refresh()
        {
            Unsubscribe();
            if (_surface.IsAttachedToVisualTree() && (GetShape(_surface) is not null || GetSize(_surface) is not null))
            {
                _observed = GetContainerShape(_surface);
                if (_observed is not null) _observed.PropertyChanged += ShapeChanged;
            }
        }
        private void Unsubscribe()
        {
            if (_observed is not null) _observed.PropertyChanged -= ShapeChanged;
            _observed = null;
        }
        private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Update(_surface);
        private void Attached(object? sender, VisualTreeAttachmentEventArgs e) { Refresh(); Update(_surface); }
        private void Detached(object? sender, VisualTreeAttachmentEventArgs e) => Unsubscribe();
    }

    internal static Matrix CalculateTransform(Size tooltip, TooltipCaretPlacement? placement)
    {
        if (placement is null) return Matrix.Identity;
        var density = (float)placement.Density;
        var width = (float)tooltip.Width * density;
        var height = (float)tooltip.Height * density;
        var anchor = placement.Anchor;
        var below = placement.Tooltip.Y > anchor.Y;
        var right = placement.Tooltip.X > anchor.X;
        var side = placement.Position;
        if (side is TooltipAnchorPosition.Left or TooltipAnchorPosition.Right or TooltipAnchorPosition.Start or TooltipAnchorPosition.End)
        {
            // Start/End resolve the same physical edge after the provider has performed fallback.
            var x = right ? 0 : width;
            return new Matrix(0, right ? 1 : -1, right ? -1 : 1, 0, x / density, height / 2 / density);
        }
        var middle = (anchor.X + anchor.Right) / 2f;
        var screenWidth = placement.Window.Width;
        var caretX = width >= screenWidth ? middle
            : middle - width / 2 < 0 ? middle + MathF.Max(width - screenWidth, -anchor.X)
            : middle + width / 2 > screenWidth ? middle + MathF.Min(width - anchor.Right, 0)
            : width / 2;
        return new Matrix(1, 0, 0, below ? -1 : 1, caretX / density, below ? 0 : height / density);
    }
}

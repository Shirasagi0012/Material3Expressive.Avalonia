// Tooltip.kt: layoutCaret, caretX, DefaultTooltipCaretShape and TooltipCaretShape.
// Copyright The Android Open Source Project; Apache-2.0. Fixed baseline in docs/upstream-baseline.json.
using Avalonia;
using Avalonia.Media;
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
    public static Size? GetSize(MaterialSurface surface) => surface.GetValue(SizeProperty);
    public static void SetSize(MaterialSurface surface, Size? value) => surface.SetValue(SizeProperty, value);

    static TooltipCaret()
    {
        SizeProperty.Changed.AddClassHandler<MaterialSurface>((surface, change) =>
        {
            if (change.OldValue is null && change.NewValue is not null) surface.PropertyChanged += SurfaceChanged;
            if (change.NewValue is null) surface.PropertyChanged -= SurfaceChanged;
            Update(surface);
        });
    }

    private static void SurfaceChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == Visual.BoundsProperty || change.Property == MaterialSurface.CornerRadiusProperty
            || change.Property == PlacementProperty) Update((MaterialSurface)sender!);
    }

    private static void Update(MaterialSurface surface)
    {
        if (GetSize(surface) is not { } size || surface.Bounds.Width <= 0 || surface.Bounds.Height <= 0)
        {
            surface.SetContainerGeometry(null, null);
            return;
        }
        var placement = surface.GetValue(PlacementProperty);
        var transform = CalculateTransform(surface.Bounds.Size, placement);
        // Popup content participates in Avalonia's RTL mirror. Caret coordinates are
        // physical, unlike content alignment, so undo that mirror on the outline alone.
        if (placement?.Rtl == true) transform *= new Matrix(-1, 0, 0, 1, surface.Bounds.Width, 0);
        Point Transform(double x, double y) => new Point(x, y).Transform(transform);
        Point[] points = [Transform(0, 0), Transform(size.Width / 2, 0),
            Transform(0, size.Height), Transform(-size.Width / 2, 0)];
        var caret = new StreamGeometry();
        using (var path = caret.Open())
        {
            path.BeginFigure(points[0], true);
            foreach (var point in points.Skip(1)) path.LineTo(point);
            path.EndFigure(true);
        }
        var bounds = new Rect(surface.Bounds.Size);
        var corners = surface.EffectiveCornerRadius;
        var geometry = new CombinedGeometry(GeometryCombineMode.Union,
            RoundedRectangleGeometry.Create(bounds, corners), caret);

        // The shadow receives the actual union, retaining Skia's conic round-rect arcs.
        using var rounded = new SKRoundRect();
        rounded.SetRectRadii(new SKRect(0, 0, (float)bounds.Width, (float)bounds.Height),
            [new((float)corners.TopLeft, (float)corners.TopLeft), new((float)corners.TopRight, (float)corners.TopRight),
             new((float)corners.BottomRight, (float)corners.BottomRight), new((float)corners.BottomLeft, (float)corners.BottomLeft)]);
        using var containerBuilder = new SKPathBuilder();
        containerBuilder.AddRoundRect(rounded, SKPathDirection.Clockwise);
        using var containerPath = containerBuilder.Detach();
        using var caretBuilder = new SKPathBuilder();
        caretBuilder.MoveTo((float)points[0].X, (float)points[0].Y);
        foreach (var point in points.Skip(1)) caretBuilder.LineTo((float)point.X, (float)point.Y);
        caretBuilder.Close();
        using var caretPath = caretBuilder.Detach();
        using var union = containerPath.Op(caretPath, SKPathOp.Union)
            ?? throw new InvalidOperationException("Unable to combine tooltip container and caret outlines.");
        surface.SetContainerGeometry(geometry, ComposeShadowShape.FromPath(union));
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

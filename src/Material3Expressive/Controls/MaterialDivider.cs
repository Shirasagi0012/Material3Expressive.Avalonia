using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Material3Expressive.Controls;

/// <summary>The direction in which a <see cref="MaterialDivider" /> occupies layout space.</summary>
public enum DividerOrientation { Horizontal, Vertical }

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/{Divider.kt,
// tokens/DividerDefaultsTokens.kt} (VERSION v0_117), plus DividerTest.kt and DividerScreenshotTest.kt.
// HorizontalDivider/VerticalDivider draw a centred 1dp OutlineVariant stroke. Dp.Hairline has a
// zero layout-axis size but Skia still draws one physical pixel. Avalonia.Skia deliberately treats
// Pen.Thickness == 0 as "do not draw" (DrawingContextImpl.TryCreatePaint), so IsHairline is the
// explicit Avalonia/XAML adaptation: it preserves zero layout size and uses a private retained
// 1 / RenderScaling-DIP visual only for painting. Thickness=0 retains this control's existing
// no-line meaning.

/// <summary>A Material 3 horizontal or vertical divider.</summary>
public sealed class MaterialDivider : TemplatedControl, IVisualBoundsProbe
{
    public static readonly StyledProperty<DividerOrientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialDivider, DividerOrientation>(nameof(Orientation));
    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<MaterialDivider, double>(nameof(Thickness), 1);
    public static readonly StyledProperty<bool> IsHairlineProperty =
        AvaloniaProperty.Register<MaterialDivider, bool>(nameof(IsHairline));
    public static readonly StyledProperty<IBrush?> DividerBrushProperty =
        AvaloniaProperty.Register<MaterialDivider, IBrush?>(nameof(DividerBrush));

    // These properties are the hidden semantic transport from the ControlTheme's typed value
    // endpoints to the public Avalonia properties.  Keeping the endpoint setters separate from
    // the public properties lets ordinary application styles and local values retain precedence.
    internal static readonly StyledProperty<IImmutableBrush?> ResolvedDividerBrushProperty =
        AvaloniaProperty.Register<MaterialDivider, IImmutableBrush?>(nameof(ResolvedDividerBrush));
    internal static readonly StyledProperty<double> ResolvedThicknessProperty =
        AvaloniaProperty.Register<MaterialDivider, double>(nameof(ResolvedThickness));

    private readonly HairlineStrokeVisual _hairlineStroke;

    public MaterialDivider()
    {
        // The retained child gives Dp.Hairline a non-zero render footprint while this control
        // keeps Compose's zero cross-axis desired/layout size. It is intentionally visual-only:
        // dividers remain non-interactive and do not gain a logical or automation child.
        _hairlineStroke = new HairlineStrokeVisual(this)
        {
            ClipToBounds = false,
            IsHitTestVisible = false,
            Focusable = false,
            IsTabStop = false,
            ZIndex = -1
        };
        IsHitTestVisible = false;
        Focusable = false;
    }

    public DividerOrientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>
    /// Gets or sets whether the divider maps Compose <c>Dp.Hairline</c>: it takes zero layout
    /// space on the cross axis and renders as exactly one physical pixel at the current DPI.
    /// </summary>
    /// <remarks>
    /// This deliberately does not overload <see cref="Thickness" /> set to zero. Existing XAML
    /// can continue to use a zero thickness to suppress the rule; opt in to the upstream hairline
    /// contract explicitly with <c>IsHairline="True"</c>.
    /// </remarks>
    public bool IsHairline { get => GetValue(IsHairlineProperty); set => SetValue(IsHairlineProperty, value); }

    public IBrush? DividerBrush { get => GetValue(DividerBrushProperty); set => SetValue(DividerBrushProperty, value); }

    /// <summary>Gets or sets the hidden MaterialValue brush projection.</summary>
    internal IImmutableBrush? ResolvedDividerBrush
    {
        get => GetValue(ResolvedDividerBrushProperty);
        set => SetValue(ResolvedDividerBrushProperty, value);
    }

    /// <summary>Gets or sets the hidden MaterialValue thickness projection.</summary>
    internal double ResolvedThickness
    {
        get => GetValue(ResolvedThicknessProperty);
        set => SetValue(ResolvedThicknessProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // TemplatedControl clears visual children when a template is reapplied. Re-add the
        // private retained stroke after that framework step, without making it logical content.
        EnsureHairlineStroke();
        _hairlineStroke.Measure(HairlineRenderSize(availableSize));

        var thickness = LayoutThickness;
        return Orientation == DividerOrientation.Horizontal
            ? new Size(0, thickness)
            : new Size(thickness, 0);
    }

    public override void Render(DrawingContext context)
    {
        var brush = DividerBrush;
        var layoutThickness = LayoutThickness;
        if (brush is null || IsHairline || layoutThickness <= 0)
            return;

        var centre = layoutThickness / 2;
        var pen = new Pen(brush, layoutThickness);
        if (Orientation == DividerOrientation.Horizontal)
            context.DrawLine(pen, new Point(0, centre), new Point(Bounds.Width, centre));
        else
            context.DrawLine(pen, new Point(centre, 0), new Point(centre, Bounds.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        EnsureHairlineStroke();
        var result = base.ArrangeOverride(finalSize);
        _hairlineStroke.Arrange(IsHairline ? new Rect(HairlineRenderSize(finalSize)) : default);
        return result;
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        // TemplatedControl expects its existing visual children to be logical template children
        // while it reconnects the logical tree. The retained stroke is deliberately visual-only,
        // so remove it before that base implementation; the next measure pass reinstates it after
        // any template work has completed.
        if (VisualChildren.Contains(_hairlineStroke))
            VisualChildren.Remove(_hairlineStroke);

        base.OnDetachedFromLogicalTree(e);
    }

    /// <summary>
    /// Exposes the rendered hairline footprint to deterministic parity capture. A hairline's
    /// framework layout bounds intentionally have a zero cross-axis size, so they cannot by
    /// themselves describe its one-physical-pixel painted extent.
    /// </summary>
    public bool TryGetVisualBounds(string id, out Rect bounds)
    {
        if (!IsHairline || !string.Equals(id, "stroke", StringComparison.Ordinal))
        {
            bounds = default;
            return false;
        }

        bounds = new Rect(HairlineRenderSize(Bounds.Size));
        return true;
    }

    static MaterialDivider()
    {
        // Dp.Hairline intentionally has a zero cross-axis layout slot. TemplatedControl normally
        // clips retained children to that slot, which removes the centred physical-pixel stroke
        // before composition. The private child is render-only, so disable that inherited clip;
        // the divider's layout and hit-testing bounds remain zero-sized.
        ClipToBoundsProperty.OverrideDefaultValue<MaterialDivider>(false);
        AffectsMeasure<MaterialDivider>(OrientationProperty, ThicknessProperty, IsHairlineProperty);
        AffectsRender<MaterialDivider>(OrientationProperty, ThicknessProperty, IsHairlineProperty, DividerBrushProperty);
    }

    private double LayoutThickness => IsHairline ? 0 : Math.Max(0, Thickness);

    private double OnePhysicalPixel => 1 / (TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);

    private Size HairlineRenderSize(Size size) =>
        Orientation == DividerOrientation.Horizontal
            ? new Size(size.Width, OnePhysicalPixel)
            : new Size(OnePhysicalPixel, size.Height);

    private void EnsureHairlineStroke()
    {
        if (!VisualChildren.Contains(_hairlineStroke))
            VisualChildren.Add(_hairlineStroke);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == OrientationProperty
            || change.Property == IsHairlineProperty
            || change.Property == DividerBrushProperty)
            _hairlineStroke?.InvalidateVisual();
    }

    private sealed class HairlineStrokeVisual(MaterialDivider owner) : Control
    {
        public override void Render(DrawingContext context)
        {
            if (!owner.IsHairline || owner.DividerBrush is not { } brush)
                return;

            // Compose sends Dp.Hairline's zero-width Skia stroke through the zero-height Canvas
            // origin. A positive Avalonia pen at that same origin produces the matching two
            // antialiased half-coverage physical rows; using the child footprint midpoint would
            // incorrectly paint one opaque row.
            var pen = new Pen(brush, owner.OnePhysicalPixel);
            if (owner.Orientation == DividerOrientation.Horizontal)
                context.DrawLine(pen, new Point(0, 0), new Point(Bounds.Width, 0));
            else
                context.DrawLine(pen, new Point(0, 0), new Point(0, Bounds.Height));
        }
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;
using MaterialSkiaShadow;

namespace Material3ExpressiveRefactor.Foundation.Rendering;

/// <summary>
/// The Material surface: one lookless template part that paints the whole chrome every component
/// sits on. Paint order mirrors the upstream surface/ripple nodes: elevation shadow → background /
/// border / clipped content → interaction state layer (Compose's CommonRipple.drawIndication draws
/// content first, then the state layer, then ripple ink — ripple lands with the motion slice and
/// will paint from the same overlay).
/// <para>
/// The class composes two private visuals instead of subclassing <see cref="Border"/>: Border's
/// Render is sealed, and a parent's own paint always lands beneath its children, so neither "shadow
/// below the background" nor "state layer above the content" is reachable from a Border subclass.
/// The internal Border keeps the platform behavior for free (inset stroke, compositor-level rounded
/// clip of the content); the overlay draws above the content.
/// </para>
/// <para>
/// The shadow is drawn by MaterialSkiaShadow (the Skia point-light key/ambient model Compose
/// uses), driven by <see cref="Elevation"/> in DIPs — the same semantics as the
/// <c>md.sys.elevation.level*</c> tokens, so a comp elevation slot binds straight onto it. The
/// shadow color resolves <c>md.sys.color.shadow</c> from this control's position unless
/// <see cref="ShadowColor"/> is set. Elevation 0 (the default) skips the shadow, which is why flat
/// and elevated components share this one part.
/// </para>
/// </summary>
public class MaterialSurface : Control
{
    /// <summary>Physical elevation in DIPs; drives the Skia shadow. 0 (the default) draws none.</summary>
    public static readonly StyledProperty<double> ElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(Elevation));

    /// <summary>
    /// Explicit shadow color. When null (the default), <c>md.sys.color.shadow</c> is resolved
    /// through the token system at render time, falling back to black.
    /// </summary>
    public static readonly StyledProperty<Color?> ShadowColorProperty =
        AvaloniaProperty.Register<MaterialSurface, Color?>(nameof(ShadowColor));

    /// <summary>
    /// The state-layer tint, painted over the content (upstream draw order) when
    /// <see cref="StateLayerOpacity"/> is non-zero. Driven by pseudo-class styles from the owning
    /// control's theme.
    /// </summary>
    public static readonly StyledProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.Register<MaterialSurface, IBrush?>(nameof(StateLayerBrush));

    /// <summary>The state-layer opacity; 0 (the default) means at rest. See the sys.state tokens.</summary>
    public static readonly StyledProperty<double> StateLayerOpacityProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(StateLayerOpacity));

    // The chrome property set mirrors Border's registrations (shared via AddOwner), so styles and
    // TemplateBindings talk to this part exactly like the Border it replaces.
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<MaterialSurface>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<MaterialSurface>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<MaterialSurface>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<MaterialSurface>();

    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<MaterialSurface>();

    public static readonly StyledProperty<Control?> ChildProperty =
        Decorator.ChildProperty.AddOwner<MaterialSurface>();

    static MaterialSurface()
    {
        AffectsRender<MaterialSurface>(ElevationProperty, ShadowColorProperty);
    }

    private readonly Border _chrome;
    private readonly StateLayerOverlay _overlay;

    public MaterialSurface()
    {
        _chrome = new Border { ClipToBounds = true };
        _overlay = new StateLayerOverlay(this);
        VisualChildren.Add(_chrome);
        VisualChildren.Add(_overlay);

        // The chrome joins the logical tree too, so it can own the content's logical parenting
        // (its Decorator.ChildChanged does the standard SetParent/LogicalChildren dance). The
        // logical chain then runs content → chrome → this control, and DataContext inheritance
        // and name scoping behave like any content host. The overlay stays visual-only.
        ((ISetLogicalParent)_chrome).SetParent(this);
        LogicalChildren.Add(_chrome);

        // The shadow color tracks a token, which no property-system invalidation reaches.
        ResourcesChanged += (_, _) => InvalidateShadow();
        ActualThemeVariantChanged += (_, _) => InvalidateShadow();
    }

    private void InvalidateShadow()
    {
        if (Elevation > 0)
            InvalidateVisual();
    }

    /// <summary>Gets or sets the content hosted on the surface.</summary>
    [Content]
    public Control? Child
    {
        get => GetValue(ChildProperty);
        set => SetValue(ChildProperty, value);
    }

    public IBrush? Background
    {
        get => GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public IBrush? BorderBrush
    {
        get => GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Thickness BorderThickness
    {
        get => GetValue(BorderThicknessProperty);
        set => SetValue(BorderThicknessProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public Thickness Padding
    {
        get => GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    public double Elevation
    {
        get => GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    public Color? ShadowColor
    {
        get => GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    public IBrush? StateLayerBrush
    {
        get => GetValue(StateLayerBrushProperty);
        set => SetValue(StateLayerBrushProperty, value);
    }

    public double StateLayerOpacity
    {
        get => GetValue(StateLayerOpacityProperty);
        set => SetValue(StateLayerOpacityProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        // The shadow paints before the visual children (chrome + content + overlay).
        // ComposeShadowRenderer requires an attached TopLevel (it reads the viewport light from
        // it); render only happens when attached, but stay defensive for offscreen hosts.
        var bounds = new Rect(Bounds.Size);
        if (Elevation > 0 && bounds.Width > 0 && bounds.Height > 0 && TopLevel.GetTopLevel(this) is { })
        {
            ComposeShadowRenderer.Draw(
                context,
                this,
                ComposeShadowShape.RoundedRectangle(bounds, CornerRadius),
                ComposeShadowParameters.Material(Elevation, ResolveShadowColor()));
        }

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _chrome.Measure(availableSize);
        var desired = _chrome.DesiredSize;
        _overlay.Measure(desired);
        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var rect = new Rect(finalSize);
        _chrome.Arrange(rect);
        _overlay.Arrange(rect);
        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ChildProperty)
        {
            ChildChanged(change);
        }
        else if (change.Property == BackgroundProperty)
        {
            _chrome.Background = change.GetNewValue<IBrush?>();
        }
        else if (change.Property == BorderBrushProperty)
        {
            _chrome.BorderBrush = change.GetNewValue<IBrush?>();
        }
        else if (change.Property == BorderThicknessProperty)
        {
            _chrome.BorderThickness = change.GetNewValue<Thickness>();
        }
        else if (change.Property == CornerRadiusProperty)
        {
            _chrome.CornerRadius = change.GetNewValue<CornerRadius>();
            _overlay.InvalidateVisual();
        }
        else if (change.Property == PaddingProperty)
        {
            _chrome.Padding = change.GetNewValue<Thickness>();
        }
        else if (change.Property == StateLayerBrushProperty || change.Property == StateLayerOpacityProperty)
        {
            _overlay.InvalidateVisual();
        }
    }

    private void ChildChanged(AvaloniaPropertyChangedEventArgs e)
    {
        // Hand the content to the chrome: its Decorator.ChildChanged does the standard
        // logical + visual parenting under the chrome, so the content sits inside the rounded
        // compositor clip and keeps a working logical chain (content → chrome → this control).
        _chrome.Child = (Control?)e.NewValue;
    }

    private Color ResolveShadowColor()
    {
        if (ShadowColor is { } color)
            return color;

        return TokenResolver.TryResolve(this, MaterialTokens.Sys.Color.Shadow, ActualThemeVariant, out var value) &&
               value is Color resolved
            ? resolved
            : Colors.Black;
    }

    /// <summary>
    /// The always-on-top paint layer: state layer now, ripple ink when the motion slice lands.
    /// A visual-only child (no logical parent) that never participates in input or layout size.
    /// </summary>
    private sealed class StateLayerOverlay : Control
    {
        private readonly MaterialSurface _owner;

        public StateLayerOverlay(MaterialSurface owner)
        {
            _owner = owner;
            IsHitTestVisible = false;
            Focusable = false;
        }

        public override void Render(DrawingContext context)
        {
            var brush = _owner.StateLayerBrush;
            var opacity = _owner.StateLayerOpacity;
            if (brush is null || opacity <= 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            using (context.PushOpacity(opacity))
                context.DrawRectangle(brush, null, new RoundedRect(new Rect(Bounds.Size), _owner.CornerRadius));
        }
    }
}

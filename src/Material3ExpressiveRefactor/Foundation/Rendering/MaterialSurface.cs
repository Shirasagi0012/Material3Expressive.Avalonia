using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3ExpressiveRefactor.Foundation.Interaction;
using Material3ExpressiveRefactor.Foundation.Motion;
using Material3ExpressiveRefactor.Foundation.Tokens;
using Material3ExpressiveRefactor.Tokens;
using MaterialSkiaShadow;

namespace Material3ExpressiveRefactor.Foundation.Rendering;

/// <summary>
/// The Material surface: one lookless template part that paints the whole chrome every component
/// sits on. Paint order mirrors the upstream surface/ripple nodes: elevation shadow → background /
/// border / clipped content → interaction layers (press ripples, then the state layer — Compose's
/// CommonRipple.drawIndication draws content first, then state layer and ripple ink above it).
/// <para>
/// The class composes two private visuals instead of subclassing <see cref="Border"/>: Border's
/// Render is sealed, and a parent's own paint always lands beneath its children, so neither "shadow
/// below the background" nor "state layer above the content" is reachable from a Border subclass.
/// The internal Border keeps the platform behavior for free (inset stroke, compositor-level rounded
/// clip of the content); the overlay draws above the content.
/// </para>
/// <para>
/// The shadow is drawn by MaterialSkiaShadow (the Skia point-light key/ambient model Compose
/// uses), driven by <see cref="ShadowElevation"/> in DIPs — the same semantics as the
/// <c>md.sys.elevation.level*</c> tokens, so a comp elevation slot binds straight onto it. The
/// shadow color resolves <c>md.sys.color.shadow</c> from this control's position unless
/// <see cref="ShadowColor"/> is set. ShadowElevation 0 (the default) skips the shadow, which is why flat
/// and elevated components share this one part.
/// </para>
/// <para>
/// <see cref="TonalElevation"/> tints the background toward <c>md.sys.color.surface-tint</c> along
/// the upstream log curve (ColorScheme.surfaceColorAtElevation) — only while the background is
/// exactly the <c>md.sys.color.surface</c> color (the upstream applyTonalElevation gate), so
/// components painted in a surfaceContainer* color never tint. Nested surfaces accumulate: a child
/// resolves its color from the ancestors' sum plus its own value; shadow elevation never
/// accumulates and never feeds the tint. The tinted color is precomputed into the brush handed to
/// the chrome on change, so the render pass is the same solid fill as an untinted surface.
/// </para>
/// <para>
/// When the surface is the template root of a control opted into
/// <c>MaterialInteraction.IsInteractive</c>, the owner's MaterialInteractionController drives the
/// overlay (ripples and timed state-layer fades) and, when <see cref="PressedCornerRadius"/> is
/// set, springs the corners to the pressed shape on the DefaultEffects spring; otherwise the
/// styled <see cref="StateLayerBrush"/>/<see cref="StateLayerOpacity"/> pair paints a static
/// state layer.
/// </para>
/// </summary>
public class MaterialSurface : Control
{
    /// <summary>Physical elevation in DIPs; drives the Skia shadow. 0 (the default) draws none.</summary>
    public static readonly StyledProperty<double> ShadowElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(ShadowElevation));

    /// <summary>
    /// Explicit shadow color. When null (the default), <c>md.sys.color.shadow</c> is resolved
    /// through the token system at render time, falling back to black.
    /// </summary>
    public static readonly StyledProperty<Color?> ShadowColorProperty =
        AvaloniaProperty.Register<MaterialSurface, Color?>(nameof(ShadowColor));
    
    /// <summary>
    /// Tonal elevation in DIPs: how strongly the background is tinted toward
    /// <c>md.sys.color.surface-tint</c> (see the class remarks). Nested surfaces accumulate this
    /// value; 0 (the default) paints the background verbatim.
    /// </summary>
    public static readonly StyledProperty<double> TonalElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(TonalElevation));

    /// <summary>The accumulated tonal elevation supplied to descendant material surfaces.</summary>
    internal static readonly DirectProperty<MaterialSurface, double> AbsoluteTonalElevationProperty =
        AvaloniaProperty.RegisterDirect<MaterialSurface, double>(
            nameof(AbsoluteTonalElevation),
            surface => surface.AbsoluteTonalElevation);

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

    /// <summary>
    /// The container's corners while pressed — the comp <c>PressedContainerShape</c> slot. With an
    /// interaction owner attached, a press springs the corners between the styled
    /// <see cref="CornerRadius"/> and this value (upstream: AnimatedShape on the DefaultEffects
    /// spring). Null (the default) keeps the resting shape.
    /// </summary>
    public static readonly StyledProperty<CornerRadius?> PressedCornerRadiusProperty =
        AvaloniaProperty.Register<MaterialSurface, CornerRadius?>(nameof(PressedCornerRadius));

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
        // The shadow's shape follows the corners, so both of its inputs invalidate this Render.
        AffectsRender<MaterialSurface>(ShadowElevationProperty, ShadowColorProperty, CornerRadiusProperty);
    }

    private readonly Border _chrome;
    private readonly StateLayerOverlay _overlay;
    private MaterialInteractionController? _interaction;
    private IDisposable? _interactionLease;
    private AnimatedCornerRadius? _shapeMorph;
    private MaterialSurface? _tonalParent;
    private double _absoluteTonalElevation;

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

        // The shadow color and the tonal tint both track tokens, which no property-system
        // invalidation reaches.
        ResourcesChanged += (_, _) => InvalidateTokenDrivenPaint();
        ActualThemeVariantChanged += (_, _) => InvalidateTokenDrivenPaint();
    }

    private void InvalidateTokenDrivenPaint()
    {
        InvalidateShadow();
        UpdateEffectiveBackground();
    }

    private void InvalidateShadow()
    {
        if (ShadowElevation > 0)
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

    public double ShadowElevation
    {
        get => GetValue(ShadowElevationProperty);
        set => SetValue(ShadowElevationProperty, value);
    }

    public Color? ShadowColor
    {
        get => GetValue(ShadowColorProperty);
        set => SetValue(ShadowColorProperty, value);
    }

    public double TonalElevation
    {
        get => GetValue(TonalElevationProperty);
        set => SetValue(TonalElevationProperty, value);
    }

    internal double AbsoluteTonalElevation => _absoluteTonalElevation;

    /// <summary>The brush the chrome actually paints: <see cref="Background"/> after the tonal tint.</summary>
    internal IBrush? EffectiveBackground => _chrome.Background;

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

    public CornerRadius? PressedCornerRadius
    {
        get => GetValue(PressedCornerRadiusProperty);
        set => SetValue(PressedCornerRadiusProperty, value);
    }

    /// <summary>
    /// The corners actually painted: the morph's value while a press shape-morph is live, the
    /// styled <see cref="CornerRadius"/> otherwise. Chrome, clip, ripple and shadow all read this.
    /// </summary>
    internal CornerRadius EffectiveCornerRadius => _shapeMorph?.Current ?? CornerRadius;

    public override void Render(DrawingContext context)
    {
        // The shadow paints before the visual children (chrome + content + overlay).
        // ComposeShadowRenderer requires an attached TopLevel (it reads the viewport light from
        // it); render only happens when attached, but stay defensive for offscreen hosts.
        var bounds = new Rect(Bounds.Size);
        if (ShadowElevation > 0 && bounds is { Width: > 0, Height: > 0 } && TopLevel.GetTopLevel(this) is { })
        {
            ComposeShadowRenderer.Draw(
                context,
                this,
                ComposeShadowShape.RoundedRectangle(bounds, EffectiveCornerRadius),
                ComposeShadowParameters.Material(ShadowElevation, ResolveShadowColor()));
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

        // The morph clamps its endpoints against the layout size; keep it current across resizes.
        if (_shapeMorph is not null)
            _shapeMorph.Size = finalSize;

        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ChildProperty)
        {
            ChildChanged(change);
        }
        else if (change.Property == BackgroundProperty
                 || change.Property == AbsoluteTonalElevationProperty)
        {
            UpdateEffectiveBackground();
        }
        else if (change.Property == TonalElevationProperty)
        {
            UpdateAbsoluteTonalElevation();
        }
        else if (change.Property == VisualParentProperty)
        {
            UpdateTonalParent();
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
            if (_shapeMorph is null)
            {
                _chrome.CornerRadius = change.GetNewValue<CornerRadius>();
            }
            else
            {
                // The resting shape moved under a live morph (e.g. a size-class change):
                // retarget the animation instead of snapping the painted corners.
                UpdateShapeMorph(_interaction?.Interaction.HasFlag(InteractionState.Pressed) ?? false);
            }

            _overlay.InvalidateVisual();
        }
        else if (change.Property == PressedCornerRadiusProperty)
        {
            if (_interaction is not null)
                UpdateShapeMorph(_interaction.Interaction.HasFlag(InteractionState.Pressed));

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

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Token resolution needs the resource ancestry, which only exists once attached.
        UpdateTonalParent();
        UpdateEffectiveBackground();

        // Interactive hosts: when the surface is the template root of a control whose theme opted
        // in via MaterialInteraction.IsInteractive, the owner's controller takes over the overlay —
        // state-layer alpha timing and ripple ink are its job, and the styled StateLayerOpacity
        // stays unused. A surface with no such TemplatedParent (plain surfaces, cards) keeps the
        // static styled path. A template re-application creates a fresh surface, so there is no
        // re-parenting path to handle here.
        if (TemplatedParent is Control host && MaterialInteraction.GetIsInteractive(host))
        {
            _interaction = MaterialInteraction.GetOrCreate(host);
            _interactionLease = _interaction.Attach(this);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        SetTonalParent(null);

        // The morph's animation track rides the owner's motion controller, which outlives this
        // surface — drop it explicitly or it would leak one track per detach.
        if (_shapeMorph is { } morph)
        {
            _interaction?.Motion.Remove(morph.Progress);
            _shapeMorph = null;
            _chrome.CornerRadius = CornerRadius;
        }

        _interactionLease?.Dispose();
        _interactionLease = null;
        _interaction = null;
    }

    /// <summary>
    /// Interaction frame callback: a ripple, a state-layer fade or the shape morph moved.
    /// The chrome and the shadow follow the morphed corners; the overlay repaints either way.
    /// </summary>
    internal void OnInteractionFrame()
    {
        if (_shapeMorph is { } morph)
        {
            _chrome.CornerRadius = morph.Current;
            InvalidateShadow();
        }

        _overlay.InvalidateVisual();
    }

    /// <summary>
    /// Interaction state edge callback: drives the press shape-morph off the Pressed flag and
    /// repaints the overlay for the state change.
    /// </summary>
    internal void OnInteractionChanged(InteractionState state)
    {
        UpdateShapeMorph(state.HasFlag(InteractionState.Pressed));
        _overlay.InvalidateVisual();
    }

    /// <summary>
    /// Aims the shape morph at the pressed or resting corners. The first press with a
    /// <see cref="PressedCornerRadius"/> configured creates the morph and registers it with the
    /// owner's motion controller; upstream runs it on the DefaultEffects spring ("prevent any
    /// bounce" — a shape squish must not overshoot).
    /// </summary>
    private void UpdateShapeMorph(bool pressed)
    {
        if (_interaction is null)
            return;

        var rest = CornerRadius;
        var target = pressed && PressedCornerRadius is { } pressedRadius ? pressedRadius : rest;

        if (_shapeMorph is null)
        {
            if (target.Equals(rest))
                return; // no pressed shape configured, or already at rest

            _shapeMorph = new AnimatedCornerRadius(rest, DefaultEffectsSpec()) { Size = Bounds.Size };
            _interaction.Motion.Add(_shapeMorph.Progress);
        }

        _shapeMorph.Size = Bounds.Size;
        _shapeMorph.AnimateTo(target, DefaultEffectsSpec());
        _interaction.Motion.RequestFrame();
    }

    private SpringSpec DefaultEffectsSpec() =>
        MotionSpecs.Resolve(TemplatedParent as IResourceHost ?? this, MotionRole.DefaultEffects, ActualThemeVariant);

    private Color ResolveShadowColor()
    {
        if (ShadowColor is { } color)
            return color;

        return TokenResolver.TryResolve(this, MaterialTokens.Sys.Color.Shadow, ActualThemeVariant, out var value) &&
               value is Color resolved
            ? resolved
            : Colors.Black;
    }

    // Tonal elevation (upstream: Surface.kt LocalAbsoluteTonalElevation and ColorScheme.kt
    // surfaceColorAtElevation/applyTonalElevation). Each surface sums its own TonalElevation onto
    // the nearest ancestor surface's accumulated value. An inherited attached property cannot do
    // this alone — a child that sets its own local sum stops inheriting, so it would miss later
    // ancestor changes; hence the explicit subscription to the nearest MaterialSurface ancestor,
    // which also keeps the change traffic on the surface chain instead of the whole subtree.

    private void UpdateTonalParent()
    {
        SetTonalParent(this.GetVisualAncestors().OfType<MaterialSurface>().FirstOrDefault());
    }

    private void SetTonalParent(MaterialSurface? parent)
    {
        if (ReferenceEquals(_tonalParent, parent))
        {
            UpdateAbsoluteTonalElevation();
            return;
        }

        if (_tonalParent is not null)
            _tonalParent.PropertyChanged -= OnTonalParentPropertyChanged;

        _tonalParent = parent;

        if (_tonalParent is not null)
            _tonalParent.PropertyChanged += OnTonalParentPropertyChanged;

        UpdateAbsoluteTonalElevation();
    }

    private void OnTonalParentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == AbsoluteTonalElevationProperty)
            UpdateAbsoluteTonalElevation();
    }

    private void UpdateAbsoluteTonalElevation()
    {
        var inherited = _tonalParent?.AbsoluteTonalElevation ?? 0;
        var resolved = Math.Max(0, inherited) + Math.Max(0, TonalElevation);
        if (Math.Abs(resolved - _absoluteTonalElevation) < 0.000001)
            return;

        // The raise feeds both the subscribed descendants and this surface's own background
        // refresh (OnPropertyChanged → UpdateEffectiveBackground).
        SetAndRaise(AbsoluteTonalElevationProperty, ref _absoluteTonalElevation, resolved);
    }

    private void UpdateEffectiveBackground()
    {
        _chrome.Background = ResolveEffectiveBackground();
    }

    // Upstream gate (applyTonalElevation): the tint applies only while the background is exactly
    // the scheme surface color — a surfaceContainer* or custom background is painted verbatim at
    // any elevation. The tinted color is precomputed here, on change, so the render pass stays
    // the same solid fill as an untinted surface.
    private IBrush? ResolveEffectiveBackground()
    {
        if (Background is not ISolidColorBrush solid || _absoluteTonalElevation <= 0)
            return Background;

        if (!TokenResolver.TryResolve(this, MaterialTokens.Sys.Color.Surface, ActualThemeVariant, out var surfaceValue) ||
            surfaceValue is not Color surface ||
            !TokenResolver.TryResolve(this, MaterialTokens.Sys.Color.SurfaceTint, ActualThemeVariant, out var tintValue) ||
            tintValue is not Color surfaceTint ||
            solid.Color != surface)
        {
            return Background;
        }

        var alpha = ((4.5 * Math.Log(_absoluteTonalElevation + 1)) + 2) / 100;
        return new ImmutableSolidColorBrush(ColorComposite.Over(surfaceTint, alpha, surface), solid.Opacity);
    }

    /// <summary>
    /// The always-on-top paint layer. With an interaction owner attached it paints what the
    /// owner's controller says (press ripples, then the state layer, both clipped to the corners);
    /// otherwise it falls back to the styled StateLayerBrush/Opacity pair for plain surfaces.
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
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            if (_owner._interaction is { } interaction)
            {
                interaction.DrawLayers(context, _owner.StateLayerBrush, _owner.EffectiveCornerRadius, Bounds.Size);
                return;
            }

            var brush = _owner.StateLayerBrush;
            var opacity = _owner.StateLayerOpacity;
            if (brush is null || opacity <= 0)
                return;

            using (context.PushOpacity(opacity))
                context.DrawRectangle(brush, null, new RoundedRect(new Rect(Bounds.Size), _owner.EffectiveCornerRadius));
        }
    }
}

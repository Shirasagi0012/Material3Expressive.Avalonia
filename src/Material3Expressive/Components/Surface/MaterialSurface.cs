using AnimatedCornerRadius = Material3Expressive.Foundation.Interaction.AnimatedCornerRadius;
using ComposeAnimation;
using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Rendering;
using MaterialSkiaShadow;

namespace Material3Expressive.Components;

/// <summary>Hosts one child and paints a Material container around it.</summary>
/// <remarks>
/// The internal <see cref="Border"/> paints the background and clips the content; sibling visuals
/// paint interaction feedback and the border above it. Keeping the border separate preserves
/// Compose's drawing order without consuming content space. <see cref="Border.Render"/> is sealed
/// and a control's own render pass precedes its visual children, so the surface itself draws only
/// the shadow below these visuals.
/// <para>
/// Tonal elevation accumulates through nested surfaces and tints only the theme surface color.
/// Shadow elevation stays local. When used as an interactive control's template root, the surface
/// also presents that control's ripple, state layer, and pressed-corner animation.
/// </para>
/// </remarks>
public sealed class MaterialSurface : Control
{
    private const double ElevationEpsilon = 0.000001;

    /// <summary>Physical elevation in DIPs; drives the Skia shadow. 0 (the default) draws none.</summary>
    public static readonly StyledProperty<double> ShadowElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(ShadowElevation));

    /// <summary>Explicit shadow color, or null to use the theme shadow color.</summary>
    public static readonly StyledProperty<Color?> ShadowColorProperty =
        AvaloniaProperty.Register<MaterialSurface, Color?>(nameof(ShadowColor));
    
    /// <summary>Tonal elevation in DIPs. Nested surfaces accumulate this value.</summary>
    public static readonly StyledProperty<double> TonalElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, double>(nameof(TonalElevation));

    // Theme configuration; runtime paint and animation never resolve tokens themselves.
    internal static readonly StyledProperty<Color> DefaultShadowColorProperty = AvaloniaProperty.Register<MaterialSurface, Color>(nameof(DefaultShadowColor));
    internal Color DefaultShadowColor { get => GetValue(DefaultShadowColorProperty); set => SetValue(DefaultShadowColorProperty, value); }
    internal static readonly StyledProperty<Color?> TonalSurfaceColorProperty = AvaloniaProperty.Register<MaterialSurface, Color?>(nameof(TonalSurfaceColor));
    internal Color? TonalSurfaceColor { get => GetValue(TonalSurfaceColorProperty); set => SetValue(TonalSurfaceColorProperty, value); }
    internal static readonly StyledProperty<Color?> TonalSurfaceTintColorProperty = AvaloniaProperty.Register<MaterialSurface, Color?>(nameof(TonalSurfaceTintColor));
    internal Color? TonalSurfaceTintColor { get => GetValue(TonalSurfaceTintColorProperty); set => SetValue(TonalSurfaceTintColorProperty, value); }
    internal static readonly StyledProperty<SpringSpec?> ShapeAnimationProperty = AvaloniaProperty.Register<MaterialSurface, SpringSpec?>(nameof(ShapeAnimation));
    internal SpringSpec? ShapeAnimation { get => GetValue(ShapeAnimationProperty); set => SetValue(ShapeAnimationProperty, value); }
    internal static readonly StyledProperty<double> HoverStateLayerOpacityProperty = AvaloniaProperty.Register<MaterialSurface, double>(nameof(HoverStateLayerOpacity));
    internal double HoverStateLayerOpacity { get => GetValue(HoverStateLayerOpacityProperty); set => SetValue(HoverStateLayerOpacityProperty, value); }
    internal static readonly StyledProperty<double> FocusStateLayerOpacityProperty = AvaloniaProperty.Register<MaterialSurface, double>(nameof(FocusStateLayerOpacity));
    internal double FocusStateLayerOpacity { get => GetValue(FocusStateLayerOpacityProperty); set => SetValue(FocusStateLayerOpacityProperty, value); }
    internal static readonly StyledProperty<double> PressedStateLayerOpacityProperty = AvaloniaProperty.Register<MaterialSurface, double>(nameof(PressedStateLayerOpacity));
    internal double PressedStateLayerOpacity { get => GetValue(PressedStateLayerOpacityProperty); set => SetValue(PressedStateLayerOpacityProperty, value); }
    internal static readonly StyledProperty<double> DraggedStateLayerOpacityProperty = AvaloniaProperty.Register<MaterialSurface, double>(nameof(DraggedStateLayerOpacity));
    internal double DraggedStateLayerOpacity { get => GetValue(DraggedStateLayerOpacityProperty); set => SetValue(DraggedStateLayerOpacityProperty, value); }

    // The surface reads its inherited value; the chrome provides the new total to its content.
    private static readonly AttachedProperty<double> AmbientTonalElevationProperty =
        AvaloniaProperty.RegisterAttached<MaterialSurface, Control, double>(
            "AmbientTonalElevation",
            inherits: true);

    /// <summary>The state-layer and ripple color used by the interaction controller.</summary>
    public static readonly StyledProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.Register<MaterialSurface, IBrush?>(nameof(StateLayerBrush));

    /// <summary>The container corners while pressed, or null to keep the resting shape.</summary>
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
        AffectsRender<MaterialSurface>(ShadowElevationProperty, ShadowColorProperty, DefaultShadowColorProperty);
    }

    private readonly Border _chrome;
    private readonly InteractionOverlay _interactionOverlay;
    private readonly SurfaceOutline _outline;
    private MaterialInteractionController? _interactionController;
    private IDisposable? _interactionAttachment;
    private AnimatedCornerRadius? _shapeMorph;
    private CornerRadius _shapeRest;
    private CornerRadius? _shapePressed;
    private CornerRadius? _shapeChecked;
    private Border? _checkedOverlay;
    private AnimatedButtonBorder? _animatedBorder;
    private SpringSpec _shapeSpec;
    private bool _shapeCommitPending;
    private bool _shapeInteractionDeferred;
    private int _shapeGeneration;
    private CancellationTokenSource? _shapeEffect;
    private Task? _shapeEffectTask;
    private double _absoluteTonalElevation;

    public MaterialSurface()
    {
        _chrome = new Border
        {
            ClipToBounds = true,
            [!Decorator.PaddingProperty] = this[!PaddingProperty],
            [!Decorator.ChildProperty] = this[!ChildProperty],
        };
        _interactionOverlay = new InteractionOverlay(this);
        // Surface.kt applies border as a draw modifier, outside background/clip/interaction.
        // Avalonia Border includes its thickness in layout, so only this childless visual paints it.
        _outline = new SurfaceOutline
        {
            // Compose's border modifier surrounds the shape clip. Clipping the border
            // itself applies its rounded antialias coverage twice, especially in dark mode.
            ClipToBounds = false,
            IsHitTestVisible = false,
            Focusable = false,
        };
        VisualChildren.Add(_chrome);
        VisualChildren.Add(_interactionOverlay);
        VisualChildren.Add(_outline);

        // Decorator owns the hosted child's logical and visual parenting. The overlay is visual-only.
        ((ISetLogicalParent)_chrome).SetParent(this);
        LogicalChildren.Add(_chrome);

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

    public CornerRadius? PressedCornerRadius
    {
        get => GetValue(PressedCornerRadiusProperty);
        set => SetValue(PressedCornerRadiusProperty, value);
    }

    /// <summary>The corners currently used by the chrome, shadow, and interaction overlay.</summary>
    internal CornerRadius EffectiveCornerRadius => _chrome.CornerRadius;

    internal CornerRadius SplitOpticalCornerRadius => _shapeMorph?.SplitOpticalCorners()
        ?? EffectiveCornerRadius;

    private CornerRadius SelectedCornerRadius =>
        _interactionController?.Interaction.HasFlag(InteractionState.Pressed) == true && PressedCornerRadius is { } pressed
            ? pressed : ButtonSurfaceState.GetIsChecked(this) == true && ButtonSurfaceState.GetCheckedCornerRadius(this) is { } selected
                ? selected : CornerRadius;
    private CornerRadius CurrentCornerRadius => _shapeMorph?.Current ?? SelectedCornerRadius;

    public override void Render(DrawingContext context)
    {
        DrawShadow(context);
        base.Render(context);
    }

    private void DrawShadow(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (ShadowElevation <= 0 || bounds.Width <= 0 || bounds.Height <= 0 || TopLevel.GetTopLevel(this) is null)
            return;

        ComposeShadowRenderer.Draw(
            context,
            this,
            ComposeShadowShape.RoundedRectangle(bounds, EffectiveCornerRadius),
            ComposeShadowParameters.Material(ShadowElevation, ResolveShadowColor()));
    }

    private void InvalidateShadow()
    {
        if (ShadowElevation > 0)
            InvalidateVisual();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _chrome.Measure(availableSize);
        var desired = _chrome.DesiredSize;
        _interactionOverlay.Measure(desired);
        _outline.Measure(desired);
        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_shapeMorph is not null)
        {
            _shapeMorph.Size = finalSize;
            if (UpdateCornerRadius())
                _interactionOverlay.InvalidateVisual();
        }
        var rect = new Rect(finalSize);
        _chrome.Arrange(rect);
        _interactionOverlay.Arrange(rect);
        _outline.Arrange(rect);
        _checkedOverlay?.Arrange(rect);

        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BackgroundProperty || change.Property == TonalSurfaceColorProperty
            || change.Property == TonalSurfaceTintColorProperty)
        {
            UpdateEffectiveBackground();
        }
        else if (change.Property == TonalElevationProperty
                 || change.Property == AmbientTonalElevationProperty)
        {
            UpdateAbsoluteTonalElevation();
        }
        else if (change.Property == CornerRadiusProperty)
        {
            QueueShapeMorph();
            UpdateInteractionVisuals();
        }
        else if (change.Property == PressedCornerRadiusProperty)
        {
            if (_interactionController is not null)
            {
                QueueShapeMorph();
                UpdateInteractionVisuals();
            }
        }
        else if (change.Property == ShapeAnimationProperty)
        {
            QueueShapeMorph();
        }
        else if (change.Property == HoverStateLayerOpacityProperty || change.Property == FocusStateLayerOpacityProperty
            || change.Property == PressedStateLayerOpacityProperty || change.Property == DraggedStateLayerOpacityProperty)
        {
            _interactionController?.UpdateStateLayerConfiguration();
        }
        else if (change.Property == StateLayerBrushProperty)
        {
            _interactionOverlay.InvalidateVisual();
            UpdateCheckedOverlay();
        }
        else if (change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty)
        {
            UpdateButtonBorder();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // The theme supplies the current tonal palette through effective properties.
        UpdateEffectiveBackground();

        if (TemplatedParent is Control host && MaterialInteraction.GetIsInteractive(host))
        {
            _interactionController = MaterialInteraction.GetOrCreate(host);
            _interactionAttachment = _interactionController.Attach(this);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        // The controller resets this presenter's motion when its attachment is released.
        _interactionAttachment?.Dispose();
        _interactionAttachment = null;
        _interactionController = null;
    }

    /// <summary>Refreshes the interaction overlay and any animated corners.</summary>
    internal void OnInteractionFrame() => UpdateInteractionVisuals();

    /// <summary>Updates the pressed shape when the interaction state changes.</summary>
    internal void OnInteractionChanged(InteractionState state)
    {
        _shapeInteractionDeferred = false;
        QueueShapeMorph();
        UpdateInteractionVisuals();
    }

    internal void DeferShapeInteraction() => _shapeInteractionDeferred = true;

    internal void OnButtonStateChanged()
    {
        QueueShapeMorph();
        UpdateCheckedOverlay();
        UpdateInteractionVisuals();
    }

    private void UpdateCheckedOverlay()
    {
        var opacity = ButtonSurfaceState.GetIsChecked(this) == true ? ButtonSurfaceState.GetCheckedOverlayOpacity(this) : 0;
        if (_checkedOverlay is null && opacity > 0)
        {
            _checkedOverlay = new Border { IsHitTestVisible = false };
            VisualChildren.Add(_checkedOverlay);
            _checkedOverlay.Arrange(new Rect(Bounds.Size));
        }
        if (_checkedOverlay is { } overlay)
        {
            // DrawOutline(color, alpha) modulates the color before painting, rather
            // than applying opacity to a separately rasterized layer.
            if (StateLayerBrush is ISolidColorBrush solid)
            {
                var ink = solid.Color;
                overlay.Background = new SolidColorBrush(Color.FromArgb(
                    (byte)Math.Clamp((int)MathF.Floor(ink.A * (float)opacity + .5f), 0, 255), ink.R, ink.G, ink.B));
                overlay.Opacity = solid.Opacity;
            }
            else
            {
                overlay.Background = StateLayerBrush;
                overlay.Opacity = opacity;
            }
            overlay.CornerRadius = EffectiveCornerRadius;
        }
    }

    internal void OnBorderAnimationConfigurationChanged() => UpdateButtonBorder();

    private void UpdateButtonBorder()
    {
        if (ButtonSurfaceState.GetAnimateBorder(this) && _interactionController?.Motion is { IsEnding: false } motion)
        {
            _animatedBorder ??= new AnimatedButtonBorder(this, _outline, motion);
            _animatedBorder.Update();
        }
        else
        {
            _outline.UseHairline = false;
            _outline.BorderBrush = BorderBrush;
            _outline.BorderThickness = BorderThickness;
            UpdateOutlineCornerRadius();
        }
    }

    internal void UpdateOutlineCornerRadius()
    {
        // SurfaceOutline owns physical stroke rounding and the inset geometry. Supply
        // the actual outer corners so the border follows the chrome and is inset once.
        _outline.CornerRadius = EffectiveCornerRadius;
    }

    internal void ResetMotion()
    {
        _shapeGeneration++;
        _shapeCommitPending = false;
        _shapeInteractionDeferred = false;
        _shapeEffect?.Cancel();
        _shapeEffect = null;
        _shapeEffectTask = null;
        _shapeMorph?.Dispose();
        _shapeMorph = null;
        _animatedBorder?.Dispose();
        _animatedBorder = null;
        _chrome.CornerRadius = _outline.CornerRadius = SelectedCornerRadius;
        // ResetAnimations disposes the parent scope immediately after this callback.
        // Paint the target without creating another child in that ending lifetime.
        _outline.UseHairline = ButtonSurfaceState.GetAnimateBorder(this);
        _outline.BorderBrush = BorderBrush;
        _outline.BorderThickness = BorderThickness;
        UpdateOutlineCornerRadius();
        InvalidateVisual();
        _interactionOverlay.InvalidateVisual();
    }

    private void UpdateInteractionVisuals()
    {
        UpdateCornerRadius();
        _interactionOverlay.InvalidateVisual();
    }

    private bool UpdateCornerRadius()
    {
        var cornerRadius = CurrentCornerRadius;
        if (_shapeMorph is { Size.Height: > 0 } morph)
        {
            // CornerBasedShape.createOutline normalizes after interpolation (including
            // spatial-spring overshoot). Keep chrome, ripple clip and shadow on that outline.
            var extent = (float)Math.Min(morph.Size.Width, morph.Size.Height);
            var left = (float)(cornerRadius.TopLeft + cornerRadius.BottomLeft);
            var right = (float)(cornerRadius.TopRight + cornerRadius.BottomRight);
            var leftScale = left > extent ? extent / left : 1f;
            var rightScale = right > extent ? extent / right : 1f;
            cornerRadius = new((float)cornerRadius.TopLeft * leftScale, (float)cornerRadius.TopRight * rightScale,
                (float)cornerRadius.BottomRight * rightScale, (float)cornerRadius.BottomLeft * leftScale);
        }
        if (_chrome.CornerRadius == cornerRadius)
            return false;

        _chrome.CornerRadius = cornerRadius;
        UpdateOutlineCornerRadius();
        if (_checkedOverlay is { } overlay) overlay.CornerRadius = cornerRadius;
        if (Child is SplitButtonOpticalLayout optical) optical.InvalidateArrange();
        InvalidateShadow();
        return true;
    }

    private void QueueShapeMorph()
    {
        if (_interactionController?.Motion is not { IsEnding: false } motion)
            return;

        // rememberAnimatedShape creates its initial state during composition. Subsequent
        // interaction/configuration changes are committed by recomposition before its effect.
        UpdateButtonBorder();
        // Required theme input can temporarily disappear during binding or reparenting.
        // Do not create a zero-stiffness spring or replace an effect's captured valid spec.
        if (!TryGetShapeMorphSpec(out var initialSpec))
            return;
        if (_shapeMorph is null && (PressedCornerRadius is not null || ButtonSurfaceState.GetCheckedCornerRadius(this) is not null))
            RecreateShapeMorph(motion, initialSpec);
        if (_shapeCommitPending)
            return;

        _shapeCommitPending = true;
        var generation = _shapeGeneration;
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            if (generation != _shapeGeneration)
                return;
            _shapeCommitPending = false;
            if (_shapeInteractionDeferred)
                return;

            // Recheck after the composition frame: a resource may have been withdrawn while
            // this commit waited. The next valid property update queues the current target again.
            if (!TryGetShapeMorphSpec(out var spec))
                return;
            var target = SelectedCornerRadius;
            var configurationChanged = _shapeRest != CornerRadius || _shapePressed != PressedCornerRadius
                || _shapeChecked != ButtonSurfaceState.GetCheckedCornerRadius(this) || !_shapeSpec.Equals(spec);
            var recreate = configurationChanged && (ButtonSurfaceState.GetResetOnShapeChange(this) || !_shapeSpec.Equals(spec));
            if (configurationChanged && !recreate)
            {
                _shapeRest = CornerRadius;
                _shapePressed = PressedCornerRadius;
                _shapeChecked = ButtonSurfaceState.GetCheckedCornerRadius(this);
            }
            // SplitButton remembers by spec and keys its effect by the selected shape.
            // Changing an inactive shape must not cancel an animation to the same target.
            if (!recreate && (_shapeMorph is null || _shapeMorph.Target == target))
                return;

            // LaunchedEffect cancels and joins the previous effect before running its replacement.
            // This also lets Animatable finish cancellation before the morph reads its velocity.
            _shapeEffect?.Cancel();
            if (_shapeEffectTask is { } previous)
            {
                try { await previous; }
                catch (OperationCanceledException) when (previous.IsCanceled) { }
            }
            ct.ThrowIfCancellationRequested();
            if (generation != _shapeGeneration)
                return;
            // Button/IconButton key(shapes), and AnimatedShape remembers by animationSpec.
            // A new configuration starts at its selected shape; a press keeps the existing state.
            if (recreate)
                RecreateShapeMorph(motion, spec);

            if (_shapeMorph is not { } shape)
                return;
            shape.Size = Bounds.Size;
            using var effect = CancellationTokenSource.CreateLinkedTokenSource(ct);
            _shapeEffect = effect;
            var task = _shapeEffectTask = shape.AnimateToAsync(target, spec, effect.Token);
            try { await task; }
            finally
            {
                if (ReferenceEquals(_shapeEffect, effect))
                {
                    _shapeEffect = null;
                    _shapeEffectTask = null;
                }
            }
        }, "Shape composition");
    }

    private void RecreateShapeMorph(VisualAnimationScope motion, SpringSpec spec)
    {
        _shapeMorph?.Dispose();
        _shapeRest = CornerRadius;
        _shapePressed = PressedCornerRadius;
        _shapeChecked = ButtonSurfaceState.GetCheckedCornerRadius(this);
        _shapeSpec = spec;
        _shapeMorph = PressedCornerRadius is null && _shapeChecked is null ? null
            : new AnimatedCornerRadius(SelectedCornerRadius, spec, motion, UpdateInteractionVisuals) { Size = Bounds.Size };
        UpdateInteractionVisuals();
    }

    private bool TryGetShapeMorphSpec(out SpringSpec spec)
    {
        var candidate = ButtonSurfaceState.GetShapeSpec(this) ?? ShapeAnimation;
        if (candidate is { } value && double.IsFinite(value.DampingRatio) && value.DampingRatio >= 0
            && double.IsFinite(value.Stiffness) && value.Stiffness > 0
            && float.IsFinite((float)value.VisibilityThreshold) && value.VisibilityThreshold >= 0)
        {
            // A zero threshold retains the runtime's documented unspecified-threshold semantics.
            spec = value;
            return true;
        }
        spec = default;
        return false;
    }

    private Color ResolveShadowColor() => ShadowColor ?? DefaultShadowColor;

    private void UpdateAbsoluteTonalElevation()
    {
        var resolved = Math.Max(0, GetValue(AmbientTonalElevationProperty)) + Math.Max(0, TonalElevation);
        if (Math.Abs(resolved - _absoluteTonalElevation) < ElevationEpsilon)
            return;

        _absoluteTonalElevation = resolved;
        _chrome.SetValue(AmbientTonalElevationProperty, resolved);
        UpdateEffectiveBackground();
    }

    private void UpdateEffectiveBackground() => _chrome.Background = ResolveEffectiveBackground();

    // Upstream gate (applyTonalElevation): the tint applies only while the background is exactly
    // the scheme surface color — a surfaceContainer* or custom background is painted verbatim at
    // any elevation. The tinted color is precomputed here, on change, so the render pass stays
    // the same solid fill as an untinted surface.
    private IBrush? ResolveEffectiveBackground()
    {
        if (Background is not ISolidColorBrush solid || _absoluteTonalElevation <= 0)
            return Background;

        if (TonalSurfaceColor is not { } surface || solid.Color != surface ||
            TonalSurfaceTintColor is not { } surfaceTint)
            return Background;

        var color = ColorComposite.Over(surfaceTint, TonalOverlayAlpha(_absoluteTonalElevation), surface);
        return new ImmutableSolidColorBrush(color, solid.Opacity);
    }

    private static double TonalOverlayAlpha(double elevation) =>
        ((4.5 * Math.Log(elevation + 1)) + 2) / 100;

    /// <summary>Visual-only layer that paints interaction feedback above the content.</summary>
    private sealed class InteractionOverlay : Control
    {
        private readonly MaterialSurface _owner;

        public InteractionOverlay(MaterialSurface owner)
        {
            _owner = owner;
            IsHitTestVisible = false;
            Focusable = false;
        }

        public override void Render(DrawingContext context)
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            _owner._interactionController?.DrawLayers(
                context,
                _owner.StateLayerBrush,
                _owner.EffectiveCornerRadius,
                Bounds.Size);
        }
    }
}

using Avalonia.LogicalTree;
using Avalonia.Layout;
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
/// The surface paints its shadow and background before the internal <see cref="Border"/>
/// clips the content. Sibling visuals paint interaction feedback and the border above it.
/// The background is outside the content clip, matching Surface.kt's background().clip().
/// The separate border preserves drawing order without consuming content space.
/// <para>
/// Tonal elevation accumulates through nested surfaces and tints only the theme surface color.
/// Shadow elevation stays local. When used as an interactive control's template root, the surface
/// also presents that control's ripple, state layer, and pressed-corner animation.
/// </para>
/// </remarks>
public sealed class MaterialSurface : Control
{
    // Some consumers (DragHandle) clip a shape around a shape-independent ripple().
    // Their path changes must not replace the indication node.
    internal static readonly StyledProperty<bool> ResetIndicationOnShapeChangeProperty =
        AvaloniaProperty.Register<MaterialSurface, bool>(nameof(ResetIndicationOnShapeChange), true);
    internal bool ResetIndicationOnShapeChange { get => GetValue(ResetIndicationOnShapeChangeProperty); set => SetValue(ResetIndicationOnShapeChangeProperty, value); }
    // Physical-layout consumers may keep the chrome LTR while their Shape still uses
    // the caller's logical direction. The outline is mirrored only for this surface's tree.
    internal static readonly StyledProperty<FlowDirection?> ShapeLayoutDirectionProperty =
        AvaloniaProperty.Register<MaterialSurface, FlowDirection?>(nameof(ShapeLayoutDirection));
    internal FlowDirection? ShapeLayoutDirection { get => GetValue(ShapeLayoutDirectionProperty); set => SetValue(ShapeLayoutDirectionProperty, value); }
    public static readonly StyledProperty<MaterialShape?> ShapeProperty =
        AvaloniaProperty.Register<MaterialSurface, MaterialShape?>(nameof(Shape));
    /// <summary>An explicit container path, or null to use CornerRadius. Path borders require uniform thickness.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    /// <summary>The pressed path for an explicit Shape. Cut corners interpolate; other paths switch directly.</summary>
    public static readonly StyledProperty<MaterialShape?> PressedShapeProperty =
        AvaloniaProperty.Register<MaterialSurface, MaterialShape?>(nameof(PressedShape));
    public MaterialShape? PressedShape { get => GetValue(PressedShapeProperty); set => SetValue(PressedShapeProperty, value); }
    internal static readonly StyledProperty<MaterialShape?> CheckedShapeProperty =
        AvaloniaProperty.Register<MaterialSurface, MaterialShape?>(nameof(CheckedShape));
    internal MaterialShape? CheckedShape { get => GetValue(CheckedShapeProperty); set => SetValue(CheckedShapeProperty, value); }
    internal static readonly StyledProperty<bool> ApplyTonalElevationProperty =
        AvaloniaProperty.Register<MaterialSurface, bool>(nameof(ApplyTonalElevation), true);
    internal bool ApplyTonalElevation { get => GetValue(ApplyTonalElevationProperty); set => SetValue(ApplyTonalElevationProperty, value); }
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
        AffectsRender<MaterialSurface>(BackgroundProperty, ShadowElevationProperty, ShadowColorProperty, DefaultShadowColorProperty);
    }

    private readonly Border _chrome;
    private readonly InteractionOverlay _interactionOverlay;
    private readonly SurfaceOutline _outline;
    private InsetFocusRingPresenter? _focusRing;
    // Components such as Switch render focus at a different location from their ripple.
    internal static readonly StyledProperty<bool> PresentsFocusRingProperty =
        AvaloniaProperty.Register<MaterialSurface, bool>(nameof(PresentsFocusRing), true);
    internal bool PresentsFocusRing { get => GetValue(PresentsFocusRingProperty); set => SetValue(PresentsFocusRingProperty, value); }
    internal static readonly StyledProperty<CornerRadius?> FocusRingCornerRadiusProperty =
        AvaloniaProperty.Register<MaterialSurface, CornerRadius?>(nameof(FocusRingCornerRadius));
    internal CornerRadius? FocusRingCornerRadius { get => GetValue(FocusRingCornerRadiusProperty); set => SetValue(FocusRingCornerRadiusProperty, value); }
    private MaterialInteractionController? _interactionController;
    private IDisposable? _interactionAttachment;
    private AnimatedCornerRadius? _shapeMorph;
    private CornerRadius _shapeRest;
    private CornerRadius? _shapePressed;
    private CornerRadius? _shapeChecked;
    private CheckedOverlay? _checkedOverlay;
    private AnimatedButtonBorder? _animatedBorder;
    private SpringSpec _shapeSpec;
    private bool _shapeConfigurationInitialized;
    private bool _shapeCommitPending;
    private bool _shapeInteractionDeferred;
    private int _shapeGeneration;
    private CancellationTokenSource? _shapeEffect;
    private Task? _shapeEffectTask;
    private double _absoluteTonalElevation;
    private Size _shapeSize;
    private CornerRadius? _animatedCorners;
    private IBrush? _effectiveBackground;
    private Geometry? _containerGeometry;
    private ComposeShadowShape? _containerShadowShape;
    private MaterialShape? _observedShape;
    private MaterialShape? _observedPressedShape;
    private MaterialShape? _observedCheckedShape;
    private MaterialShape? _pathRest;
    private MaterialShape? _pathPressed;
    private MaterialShape? _pathChecked;
    private CornerShape? _cornerRest;
    private CornerShape? _cornerPressed;
    private CornerShape? _cornerChecked;
    private MaterialShape? _indicationPath;
    private bool _shapeUsesCorners;
    private PathGeometry? _shapeGeometry;
    private ComposeShadowShape? _shapeShadow;
    private Geometry? EffectiveGeometry => _shapeGeometry ?? _containerGeometry;
    private bool CanMorphCorners => Shape is CutCornerShape or RoundedCornerShape
        && (PressedShape is not null || CheckedShape is not null || ButtonSurfaceState.GetSplitOuterCorners(this))
        && (PressedShape is null or CutCornerShape or RoundedCornerShape) && (CheckedShape is null or CutCornerShape or RoundedCornerShape);
    private MaterialShape? SelectedPath => Shape is null ? null
        : (ButtonSurfaceState.GetIsPressed(this) ?? _interactionController?.Interaction.HasFlag(InteractionState.Pressed)) == true
            ? PressedShape ?? Shape
            : ButtonSurfaceState.GetIsChecked(this) == true && CheckedShape is { } selected ? selected : Shape;
    private bool UsesRoundOutline => Shape is not null && (CanMorphCorners && _shapeMorph is { } morph
        ? !morph.CurrentIsCut : SelectedPath is RoundedCornerShape);
    private PathGeometry? GenericShapePath => SelectedPath is CutCornerShape
        && (_animatedCorners is { } externalCorners ? externalCorners == default : ((CutCornerShape)SelectedPath).IsRectangle)
        && (!CanMorphCorners || _shapeMorph is null || _shapeMorph.Current == default) ? null : _shapeGeometry;

    private void ObserveShape()
    {
        if (_observedShape is { } old) old.PropertyChanged -= ShapeChanged;
        if (_observedPressedShape is { } oldPressed) oldPressed.PropertyChanged -= ShapeChanged;
        if (_observedCheckedShape is { } oldChecked) oldChecked.PropertyChanged -= ShapeChanged;
        _observedShape = this.IsAttachedToVisualTree() ? Shape : null;
        _observedPressedShape = this.IsAttachedToVisualTree() ? PressedShape : null;
        _observedCheckedShape = this.IsAttachedToVisualTree() ? CheckedShape : null;
        if (_observedShape is { } shape) shape.PropertyChanged += ShapeChanged;
        if (_observedPressedShape is { } pressed) pressed.PropertyChanged += ShapeChanged;
        if (_observedCheckedShape is { } selected) selected.PropertyChanged += ShapeChanged;
    }

    private void ShapeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        QueueShapeMorph();
        UpdateShape();
    }

    private void UpdateShape()
    {
        if (!UsesRoundOutline && SelectedPath is { } shape && _shapeSize.Width > 0 && _shapeSize.Height > 0)
        {
            if (!BorderThickness.IsUniform)
                throw new InvalidOperationException("MaterialShape borders require uniform BorderThickness.");
            var density = LayoutHelper.GetLayoutScale(this);
            var direction = ShapeLayoutDirection ?? FlowDirection;
            var geometry = shape is CutCornerShape && _animatedCorners is { } externalCorners
                ? CutCornerShape.CreateOutline(_shapeSize, direction, density, externalCorners)
                : CanMorphCorners && _shapeMorph is { } morph
                ? CutCornerShape.CreateOutline(_shapeSize, direction, density, morph.Current)
                : shape.CreateOutline(_shapeSize, direction, density);
            _shapeGeometry = PathGeometry.Parse(geometry.ToString());
            _shapeGeometry.FillRule = geometry.FillRule;
            _shapeGeometry.Transform = new MatrixTransform(geometry.Transform?.Value ?? Matrix.Identity);
            // The factory returns physical coordinates; Avalonia mirrors the entire RTL
            // subtree, so cancel that mirror on the outline while retaining logical content.
            if (FlowDirection == FlowDirection.RightToLeft)
                _shapeGeometry.Transform = new MatrixTransform((geometry.Transform?.Value ?? Matrix.Identity)
                    * new Matrix(-1, 0, 0, 1, _shapeSize.Width, 0));
            using var path = ShapeGeometry.ToSkiaPath(_shapeGeometry);
            _shapeShadow = path.IsEmpty ? null : ComposeShadowShape.FromPath(path);
        }
        else { _shapeGeometry = null; _shapeShadow = null; }
        UpdateCornerRadius();
        _outline.ShapePath = GenericShapePath;
        _chrome.ClipToBounds = EffectiveGeometry is null;
        _chrome.Clip = EffectiveGeometry;
        if (_checkedOverlay is { } overlay)
        {
            overlay.Shape = EffectiveGeometry;
            overlay.InvalidateVisual();
        }
        _interactionOverlay.InvalidateVisual();
        UpdateFocusRingGeometry();
        InvalidateVisual();
    }

    // Tooltip's union outline: background, child clip and shadow share the same shape.
    // Retained for Tooltip's internally assembled union; public paths use MaterialShape.
    internal void SetContainerGeometry(Geometry? geometry, ComposeShadowShape? shadowShape)
    {
        _containerGeometry = geometry;
        _containerShadowShape = shadowShape;
        _chrome.ClipToBounds = EffectiveGeometry is null;
        _chrome.Clip = EffectiveGeometry;
        InvalidateVisual();
    }

    internal Geometry? ContainerGeometry => EffectiveGeometry;

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
            // Visual-only layers do not inherit FlowDirection. Keep their local axes
            // aligned with Surface instead of applying a second RTL mirror.
            [!FlowDirectionProperty] = this[!FlowDirectionProperty],
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

    /// <summary>The painted background: <see cref="Background"/> after the tonal tint.</summary>
    internal IBrush? EffectiveBackground => _effectiveBackground;

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

    internal CornerRadius SplitOpticalCornerRadius
    {
        get
        {
            // A generic Shape has no optical centering contract. If any configured state
            // is generic, Split uses CornerBasedShape directly for its remaining cut states
            // (without AnimatedShape's per-corner height clamp).
            if (SelectedPath is { } path)
            {
                if (CornerShape.FromMaterialShape(path) is not { } configuredCorners) return default;
                if (!CanMorphCorners) return configuredCorners.Resolve(_shapeSize);
            }
            // AnimatedShape.offset uses unnormalized corners, also when no morph is running.
            var corners = CurrentCornerRadius;
            var maximum = _shapeSize.Height / 2;
            return new(Math.Clamp(corners.TopLeft, 0, maximum), Math.Clamp(corners.TopRight, 0, maximum),
                Math.Clamp(corners.BottomRight, 0, maximum), Math.Clamp(corners.BottomLeft, 0, maximum));
        }
    }

    private CornerShape SelectedShape
    {
        get
        {
            if (CornerShape.FromMaterialShape(SelectedPath) is { } cornerShape) return cornerShape;
            var fullFraction = ButtonSurfaceState.GetSplitOuterCorners(this) ? 1f : .5f;
            if ((ButtonSurfaceState.GetIsPressed(this) ?? _interactionController?.Interaction.HasFlag(InteractionState.Pressed)) == true
                && PressedCornerRadius is { } pressed)
                return new CornerShape.Defined(pressed, fullFraction);
            if (ButtonSurfaceState.GetIsChecked(this) == true && ButtonSurfaceState.GetCheckedCornerRadius(this) is { } selected)
                return new CornerShape.Defined(selected);
            return new CornerShape.Defined(CornerRadius, fullFraction);
        }
    }
    private CornerRadius CurrentCornerRadius => _animatedCorners ?? _shapeMorph?.Current ?? SelectedShape.Resolve(_shapeSize);

    // Chip/Menu own their animation. These are evaluated DIP values, never legacy Full inputs.
    internal void SetAnimatedCorners(CornerRadius? corners, bool shapeStateChanged = false)
    {
        var changed = _animatedCorners != corners;
        _animatedCorners = corners;
        if (changed && Shape is CutCornerShape) UpdateShape();
        UpdateInteractionVisuals();
        // AnimatedShape's progress is observable even when its new coordinate system resolves
        // to the same outline. A consumer can preserve that draw invalidation at retargeting.
        if (shapeStateChanged) _focusRing?.InvalidateRing();
    }

    public override void Render(DrawingContext context)
    {
        DrawShadow(context);
        if (EffectiveGeometry is { } geometry) context.DrawGeometry(EffectiveBackground, null, geometry);
        else context.DrawRectangle(EffectiveBackground, null, new RoundedRect(new Rect(Bounds.Size), EffectiveCornerRadius));
        base.Render(context);
    }

    private void DrawShadow(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        if (ShadowElevation <= 0 || bounds.Width <= 0 || bounds.Height <= 0 || TopLevel.GetTopLevel(this) is null)
            return;
        if (Shape is not null && !UsesRoundOutline && _shapeShadow is null) return;

        ComposeShadowRenderer.Draw(
            context,
            this,
            _shapeShadow ?? _containerShadowShape ?? ComposeShadowShape.RoundedRectangle(bounds, EffectiveCornerRadius),
            ComposeShadowParameters.Material(ShadowElevation, ResolveShadowColor()));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _chrome.Measure(availableSize);
        var desired = _chrome.DesiredSize;
        _interactionOverlay.Measure(desired);
        _outline.Measure(desired);
        _focusRing?.Measure(desired);
        return desired;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        _shapeSize = finalSize;
        if (_shapeMorph is not null)
            _shapeMorph.Size = finalSize;
        UpdateShape();
        if (UpdateCornerRadius())
            _interactionOverlay.InvalidateVisual();
        var rect = new Rect(finalSize);
        _chrome.Arrange(rect);
        _interactionOverlay.Arrange(rect);
        _outline.Arrange(rect);
        _checkedOverlay?.Arrange(rect);
        if (_focusRing is { } focusRing)
        {
            focusRing.Corners = FocusRingCornerRadius ?? EffectiveCornerRadius;
            focusRing.Arrange(rect);
        }

        return finalSize;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ShapeProperty || change.Property == PressedShapeProperty || change.Property == CheckedShapeProperty)
        {
            ObserveShape();
            QueueShapeMorph();
            UpdateShape();
        }
        else if ((change.Property == FlowDirectionProperty || change.Property == ShapeLayoutDirectionProperty) && _chrome is not null) UpdateShape();

        if (change.Property == ApplyTonalElevationProperty || change.Property == BackgroundProperty || change.Property == TonalSurfaceColorProperty
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
            if (Shape is not null && !BorderThickness.IsUniform)
                throw new InvalidOperationException("MaterialShape borders require uniform BorderThickness.");
            UpdateButtonBorder();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ObserveShape();
        UpdateShape();

        // The theme supplies the current tonal palette through effective properties.
        UpdateEffectiveBackground();

        if (TemplatedParent is Control host && MaterialInteraction.GetIsInteractive(host)
            && !IsInTemplatePopup(host))
            ConnectInteraction(host);
    }

    // Explicit attachment for a component with indication but no native input modifier
    // (Switch onCheckedChange=null). Input eligibility stays on the owning controller.
    internal void ConnectInteraction(Control host)
    {
        if (_interactionAttachment is not null) return;
        _interactionController = MaterialInteraction.GetOrCreate(host);
        host.PropertyChanged += FocusConfigurationChanged;
        UpdateFocusRing(host);
        _interactionAttachment = _interactionController.Attach(this);
    }

    // A surface inside a popup of its templated parent (a MenuItem's submenu container) is not that
    // parent's interaction front-end. Attaching it replaced the item's own surface as the
    // controller's presenter, so the item stopped drawing its hover layer once the submenu opened.
    private bool IsInTemplatePopup(Control host)
    {
        for (var node = ((ILogical)this).LogicalParent; node is not null && !ReferenceEquals(node, host); node = node.LogicalParent)
            if (node is Avalonia.Controls.Primitives.Popup)
                return true;
        return false;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observedShape is { } shape) shape.PropertyChanged -= ShapeChanged;
        _observedShape = null;
        if (_observedPressedShape is { } pressed) pressed.PropertyChanged -= ShapeChanged;
        _observedPressedShape = null;
        if (_observedCheckedShape is { } selected) selected.PropertyChanged -= ShapeChanged;
        _observedCheckedShape = null;
        if (TemplatedParent is Control host) host.PropertyChanged -= FocusConfigurationChanged;
        _focusRing?.Disconnect();
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
            _checkedOverlay = new CheckedOverlay
            {
                IsHitTestVisible = false,
                Shape = EffectiveGeometry,
                [!FlowDirectionProperty] = this[!FlowDirectionProperty],
            };
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
        if (ButtonSurfaceState.GetAnimateBorder(this) && _interactionController?.Motion is { } motion
            && !motion.Lifetime.IsCancellationRequested)
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
        _shapeConfigurationInitialized = false;
        _animatedBorder?.Dispose();
        _animatedBorder = null;
        UpdateCornerRadius();
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
        if (_observedShape is not null && (PressedShape is not null || CheckedShape is not null || _shapeMorph is not null)) UpdateShape();
        UpdateCornerRadius();
        _interactionOverlay.InvalidateVisual();
        UpdateFocusRingGeometry();
    }

    private void UpdateFocusRingGeometry()
    {
        if (_focusRing is not { } ring) return;
        ring.ShapePath = GenericShapePath;
        ring.Corners = FocusRingCornerRadius ?? EffectiveCornerRadius;
        // Surface's separate indication precedes its clip; ListItem's single indication
        // follows clip(shape). Preserve the modifier order for the actual consumer.
        ring.Clip = _interactionController?.HasSeparateFocusIndication == false
            ? EffectiveGeometry ?? RoundedRectangleGeometry.Create(new Rect(_shapeSize), EffectiveCornerRadius) : null;
    }

    private void FocusConfigurationChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialInteraction.FocusFeedbackProperty && sender is Control host)
            UpdateFocusRing(host);
    }

    private void UpdateFocusRing(Control host)
    {
        if (!PresentsFocusRing || MaterialInteraction.GetFocusFeedback(host) != FocusFeedback.InsetRing
            && _interactionController?.HasSeparateFocusIndication != false) return;
        if (_focusRing is null)
        {
            _focusRing = new InsetFocusRingPresenter { ZIndex = 2, Corners = FocusRingCornerRadius ?? EffectiveCornerRadius, ShapePath = GenericShapePath };
            _focusRing[!FlowDirectionProperty] = this[!FlowDirectionProperty];
            VisualChildren.Add(_focusRing);
            InvalidateMeasure();
        }
        _focusRing.Connect(host);
        UpdateFocusRingGeometry();
    }

    private bool UpdateCornerRadius()
    {
        var cornerRadius = Shape is null || UsesRoundOutline ? CornerShape.Normalize(CurrentCornerRadius, _shapeSize) : default;
        if (UsesRoundOutline && ShapeLayoutDirection is { } direction && direction != FlowDirection)
            cornerRadius = new(cornerRadius.TopRight, cornerRadius.TopLeft, cornerRadius.BottomLeft, cornerRadius.BottomRight);
        // Optical centering consumes unnormalized corners: they can change even while the
        // painted outline remains at its size limit.
        if (Child is SplitButtonOpticalLayout optical) optical.InvalidateArrange();
        if (_chrome.CornerRadius == cornerRadius)
            return false;

        _chrome.CornerRadius = cornerRadius;
        UpdateOutlineCornerRadius();
        if (_checkedOverlay is { } overlay) overlay.CornerRadius = cornerRadius;
        InvalidateVisual();
        return true;
    }

    private void QueueShapeMorph()
    {
        if (_interactionController?.Motion is not { } motion || motion.Lifetime.IsCancellationRequested)
            return;

        // rememberAnimatedShape creates its initial state during composition. Subsequent
        // interaction/configuration changes are committed by recomposition before its effect.
        UpdateButtonBorder();
        // Required theme input can temporarily disappear during binding or reparenting.
        // Do not create a zero-stiffness spring or replace an effect's captured valid spec.
        if (!TryGetShapeMorphSpec(out var initialSpec))
            return;
        if (_shapeMorph is null && (CanMorphCorners || Shape is null
            && (PressedCornerRadius is not null || ButtonSurfaceState.GetCheckedCornerRadius(this) is not null)))
            RecreateShapeMorph(motion, initialSpec);
        if (_shapeCommitPending)
            return;

        _shapeCommitPending = true;
        var generation = _shapeGeneration;
        _ = motion.Launch(async ct =>
        {
            if (!await motion.WaitForNextFrameAsync(ct)) return;
            if (generation != _shapeGeneration)
                return;
            _shapeCommitPending = false;
            if (_shapeInteractionDeferred)
                return;

            // Recheck after the composition frame: a resource may have been withdrawn while
            // this commit waited. The next valid property update queues the current target again.
            if (!TryGetShapeMorphSpec(out var spec))
                return;
            var target = SelectedShape;
            if (Shape is not null && !CanMorphCorners && !ReferenceEquals(_indicationPath, SelectedPath))
            {
                // Core Surface passes the selected Shape into ripple(focusRingShape). A
                // direct path switch replaces that indication node; active presses are not replayed.
                var replaced = _indicationPath is not null;
                _indicationPath = SelectedPath;
                if (replaced && ResetIndicationOnShapeChange) _interactionController?.ResetSourceIndication(preserveSurfaceMotion: true);
            }
            var configurationChanged = _shapeRest != CornerRadius || _shapePressed != PressedCornerRadius
                || _shapeChecked != ButtonSurfaceState.GetCheckedCornerRadius(this) || !_shapeSpec.Equals(spec)
                || _pathRest != Shape || _pathPressed != PressedShape || _pathChecked != CheckedShape
                || _cornerRest != CornerShape.FromMaterialShape(Shape)
                || _cornerPressed != CornerShape.FromMaterialShape(PressedShape)
                || _cornerChecked != CornerShape.FromMaterialShape(CheckedShape);
            var recreate = configurationChanged && (ButtonSurfaceState.GetResetOnShapeChange(this) || !_shapeSpec.Equals(spec)
                || _shapeUsesCorners != CanMorphCorners || (_pathRest is null) != (Shape is null));
            if (configurationChanged && !recreate)
            {
                _shapeRest = CornerRadius;
                _shapePressed = PressedCornerRadius;
                _shapeChecked = ButtonSurfaceState.GetCheckedCornerRadius(this);
                _pathRest = Shape; _pathPressed = PressedShape; _pathChecked = CheckedShape;
                _cornerRest = CornerShape.FromMaterialShape(Shape);
                _cornerPressed = CornerShape.FromMaterialShape(PressedShape);
                _cornerChecked = CornerShape.FromMaterialShape(CheckedShape);
            }
            // SplitButton remembers by spec and keys its effect by the selected shape.
            // Changing an inactive shape must not cancel an animation to the same target.
            if (!recreate && (_shapeMorph is null || _shapeMorph.TargetShape == target))
                return;

            // LaunchedEffect cancels and joins the previous effect before running its replacement.
            // This also lets Animatable finish cancellation before the morph reads its velocity.
            _shapeEffect?.Cancel();
            if (_shapeEffectTask is { } previous)
            {
                try { await previous; }
                catch (OperationCanceledException) when (previous.IsCanceled) { }
            }
            if (ct.IsCancellationRequested) return;
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

    private void RecreateShapeMorph(AnimationScope motion, SpringSpec spec)
    {
        // IconButton passes the shape instance to ripple(focusRingShape = shape). Replacing
        // AnimatedShape also replaces that indication node; ongoing interactions are not replayed.
        if (_shapeConfigurationInitialized && TemplatedParent is IconButton)
            _interactionController?.ResetSourceIndication(preserveSurfaceMotion: true);
        _shapeConfigurationInitialized = true;
        _shapeMorph?.Dispose();
        _shapeRest = CornerRadius;
        _shapePressed = PressedCornerRadius;
        _shapeChecked = ButtonSurfaceState.GetCheckedCornerRadius(this);
        _pathRest = Shape; _pathPressed = PressedShape; _pathChecked = CheckedShape;
        _shapeUsesCorners = CanMorphCorners;
        _indicationPath = SelectedPath;
        _cornerRest = CornerShape.FromMaterialShape(Shape);
        _cornerPressed = CornerShape.FromMaterialShape(PressedShape);
        _cornerChecked = CornerShape.FromMaterialShape(CheckedShape);
        _shapeSpec = spec;
        _shapeMorph = Shape is not null ? CanMorphCorners
            ? new AnimatedCornerRadius(SelectedShape, spec, motion, UpdateInteractionVisuals) { Size = Bounds.Size } : null
            : PressedCornerRadius is null && _shapeChecked is null ? null
            : new AnimatedCornerRadius(SelectedShape, spec, motion, UpdateInteractionVisuals) { Size = Bounds.Size };
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

    private void UpdateEffectiveBackground()
    {
        _effectiveBackground = ResolveEffectiveBackground();
        InvalidateVisual();
    }

    // Upstream gate (applyTonalElevation): the tint applies only while the background is exactly
    // the scheme surface color — a surfaceContainer* or custom background is painted verbatim at
    // any elevation. The tinted color is precomputed here, on change, so the render pass stays
    // the same solid fill as an untinted surface.
    private IBrush? ResolveEffectiveBackground()
    {
        if (!ApplyTonalElevation || Background is not ISolidColorBrush solid || _absoluteTonalElevation <= 0)
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
    // Split's checked drawOutline follows the actual animated path, above its content.
    private sealed class CheckedOverlay : Control
    {
        public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<CheckedOverlay>();
        public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<CheckedOverlay>();
        public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
        public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
        static CheckedOverlay() => AffectsRender<CheckedOverlay>(BackgroundProperty, CornerRadiusProperty);
        internal Geometry? Shape { get; set; }
        public override void Render(DrawingContext context)
        {
            if (Shape is { } shape) context.DrawGeometry(Background, null, shape);
            else context.DrawRectangle(Background, null, new RoundedRect(new Rect(Bounds.Size), CornerRadius));
        }
    }

    private sealed class InteractionOverlay : Control
    {
        private readonly MaterialSurface _owner;

        public InteractionOverlay(MaterialSurface owner)
        {
            _owner = owner;
            this[!FlowDirectionProperty] = owner[!FlowDirectionProperty];
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
                Bounds.Size,
                _owner._shapeGeometry);
        }
    }
}

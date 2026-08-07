// Upstream behavior reference: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// ListItem.kt:262-299 and 1450-1603, plus material3-ripple/Ripple.kt:371-388. Its non-interactive ListItem passes onClick = null,
// creates no MutableInteractionSource, and installs no combinedClickable. This shared Avalonia
// controller therefore has an explicit input gate so MaterialListItem.InteractionMode=None can remain
// enabled static content without hover, press, focus, or ripple feedback. The alpha22 executable
// reference cannot execute that alpha25 overload; deterministic Avalonia behavior tests cover it.
// Both alpha25 Ripple.kt:377-388 and alpha22 internal/ripple/Ripple.kt:325-335 draw
// color.copy(alpha = animatedAlpha): the state alpha replaces, rather than multiplies, an ambient
// disabled content alpha. The controller consequently strips a solid brush's opacity before applying
// its own quantized Compose opacity.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>
/// Everything Material specifies about interaction feedback, hosted on any control.
/// </summary>
/// <remarks>
/// Avalonia's pseudo-classes can say which state a control is in but not how it got there, and
/// Material cares about the transition: the fades are asymmetric, press feedback starts where the
/// pointer went down, and the shape morph has to survive being interrupted. This owns all of that,
/// so a component only has to say what colour and shape it is.
/// <para>
/// Layers draw in the order used by the current Material 3 ripple node — content, press ripples,
/// non-press state layer, then the optional focus ring. The controller paints the interaction layers
/// after the owner has painted its container and content.
/// </para>
/// </remarks>
internal sealed class MaterialInteractionController
{
    private readonly Control _owner;
    private readonly MaterialElementContext _context;
    private readonly List<RippleAnimation> _ripples = new();
    private readonly List<InteractionState> _stateLayerInteractions = new();
    private readonly List<IMaterialInteractionObserver> _observers = new();
    private readonly SolidColorBrush _alphaReplacedBrush = new();

    private TweenAnimation? _stateLayerAlpha;
    private SpringAnimation? _focusRingInterpolation;
    private MaterialInteractionPresentationLease? _presentation;
    private MaterialInteractionGeometry _geometry;
    private bool _hasExplicitGeometry;
    private InteractionState _interaction;
    private InteractionState _shownInteraction;
    private bool _isRippleEnabled = true;
    private bool _isFocusIndicationEnabled = true;
    private bool _isHoverIndicationEnabled = true;
    private bool _isDragIndicationEnabled = true;

    internal MaterialInteractionController(
        MaterialElementContext context,
        MaterialMotionCoordinator coordinator)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _owner = context.Owner;
        _geometry = DefaultGeometry;
        Motion = coordinator.CreateScope("interaction");
        Motion.Ticked += (_, _) => OnTick();
        _owner.PointerEntered += (_, _) =>
        {
            if (IsInputEnabled)
                Set(InteractionState.Hovered, true);
        };
        _owner.PointerExited += (_, _) =>
        {
            if (IsInputEnabled)
                Set(InteractionState.Hovered, false);
        };
        // Button-derived controls handle pointer presses in their class handler. Listen on the
        // tunnel route so Material feedback still sees the physical input before it is consumed.
        _owner.AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.AddHandler(
            InputElement.PointerReleasedEvent,
            OnPointerReleased,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.AddHandler(
            InputElement.KeyDownEvent,
            OnKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.AddHandler(
            InputElement.KeyUpEvent,
            OnKeyUp,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        _owner.PointerCaptureLost += (_, _) => EndPress();
        _owner.GotFocus += OnGotFocus;
        _owner.LostFocus += (_, _) =>
        {
            EndPress();
            Set(InteractionState.Focused, false);
        };

    }

    /// <summary>This control's animations and the frame loop that drives them.</summary>
    internal MaterialMotionScope Motion { get; }

    /// <summary>Which interactions the owner is currently under.</summary>
    internal InteractionState Interaction => _interaction;

    internal MaterialInteractionDebugSnapshot DebugSnapshot => new(
        _interaction,
        _ripples.Count,
        _ripples.Count == 0 ? null : _ripples[^1].Center);

    internal MaterialInteractionGeometry Geometry =>
        _hasExplicitGeometry ? _geometry : DefaultGeometry;

    /// <summary>The state layer's current opacity, mid-fade included.</summary>
    internal double StateLayerOpacity => _stateLayerAlpha?.Value ?? 0;

    /// <summary>Whether a press raises a ripple.</summary>
    internal bool IsRippleEnabled
    {
        get => _isRippleEnabled;
        set
        {
            if (_isRippleEnabled == value)
                return;

            _isRippleEnabled = value;
            InvalidateIndicationPresentation();
        }
    }

    /// <summary>Whether focus produces its configured opacity layer or inset ring.</summary>
    internal bool IsFocusIndicationEnabled
    {
        get => _isFocusIndicationEnabled;
        set
        {
            if (_isFocusIndicationEnabled == value)
                return;

            _isFocusIndicationEnabled = value;
            InvalidateIndicationPresentation();
        }
    }

    /// <summary>Whether hover produces a state layer.</summary>
    internal bool IsHoverIndicationEnabled
    {
        get => _isHoverIndicationEnabled;
        set
        {
            if (_isHoverIndicationEnabled == value)
                return;

            _isHoverIndicationEnabled = value;
            InvalidateIndicationPresentation();
        }
    }

    /// <summary>Whether drag produces a state layer.</summary>
    internal bool IsDragIndicationEnabled
    {
        get => _isDragIndicationEnabled;
        set
        {
            if (_isDragIndicationEnabled == value)
                return;

            _isDragIndicationEnabled = value;
            InvalidateIndicationPresentation();
        }
    }

    /// <summary>
    /// Gets or sets whether this controller translates physical pointer and keyboard focus input into
    /// Material interaction feedback.
    /// </summary>
    /// <remarks>
    /// This is deliberately independent from the owner's <see cref="InputElement.IsEnabled" />.
    /// A control can remain enabled as content while choosing a non-interactive Material contract
    /// (for example, a static ListItem). Turning this off clears only input-derived state; a
    /// control-owned drag interaction can still be reported through <see cref="Set" />.
    /// </remarks>
    internal bool IsInputEnabled => _isInputEnabled;

    internal void SetInputEnabled(bool enabled)
    {
        if (_isInputEnabled == enabled)
            return;

        _isInputEnabled = enabled;
        if (!enabled)
            ClearInputInteractions();
    }

    internal void ClearInputInteractionsOnDetach() => ClearInputInteractions();

    internal void SetExternalState(InteractionState state, bool active) => Set(state, active);

    internal IDisposable Subscribe(IMaterialInteractionObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
        return new MaterialInteractionObserverLease(this, observer);
    }

    internal void RemoveObserver(IMaterialInteractionObserver observer)
    {
        for (var index = _observers.Count - 1; index >= 0; index--)
        {
            if (ReferenceEquals(_observers[index], observer))
            {
                _observers.RemoveAt(index);
                return;
            }
        }
    }

    internal void OnMaterialThemeChanged(MaterialThemeState state)
    {
        UpdateStateLayer();
        UpdateFocusRing(_interaction.HasFlag(InteractionState.Focused));
        Motion.Start();
        _owner.InvalidateVisual();
    }

    internal void UpdateIndicationPolicy(MaterialInteractionPolicy policy)
    {
        var changed = _isRippleEnabled != policy.IsRippleEnabled
            || _isFocusIndicationEnabled != policy.IsFocusIndicationEnabled
            || _isHoverIndicationEnabled != policy.IsHoverIndicationEnabled
            || _isDragIndicationEnabled != policy.IsDragIndicationEnabled;

        _isRippleEnabled = policy.IsRippleEnabled;
        _isFocusIndicationEnabled = policy.IsFocusIndicationEnabled;
        _isHoverIndicationEnabled = policy.IsHoverIndicationEnabled;
        _isDragIndicationEnabled = policy.IsDragIndicationEnabled;

        if (changed)
            InvalidateIndicationPresentation();
    }

    internal MaterialInteractionPresentationLease AttachPresentation(Control presenter)
    {
        _presentation?.Dispose();
        var lease = new MaterialInteractionPresentationLease(this, presenter);
        _presentation = lease;
        return lease;
    }

    internal MaterialInteractionGeometry DefaultGeometry =>
        new(new Rect(_owner.Bounds.Size), null, null);

    internal void SetPresentationGeometry(
        MaterialInteractionPresentationLease lease,
        in MaterialInteractionGeometry geometry)
    {
        if (!ReferenceEquals(_presentation, lease))
            return;

        _geometry = geometry;
        _hasExplicitGeometry = true;
    }

    internal void ClearPresentation(MaterialInteractionPresentationLease lease)
    {
        if (!ReferenceEquals(_presentation, lease))
            return;

        _presentation = null;
        _hasExplicitGeometry = false;
    }

    /// <summary>Whether the ripple is clipped to the owner's bounds.</summary>
    internal bool IsRippleBounded { get; set; } = true;

    /// <summary>
    /// An explicit ripple radius in device-independent pixels, or null to derive it from the
    /// interaction area's size.
    /// </summary>
    internal double? RippleRadius { get; set; }

    private Rect Area => Geometry.Area;

    private bool _isInputEnabled = true;

    private TweenAnimation StateLayerAlpha =>
        _stateLayerAlpha ??= Motion.AddTween(0, StateLayer.FadeInDuration(InteractionState.Hovered));

    private SpringAnimation FocusRingInterpolation =>
        _focusRingInterpolation ??= Motion.AddSpring(
            0,
            _context.Spring(MotionRole.FastSpatial));

    /// <summary>Creates a shape that morphs with a spring, on this controller's frame loop.</summary>
    internal AnimatedShape CreateAnimatedShape(RoundedCornerShape initial, SpringSpec spec)
    {
        var shape = new AnimatedShape(initial, spec);
        Motion.Add(shape.Progress);
        return shape;
    }

    /// <summary>
    /// Draws the state layer and any live ripples over the container.
    /// </summary>
    /// <param name="context">The owner's drawing context.</param>
    /// <param name="brush">The state layer colour — the content colour, in Material.</param>
    /// <param name="cornerRadius">The container's corners, so the layer follows any morph.</param>
    internal void DrawLayers(DrawingContext context, IBrush? brush, CornerRadius cornerRadius)
    {
        if (brush is null)
            return;

        var area = Area;
        var opacity = StateLayerOpacity;
        var composeColorBrush = WithComposeReplacedAlpha(brush);

        if (opacity <= 0 && _ripples.Count == 0 && (_focusRingInterpolation?.Value ?? 0) <= 0)
            return;

        // New material3-ripple draws press ripples before non-press state layers. A bounded node
        // clips both to its target layout; an unbounded node deliberately clips neither.
        if (_ripples.Count > 0)
        {
            using var origin = context.PushTransform(Matrix.CreateTranslation(area.X, area.Y));
            if (IsRippleBounded)
            {
                using var rippleClip = context.PushClip(new RoundedRect(new Rect(area.Size), cornerRadius));
                DrawRipples(context, composeColorBrush);
            }
            else
            {
                DrawRipples(context, composeColorBrush);
            }
        }

        if (opacity > 0)
        {
            if (IsRippleBounded)
            {
                using var stateClip = context.PushClip(new RoundedRect(area, cornerRadius));
                DrawStateLayer(context, composeColorBrush, area, opacity);
            }
            else
            {
                var radius = RippleRadius ?? RippleAnimationEndRadius(area.Size, bounded: false);
                using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(opacity)))
                    context.DrawEllipse(composeColorBrush, null, area.Center, radius, radius);
            }
        }

        var geometry = Geometry;
        DrawFocusRing(context, geometry.FocusRingArea ?? area, geometry.FocusRingCornerRadius ?? cornerRadius);
    }

    /// <summary>Sets or clears one interaction flag.</summary>
    internal void Set(InteractionState flag, bool active)
    {
        var updated = active ? _interaction | flag : _interaction & ~flag;
        if (updated == _interaction)
            return;

        _interaction = updated;
        UpdateInteractionStack(flag, active);
        if (flag == InteractionState.Focused)
            UpdateFocusRing(active);
        UpdateStateLayer();
        NotifyInteractionChanged();
        Motion.Start();
        _owner.InvalidateVisual();
    }

    /// <summary>Begins the pressed state, raising a ripple at <paramref name="origin" />.</summary>
    internal void StartPress(Point origin)
    {
        if (_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, true);

        var area = Area;
        if (!IsRippleEnabled || area.Width <= 0 || area.Height <= 0)
            return;

        // The press point is in control coordinates; the ripple works in area-local ones.
        // CommonRipple finishes every older animation before adding a new press. This matters for
        // rapid taps: the old wave is allowed to complete while the new one starts independently.
        foreach (var existing in _ripples)
            existing.Finish();

        var ripple = new RippleAnimation(
            origin - area.Position,
            area.Size,
            IsRippleBounded,
            RippleRadius);
        _ripples.Add(ripple);
        Motion.Add(ripple);
        Motion.Start();
    }

    /// <summary>Ends the pressed state and lets any live ripple fade out.</summary>
    internal void EndPress()
    {
        if (!_interaction.HasFlag(InteractionState.Pressed))
            return;

        Set(InteractionState.Pressed, false);

        foreach (var ripple in _ripples)
            ripple.Release();

        Motion.Start();
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsInputEnabled)
            return;

        if (e.GetCurrentPoint(_owner).Properties.IsLeftButtonPressed)
            StartPress(e.GetPosition(_owner));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!IsInputEnabled)
            return;

        if (e.InitialPressMouseButton == MouseButton.Left)
            EndPress();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // Compose clickable emits a centred press for keyboard activation. Limit this bridge to
        // Button-derived owners so a space typed into SearchBar's editor cannot start a ripple on
        // its surrounding container.
        if (!IsInputEnabled
            || _owner is not Button
            || e.Key is not (Key.Space or Key.Enter))
            return;

        StartPress(Area.Center);
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (_owner is Button && e.Key is (Key.Space or Key.Enter))
            EndPress();
    }

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (!IsInputEnabled)
            return;

        // Material shows the focus layer for keyboard focus only; a click that focuses a control
        // already has a ripple to say so.
        Set(
            InteractionState.Focused,
            e.NavigationMethod is NavigationMethod.Directional or NavigationMethod.Tab);
    }

    private void ClearInputInteractions()
    {
        var hadInputInteraction = _interaction.HasFlag(InteractionState.Hovered)
            || _interaction.HasFlag(InteractionState.Focused)
            || _interaction.HasFlag(InteractionState.Pressed);
        var hadRipples = _ripples.Count != 0;

        _interaction &= ~(InteractionState.Hovered | InteractionState.Focused | InteractionState.Pressed);
        _stateLayerInteractions.RemoveAll(
            static state => state is InteractionState.Hovered or InteractionState.Focused);
        UpdateFocusRing(focused: false);

        foreach (var ripple in _ripples)
            Motion.Remove(ripple);

        _ripples.Clear();

        // A released ripple can outlive Pressed while its fade completes. Clearing that transient
        // animation is still a visual change even though the interaction flags are already clear;
        // otherwise a detach/input-gate transition can leave the owner showing one stale frame.
        if (!hadInputInteraction && !hadRipples)
            return;

        if (hadInputInteraction)
        {
            UpdateStateLayer();
            NotifyInteractionChanged();
        }

        Motion.Start();
        _owner.InvalidateVisual();
    }

    private void OnTick()
    {
        // Completed ripples would otherwise accumulate for the control's lifetime.
        for (var i = _ripples.Count - 1; i >= 0; i--)
        {
            if (!_ripples[i].IsComplete)
                continue;

            Motion.Remove(_ripples[i]);
            _ripples.RemoveAt(i);
        }

        // The context publishes secondary observers after the owner consumer has synchronized.
        // A direct scope-level settle has no surrounding coordinator transaction, so preserve a
        // useful standalone behavior for that call without duplicating normal frame callbacks.
        if (!Motion.Coordinator.IsPublishingFrameCallbacks)
            PublishInteractionFrame();
    }

    internal void PublishInteractionFrame()
    {
        var observers = _observers.ToArray();
        for (var index = 0; index < observers.Length; index++)
            observers[index].OnMaterialInteractionFrame();
    }

    private void NotifyInteractionChanged()
    {
        if (_owner is IMaterialInteractionConsumer consumer)
            consumer.OnMaterialInteractionChanged(_interaction);

        var observers = _observers.ToArray();
        for (var index = 0; index < observers.Length; index++)
            observers[index].OnMaterialInteractionChanged(_interaction);
    }

    private void UpdateStateLayer()
    {
        // AndroidX stores entry interactions in arrival order and shows lastOrNull(). A fixed
        // focus-over-hover priority is observably wrong when a pointer enters an already focused
        // control.
        var topmost = _stateLayerInteractions.Count == 0
            ? InteractionState.None
            : _stateLayerInteractions[^1];

        var targetOpacity = IsIndicationEnabled(topmost)
            && !(topmost == InteractionState.Focused
                                 && _context.Theme.Ripple.FocusIndication == RippleFocusIndication.InsetRing)
            ? StateLayer.Opacity(topmost)
            : 0;

        if (topmost == _shownInteraction
            && (_stateLayerAlpha is null || _stateLayerAlpha.Target == targetOpacity))
            return;

        // An idle controller should not allocate animation tracks merely because its owner was
        // themed. The first visible interaction creates the state-layer track on demand.
        if (targetOpacity == 0 && _stateLayerAlpha is null)
            return;

        var duration = topmost == InteractionState.None
            ? StateLayer.FadeOutDuration(_shownInteraction)
            : StateLayer.FadeInDuration(topmost);

        _shownInteraction = topmost;
        StateLayerAlpha.AnimateTo(targetOpacity, duration, Easings.Linear);
    }

    private void UpdateFocusRing(bool focused)
    {
        if (!IsFocusIndicationEnabled
            || _context.Theme.Ripple.FocusIndication != RippleFocusIndication.InsetRing)
            focused = false;

        if (!focused && _focusRingInterpolation is null)
            return;

        FocusRingInterpolation.AnimateTo(
            focused ? 1 : 0,
            _context.Spring(focused ? MotionRole.FastSpatial : MotionRole.FastEffects));
        Motion.Start();
    }

    private void UpdateInteractionStack(InteractionState flag, bool active)
    {
        if (flag is not (InteractionState.Hovered or InteractionState.Focused or InteractionState.Dragged))
            return;

        _stateLayerInteractions.Remove(flag);
        if (active)
            _stateLayerInteractions.Add(flag);
    }

    private bool IsIndicationEnabled(InteractionState interaction)
    {
        return interaction switch
        {
            InteractionState.Focused => IsFocusIndicationEnabled,
            InteractionState.Hovered => IsHoverIndicationEnabled,
            InteractionState.Dragged => IsDragIndicationEnabled,
            _ => true
        };
    }

    private void InvalidateIndicationPresentation()
    {
        UpdateStateLayer();
        UpdateFocusRing(_interaction.HasFlag(InteractionState.Focused));
        Motion.Start();
        _owner.InvalidateVisual();
    }

    private void DrawRipples(DrawingContext context, IBrush brush)
    {
        foreach (var ripple in _ripples)
            ripple.Draw(context, brush, StateLayer.Opacity(InteractionState.Pressed));
    }

    private static void DrawStateLayer(DrawingContext context, IBrush brush, Rect area, double opacity)
    {
        // Compose's RippleNode draws `color.copy(alpha = animatedAlpha)` before Skia compositing.
        // Quantize to the same nearest 8-bit alpha before entering Avalonia's opacity stack.
        using (context.PushOpacity(MaterialInteractionRendering.ComposeOpacity(opacity)))
            // The rounded clip already antialiases the edge. Filling an ordinary rectangle avoids
            // antialiasing that edge a second time and producing a faint corner ring.
            context.DrawRectangle(brush, null, area);
    }

    private void DrawFocusRing(DrawingContext context, Rect area, CornerRadius cornerRadius)
    {
        var interpolation = _focusRingInterpolation?.Value ?? 0;
        if (interpolation <= 0
            || _context.Theme.Ripple.FocusIndication != RippleFocusIndication.InsetRing
            || area.Width <= 0
            || area.Height <= 0)
            return;

        var ripple = _context.Theme.Ripple;
        var scheme = _context.Theme.ColorScheme;
        var innerColor = scheme?.GetColor(SysColorToken.OnSecondary, _context.IsDark) ?? Colors.White;
        var outerColor = scheme?.GetColor(SysColorToken.Secondary, _context.IsDark) ?? Colors.Black;

        // AndroidX draws the inner stroke first and the outer stroke second. Width and inset are
        // both multiplied by the spring interpolation, so the ring grows out of the shape edge.
        DrawInsetStroke(
            context,
            area,
            cornerRadius,
            ripple.InnerStrokeInset * interpolation,
            ripple.InnerStrokeWidth * interpolation,
            innerColor);
        DrawInsetStroke(
            context,
            area,
            cornerRadius,
            ripple.OuterStrokeInset * interpolation,
            ripple.OuterStrokeWidth * interpolation,
            outerColor);
    }

    private static void DrawInsetStroke(
        DrawingContext context,
        Rect area,
        CornerRadius cornerRadius,
        double inset,
        double width,
        Color color)
    {
        if (width <= 0)
            return;

        // Avalonia centres a pen on its geometry. Deflating by inset plus half the width aligns
        // the stroke's outer edge with AndroidX BorderLogic's requested inset.
        var geometryInset = Math.Max(0, inset + width / 2);
        var rect = area.Deflate(geometryInset);
        if (rect.Width <= 0 || rect.Height <= 0)
            return;

        context.DrawRectangle(
            null,
            new ImmutablePen(new ImmutableSolidColorBrush(color), width),
            new RoundedRect(rect, MaterialContainer.Deflate(cornerRadius, geometryInset)));
    }

    private static double RippleAnimationEndRadius(Size bounds, bool bounded)
    {
        var covering = Math.Sqrt(bounds.Width * bounds.Width + bounds.Height * bounds.Height) / 2;
        return bounded ? covering + 10 : covering;
    }

    private IBrush WithComposeReplacedAlpha(IBrush source)
    {
        // Compose's `Color.copy(alpha = ...)` replaces the Color alpha. A disabled ListItem
        // provides LocalContentColor with 38% opacity, but an externally supplied drag state
        // layer must still use that colour's RGB at the full 16% dragged alpha. Passing the
        // source brush through PushOpacity would incorrectly multiply the two alphas.
        if (source is not ISolidColorBrush solid)
            return source;

        var color = solid.Color;
        _alphaReplacedBrush.Color = Color.FromArgb(byte.MaxValue, color.R, color.G, color.B);
        return _alphaReplacedBrush;
    }

}

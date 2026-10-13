/*
 * Portions translated from AndroidX Switch.kt, Copyright 2022 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed baseline: docs/upstream-baseline.json; ThumbNode.
 */
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

// Also PART_SwitchKnob: native ToggleSwitch owns Canvas.Left and drag activation.
internal sealed class SwitchThumbLayout : Panel
{
    public static readonly StyledProperty<Foundation.Motion.SpringSpec?> ThumbAnimationProperty =
        AvaloniaProperty.Register<SwitchThumbLayout, Foundation.Motion.SpringSpec?>(nameof(ThumbAnimation));
    public Foundation.Motion.SpringSpec? ThumbAnimation { get => GetValue(ThumbAnimationProperty); set => SetValue(ThumbAnimationProperty, value); }


    private ToggleSwitch? _owner;
    private MaterialInteractionController? _interaction;
    private Animatable<float>? _sizeAnim, _offsetAnim;
    private float _initialSize = float.NaN, _initialOffset = float.NaN;
    private float _sizePx, _offsetPx;
    private bool _updatePending;
    private bool _checked;
    private bool _nativeDragging, _dragging, _dragHandoff;
    private float _dragSizePx, _dragOffsetPx;
    internal double PaintedSize { get; private set; }
    internal double PaintedOffset { get; private set; }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = (ToggleSwitch)TemplatedParent!;
        _checked = _owner.IsChecked == true;
        _interaction = MaterialInteraction.GetOrCreate(_owner);
        _interaction.IsRippleBounded = false;
        _interaction.RippleRadius = 20;
        _interaction.RippleFollowsPresenterCenter = true;
        // Observe the existing controller after its tunnel handlers, without another input
        // state machine or an activation path.
        _owner.AddHandler(InputElement.PointerPressedEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.PointerReleasedEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.PointerMovedEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.KeyDownEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.KeyUpEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.PointerCaptureLost += CaptureLost;
        _owner.LostFocus += InputChanged;
        _owner.PropertyChanged += OwnerChanged;
        _owner.Classes.CollectionChanged += ClassesChanged;
        if (Children.FirstOrDefault() is { } moving) moving.PropertyChanged += MovingChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is { } owner)
        {
            owner.RemoveHandler(InputElement.PointerPressedEvent, InputChanged);
            owner.RemoveHandler(InputElement.PointerReleasedEvent, InputChanged);
            owner.RemoveHandler(InputElement.PointerMovedEvent, InputChanged);
            owner.RemoveHandler(InputElement.KeyDownEvent, InputChanged);
            owner.RemoveHandler(InputElement.KeyUpEvent, InputChanged);
            owner.PointerCaptureLost -= CaptureLost;
            owner.LostFocus -= InputChanged;
            owner.PropertyChanged -= OwnerChanged;
            owner.Classes.CollectionChanged -= ClassesChanged;
        }
        if (Children.FirstOrDefault() is { } moving) moving.PropertyChanged -= MovingChanged;
        _sizeAnim = _offsetAnim = null;
        _initialSize = _initialOffset = float.NaN;
        _updatePending = false;
        _nativeDragging = _dragging = _dragHandoff = false;
        _owner = null; _interaction = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThumbAnimationProperty) InvalidateMeasure();
    }

    private void InputChanged(object? sender, RoutedEventArgs e) => InvalidateMeasure();
    private void ClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var dragging = _owner?.Classes.Contains(":dragging") == true;
        if (dragging != _nativeDragging)
        {
            _nativeDragging = dragging;
            if (dragging) ReadDragPosition();
            else EndDrag();
        }
        InvalidateMeasure();
    }
    private void MovingChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Canvas.LeftProperty) return;
        if (_nativeDragging && _interaction?.Interaction.HasFlag(InteractionState.Pressed) == true)
            ReadDragPosition();
        InvalidateMeasure();
    }
    private void ReadDragPosition()
    {
        var left = Canvas.GetLeft(Children[0]);
        // ToggleSwitch clears Canvas.Left before removing :dragging and writing IsChecked.
        // Keep the last finite position until that native release sequence has finished.
        if (!double.IsFinite(left)) return;
        if (!_dragging) _offsetAnim?.Stop();
        _dragging = true;
        _dragOffsetPx = (float)((2 + left) * LayoutHelper.GetLayoutScale(this));
    }
    private void CaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        EndDrag();
        InvalidateMeasure();
    }
    private void EndDrag()
    {
        if (!_dragging) return;
        _dragging = false;
        _dragHandoff = true;
        _dragSizePx = (float)(PaintedSize * LayoutHelper.GetLayoutScale(this));
        ScheduleThumbUpdate();
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty)
        {
            if (_owner?.IsEffectivelyEnabled == false) EndDrag();
            InvalidateMeasure();
        }
        if (e.Property == SwitchAppearance.ThumbIconProperty || e.Property == SwitchAppearance.ThumbIconTemplateProperty)
            InvalidateMeasure();
        if (e.Property != Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty) return;
        ScheduleThumbUpdate();
    }
    private void ScheduleThumbUpdate()
    {
        if (!this.IsAttachedToVisualTree() || _updatePending) return;
        var motion = AnimationScope.Get(this);
        _updatePending = true;
        // ThumbElement.update runs during recomposition. Ordinary state changes initialize
        // from the first measurement; native drag release instead hands off the current pose.
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            _updatePending = false;
            _checked = _owner?.IsChecked == true;
            if (!float.IsNaN(_initialSize) && _sizeAnim is null)
            {
                _sizeAnim = new Animatable<float>(new FloatConverter(), _dragHandoff ? _dragSizePx : _initialSize, VisibilityThresholds.Float);
                _offsetAnim = new Animatable<float>(new FloatConverter(), _dragHandoff ? _dragOffsetPx : _initialOffset, VisibilityThresholds.Float);
                motion.SubscribeValue(_sizeAnim, value => { _sizePx = value; InvalidateMeasure(); });
                motion.SubscribeValue(_offsetAnim, value => { _offsetPx = value; InvalidateMeasure(); });
            }
            if (_dragHandoff && _sizeAnim is { } size && _offsetAnim is { } offset)
            {
                // A drag owns the visible position, not the old animation's value or velocity.
                // Resume both tracks together from the displayed pose after native writeback.
                await Task.WhenAll(size.SnapToAsync(_dragSizePx, ct), offset.SnapToAsync(_dragOffsetPx, ct));
                _dragHandoff = false;
                if (!_dragging && _owner is { } owner)
                    Canvas.SetLeft(Children[0], owner.IsChecked == true ? 20 : 0);
            }
            InvalidateMeasure();
        }, "Switch thumb update");
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count == 0 || _owner is null) return new Size(20, 32);
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var content = this.GetVisualDescendants().OfType<ContentPresenter>().First();
        content.Measure(Size.Infinity);
        var hasContent = content.DesiredSize.Width != 0 && content.DesiredSize.Height != 0;
        var pressed = _interaction?.Interaction.HasFlag(InteractionState.Pressed) == true;
        var size = (pressed ? 28 : hasContent || _checked ? 24 : 16) * scale;
        var minBound = (32 * scale - size) / 2;
        var maxBound = ((52 - 24) - (32 - 24) / 2) * scale;
        var offset = pressed ? _checked ? maxBound - 2 * scale : 2 * scale
            : _checked ? maxBound : minBound;
        if (float.IsNaN(_initialSize) && float.IsNaN(_initialOffset))
        {
            _initialSize = size; _initialOffset = offset;
        }
        var measuredSize = (int)(_sizeAnim is null ? size : _sizePx) / (double)scale;
        _interaction?.InitializeRippleDrawGeometry(new Size(measuredSize, measuredSize));
        if (!_dragHandoff) Animate(_sizeAnim, size);
        if (!_dragging && !_dragHandoff) Animate(_offsetAnim, offset);
        void Animate(Animatable<float>? track, float target)
        {
            if (track is null || track.TargetValue == target || !this.IsAttachedToVisualTree()) return;
            if (!pressed && ThumbAnimation is null) return;
            IFiniteAnimationSpec<float> animationSpec = pressed ? new SnapSpec<float>()
                : new SpringSpec<float>(ThumbAnimation!.Value.DampingRatio, ThumbAnimation.Value.Stiffness, 0.01f);
            var motion = AnimationScope.Get(this);
            // ThumbNode launches animateTo directly; interruption retains current velocity.
            _ = motion.Launch(ct => track.AnimateToAsync(motion, target, animationSpec, cancellationToken: ct), "Switch thumb animation");
        }
        PaintedSize = (int)(_dragHandoff ? _dragSizePx : _sizeAnim is null ? size : _sizePx) / (double)scale;
        PaintedOffset = (int)(_offsetAnim is null ? offset : _offsetPx) / (double)scale;
        if (_dragging || _dragHandoff) PaintedOffset = _dragOffsetPx / scale;
        Children[0].Measure(new Size(PaintedSize, PaintedSize));
        return new Size(20, 32);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 0)
        {
            var scale = LayoutHelper.GetLayoutScale(this);
            var top = MathUtils.RoundToInt((32 - PaintedSize) * scale / 2) / scale;
            Children[0].Arrange(new Rect(PaintedOffset, top, PaintedSize, PaintedSize));
        }
        return finalSize;
    }
}

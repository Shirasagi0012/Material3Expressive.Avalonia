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
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;

namespace Material3Expressive.Components;

// Also PART_SwitchKnob: native ToggleSwitch owns Canvas.Left and drag activation.
internal sealed class SwitchThumbLayout : Panel
{
    private ToggleSwitch? _owner;
    private MaterialInteractionController? _interaction;
    private VisualAnimationScope? _motion;
    private Animatable<float>? _sizeAnim, _offsetAnim;
    private float _initialSize = float.NaN, _initialOffset = float.NaN;
    private float _sizePx, _offsetPx;
    private bool _updatePending;
    private bool _checked;
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
        _motion = VisualAnimationScope.Create(this);
        // Observe the existing controller after its tunnel handlers, without another input
        // state machine or an activation path.
        _owner.AddHandler(InputElement.PointerPressedEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.PointerReleasedEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.PointerMovedEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.KeyDownEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.AddHandler(InputElement.KeyUpEvent, InputChanged, RoutingStrategies.Tunnel, true);
        _owner.PointerCaptureLost += InputChanged;
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
            owner.PointerCaptureLost -= InputChanged;
            owner.LostFocus -= InputChanged;
            owner.PropertyChanged -= OwnerChanged;
            owner.Classes.CollectionChanged -= ClassesChanged;
        }
        if (Children.FirstOrDefault() is { } moving) moving.PropertyChanged -= MovingChanged;
        _motion?.Dispose(); _motion = null;
        _sizeAnim = _offsetAnim = null;
        _initialSize = _initialOffset = float.NaN;
        _updatePending = false;
        _owner = null; _interaction = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void InputChanged(object? sender, RoutedEventArgs e) => InvalidateMeasure();
    private void ClassesChanged(object? sender, NotifyCollectionChangedEventArgs e) => InvalidateMeasure();
    private void MovingChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Canvas.LeftProperty) InvalidateMeasure();
    }
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == InputElement.IsEffectivelyEnabledProperty) InvalidateMeasure();
        if (e.Property == SwitchAppearance.ThumbContentProperty || e.Property == SwitchAppearance.ThumbContentTemplateProperty)
            InvalidateMeasure();
        if (e.Property != Avalonia.Controls.Primitives.ToggleButton.IsCheckedProperty) return;
        if (_motion is not { IsEnding: false } motion || _updatePending) return;
        _updatePending = true;
        // ThumbElement.update runs during recomposition. Animatable starts from the
        // first measurement, rather than from a prior unanimated press.
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            _updatePending = false;
            _checked = _owner?.IsChecked == true;
            if (!float.IsNaN(_initialSize) && _sizeAnim is null)
            {
                _sizeAnim = motion.CreateAnimatable(_initialSize);
                _offsetAnim = motion.CreateAnimatable(_initialOffset);
                motion.SubscribeValue(_sizeAnim, value => { _sizePx = value; InvalidateMeasure(); });
                motion.SubscribeValue(_offsetAnim, value => { _offsetPx = value; InvalidateMeasure(); });
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
        var spring = TokenResolver.TryResolve(this, MaterialTokens.Sys.Motion.Spring.SpatialFast, ActualThemeVariant, out var value)
            && value is Foundation.Motion.SpringSpec spec ? spec : MotionScheme.Expressive.FastSpatial;
        IFiniteAnimationSpec<float> animationSpec = pressed ? new SnapSpec<float>()
            : new SpringSpec<float>(spring.DampingRatio, spring.Stiffness, 0.01f);
        var measuredSize = (int)(_sizeAnim is null ? size : _sizePx) / (double)scale;
        _interaction?.InitializeRippleDrawGeometry(new Size(measuredSize, measuredSize));
        Animate(_sizeAnim, size); Animate(_offsetAnim, offset);
        void Animate(Animatable<float>? track, float target)
        {
            if (track is null || track.TargetValue == target || _motion is not { IsEnding: false } motion) return;
            // ThumbNode launches animateTo directly; interruption retains current velocity.
            _ = motion.Launch(ct => track.AnimateToAsync(target, animationSpec, cancellationToken: ct), "Switch thumb animation");
        }
        PaintedSize = (int)(_sizeAnim is null ? size : _sizePx) / (double)scale;
        PaintedOffset = (int)(_offsetAnim is null ? offset : _offsetPx) / (double)scale;
        if (_owner.Classes.Contains(":dragging")) PaintedOffset = 2 + Canvas.GetLeft(Children[0]);
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

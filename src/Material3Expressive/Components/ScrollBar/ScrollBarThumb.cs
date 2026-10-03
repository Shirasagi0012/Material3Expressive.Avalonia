/*
 * Feedback translated from Compose Multiplatform Core Scrollbar.skiko.kt,
 * Copyright 2023 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed baseline: docs/upstream-baseline.json; OldOrNewScrollbar / isHighlighted.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

// Template-only feedback. Native Thumb / Track keep capture, drag and value calculation.
internal sealed class ScrollBarThumb : Border
{
    private readonly SolidColorBrush _brush = new();
    private ScrollBar? _owner;
    private Thumb? _thumb;
    private VisualAnimationScope? _motion;
    private Animatable<Color>? _color;
    private bool _dragging, _pending;
    internal Color PaintedColor => _brush.Color;
    private Color ColorTarget => _owner is not { } owner ? Colors.Transparent
        : owner.IsPointerOver || _dragging ? ScrollBarAppearance.GetHoverColor(owner) : ScrollBarAppearance.GetUnhoverColor(owner);

    public ScrollBarThumb() => Background = _brush;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<ScrollBar>();
        _thumb = this.FindAncestorOfType<Thumb>();
        if (_owner is not null) _owner.PropertyChanged += OwnerChanged;
        if (_thumb is not null)
        {
            _dragging = _thumb.Classes.Contains(":pressed");
            _thumb.DragStarted += DragStarted;
            _thumb.DragCompleted += DragCompleted;
        }
        _motion = VisualAnimationScope.Create(this);
        _brush.Color = ColorTarget;
        _color = _motion.CreateAnimatable(ColorTarget);
        _motion.SubscribeValue(_color, value => _brush.Color = value);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PropertyChanged -= OwnerChanged;
        if (_thumb is not null)
        {
            _thumb.DragStarted -= DragStarted;
            _thumb.DragCompleted -= DragCompleted;
        }
        _motion?.Dispose();
        _motion = null;
        _color = null;
        _owner = null;
        _thumb = null;
        _dragging = _pending = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == IsPointerOverProperty || change.Property == ScrollBarAppearance.UnhoverColorProperty ||
            change.Property == ScrollBarAppearance.HoverColorProperty || change.Property == ScrollBarAppearance.HoverDurationProperty)
            UpdateTarget();
    }

    private void DragStarted(object? sender, VectorEventArgs e) { _dragging = true; UpdateTarget(); }
    private void DragCompleted(object? sender, VectorEventArgs e) { _dragging = false; UpdateTarget(); }

    private void UpdateTarget()
    {
        if (_motion is not { IsEnding: false } motion || _pending) return;
        _pending = true;
        // Like animateColorAsState, coalesce property changes before retargeting the animation.
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            _pending = false;
            if (_color is not { } color || color.TargetValue == ColorTarget) return;
            var target = ColorTarget;
            var spec = new TweenSpec<Color>(ScrollBarAppearance.GetHoverDuration(_owner!));
            _ = motion.Launch(async token =>
            {
                await color.StopAsync();
                token.ThrowIfCancellationRequested();
                await color.AnimateToAsync(target, spec, cancellationToken: token);
            }, "ScrollBar color");
        }, "ScrollBar target");
    }
}

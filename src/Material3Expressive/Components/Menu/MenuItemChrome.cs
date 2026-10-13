/* Translated from fixed Menu.kt: animateColorAsState(containerColor, FastEffects).
 * Copyright 2025 The Android Open Source Project; Apache-2.0.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Interaction;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

// Template part: retain configuration on MenuItem.Background; publish animated paint to Surface.
internal sealed class MenuItemChrome : Decorator
{
    public static readonly StyledProperty<double> MinimumContainerWidthProperty = AvaloniaProperty.Register<MenuItemChrome, double>(nameof(MinimumContainerWidth));
    public double MinimumContainerWidth { get => GetValue(MinimumContainerWidthProperty); set => SetValue(MinimumContainerWidthProperty, value); }
    public static readonly StyledProperty<double> MaximumIntrinsicWidthProperty = AvaloniaProperty.Register<MenuItemChrome, double>(nameof(MaximumIntrinsicWidth), double.PositiveInfinity);
    public double MaximumIntrinsicWidth { get => GetValue(MaximumIntrinsicWidthProperty); set => SetValue(MaximumIntrinsicWidthProperty, value); }
    public static readonly StyledProperty<double> MinimumContainerHeightProperty = AvaloniaProperty.Register<MenuItemChrome, double>(nameof(MinimumContainerHeight));
    public double MinimumContainerHeight { get => GetValue(MinimumContainerHeightProperty); set => SetValue(MinimumContainerHeightProperty, value); }
    public static readonly StyledProperty<double> MinimumInteractiveHeightProperty = AvaloniaProperty.Register<MenuItemChrome, double>(nameof(MinimumInteractiveHeight));
    public double MinimumInteractiveHeight { get => GetValue(MinimumInteractiveHeightProperty); set => SetValue(MinimumInteractiveHeightProperty, value); }
    public static readonly StyledProperty<IBrush?> TargetBrushProperty = AvaloniaProperty.Register<MenuItemChrome, IBrush?>(nameof(TargetBrush));
    public IBrush? TargetBrush { get => GetValue(TargetBrushProperty); set => SetValue(TargetBrushProperty, value); }
    public static readonly StyledProperty<MaterialSpring?> ColorAnimationProperty = AvaloniaProperty.Register<MenuItemChrome, MaterialSpring?>(nameof(ColorAnimation));
    public MaterialSpring? ColorAnimation { get => GetValue(ColorAnimationProperty); set => SetValue(ColorAnimationProperty, value); }
    public static readonly DirectProperty<MenuItemChrome, IBrush?> PaintedBrushProperty = AvaloniaProperty.RegisterDirect<MenuItemChrome, IBrush?>(nameof(PaintedBrush), o => o.PaintedBrush);
    private IBrush? _paintedBrush;
    public IBrush? PaintedBrush { get => _paintedBrush; private set => SetAndRaise(PaintedBrushProperty, ref _paintedBrush, value); }
    private AnimationScope? _motion;
    private Animatable<Color>? _color;
    private bool _pending;
    private MenuItem? _keyboardOwner;

    static MenuItemChrome() => AffectsMeasure<MenuItemChrome>(MinimumContainerWidthProperty, MaximumIntrinsicWidthProperty,
        MinimumContainerHeightProperty, MinimumInteractiveHeightProperty);

    protected override Size MeasureOverride(Size availableSize)
    {
        // sizeIn supplies preferred minima/maxima within incoming constraints. Native
        // MenuItem.MinWidth would instead force an oversized item in a narrow popup.
        Child?.Measure(new Size(Math.Min(availableSize.Width, MaximumIntrinsicWidth), availableSize.Height));
        var size = Child?.DesiredSize ?? default;
        return new(Math.Min(availableSize.Width, Math.Max(MinimumContainerWidth, size.Width)),
            Math.Min(availableSize.Height, Math.Max(MinimumInteractiveHeight, Math.Max(MinimumContainerHeight, size.Height))));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Child is { } child)
        {
            var height = Math.Min(finalSize.Height, Math.Max(MinimumContainerHeight, child.DesiredSize.Height));
            child.Arrange(new Rect(0, (finalSize.Height - height) / 2, finalSize.Width, height));
        }
        return finalSize;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (TemplatedParent is MenuItem owner)
        {
            _keyboardOwner = owner;
            owner.AddHandler(InputElement.KeyDownEvent, OnOwnerKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
            owner.AddHandler(InputElement.KeyUpEvent, OnOwnerKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        }
        _motion = AnimationScope.Get(this);
        PaintedBrush = TargetBrush;
        if (TargetBrush is ISolidColorBrush { Opacity: 1 } brush)
        {
            _color = new Animatable<Color>(new ColorConverter(), brush.Color, default);
            _motion.SubscribeValue(_color, color => PaintedBrush = new SolidColorBrush(color));
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_keyboardOwner is { } owner)
        {
            owner.RemoveHandler(InputElement.KeyDownEvent, OnOwnerKeyDown);
            owner.RemoveHandler(InputElement.KeyUpEvent, OnOwnerKeyUp);
            _keyboardOwner = null;
        }
        _motion = null; _color = null; _pending = false;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnOwnerKeyDown(object? sender, KeyEventArgs e)
    {
        // The shared controller limits keyboard activation to Button owners. Bridge native
        // MenuItem feedback here; DefaultMenuInteractionHandler still owns Enter activation.
        if (_keyboardOwner is { } owner && ReferenceEquals(e.Source, owner) && e.Key == Key.Enter)
            MaterialInteraction.GetOrCreate(owner).StartPress(new Rect(owner.Bounds.Size).Center);
    }
    private void OnOwnerKeyUp(object? sender, KeyEventArgs e)
    {
        if (_keyboardOwner is { } owner && ReferenceEquals(e.Source, owner) && e.Key == Key.Enter)
            MaterialInteraction.GetOrCreate(owner).EndPress();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property != TargetBrushProperty && change.Property != ColorAnimationProperty) return;
        if (!this.IsAttachedToVisualTree() || _motion is null || _pending) return;
        _pending = true;
        var motion = _motion = AnimationScope.Get(this);
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            _pending = false;
            if (TargetBrush is not ISolidColorBrush { Opacity: 1 } target)
            {
                if (_color is { } old) await old.StopAsync();
                PaintedBrush = TargetBrush; _color = null; return;
            }
            if (_color is null)
            {
                _color = new Animatable<Color>(new ColorConverter(), target.Color, default);
                motion.SubscribeValue(_color, color => PaintedBrush = new SolidColorBrush(color));
            }
            var color = _color;
            if (color.TargetValue == target.Color || ColorAnimation is not { } spec) return;
            var targetColor = target.Color;
            _ = motion.Launch(async token =>
            {
                // The pinned AnimateAsState.restartAnimation cancels its job before
                // starting animateTo. Join that cancellation in the local runtime so
                // the old mutation cannot unwind after the replacement starts.
                await color.StopAsync();
                token.ThrowIfCancellationRequested();
                await color.AnimateToAsync(motion, targetColor, new SpringSpec<Color>(spec.DampingRatio, spec.Stiffness), cancellationToken: token);
            }, "Menu container color");
        }, "Menu color target");
    }
}

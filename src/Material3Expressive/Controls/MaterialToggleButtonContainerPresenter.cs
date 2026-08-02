using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Draws the checked-aware container chrome for a native Material ToggleButton.</summary>
public sealed class MaterialToggleButtonContainerPresenter : Control
{
    public static readonly DirectProperty<MaterialToggleButtonContainerPresenter, CornerRadius>
        CurrentCornerRadiusProperty = AvaloniaProperty.RegisterDirect<
            MaterialToggleButtonContainerPresenter,
            CornerRadius>(nameof(CurrentCornerRadius), presenter => presenter.CurrentCornerRadius);

    private readonly MaterialThemeWatcher _theme;
    private ToggleButton? _owner;
    private MaterialInteractionHost? _interaction;
    private AnimatedShape? _shape;
    private ButtonSizeTokens _metrics = ButtonSizeTokens.Small;
    private CornerRadius _currentCornerRadius;

    public MaterialToggleButtonContainerPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += OnThemeChanged;
    }

    public CornerRadius CurrentCornerRadius
    {
        get => _currentCornerRadius;
        private set => SetAndRaise(CurrentCornerRadiusProperty, ref _currentCornerRadius, value);
    }

    internal MaterialInteractionHost? InteractionHost => _interaction;

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        if (_owner is not { } owner || Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        UpdateCurrentCornerRadius();
        MaterialContainer.Draw(
            context,
            Bounds.Size,
            CurrentCornerRadius,
            owner.Background,
            owner.BorderBrush,
            Uniform(owner.BorderThickness));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = TemplatedParent as ToggleButton
                 ?? throw new InvalidOperationException(
                     $"{nameof(MaterialToggleButtonContainerPresenter)} requires a ToggleButton template.");
        _interaction = MaterialInteraction.GetOrCreate(_owner);
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _interaction.InteractionChanged += OnInteractionChanged;
        _interaction.Ticked += OnInteractionTicked;
        ApplyTokens(snapShape: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is { })
            _owner.PropertyChanged -= OnOwnerPropertyChanged;
        if (_interaction is { } interaction)
        {
            interaction.InteractionChanged -= OnInteractionChanged;
            interaction.Ticked -= OnInteractionTicked;
            if (_shape is { })
                interaction.Motion.Remove(_shape.Progress);
        }

        _shape = null;
        _interaction = null;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BoundsProperty || change.Property == FlowDirectionProperty)
            UpdateCurrentCornerRadius();
    }

    private AnimatedShape Shape => _shape ??= _interaction!.CreateAnimatedShape(
        RestingShape(), _theme.Spring(MotionRole.DefaultEffects));

    private void OnThemeChanged(object? sender, EventArgs e) => ApplyTokens(snapShape: true);

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ButtonAssist.VariantProperty
            || change.Property == ButtonAssist.SizeProperty)
            ApplyTokens(snapShape: true);
        else if (change.Property == ToggleButton.IsCheckedProperty
                 || change.Property == ButtonAssist.IsSquareProperty)
            UpdateShape();
        else if (change.Property == ButtonAssist.ContainerShapeOverrideProperty)
            UpdateShape(snap: true);
        else if (change.Property == ToggleButton.BackgroundProperty
                 || change.Property == ToggleButton.BorderBrushProperty
                 || change.Property == ToggleButton.BorderThicknessProperty)
            InvalidateVisual();
    }

    private void OnInteractionChanged(object? sender, EventArgs e) => UpdateShape();

    private void OnInteractionTicked(object? sender, EventArgs e)
    {
        UpdateCurrentCornerRadius();
        InvalidateVisual();
    }

    private void ApplyTokens(bool snapShape)
    {
        if (_owner is not { } owner)
            return;
        _metrics = _theme.Theme.GetButtonSizeTokens(ButtonAssist.GetSize(owner));
        UpdateShape(snapShape);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        if (_owner is null || _interaction is null)
            return;
        var shape = Shape;
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _theme.Theme.Shapes.Get(_metrics.PressedContainerShape)
            : RestingShape();
        if (snap && !shape.IsRunning)
            shape.SnapTo(target);
        else
        {
            shape.AnimateTo(target, _theme.Spring(MotionRole.DefaultEffects));
            _interaction.Motion.Start();
        }
        UpdateCurrentCornerRadius();
        InvalidateVisual();
    }

    private RoundedCornerShape RestingShape()
    {
        if (_owner is not { } owner)
            return _theme.Theme.Shapes.Get(_metrics.ContainerShape);
        if (ButtonAssist.GetContainerShapeOverride(owner) is { } shape)
            return shape;

        var selected = owner.IsChecked == true;
        var round = ButtonAssist.GetIsSquare(owner) == selected;
        return _theme.Theme.Shapes.Get(
            round ? _metrics.ContainerShape : _metrics.SquareContainerShape);
    }

    private void UpdateCurrentCornerRadius()
    {
        if (_shape is null)
            return;
        CurrentCornerRadius = Shape.ToCornerRadius(
            Bounds.Size, FlowDirection == FlowDirection.RightToLeft);
    }

    private static double Uniform(Thickness thickness) =>
        thickness.Left.Equals(thickness.Top)
        && thickness.Left.Equals(thickness.Right)
        && thickness.Left.Equals(thickness.Bottom)
            ? thickness.Left
            : 0;
}

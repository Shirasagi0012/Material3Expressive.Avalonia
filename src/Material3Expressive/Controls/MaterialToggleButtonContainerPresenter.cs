using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Draws the checked-aware container chrome for a native Material ToggleButton.</summary>
public sealed class MaterialToggleButtonContainerPresenter : Control, IMaterialInteractionObserver
{
    private static readonly ToggleButtonGeometryProfile DefaultGeometryProfile = new(
        40,
        48,
        new Thickness(24, 10),
        new Thickness(1),
        TypographyRole.LabelLarge,
        ShapeTokens.CornerFull,
        ShapeTokens.CornerSmall,
        ShapeTokens.CornerMedium);

    public static readonly StyledProperty<ToggleButtonGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialToggleButtonContainerPresenter, ToggleButtonGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    public static readonly StyledProperty<SpringSpec> ShapeMotionProperty =
        AvaloniaProperty.Register<MaterialToggleButtonContainerPresenter, SpringSpec>(
            nameof(ShapeMotion),
            new SpringSpec(0.9, 1400));

    public static readonly DirectProperty<MaterialToggleButtonContainerPresenter, CornerRadius>
        CurrentCornerRadiusProperty = AvaloniaProperty.RegisterDirect<
            MaterialToggleButtonContainerPresenter,
            CornerRadius>(nameof(CurrentCornerRadius), presenter => presenter.CurrentCornerRadius);

    private ToggleButton? _owner;
    private MaterialInteractionController? _interaction;
    private IDisposable? _interactionLease;
    private AnimatedShape? _shape;
    private CornerRadius _currentCornerRadius;

    public MaterialToggleButtonContainerPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
    }

    public ToggleButtonGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    public SpringSpec ShapeMotion
    {
        get => GetValue(ShapeMotionProperty);
        set => SetValue(ShapeMotionProperty, value);
    }

    public CornerRadius CurrentCornerRadius
    {
        get => _currentCornerRadius;
        private set => SetAndRaise(CurrentCornerRadiusProperty, ref _currentCornerRadius, value);
    }

    internal MaterialInteractionController? Interaction => _interaction;

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
        _interaction = MaterialElementContext.Ensure(_owner).Interaction;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _interactionLease = _interaction.Subscribe(this);
        UpdateShape(snap: true);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is { })
            _owner.PropertyChanged -= OnOwnerPropertyChanged;
        if (_interaction is { } interaction)
        {
            if (_shape is { })
                interaction.Motion.Remove(_shape.Progress);
        }

        _interactionLease?.Dispose();
        _interactionLease = null;

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
        else if (change.Property == GeometryProfileProperty)
            UpdateShape(snap: true);
        else if (change.Property == ShapeMotionProperty && _shape is { })
            UpdateShape();
    }

    private AnimatedShape Shape => _shape ??= CreateShape();

    private AnimatedShape CreateShape()
    {
        var interaction = _interaction
                          ?? throw new InvalidOperationException("The presenter is not attached.");
        var shape = new AnimatedShape(RestingShape(), ShapeMotion);
        interaction.Motion.Add(shape.Progress);
        return shape;
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ToggleButton.IsCheckedProperty
                 || change.Property == M3Button.IsSquareProperty)
            UpdateShape();
        else if (change.Property == M3Button.ContainerShapeOverrideProperty)
            UpdateShape(snap: true);
        else if (change.Property == ToggleButton.BackgroundProperty
                 || change.Property == ToggleButton.BorderBrushProperty
                 || change.Property == ToggleButton.BorderThicknessProperty)
            InvalidateVisual();
    }

    void IMaterialInteractionObserver.OnMaterialInteractionChanged(InteractionState state) => UpdateShape();

    void IMaterialInteractionObserver.OnMaterialInteractionFrame()
    {
        UpdateCurrentCornerRadius();
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        if (_owner is null || _interaction is null)
            return;
        var shape = Shape;
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? GeometryProfile.PressedShape
            : RestingShape();
        if (snap && !shape.IsRunning)
            shape.SnapTo(target);
        else
        {
            shape.AnimateTo(target, ShapeMotion);
            _interaction.Motion.RequestFrame();
        }
        UpdateCurrentCornerRadius();
        InvalidateVisual();
    }

    private RoundedCornerShape RestingShape()
    {
        if (_owner is not { } owner)
            return GeometryProfile.UncheckedShape;
        if (M3Button.GetContainerShapeOverride(owner) is { } shape)
            return shape;

        var selected = owner.IsChecked == true;
        var round = M3Button.GetIsSquare(owner) == selected;
        return round ? GeometryProfile.UncheckedShape : GeometryProfile.CheckedShape;
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

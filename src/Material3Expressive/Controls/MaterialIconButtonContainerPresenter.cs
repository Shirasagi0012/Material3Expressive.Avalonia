using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Draws normal and checked icon-button container geometry for native button controls.</summary>
public sealed class MaterialIconButtonContainerPresenter : Control, IMaterialThemeConsumer, IMaterialInteractionObserver
{
    public static readonly DirectProperty<MaterialIconButtonContainerPresenter, CornerRadius>
        CurrentCornerRadiusProperty = AvaloniaProperty.RegisterDirect<
            MaterialIconButtonContainerPresenter,
            CornerRadius>(nameof(CurrentCornerRadius), presenter => presenter.CurrentCornerRadius);

    private readonly MaterialThemeController _theme;
    private Button? _owner;
    private MaterialInteractionController? _interaction;
    private IDisposable? _interactionLease;
    private AnimatedShape? _shape;
    private IconButtonShapes _shapes = IconButtonDefaults.Shapes(IconButtonSize.Small);
    private SpringSpec _shapeMotion = IconButtonDefaults.ShapeMotion(ResolvedTheme.Default);
    private CornerRadius _currentCornerRadius;

    public MaterialIconButtonContainerPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
        _theme = MaterialElementContext.Ensure(this).Theme;
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) =>
        ApplyTokens(snapShape: true);

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
        _owner = TemplatedParent as Button
                 ?? throw new InvalidOperationException(
                     $"{nameof(MaterialIconButtonContainerPresenter)} requires a Button template.");
        _interaction = MaterialElementContext.Ensure(_owner).Interaction;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _interactionLease = _interaction.Subscribe(this);
        ApplyTokens(snapShape: true);
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
    }

    private AnimatedShape Shape => _shape ??= CreateShape();

    private AnimatedShape CreateShape()
    {
        var interaction = _interaction
                          ?? throw new InvalidOperationException("The presenter is not attached.");
        var shape = new AnimatedShape(RestingShape(), _shapeMotion);
        interaction.Motion.Add(shape.Progress);
        return shape;
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == M3IconButton.DefaultMetricsProperty
            || change.Property == M3IconButton.DefaultShapesProperty
            || change.Property == M3IconButton.DefaultShapeMotionProperty
            || change.Property == M3ToggleIconButton.DefaultMetricsProperty
            || change.Property == M3ToggleIconButton.DefaultShapesProperty
            || change.Property == M3ToggleIconButton.DefaultShapeMotionProperty)
            ApplyTokens(snapShape: true);
        else if (change.Property == ToggleButton.IsCheckedProperty
                 || change.Property == M3IconButton.IsSquareProperty)
            UpdateShape();
        else if (change.Property == M3IconButton.ContainerShapeOverrideProperty)
            UpdateShape(snap: true);
        else if (change.Property == Button.BackgroundProperty
                 || change.Property == Button.BorderBrushProperty
                 || change.Property == Button.BorderThicknessProperty)
            InvalidateVisual();
    }

    void IMaterialInteractionObserver.OnMaterialInteractionChanged(InteractionState state) => UpdateShape();

    void IMaterialInteractionObserver.OnMaterialInteractionFrame()
    {
        UpdateCurrentCornerRadius();
        InvalidateVisual();
    }

    private void ApplyTokens(bool snapShape)
    {
        if (_owner is not { } owner)
            return;
        if (owner is ToggleButton toggle)
        {
            _shapes = M3ToggleIconButton.GetDefaultShapes(toggle)
                      ?? ToggleIconButtonDefaults.Shapes(M3IconButton.GetSize(owner));
            _shapeMotion = M3ToggleIconButton.GetDefaultShapeMotion(toggle);
            if (_shapeMotion.Stiffness <= 0)
                _shapeMotion = ToggleIconButtonDefaults.ShapeMotion(_theme.Current.Theme);
        }
        else
        {
            _shapes = M3IconButton.GetDefaultShapes(owner)
                      ?? IconButtonDefaults.Shapes(M3IconButton.GetSize(owner));
            _shapeMotion = M3IconButton.GetDefaultShapeMotion(owner);
            if (_shapeMotion.Stiffness <= 0)
                _shapeMotion = IconButtonDefaults.ShapeMotion(_theme.Current.Theme);
        }
        UpdateShape(snapShape);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        if (_owner is null || _interaction is null)
            return;
        var shape = Shape;
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _theme.Current.Theme.Shapes.Get(_shapes.Pressed)
            : RestingShape();
        if (snap && !shape.IsRunning)
            shape.SnapTo(target);
        else
        {
            shape.AnimateTo(target, _shapeMotion);
            _interaction.Motion.RequestFrame();
        }
        UpdateCurrentCornerRadius();
        InvalidateVisual();
    }

    private RoundedCornerShape RestingShape()
    {
        if (_owner is not { } owner)
            return _theme.Current.Theme.Shapes.Get(_shapes.Container);
        if (M3IconButton.GetContainerShapeOverride(owner) is { } shape)
            return shape;

        var selected = owner is ToggleButton { IsChecked: true };
        var square = M3IconButton.GetIsSquare(owner);
        var role = (selected, square) switch
        {
            (true, false) => _shapes.Selected,
            (true, true) => _shapes.SelectedSquare,
            (false, false) => _shapes.Container,
            _ => _shapes.Square
        };
        return _theme.Current.Theme.Shapes.Get(role);
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

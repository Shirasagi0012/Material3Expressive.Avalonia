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
    private static readonly IconButtonGeometryProfile DefaultGeometryProfile = new(
        ContainerHeight: 40,
        IconSize: 24,
        ContentPadding: new Thickness(8, 0),
        ContainerWidth: 40,
        OutlineWidth: 1,
        ContainerShape: ShapeRole.CornerFull,
        PressedShape: ShapeRole.CornerSmall,
        SquareShape: ShapeRole.CornerMedium,
        SelectedShape: ShapeRole.CornerMedium,
        SelectedSquareShape: ShapeRole.CornerFull);

    /// <summary>Gets or sets the typed geometry profile selected by the owning ControlTheme.</summary>
    public static readonly StyledProperty<IconButtonGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialIconButtonContainerPresenter, IconButtonGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    public static readonly DirectProperty<MaterialIconButtonContainerPresenter, CornerRadius>
        CurrentCornerRadiusProperty = AvaloniaProperty.RegisterDirect<
            MaterialIconButtonContainerPresenter,
            CornerRadius>(nameof(CurrentCornerRadius), presenter => presenter.CurrentCornerRadius);

    private readonly MaterialThemeController _theme;
    private Button? _owner;
    private MaterialInteractionController? _interaction;
    private IDisposable? _interactionLease;
    private AnimatedShape? _shape;
    private SpringSpec _shapeMotion = new(1, 1600);
    private CornerRadius _currentCornerRadius;

    public MaterialIconButtonContainerPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
        _theme = MaterialElementContext.Ensure(this).Theme;
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) =>
        ApplyTheme(snapShape: true);

    /// <summary>Gets or sets the size-selected geometry profile used by the presenter.</summary>
    public IconButtonGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
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
        _owner = TemplatedParent as Button
                 ?? throw new InvalidOperationException(
                     $"{nameof(MaterialIconButtonContainerPresenter)} requires a Button template.");
        _interaction = MaterialElementContext.Ensure(_owner).Interaction;
        _owner.PropertyChanged += OnOwnerPropertyChanged;
        _interactionLease = _interaction.Subscribe(this);
        ApplyTheme(snapShape: true);
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
        if (change.Property == M3IconButton.GeometryProfileProperty)
            UpdateShape(snap: true);
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

    private void ApplyTheme(bool snapShape)
    {
        if (_owner is not { } owner)
            return;
        _shapeMotion = _theme.Current.Theme.Motion.Get(
            owner is ToggleButton ? MotionRole.FastSpatial : MotionRole.DefaultEffects);
        UpdateShape(snapShape);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        if (_owner is null || _interaction is null)
            return;
        var shape = Shape;
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? _theme.Current.Theme.Shapes.Get(GeometryProfile.PressedShape)
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
            return _theme.Current.Theme.Shapes.Get(GeometryProfile.ContainerShape);
        if (M3IconButton.GetContainerShapeOverride(owner) is { } shape)
            return shape;

        var selected = owner is ToggleButton { IsChecked: true };
        var square = M3IconButton.GetIsSquare(owner);
        var role = (selected, square) switch
        {
            (true, false) => GeometryProfile.SelectedShape,
            (true, true) => GeometryProfile.SelectedSquareShape,
            (false, false) => GeometryProfile.ContainerShape,
            _ => GeometryProfile.SquareShape
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

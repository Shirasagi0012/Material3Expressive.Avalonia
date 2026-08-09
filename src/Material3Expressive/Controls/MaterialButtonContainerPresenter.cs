using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Draws the animated container chrome for a Material-themed native button.</summary>
/// <remarks>
/// The templated <see cref="Button" /> remains responsible for commands, input, automation and
/// content. This leaf presenter owns only the geometry that cannot be expressed by a static
/// Border: elevation and the interruptible pressed-shape morph.
/// </remarks>
public sealed class MaterialButtonContainerPresenter : Control, IMaterialThemeConsumer, IMaterialInteractionObserver
{
    public static readonly DirectProperty<MaterialButtonContainerPresenter, CornerRadius>
        CurrentCornerRadiusProperty = AvaloniaProperty.RegisterDirect<
            MaterialButtonContainerPresenter,
            CornerRadius>(nameof(CurrentCornerRadius), presenter => presenter.CurrentCornerRadius);

    private readonly MaterialThemeController _theme;

    private Button? _owner;
    private MaterialInteractionController? _interaction;
    private IDisposable? _interactionLease;
    private AnimatedShape? _shape;
    private ButtonShapes _shapes = ButtonDefaults.Shapes(ButtonSize.Small);
    private ButtonElevation _elevation = ButtonDefaults.Elevation(ButtonVariant.Filled);
    private SpringSpec _shapeMotion = ButtonDefaults.ShapeMotion(ResolvedTheme.Default);
    private CornerRadius _currentCornerRadius;

    public MaterialButtonContainerPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
        _theme = MaterialElementContext.Ensure(this).Theme;
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) =>
        ApplyTokens(snapShape: true);

    /// <summary>The live, bounds-resolved corners used by the ripple overlay.</summary>
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

        var enabled = owner.IsEffectivelyEnabled;
        var shadow = _theme.Current.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.Current.IsDark)
                     ?? Colors.Black;

        MaterialContainer.Draw(
            context,
            owner,
            Bounds.Size,
            CurrentCornerRadius,
            owner.Background,
            owner.BorderBrush,
            Uniform(owner.BorderThickness),
            CurrentElevation(enabled),
            shadow);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _owner = TemplatedParent as Button
                 ?? throw new InvalidOperationException(
                     $"{nameof(MaterialButtonContainerPresenter)} must be used in a Button template.");
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
        if (change.Property == M3Button.DefaultShapesProperty
            || change.Property == M3Button.DefaultElevationProperty
            || change.Property == M3Button.DefaultShapeMotionProperty)
        {
            ApplyTokens(snapShape: true);
        }
        else if (change.Property == M3Button.IsSquareProperty
                 || change.Property == M3Button.ContainerShapeOverrideProperty)
        {
            UpdateShape(change.Property == M3Button.ContainerShapeOverrideProperty);
        }
        else if (change.Property == IsEffectivelyEnabledProperty
                 || change.Property == Button.BackgroundProperty
                 || change.Property == Button.BorderBrushProperty
                 || change.Property == Button.BorderThicknessProperty)
        {
            InvalidateVisual();
        }
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

        // Defaults are projected by the owner-local adapter. The fallbacks only cover the short
        // construction window before the ControlTheme marker has activated that adapter.
        _shapes = M3Button.GetDefaultShapes(owner)
                  ?? ButtonDefaults.Shapes(M3Button.GetSize(owner));
        _elevation = M3Button.GetDefaultElevation(owner)
                     ?? ButtonDefaults.Elevation(M3Button.GetVariant(owner));
        _shapeMotion = M3Button.GetDefaultShapeMotion(owner);
        if (_shapeMotion.Stiffness <= 0)
            _shapeMotion = ButtonDefaults.ShapeMotion(_theme.Current.Theme);
        UpdateShape(snapShape);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        if (_owner is null || _interaction is null)
            return;

        var shape = Shape;
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? ShapeFor(_shapes.Pressed)
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

    private void UpdateCurrentCornerRadius()
    {
        if (_owner is null || _shape is null)
            return;

        CurrentCornerRadius = Shape.ToCornerRadius(
            Bounds.Size,
            FlowDirection == FlowDirection.RightToLeft);
    }

    private RoundedCornerShape RestingShape()
    {
        if (_owner is not { } owner)
            return ShapeFor(_shapes.Container);

        return M3Button.GetContainerShapeOverride(owner)
               ?? ShapeFor(
                   M3Button.GetIsSquare(owner)
                       ? _shapes.Square
                       : _shapes.Container);
    }

    private RoundedCornerShape ShapeFor(ShapeRole role) => _theme.Current.Theme.Shapes.Get(role);

    private double CurrentElevation(bool enabled)
    {
        var interaction = _interaction?.Interaction ?? InteractionState.None;

        return _elevation.For(enabled, interaction);
    }

    private static double Uniform(Thickness thickness)
    {
        return thickness.Left.Equals(thickness.Top)
               && thickness.Left.Equals(thickness.Right)
               && thickness.Left.Equals(thickness.Bottom)
            ? thickness.Left
            : 0;
    }
}

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

    private readonly MaterialElementContext _theme;

    private Button? _owner;
    private MaterialInteractionController? _interaction;
    private IDisposable? _interactionLease;
    private AnimatedShape? _shape;
    private ButtonColorTokens _tokens = ButtonColorTokens.Filled;
    private ButtonShapes _shapes = ButtonDefaults.Shapes(ResolvedTheme.Default, ButtonSize.Small);
    private CornerRadius _currentCornerRadius;

    public MaterialButtonContainerPresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
        _theme = MaterialElementContext.GetOrCreate(this);
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
        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark)
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
        _interaction = MaterialElementContext.GetOrCreate(_owner).Interaction;
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
        return interaction.CreateAnimatedShape(
            RestingShape(),
            _theme.Spring(MotionRole.DefaultEffects));
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ButtonAssist.VariantProperty
            || change.Property == ButtonAssist.SizeProperty)
        {
            ApplyTokens(snapShape: true);
        }
        else if (change.Property == ButtonAssist.IsSquareProperty
                 || change.Property == ButtonAssist.ContainerShapeOverrideProperty)
        {
            UpdateShape(change.Property == ButtonAssist.ContainerShapeOverrideProperty);
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

        _tokens = ButtonDefaults.ColorTokens(_theme.Theme, ButtonAssist.GetVariant(owner));
        _shapes = ButtonDefaults.Shapes(_theme.Theme, ButtonAssist.GetSize(owner));
        UpdateShape(snapShape);
        InvalidateVisual();
    }

    private void UpdateShape(bool snap = false)
    {
        if (_owner is null || _interaction is null)
            return;

        var shape = Shape;
        var target = _interaction.Interaction.HasFlag(InteractionState.Pressed)
            ? ShapeFor(_shapes.PressedContainerShape)
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
            return ShapeFor(_shapes.ContainerShape);

        return ButtonAssist.GetContainerShapeOverride(owner)
               ?? ShapeFor(
                   ButtonAssist.GetIsSquare(owner)
                       ? _shapes.SquareContainerShape
                       : _shapes.ContainerShape);
    }

    private RoundedCornerShape ShapeFor(ShapeRole role) => _theme.Theme.Shapes.Get(role);

    private double CurrentElevation(bool enabled)
    {
        var interaction = _interaction?.Interaction ?? InteractionState.None;

        return ButtonDefaults.Elevation(_tokens, enabled, interaction);
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

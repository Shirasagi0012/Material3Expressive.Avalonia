using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Template overlay that gives an ordinary Avalonia control the Material 3 ripple engine.
/// </summary>
/// <remarks>
/// Place this after the content presenter in a template so press ripples, state layers, and focus
/// rings are drawn above the content, matching <c>RippleNode.draw()</c>. Input remains owned by the
/// templated control; this overlay is never a hit-test or focus target.
/// </remarks>
public sealed class MaterialRipplePresenter : Control, IMaterialInteractionObserver
{
    public static readonly StyledProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, IBrush?>(nameof(StateLayerBrush));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<bool> IsBoundedProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, bool>(nameof(IsBounded), true);

    public static readonly StyledProperty<double> RippleRadiusProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, double>(nameof(RippleRadius), double.NaN);

    private MaterialInteractionController? _interaction;
    private MaterialInteractionPresentationLease? _presentation;
    private IDisposable? _interactionLease;
    private Control? _owner;

    public MaterialRipplePresenter()
    {
        IsHitTestVisible = false;
        Focusable = false;
    }

    public IBrush? StateLayerBrush
    {
        get => GetValue(StateLayerBrushProperty);
        set => SetValue(StateLayerBrushProperty, value);
    }

    public CornerRadius CornerRadius
    {
        get => GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    public bool IsBounded
    {
        get => GetValue(IsBoundedProperty);
        set => SetValue(IsBoundedProperty, value);
    }

    /// <summary>Explicit radius, or <see cref="double.NaN" /> to derive it from the bounds.</summary>
    public double RippleRadius
    {
        get => GetValue(RippleRadiusProperty);
        set => SetValue(RippleRadiusProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        // A template can briefly contain two overlays while it is being replaced. The owner's
        // controller keeps one presentation lease, so the older presenter must stop observing as
        // soon as that lease is revoked rather than retaining the old template through a frame.
        if (_presentation?.IsDisposed == true)
        {
            _interactionLease?.Dispose();
            _interactionLease = null;
            _presentation = null;
            _interaction = null;
            _owner = null;
        }

        ApplyConfiguration();
        if (_interaction is not { } interaction
            || _presentation is not { IsDisposed: false } presentation)
            return;

        var area = new Rect(Bounds.Size);
        if (_owner is { } owner)
        {
            var origin = this.TranslatePoint(default, owner) ?? default;
            area = new Rect(origin, Bounds.Size);
            presentation.UpdateGeometry(new MaterialInteractionGeometry(
                area,
                area,
                CornerRadius));
            area = presentation.Geometry.Area;
        }

        using (context.PushTransform(Matrix.CreateTranslation(-area.X, -area.Y)))
            interaction.DrawLayers(context, StateLayerBrush, CornerRadius);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        _owner = TemplatedParent as Control ?? this;
        _interaction = MaterialElementContext.Ensure(_owner).Interaction;
        _presentation = _interaction.AttachPresentation(this);
        _presentation.Disposed += OnPresentationDisposed;
        _interactionLease = _interaction.Subscribe(this);
        ApplyConfiguration();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _interactionLease?.Dispose();
        _interactionLease = null;

        _presentation?.Dispose();
        _presentation = null;

        _interaction = null;
        _owner = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == StateLayerBrushProperty
            || change.Property == CornerRadiusProperty
            || change.Property == IsBoundedProperty
            || change.Property == RippleRadiusProperty)
        {
            ApplyConfiguration();
            InvalidateVisual();
        }
    }

    private void ApplyConfiguration()
    {
        if (_interaction is not { } interaction
            || _presentation is not { IsDisposed: false })
            return;

        interaction.IsRippleBounded = IsBounded;
        interaction.RippleRadius = double.IsFinite(RippleRadius) ? RippleRadius : null;
    }

    void IMaterialInteractionObserver.OnMaterialInteractionChanged(InteractionState state) => InvalidateVisual();

    void IMaterialInteractionObserver.OnMaterialInteractionFrame() => InvalidateVisual();

    private void OnPresentationDisposed()
    {
        _interactionLease?.Dispose();
        _interactionLease = null;
        _presentation = null;
        _interaction = null;
        _owner = null;
    }
}

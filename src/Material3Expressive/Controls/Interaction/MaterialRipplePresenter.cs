using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Controls;

/// <summary>
/// Template overlay that gives an ordinary Avalonia control the Material 3 ripple engine.
/// </summary>
/// <remarks>
/// Place this after the content presenter in a template so press ripples, state layers, and focus
/// rings are drawn above the content, matching <c>RippleNode.draw()</c>. Input remains owned by the
/// templated control; this overlay is never a hit-test or focus target.
/// </remarks>
public sealed class MaterialRipplePresenter : Control
{
    public static readonly StyledProperty<IBrush?> StateLayerBrushProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, IBrush?>(nameof(StateLayerBrush));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, CornerRadius>(nameof(CornerRadius));

    public static readonly StyledProperty<bool> IsBoundedProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, bool>(nameof(IsBounded), true);

    public static readonly StyledProperty<double> RippleRadiusProperty =
        AvaloniaProperty.Register<MaterialRipplePresenter, double>(nameof(RippleRadius), double.NaN);

    private MaterialInteractionHost? _interaction;

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
        ApplyConfiguration();
        _interaction?.DrawLayers(context, StateLayerBrush, CornerRadius);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        var owner = TemplatedParent as Control ?? this;
        _interaction = MaterialInteraction.GetOrCreate(owner);
        _interaction.Ticked += OnInteractionChanged;
        _interaction.InteractionChanged += OnInteractionChanged;
        ApplyConfiguration();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_interaction is { })
        {
            _interaction.Ticked -= OnInteractionChanged;
            _interaction.InteractionChanged -= OnInteractionChanged;
        }

        _interaction = null;
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
        if (_interaction is not { } interaction)
            return;

        interaction.InteractionArea = new Rect(Bounds.Size);
        interaction.IsRippleBounded = IsBounded;
        interaction.RippleRadius = double.IsFinite(RippleRadius) ? RippleRadius : null;
    }

    private void OnInteractionChanged(object? sender, EventArgs e) => InvalidateVisual();
}

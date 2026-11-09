using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>Positions and clips custom pull-to-refresh content above the top edge of its layout box.</summary>
public class PullToRefreshIndicatorBox : ContentControl
{
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<PullToRefreshIndicatorBox>();
    /// <summary>An explicit container path; null retains CornerRadius. Translation and top-edge clipping are unchanged.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public static readonly StyledProperty<IPullToRefreshState?> StateProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, IPullToRefreshState?>(nameof(State));
    public static readonly StyledProperty<bool> IsRefreshingProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, bool>(nameof(IsRefreshing));
    public static readonly StyledProperty<double> MaxDistanceProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, double>(nameof(MaxDistance));
    public static readonly StyledProperty<double> RefreshThresholdProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, double>(nameof(RefreshThreshold));
    public static readonly StyledProperty<PullToRefreshScrollBehavior?> ScrollBehaviorProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, PullToRefreshScrollBehavior?>(nameof(ScrollBehavior));
    public static readonly StyledProperty<double> ElevationProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, double>(nameof(Elevation));
    public static readonly StyledProperty<double> ContainerWidthProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, double>(nameof(ContainerWidth));
    public static readonly StyledProperty<double> ContainerHeightProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorBox, double>(nameof(ContainerHeight));

    public IPullToRefreshState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public bool IsRefreshing { get => GetValue(IsRefreshingProperty); set => SetValue(IsRefreshingProperty, value); }
    public double MaxDistance { get => GetValue(MaxDistanceProperty); set => SetValue(MaxDistanceProperty, value); }
    public double RefreshThreshold { get => GetValue(RefreshThresholdProperty); set => SetValue(RefreshThresholdProperty, value); }
    public PullToRefreshScrollBehavior? ScrollBehavior { get => GetValue(ScrollBehaviorProperty); set => SetValue(ScrollBehaviorProperty, value); }
    public double Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ContainerWidth { get => GetValue(ContainerWidthProperty); set => SetValue(ContainerWidthProperty, value); }
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ContainerHeight { get => GetValue(ContainerHeightProperty); set => SetValue(ContainerHeightProperty, value); }
    private readonly PullToRefreshState _localState = new();
    private IPullToRefreshState? _attachedState;
    internal IPullToRefreshState EffectiveState => State ?? _localState;
    internal event Action? PresentationChanged;

    static PullToRefreshIndicatorBox()
    {
        TemplateProperty.OverrideDefaultValue<PullToRefreshIndicatorBox>(null);
        HorizontalAlignmentProperty.OverrideDefaultValue<PullToRefreshIndicatorBox>(HorizontalAlignment.Center);
        VerticalAlignmentProperty.OverrideDefaultValue<PullToRefreshIndicatorBox>(VerticalAlignment.Top);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<PullToRefreshIndicatorBox>(HorizontalAlignment.Center);
        VerticalContentAlignmentProperty.OverrideDefaultValue<PullToRefreshIndicatorBox>(VerticalAlignment.Center);
    }
    protected override Type StyleKeyOverride => typeof(PullToRefreshIndicatorBox);
    protected override bool BypassFlowDirectionPolicies => true;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachState();
        ScrollBehavior?.Attach(this);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ScrollBehavior?.Detach(this);
        DetachState();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (this.IsAttachedToVisualTree())
        {
            if (change.Property == ScrollBehaviorProperty)
            {
                change.GetOldValue<PullToRefreshScrollBehavior?>()?.Detach(this);
                ScrollBehavior?.Attach(this);
            }
            if (change.Property == IsRefreshingProperty) ScrollBehavior?.RefreshingChanged();
        }
        if (change.Property == StateProperty && this.IsAttachedToVisualTree()) { DetachState(); AttachState(); }
        if (change.Property == StateProperty || change.Property == IsRefreshingProperty ||
            change.Property == MaxDistanceProperty || change.Property == ElevationProperty)
            PresentationChanged?.Invoke();
    }
    private void AttachState()
    {
        var state = EffectiveState;
        if (state is PullToRefreshState builtIn) builtIn.Attach(this);
        _attachedState = state;
        state.PropertyChanged += StateChanged;
        PresentationChanged?.Invoke();
    }
    private void DetachState()
    {
        if (_attachedState is not { } state) return;
        state.PropertyChanged -= StateChanged;
        if (state is PullToRefreshState builtIn) builtIn.Detach(this);
        _attachedState = null;
    }
    private void StateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName is nameof(IPullToRefreshState.DistanceFraction) or nameof(IPullToRefreshState.IsAnimating))
            PresentationChanged?.Invoke();
    }
}

/// <summary>The circular arrow and spinning progress pull-to-refresh indicator.</summary>
public class PullToRefreshIndicator : PullToRefreshIndicatorBox
{
    protected override Type StyleKeyOverride => typeof(PullToRefreshIndicator);
    public static readonly StyledProperty<Color> ColorProperty =
        AvaloniaProperty.Register<PullToRefreshIndicator, Color>(nameof(Color));
    public Color Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }
}

/// <summary>The Expressive morphing-shape pull-to-refresh indicator.</summary>
public sealed class PullToRefreshLoadingIndicator : PullToRefreshIndicator
{
    protected override Type StyleKeyOverride => typeof(PullToRefreshLoadingIndicator);
}

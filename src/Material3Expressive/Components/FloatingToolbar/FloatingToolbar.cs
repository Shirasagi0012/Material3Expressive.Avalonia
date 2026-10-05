using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Presenters;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum FloatingToolbarHorizontalFabPosition { Start, End }
public enum FloatingToolbarVerticalFabPosition { Top, Bottom }
public enum FloatingToolbarExitDirection { Start, End, Top, Bottom }

/// <summary>Shared parameters of the fixed Compose floating toolbar overloads.</summary>
public abstract class FloatingToolbar : ContentControl
{
    internal static readonly StyledProperty<Avalonia.Media.IBrush?> DefaultBackgroundProperty = AvaloniaProperty.Register<FloatingToolbar, Avalonia.Media.IBrush?>(nameof(DefaultBackground));
    internal Avalonia.Media.IBrush? DefaultBackground { get => GetValue(DefaultBackgroundProperty); set => SetValue(DefaultBackgroundProperty, value); }
    internal static readonly StyledProperty<double> DefaultExpandedShadowElevationProperty = AvaloniaProperty.Register<FloatingToolbar, double>(nameof(DefaultExpandedShadowElevation));
    internal double DefaultExpandedShadowElevation { get => GetValue(DefaultExpandedShadowElevationProperty); set => SetValue(DefaultExpandedShadowElevationProperty, value); }
    internal static readonly StyledProperty<IControlTemplate?> DefaultTemplateProperty = AvaloniaProperty.Register<FloatingToolbar, IControlTemplate?>(nameof(DefaultTemplate));
    internal IControlTemplate? DefaultTemplate { get => GetValue(DefaultTemplateProperty); set => SetValue(DefaultTemplateProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> ScrollSnapAnimationProperty = AvaloniaProperty.Register<FloatingToolbar, Foundation.Motion.SpringSpec>(nameof(ScrollSnapAnimation));
    internal Foundation.Motion.SpringSpec ScrollSnapAnimation { get => GetValue(ScrollSnapAnimationProperty); set => SetValue(ScrollSnapAnimationProperty, value); }
    public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<FloatingToolbar, bool>(nameof(Expanded), true);
    public static readonly StyledProperty<object?> LeadingContentProperty = AvaloniaProperty.Register<FloatingToolbar, object?>(nameof(LeadingContent));
    public static readonly StyledProperty<IDataTemplate?> LeadingContentTemplateProperty = AvaloniaProperty.Register<FloatingToolbar, IDataTemplate?>(nameof(LeadingContentTemplate));
    public static readonly StyledProperty<object?> TrailingContentProperty = AvaloniaProperty.Register<FloatingToolbar, object?>(nameof(TrailingContent));
    public static readonly StyledProperty<IDataTemplate?> TrailingContentTemplateProperty = AvaloniaProperty.Register<FloatingToolbar, IDataTemplate?>(nameof(TrailingContentTemplate));
    public static readonly StyledProperty<object?> FloatingActionButtonProperty = AvaloniaProperty.Register<FloatingToolbar, object?>(nameof(FloatingActionButton));
    public static readonly StyledProperty<IDataTemplate?> FloatingActionButtonTemplateProperty = AvaloniaProperty.Register<FloatingToolbar, IDataTemplate?>(nameof(FloatingActionButtonTemplate));
    public static readonly StyledProperty<double> ExpandedShadowElevationProperty = AvaloniaProperty.Register<FloatingToolbar, double>(nameof(ExpandedShadowElevation));
    public static readonly StyledProperty<double> CollapsedShadowElevationProperty = AvaloniaProperty.Register<FloatingToolbar, double>(nameof(CollapsedShadowElevation));
    public static readonly StyledProperty<IFiniteAnimationSpec<float>> AnimationSpecProperty = AvaloniaProperty.Register<FloatingToolbar, IFiniteAnimationSpec<float>>(nameof(AnimationSpec));
    public static readonly StyledProperty<FloatingToolbarScrollBehavior?> ScrollBehaviorProperty = AvaloniaProperty.Register<FloatingToolbar, FloatingToolbarScrollBehavior?>(nameof(ScrollBehavior));
    public static readonly StyledProperty<ScrollViewer?> ScrollSourceProperty = AvaloniaProperty.Register<FloatingToolbar, ScrollViewer?>(nameof(ScrollSource));
    public static readonly StyledProperty<bool> TouchExplorationEnabledProperty = AvaloniaProperty.Register<FloatingToolbar, bool>(nameof(TouchExplorationEnabled));
    public static readonly DirectProperty<FloatingToolbar, bool> EffectiveExpandedProperty = AvaloniaProperty.RegisterDirect<FloatingToolbar, bool>(nameof(EffectiveExpanded), o => o.EffectiveExpanded);
    private bool _forceCollapse;
    private bool _effectiveExpanded = true;
    public bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
    public object? LeadingContent { get => GetValue(LeadingContentProperty); set => SetValue(LeadingContentProperty, value); }
    public IDataTemplate? LeadingContentTemplate { get => GetValue(LeadingContentTemplateProperty); set => SetValue(LeadingContentTemplateProperty, value); }
    public object? TrailingContent { get => GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }
    public IDataTemplate? TrailingContentTemplate { get => GetValue(TrailingContentTemplateProperty); set => SetValue(TrailingContentTemplateProperty, value); }
    /// <summary>Presence selects the FAB overload. Leading/trailing slots belong to the ordinary overload.</summary>
    public object? FloatingActionButton { get => GetValue(FloatingActionButtonProperty); set => SetValue(FloatingActionButtonProperty, value); }
    public IDataTemplate? FloatingActionButtonTemplate { get => GetValue(FloatingActionButtonTemplateProperty); set => SetValue(FloatingActionButtonTemplateProperty, value); }
    /// <summary>Theme default is 0 DIP without FAB and 1 DIP with FAB.</summary>
    public double ExpandedShadowElevation { get => GetValue(ExpandedShadowElevationProperty); set => SetValue(ExpandedShadowElevationProperty, value); }
    public double CollapsedShadowElevation { get => GetValue(CollapsedShadowElevationProperty); set => SetValue(CollapsedShadowElevationProperty, value); }
    public IFiniteAnimationSpec<float> AnimationSpec { get => GetValue(AnimationSpecProperty); set => SetValue(AnimationSpecProperty, value); }
    public FloatingToolbarScrollBehavior? ScrollBehavior { get => GetValue(ScrollBehaviorProperty); set => SetValue(ScrollBehaviorProperty, value); }
    /// <summary>Observes actual vertical consumed scroll; custom hosts can call the behavior directly.</summary>
    public ScrollViewer? ScrollSource { get => GetValue(ScrollSourceProperty); set => SetValue(ScrollSourceProperty, value); }
    /// <summary>Host-provided accessibility service state; suppresses scrolling and forces expansion unless accessibility collapsed it.</summary>
    public bool TouchExplorationEnabled { get => GetValue(TouchExplorationEnabledProperty); set => SetValue(TouchExplorationEnabledProperty, value); }
    public bool EffectiveExpanded => _effectiveExpanded;
    internal bool Horizontal => this is HorizontalFloatingToolbar;
    internal bool FabAtEnd => this is HorizontalFloatingToolbar horizontal ? horizontal.FloatingActionButtonPosition == FloatingToolbarHorizontalFabPosition.End
        : ((VerticalFloatingToolbar)this).FloatingActionButtonPosition == FloatingToolbarVerticalFabPosition.Bottom;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExpandedProperty || change.Property == TouchExplorationEnabledProperty)
            SetAndRaise(EffectiveExpandedProperty, ref _effectiveExpanded, !_forceCollapse && (Expanded || TouchExplorationEnabled));
        if (change.Property == FloatingActionButtonProperty)
            PseudoClasses.Set(":with-fab", FloatingActionButton is not null);
        if (change.Property == LeadingContentProperty || change.Property == TrailingContentProperty || change.Property == FloatingActionButtonProperty)
        {
            if (change.OldValue is ILogical oldChild) LogicalChildren.Remove(oldChild);
            if (change.NewValue is ILogical newChild) LogicalChildren.Add(newChild);
        }
    }
    protected override bool RegisterContentPresenter(ContentPresenter presenter) => presenter.Name is "PART_LeadingPresenter" or "PART_TrailingPresenter" or "PART_FabPresenter" || base.RegisterContentPresenter(presenter);
    internal void AccessibilityCollapse(bool collapse)
    {
        _forceCollapse = collapse;
        SetAndRaise(EffectiveExpandedProperty, ref _effectiveExpanded, !_forceCollapse && (Expanded || TouchExplorationEnabled));
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new FloatingToolbarAutomationPeer(this);
    private sealed class FloatingToolbarAutomationPeer(FloatingToolbar owner) : ControlAutomationPeer(owner), IExpandCollapseProvider
    {
        public ExpandCollapseState ExpandCollapseState => owner.EffectiveExpanded ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
        public bool ShowsMenu => false;
        public void Collapse() => owner.AccessibilityCollapse(true);
        public void Expand() => owner.AccessibilityCollapse(false);
        protected override string GetClassNameCore() => owner.GetType().Name;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;
    }
}

public sealed class HorizontalFloatingToolbar : FloatingToolbar
{
    public static readonly StyledProperty<FloatingToolbarHorizontalFabPosition> FloatingActionButtonPositionProperty = AvaloniaProperty.Register<HorizontalFloatingToolbar, FloatingToolbarHorizontalFabPosition>(nameof(FloatingActionButtonPosition), FloatingToolbarHorizontalFabPosition.End);
    public FloatingToolbarHorizontalFabPosition FloatingActionButtonPosition { get => GetValue(FloatingActionButtonPositionProperty); set => SetValue(FloatingActionButtonPositionProperty, value); }
}
public sealed class VerticalFloatingToolbar : FloatingToolbar
{
    public static readonly StyledProperty<FloatingToolbarVerticalFabPosition> FloatingActionButtonPositionProperty = AvaloniaProperty.Register<VerticalFloatingToolbar, FloatingToolbarVerticalFabPosition>(nameof(FloatingActionButtonPosition), FloatingToolbarVerticalFabPosition.Bottom);
    public FloatingToolbarVerticalFabPosition FloatingActionButtonPosition { get => GetValue(FloatingActionButtonPositionProperty); set => SetValue(FloatingActionButtonPositionProperty, value); }
}

public static class FloatingToolbarDefaults
{
    public const double ScrollDistanceThreshold = 40;
    public static FloatingToolbarScrollBehavior ExitAlwaysScrollBehavior(FloatingToolbarExitDirection exitDirection,
        FloatingToolbarState? state = null, IAnimationSpec<float>? snapAnimationSpec = null, IDecayAnimationSpec<float>? flingAnimationSpec = null) =>
        new(exitDirection, state ?? new(), snapAnimationSpec, flingAnimationSpec);
    /// <summary>Observes actual consumed vertical scroll and notifies the application; does not write Expanded. Dispose to unsubscribe.</summary>
    public static IDisposable FloatingToolbarVerticalNestedScroll(ScrollViewer source, Func<bool> expanded,
        Action onExpand, Action onCollapse, bool reverseLayout = false,
        double expandScrollDistanceThreshold = ScrollDistanceThreshold, double collapseScrollDistanceThreshold = ScrollDistanceThreshold)
    {
        var policy = new FloatingToolbarExpansionPolicy(expanded, onExpand, onCollapse, reverseLayout,
            (float)expandScrollDistanceThreshold, (float)collapseScrollDistanceThreshold);
        return new FloatingToolbarExpansionSubscription(source, policy);
    }
}

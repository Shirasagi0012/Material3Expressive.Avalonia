using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>A rail whose destinations and container animate between collapsed and expanded forms.</summary>
public class WideNavigationRail : ItemsControl
{
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec> StateAnimationProperty = AvaloniaProperty.Register<WideNavigationRail, Foundation.Motion.SpringSpec>(nameof(StateAnimation));
    internal Foundation.Motion.SpringSpec StateAnimation { get => GetValue(StateAnimationProperty); set => SetValue(StateAnimationProperty, value); }
    public static readonly StyledProperty<object?> HeaderProperty = AvaloniaProperty.Register<WideNavigationRail, object?>(nameof(Header));
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty = AvaloniaProperty.Register<WideNavigationRail, IDataTemplate?>(nameof(HeaderTemplate));
    public static readonly StyledProperty<Thickness> WindowInsetsProperty = AvaloniaProperty.Register<WideNavigationRail, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<WideNavigationRail, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<NavigationRailArrangement> ArrangementProperty = AvaloniaProperty.Register<WideNavigationRail, NavigationRailArrangement>(nameof(Arrangement), NavigationRailArrangement.Top);
    public static readonly StyledProperty<WideNavigationRailState> StateProperty = AvaloniaProperty.Register<WideNavigationRail, WideNavigationRailState>(nameof(State), null!);
    internal static readonly StyledProperty<bool> LayoutExpandedProperty = AvaloniaProperty.Register<WideNavigationRail, bool>(nameof(LayoutExpanded));

    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    /// <summary>Logical start/top/end/bottom spacing inside the insets.</summary>
    public Thickness ContentPadding { get => GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public NavigationRailArrangement Arrangement { get => GetValue(ArrangementProperty); set => SetValue(ArrangementProperty, value); }
    public WideNavigationRailState State { get => GetValue(StateProperty); set { ArgumentNullException.ThrowIfNull(value); SetValue(StateProperty, value); } }

    private WideNavigationRailState? _boundState;
    internal WideNavigationRailLayout? RailLayout { get; private set; }
    internal bool LayoutExpanded { get => GetValue(LayoutExpandedProperty); private set => SetValue(LayoutExpandedProperty, value); }
    internal virtual bool ComputeLayoutExpanded => State.TargetValue == WideNavigationRailValue.Expanded;
    internal bool IsStateAttached => _boundState is not null;

    public WideNavigationRail()
    {
        // A fresh state belongs to each default rail, rather than to property metadata.
        State = new WideNavigationRailState();
    }
    protected override Type StyleKeyOverride => typeof(WideNavigationRail);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        RailLayout = e.NameScope.Find<WideNavigationRailLayout>("PART_Layout");
        UpdateExpansion();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachState();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ReleaseState();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty)
        {
            ReleaseState();
            if (this.IsAttachedToVisualTree()) AttachState();
            UpdateExpansion();
        }
    }
    private void AttachState()
    {
        if (_boundState is not null) return;
        _boundState = State;
        _boundState.Attach(this, StateAnimation);
        _boundState.PropertyChanged += OnStateChanged;
        UpdateExpansion();
    }
    private void ReleaseState()
    {
        if (_boundState is null) return;
        _boundState.PropertyChanged -= OnStateChanged;
        _boundState.Detach(this);
        _boundState = null;
    }
    private void OnStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WideNavigationRailState.TargetValueProperty) UpdateExpansion();
    }
    internal virtual void UpdateExpansion()
    {
        var expanded = State.TargetValue == WideNavigationRailValue.Expanded;
        SetValue(WideNavigationRailItem.RailExpandedProperty, expanded);
        LayoutExpanded = ComputeLayoutExpanded;
        if (RailLayout is { } layout) layout.Expanded = LayoutExpanded;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new WideNavigationRailAutomationPeer(this);
    internal virtual IEnumerable<WideNavigationRailItem> NavigationItems => this.GetVisualDescendants().OfType<WideNavigationRailItem>();

    private sealed class WideNavigationRailAutomationPeer(WideNavigationRail owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.NavigationItems
            .Where(item => item.IsSelected).Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        protected override string GetClassNameCore() => owner.GetType().Name;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}


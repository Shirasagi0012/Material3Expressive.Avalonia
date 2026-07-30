// Upstream behavior source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// AppBar.kt:1384-1425, 2249-2375. Compose exposes collapse state through TopAppBarState;
// Avalonia maps it to the desktop ExpandCollapse pattern on the app-bar container.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialAppBarAutomationPeer :
    ControlAutomationPeer,
    IExpandCollapseProvider
{
    private AppBarScrollState _observedState;
    private ExpandCollapseState _lastState;

    public MaterialAppBarAutomationPeer(MaterialAppBar owner)
        : base(owner)
    {
        _observedState = owner.State;
        _lastState = ToAutomationState(_observedState.CollapsedFraction);
        _observedState.PropertyChanged += StatePropertyChanged;
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialAppBar Owner => (MaterialAppBar)base.Owner;

    public ExpandCollapseState ExpandCollapseState => _lastState;

    public bool ShowsMenu => false;

    public void Expand()
    {
        EnsureEnabled();
        _observedState.Expand();
    }

    public void Collapse()
    {
        EnsureEnabled();
        _observedState.Collapse();
    }

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Pane;

    protected override string? GetNameCore() =>
        AutomationProperties.GetName(Owner)
        ?? Owner.Title?.ToString()
        ?? base.GetNameCore();

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialAppBar.StateProperty)
            return;

        var old = _lastState;
        _observedState.PropertyChanged -= StatePropertyChanged;
        _observedState = Owner.State;
        _observedState.PropertyChanged += StatePropertyChanged;
        _lastState = ToAutomationState(_observedState.CollapsedFraction);
        if (old != _lastState)
        {
            RaisePropertyChangedEvent(
                ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
                old,
                _lastState);
        }
    }

    private void StatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppBarScrollState.CollapsedFraction))
            return;

        var next = ToAutomationState(_observedState.CollapsedFraction);
        var old = _lastState;
        if (old == next)
            return;
        _lastState = next;
        RaisePropertyChangedEvent(
            ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            old,
            next);
    }

    private static ExpandCollapseState ToAutomationState(double fraction) =>
        fraction switch
        {
            <= 0.001 => ExpandCollapseState.Expanded,
            >= 0.999 => ExpandCollapseState.Collapsed,
            _ => ExpandCollapseState.PartiallyExpanded
        };
}

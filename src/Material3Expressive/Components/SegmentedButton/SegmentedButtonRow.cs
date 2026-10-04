using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>Equal-weight, overlapping segments. The application owns selection.</summary>
public abstract class SegmentedButtonRow : ItemsControl
{
    public static readonly StyledProperty<double> SpaceProperty = AvaloniaProperty.Register<SegmentedButtonRow, double>(nameof(Space), 1);
    public static readonly StyledProperty<CornerRadius> BaseShapeProperty = AvaloniaProperty.Register<SegmentedButtonRow, CornerRadius>(nameof(BaseShape));
    /// <summary>Overlap; use the same value as the items' border width.</summary>
    public double Space { get => GetValue(SpaceProperty); set => SetValue(SpaceProperty, value); }
    public CornerRadius BaseShape { get => GetValue(BaseShapeProperty); set => SetValue(BaseShapeProperty, value); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SpaceProperty || change.Property == BaseShapeProperty || change.Property == FlowDirectionProperty)
            InvalidateItemsLayout();
    }
    internal void InvalidateItemsLayout()
    {
        foreach (var panel in this.GetVisualDescendants().OfType<SegmentedButtonItemsPanel>()) panel.InvalidateMeasure();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => this is SingleChoiceSegmentedButtonRow
        ? new SingleChoicePeer(this) : new ItemsControlAutomationPeer(this);
    private sealed class SingleChoicePeer(SegmentedButtonRow owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.GetVisualDescendants().OfType<SegmentedButton>()
            .Where(item => item.IsSelected).Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(SingleChoiceSegmentedButtonRow);
    }
}

public sealed class SingleChoiceSegmentedButtonRow : SegmentedButtonRow { }
public sealed class MultiChoiceSegmentedButtonRow : SegmentedButtonRow { }

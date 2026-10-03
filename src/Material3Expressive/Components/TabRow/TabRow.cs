using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>Material fixed or scrollable tabs. The application owns selection and content navigation.</summary>
public class TabRow : ItemsControl
{
    public static readonly StyledProperty<int> SelectedIndexProperty = AvaloniaProperty.Register<TabRow, int>(nameof(SelectedIndex));
    public static readonly AttachedProperty<TabRowVariant> VariantProperty = TabRowAppearance.VariantProperty;
    public static readonly AttachedProperty<bool> IsScrollableProperty = TabRowAppearance.IsScrollableProperty;
    public static readonly AttachedProperty<double> EdgePaddingProperty = TabRowAppearance.EdgePaddingProperty;
    public static readonly AttachedProperty<double> MinTabWidthProperty = TabRowAppearance.MinTabWidthProperty;
    public static readonly AttachedProperty<IControlTemplate?> IndicatorProperty = TabRowAppearance.IndicatorProperty;
    public static readonly AttachedProperty<IControlTemplate?> DividerProperty = TabRowAppearance.DividerProperty;
    public int SelectedIndex { get => GetValue(SelectedIndexProperty); set => SetValue(SelectedIndexProperty, value); }
    public TabRowVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    public bool IsScrollable { get => GetValue(IsScrollableProperty); set => SetValue(IsScrollableProperty, value); }
    public double EdgePadding { get => GetValue(EdgePaddingProperty); set => SetValue(EdgePaddingProperty, value); }
    public double MinTabWidth { get => GetValue(MinTabWidthProperty); set => SetValue(MinTabWidthProperty, value); }
    /// <summary>The indicator template, laid out at the animated selected position. Null removes it.</summary>
    public IControlTemplate? Indicator { get => GetValue(IndicatorProperty); set => SetValue(IndicatorProperty, value); }
    /// <summary>The bottom divider template. Null removes it.</summary>
    public IControlTemplate? Divider { get => GetValue(DividerProperty); set => SetValue(DividerProperty, value); }
    public IReadOnlyList<TabPosition> TabPositions => TabRowAppearance.GetTabPositions(this);

    protected override AutomationPeer OnCreateAutomationPeer() => new TabRowAutomationPeer(this);
    private sealed class TabRowAutomationPeer(TabRow owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.GetVisualDescendants().OfType<Tab>().Where(tab => tab.IsSelected)
            .Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;
        protected override string GetClassNameCore() => nameof(TabRow);
    }
}

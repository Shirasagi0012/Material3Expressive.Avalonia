using Material3Expressive.Foundation.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>The regular, narrow Material navigation rail. Selection belongs to the application.</summary>
public class NavigationRail : ItemsControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<NavigationRail, object?>(nameof(Header));
    public static readonly StyledProperty<IDataTemplate?> HeaderTemplateProperty =
        AvaloniaProperty.Register<NavigationRail, IDataTemplate?>(nameof(HeaderTemplate));
    public static readonly StyledProperty<Thickness> WindowInsetsProperty =
        AvaloniaProperty.Register<NavigationRail, Thickness>(nameof(WindowInsets));

    public object? Header { get => GetValue(HeaderProperty); set => SetValue(HeaderProperty, value); }
    public IDataTemplate? HeaderTemplate { get => GetValue(HeaderTemplateProperty); set => SetValue(HeaderTemplateProperty, value); }
    /// <summary>Physical insets supplied by the host. Compose Desktop system-bar insets are zero.</summary>
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }

    public NavigationRail() => MaterialWindowInsets.EnsureScope(this);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowInsetsProperty) SetValue(MaterialWindowInsets.AppliedInsetsProperty, WindowInsets);
    }
    protected override Size MeasureOverride(Size availableSize)
    {
        MaterialWindowInsets.Refresh(this);
        return base.MeasureOverride(availableSize);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new NavigationRailAutomationPeer(this);

    private sealed class NavigationRailAutomationPeer(NavigationRail owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.GetVisualDescendants().OfType<NavigationRailItem>()
            .Where(item => item.IsSelected).Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        protected override string GetClassNameCore() => nameof(NavigationRail);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}

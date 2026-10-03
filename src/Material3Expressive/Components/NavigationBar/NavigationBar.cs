using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>A Material bottom navigation bar. The application owns destination selection.</summary>
public class NavigationBar : ItemsControl
{
    public static readonly StyledProperty<Thickness> WindowInsetsProperty =
        AvaloniaProperty.Register<NavigationBar, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<double> TonalElevationProperty =
        MaterialSurface.TonalElevationProperty.AddOwner<NavigationBar>();

    /// <summary>Physical host insets; Compose Desktop defaults to zero.</summary>
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }

    protected override AutomationPeer OnCreateAutomationPeer() => new NavigationBarAutomationPeer(this);

    private sealed class NavigationBarAutomationPeer(NavigationBar owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.GetVisualDescendants().OfType<NavigationBarItem>()
            .Where(item => item.IsSelected).Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        protected override string GetClassNameCore() => nameof(NavigationBar);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}

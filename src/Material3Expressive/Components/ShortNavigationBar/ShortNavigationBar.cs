using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

public enum ShortNavigationBarArrangement { EqualWeight, Centered }

/// <summary>A short Material navigation bar. Selection belongs to the application.</summary>
public class ShortNavigationBar : ItemsControl
{
    public static readonly StyledProperty<Thickness> WindowInsetsProperty =
        AvaloniaProperty.Register<ShortNavigationBar, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<ShortNavigationBarArrangement> ArrangementProperty =
        AvaloniaProperty.Register<ShortNavigationBar, ShortNavigationBarArrangement>(nameof(Arrangement));
    /// <summary>Physical host insets; Compose Desktop defaults to zero.</summary>
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    public ShortNavigationBarArrangement Arrangement { get => GetValue(ArrangementProperty); set => SetValue(ArrangementProperty, value); }

    protected override AutomationPeer OnCreateAutomationPeer() => new ShortNavigationBarAutomationPeer(this);
    private sealed class ShortNavigationBarAutomationPeer(ShortNavigationBar owner) : ItemsControlAutomationPeer(owner), ISelectionProvider
    {
        public bool CanSelectMultiple => false;
        public bool IsSelectionRequired => false;
        public IReadOnlyList<AutomationPeer> GetSelection() => owner.GetVisualDescendants().OfType<ShortNavigationBarItem>()
            .Where(item => item.IsSelected).Select(ControlAutomationPeer.CreatePeerForElement).OfType<AutomationPeer>().ToArray();
        protected override string GetClassNameCore() => nameof(ShortNavigationBar);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}

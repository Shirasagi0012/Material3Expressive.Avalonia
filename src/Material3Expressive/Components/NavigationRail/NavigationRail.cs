using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.VisualTree;
using Avalonia.Data.Converters;
using System.Globalization;

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

internal sealed class NavigationRailInsetsConverter : IMultiValueConverter
{
    public static IMultiValueConverter Instance { get; } = new NavigationRailInsetsConverter();
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture) =>
        values.Count == 2 && values[0] is Thickness insets && values[1] is Avalonia.Media.FlowDirection direction
            // Border participates in Avalonia's RTL mirror. WindowInsets is physical,
            // so undo that one logical-to-physical exchange at the template boundary.
            ? direction == Avalonia.Media.FlowDirection.RightToLeft ? new Thickness(insets.Right, insets.Top, insets.Left, insets.Bottom) : insets
            : AvaloniaProperty.UnsetValue;
}

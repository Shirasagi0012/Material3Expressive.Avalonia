// SearchBar.kt / AppBarWithSearch, fixed AndroidX baseline; Apache-2.0.
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>A top app bar containing a search surface, navigation slot and action row.</summary>
public class AppBarWithSearch : ItemsControl
{
    public static readonly StyledProperty<SearchBarState?> StateProperty = SearchBar.StateProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<object?> InputFieldProperty = DockedSearchBar.InputFieldProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<IDataTemplate?> InputFieldTemplateProperty = DockedSearchBar.InputFieldTemplateProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<object?> NavigationIconProperty = TopAppBar.NavigationIconProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<IDataTemplate?> NavigationIconTemplateProperty = TopAppBar.NavigationIconTemplateProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<AppBarWithSearch, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<Thickness> WindowInsetsProperty = AvaloniaProperty.Register<AppBarWithSearch, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<Color> SearchBarContainerColorProperty = AvaloniaProperty.Register<AppBarWithSearch, Color>(nameof(SearchBarContainerColor));
    public static readonly StyledProperty<Color?> ScrolledSearchBarContainerColorProperty = AvaloniaProperty.Register<AppBarWithSearch, Color?>(nameof(ScrolledSearchBarContainerColor));
    public static readonly StyledProperty<Color?> ScrolledContainerColorProperty = AvaloniaProperty.Register<AppBarWithSearch, Color?>(nameof(ScrolledContainerColor));
    public static readonly StyledProperty<Color> NavigationIconContentColorProperty = TopAppBar.NavigationIconContentColorProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<Color> ActionIconContentColorProperty = TopAppBar.ActionIconContentColorProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<double> TonalElevationProperty = MaterialSurface.TonalElevationProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<double> ShadowElevationProperty = MaterialSurface.ShadowElevationProperty.AddOwner<AppBarWithSearch>();
    public static readonly StyledProperty<SearchBarScrollBehavior?> ScrollBehaviorProperty = AvaloniaProperty.Register<AppBarWithSearch, SearchBarScrollBehavior?>(nameof(ScrollBehavior));
    public SearchBarState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public object? InputField { get => GetValue(InputFieldProperty); set => SetValue(InputFieldProperty, value); }
    public IDataTemplate? InputFieldTemplate { get => GetValue(InputFieldTemplateProperty); set => SetValue(InputFieldTemplateProperty, value); }
    public object? NavigationIcon { get => GetValue(NavigationIconProperty); set => SetValue(NavigationIconProperty, value); }
    public IDataTemplate? NavigationIconTemplate { get => GetValue(NavigationIconTemplateProperty); set => SetValue(NavigationIconTemplateProperty, value); }
    public Thickness ContentPadding { get => GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    public Color SearchBarContainerColor { get => GetValue(SearchBarContainerColorProperty); set => SetValue(SearchBarContainerColorProperty, value); }
    public Color? ScrolledSearchBarContainerColor { get => GetValue(ScrolledSearchBarContainerColorProperty); set => SetValue(ScrolledSearchBarContainerColorProperty, value); }
    public Color? ScrolledContainerColor { get => GetValue(ScrolledContainerColorProperty); set => SetValue(ScrolledContainerColorProperty, value); }
    public Color NavigationIconContentColor { get => GetValue(NavigationIconContentColorProperty); set => SetValue(NavigationIconContentColorProperty, value); }
    public Color ActionIconContentColor { get => GetValue(ActionIconContentColorProperty); set => SetValue(ActionIconContentColorProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    public SearchBarScrollBehavior? ScrollBehavior { get => GetValue(ScrollBehaviorProperty); set => SetValue(ScrollBehaviorProperty, value); }
    private AppBarWithSearchLayout? _layout;
    internal SearchBar? CollapsedBar => _layout?.Bar;
    static AppBarWithSearch() => TemplateProperty.OverrideDefaultValue<AppBarWithSearch>(null);
    protected override Type StyleKeyOverride => typeof(AppBarWithSearch);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _layout = e.NameScope.Find<AppBarWithSearchLayout>("PART_Layout");
        base.OnApplyTemplate(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var property = change.Property;
        if (property.OwnerType == typeof(AppBarWithSearch) || property == StateProperty ||
            property == InputFieldProperty || property == InputFieldTemplateProperty ||
            property == NavigationIconProperty || property == NavigationIconTemplateProperty ||
            property == NavigationIconContentColorProperty || property == ActionIconContentColorProperty ||
            property == BackgroundProperty || property == TonalElevationProperty || property == ShadowElevationProperty ||
            property == FlowDirectionProperty || property == ItemCountProperty ||
            property == MaterialWindowInsets.ConsumedPaddingProperty)
            _layout?.OwnerChanged();
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SearchAppBarPeer(this);
    private sealed class SearchAppBarPeer(AppBarWithSearch owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string GetClassNameCore() => nameof(AppBarWithSearch);
    }
}

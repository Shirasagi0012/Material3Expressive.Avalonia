using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>A drawer destination. Content is the label; Click/Command never changes IsSelected.</summary>
public class NavigationDrawerItem : Button
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<NavigationDrawerItem, bool>(nameof(IsSelected));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<NavigationDrawerItem, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<NavigationDrawerItem, IDataTemplate?>(nameof(IconTemplate));
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public static readonly StyledProperty<object?> BadgeProperty = AvaloniaProperty.Register<NavigationDrawerItem, object?>(nameof(Badge));
    public static readonly StyledProperty<IDataTemplate?> BadgeTemplateProperty = AvaloniaProperty.Register<NavigationDrawerItem, IDataTemplate?>(nameof(BadgeTemplate));
    public object? Badge { get => GetValue(BadgeProperty); set => SetValue(BadgeProperty, value); }
    public IDataTemplate? BadgeTemplate { get => GetValue(BadgeTemplateProperty); set => SetValue(BadgeTemplateProperty, value); }
    public static readonly StyledProperty<Color> SelectedContainerColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(SelectedContainerColor));
    public Color SelectedContainerColor { get => GetValue(SelectedContainerColorProperty); set => SetValue(SelectedContainerColorProperty, value); }
    public static readonly StyledProperty<Color> UnselectedContainerColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(UnselectedContainerColor));
    public Color UnselectedContainerColor { get => GetValue(UnselectedContainerColorProperty); set => SetValue(UnselectedContainerColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedIconColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(SelectedIconColor));
    public Color SelectedIconColor { get => GetValue(SelectedIconColorProperty); set => SetValue(SelectedIconColorProperty, value); }
    public static readonly StyledProperty<Color> UnselectedIconColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(UnselectedIconColor));
    public Color UnselectedIconColor { get => GetValue(UnselectedIconColorProperty); set => SetValue(UnselectedIconColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedTextColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(SelectedTextColor));
    public Color SelectedTextColor { get => GetValue(SelectedTextColorProperty); set => SetValue(SelectedTextColorProperty, value); }
    public static readonly StyledProperty<Color> UnselectedTextColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(UnselectedTextColor));
    public Color UnselectedTextColor { get => GetValue(UnselectedTextColorProperty); set => SetValue(UnselectedTextColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedBadgeColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(SelectedBadgeColor));
    public Color SelectedBadgeColor { get => GetValue(SelectedBadgeColorProperty); set => SetValue(SelectedBadgeColorProperty, value); }
    public static readonly StyledProperty<Color> UnselectedBadgeColorProperty = AvaloniaProperty.Register<NavigationDrawerItem, Color>(nameof(UnselectedBadgeColor));
    public Color UnselectedBadgeColor { get => GetValue(UnselectedBadgeColorProperty); set => SetValue(UnselectedBadgeColorProperty, value); }
    private MaterialSurface? _surface;
    private ContentPresenter? _icon, _label, _badge;
    private Key? _activationKey;
    private bool _suppressClick;
    public NavigationDrawerItem() => LostFocus += (_, _) => _activationKey = null;
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _surface = e.NameScope.Find<MaterialSurface>("PART_Surface");
        _icon = e.NameScope.Find<ContentPresenter>("PART_Icon");
        _label = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _badge = e.NameScope.Find<ContentPresenter>("PART_Badge");
        Refresh();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty) PseudoClasses.Set(":selected", IsSelected);
        if (change.Property == IsSelectedProperty || change.Property == IconProperty || change.Property == IconTemplateProperty
            || change.Property.PropertyType == typeof(Color)) Refresh();
    }
    private void Refresh()
    {
        if (_surface is not null)
        {
            _surface.Background = new SolidColorBrush(IsSelected ? SelectedContainerColor : UnselectedContainerColor);
        }
        if (_icon is not null)
        {
            _icon.IsVisible = Icon is not null || IconTemplate is not null;
            _icon.Foreground = new SolidColorBrush(IsSelected ? SelectedIconColor : UnselectedIconColor);
        }
        if (_label is not null) _label.Foreground = new SolidColorBrush(IsSelected ? SelectedTextColor : UnselectedTextColor);
        if (_badge is not null) _badge.Foreground = new SolidColorBrush(IsSelected ? SelectedBadgeColor : UnselectedBadgeColor);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && IsFocused && IsEffectivelyEnabled)
        {
            _activationKey ??= e.Key; _suppressClick = true;
            try { base.OnKeyDown(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressClick = false; }
            e.Handled = true; return;
        }
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            var invoke = _activationKey == e.Key && IsFocused && IsEffectivelyEnabled;
            _activationKey = null; _suppressClick = true;
            try { base.OnKeyUp(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressClick = false; }
            e.Handled = true; if (invoke) OnClick(); return;
        }
        base.OnKeyUp(e);
    }
    protected override void OnClick() { if (!_suppressClick) base.OnClick(); }
    protected override AutomationPeer OnCreateAutomationPeer() => new ItemPeer(this);
    private sealed class ItemPeer(NavigationDrawerItem owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
    {
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.FindAncestorOfType<NavigationDrawer>() is { } drawer ? CreatePeerForElement(drawer) as ISelectionProvider : null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        public void RemoveFromSelection() { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
        protected override string? GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? owner.Content as string ?? base.GetNameCore() : base.GetNameCore();
    }
}

internal sealed class NavigationDrawerItemLayout : Panel
{
    private double _iconGap, _badgeGap;
    public static readonly StyledProperty<double> ContainerMinHeightProperty = AvaloniaProperty.Register<NavigationDrawerItemLayout, double>(nameof(ContainerMinHeight), 56);
    public double ContainerMinHeight { get => GetValue(ContainerMinHeightProperty); set => SetValue(ContainerMinHeightProperty, value); }
    static NavigationDrawerItemLayout() => AffectsMeasure<NavigationDrawerItemLayout>(ContainerMinHeightProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        var width = availableSize.Width;
        if (Children.Count != 3) return default;
        var icon = Children[0]; var label = Children[1]; var badge = Children[2];
        var remaining = Math.Max(0, width - 40);
        icon.Measure(new Size(remaining, availableSize.Height));
        remaining = Math.Max(0, remaining - icon.DesiredSize.Width);
        _iconGap = icon.IsVisible ? Math.Min(12, remaining) : 0; remaining -= _iconGap;
        // Row measures unweighted children in source order: icon, its spacer, badge spacer,
        // badge. The weighted label gets the remainder, including zero under tight constraints.
        _badgeGap = badge.IsVisible ? Math.Min(12, remaining) : 0; remaining -= _badgeGap;
        badge.Measure(new Size(remaining, availableSize.Height));
        label.Measure(new Size(Math.Max(0, remaining - badge.DesiredSize.Width), availableSize.Height));
        return new Size(double.IsFinite(width) ? width : 40 + icon.DesiredSize.Width + label.DesiredSize.Width + badge.DesiredSize.Width + _iconGap + _badgeGap,
            Math.Min(availableSize.Height, Math.Max(ContainerMinHeight, Children.Max(c => c.DesiredSize.Height))));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count != 3) return finalSize;
        var icon = Children[0]; var label = Children[1]; var badge = Children[2];
        var x = 16d;
        icon.Arrange(new Rect(x, (finalSize.Height - icon.DesiredSize.Height) / 2, icon.DesiredSize.Width, icon.DesiredSize.Height));
        if (icon.IsVisible) x += icon.DesiredSize.Width + _iconGap;
        var badgeWidth = badge.IsVisible ? badge.DesiredSize.Width + _badgeGap : 0;
        label.Arrange(new Rect(x, (finalSize.Height - label.DesiredSize.Height) / 2, Math.Max(0, finalSize.Width - x - 24 - badgeWidth), label.DesiredSize.Height));
        badge.Arrange(new Rect(finalSize.Width - 24 - badge.DesiredSize.Width, (finalSize.Height - badge.DesiredSize.Height) / 2, badge.DesiredSize.Width, badge.DesiredSize.Height));
        return finalSize;
    }
}

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>A Compose-style controlled Tab. Click and Command never mutate IsSelected.</summary>
public class Tab : Avalonia.Controls.Button
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<Tab, bool>(nameof(IsSelected));
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<Tab, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<Tab, IDataTemplate?>(nameof(IconTemplate));
    public static readonly AttachedProperty<TabContentLayout> ContentLayoutProperty = TabAppearance.ContentLayoutProperty;
    public static readonly AttachedProperty<Color?> SelectedContentColorProperty = TabAppearance.SelectedContentColorProperty;
    public static readonly AttachedProperty<Color?> UnselectedContentColorProperty = TabAppearance.UnselectedContentColorProperty;
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public TabContentLayout ContentLayout { get => GetValue(ContentLayoutProperty); set => SetValue(ContentLayoutProperty, value); }
    public Color? SelectedContentColor { get => GetValue(SelectedContentColorProperty); set => SetValue(SelectedContentColorProperty, value); }
    public Color? UnselectedContentColor { get => GetValue(UnselectedContentColorProperty); set => SetValue(UnselectedContentColorProperty, value); }
    private Key? _activationKey;
    private bool _suppressKeyboardClick;

    public Tab() => LostFocus += (_, _) => _activationKey = null;
    protected override Type StyleKeyOverride => typeof(Tab);
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", IsSelected);
            (ControlAutomationPeer.CreatePeerForElement(this) as TabAutomationPeer)?.RaisePropertyChangedEvent(
                SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
        }
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) _activationKey = null;
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter && IsFocused && IsEffectivelyEnabled)
        {
            _activationKey ??= e.Key;
            _suppressKeyboardClick = true;
            try { base.OnKeyDown(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true;
        }
        else base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            var invoke = _activationKey == e.Key && IsFocused && IsEffectivelyEnabled;
            _activationKey = null;
            _suppressKeyboardClick = true;
            try { base.OnKeyUp(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true;
            if (invoke) OnClick();
        }
        else base.OnKeyUp(e);
    }
    protected override void OnClick() { if (!_suppressKeyboardClick) base.OnClick(); }
    protected override AutomationPeer OnCreateAutomationPeer() => new TabAutomationPeer(this);
    private sealed class TabAutomationPeer(Tab owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
    {
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.FindAncestorOfType<TabRow>() is { } row ? ControlAutomationPeer.CreatePeerForElement(row) as ISelectionProvider : null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        public void RemoveFromSelection() { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
        protected override string GetClassNameCore() => nameof(Tab);
    }
}

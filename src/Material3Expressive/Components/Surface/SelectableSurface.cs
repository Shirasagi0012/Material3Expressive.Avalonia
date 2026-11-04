using Avalonia;
using Avalonia.Controls;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Components;

/// <summary>A clickable surface exposing an application-owned selection state.</summary>
public class SelectableSurface : ClickableSurface
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SelectableSurface, bool>(nameof(IsSelected));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty) PseudoClasses.Set(":selected", IsSelected);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SurfacePeer(this);
    private sealed class SurfacePeer : ButtonAutomationPeer, ISelectionItemProvider
    {
        private readonly SelectableSurface _owner;
        public SurfacePeer(SelectableSurface owner) : base(owner)
        {
            _owner = owner;
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsSelectedProperty)
                    RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
            };
        }
        public bool IsSelected => _owner.IsSelected;
        public ISelectionProvider? SelectionContainer => null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        public void RemoveFromSelection() { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;
    }
}

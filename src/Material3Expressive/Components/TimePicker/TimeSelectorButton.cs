// TimePicker.kt: TimeSelector's selected Surface and Role.RadioButton semantics.
// Fixed AndroidX baseline f3ed195f0c9e; Copyright The Android Open Source Project.
// Licensed under Apache-2.0 (LICENSE.txt).
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Material3Expressive.Components;

internal sealed class TimeSelectorButton : Button
{
    public static readonly DirectProperty<TimeSelectorButton, bool> IsSelectedProperty =
        AvaloniaProperty.RegisterDirect<TimeSelectorButton, bool>(nameof(IsSelected), x => x.IsSelected);
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetAndRaise(IsSelectedProperty, ref _isSelected, value); }
    protected override Type StyleKeyOverride => typeof(Button);
    protected override AutomationPeer OnCreateAutomationPeer() => new SelectorPeer(this);

    private sealed class SelectorPeer : ButtonAutomationPeer, ISelectionItemProvider
    {
        private readonly TimeSelectorButton _owner;
        public SelectorPeer(TimeSelectorButton owner) : base(owner)
        {
            _owner = owner;
            owner.PropertyChanged += (_, change) => {
                if (change.Property == IsSelectedProperty)
                    RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
            };
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;
        public bool IsSelected => _owner.IsSelected;
        public ISelectionProvider? SelectionContainer => null;
        public void Select() => ((IInvokeProvider)this).Invoke();
        public void AddToSelection() => Select();
        public void RemoveFromSelection() { }
    }
}

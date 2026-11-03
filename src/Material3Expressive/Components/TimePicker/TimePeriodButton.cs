// Portions translated from AndroidX TimePicker.kt (f3ed195f0c9e).
// Copyright 2023 The Android Open Source Project. Licensed under Apache-2.0 (LICENSE.txt).
// Fixed AndroidX TimePicker.kt: ToggleItem uses a controlled ToggleButton.
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls.Primitives;

namespace Material3Expressive.Components;

internal sealed class TimePeriodButton : ToggleButton
{
    protected override Type StyleKeyOverride => typeof(ToggleButton);
    // The controller owns AM/PM. A second activation of the selected period is a no-op.
    protected override void Toggle() { }
    protected override AutomationPeer OnCreateAutomationPeer() => new PeriodPeer(this);
    private sealed class PeriodPeer : ToggleButtonAutomationPeer, ISelectionItemProvider, IInvokeProvider
    {
        private readonly TimePeriodButton _owner;
        public PeriodPeer(TimePeriodButton owner) : base(owner)
        {
            _owner = owner;
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsCheckedProperty)
                    RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty,
                        change.OldValue is true, change.NewValue is true);
            };
        }
        public bool IsSelected => _owner.IsChecked == true;
        public ISelectionProvider? SelectionContainer => null;
        public void Invoke() => ((IToggleProvider)this).Toggle();
        public void Select() => ((IToggleProvider)this).Toggle();
        public void AddToSelection() => ((IToggleProvider)this).Toggle();
        public void RemoveFromSelection() { }
    }
}

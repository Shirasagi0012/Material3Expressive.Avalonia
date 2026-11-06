using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Components;

public partial class ScrollField
{
    protected override AutomationPeer OnCreateAutomationPeer() => new ScrollFieldPeer(this);
    private sealed class ScrollFieldPeer : ControlAutomationPeer, IRangeValueProvider
    {
        private readonly ScrollField _owner;
        private ScrollFieldState? State => _owner.State;
        private double _lastValue;
        private string? _lastDescription;
        public ScrollFieldPeer(ScrollField owner) : base(owner)
        {
            _owner = owner; owner.PropertyChanged += OwnerChanged; owner.SelectionChanged += Publish; Publish();
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Spinner;
        protected override string GetClassNameCore() => nameof(ScrollField);
        protected override IReadOnlyList<AutomationPeer> GetChildrenCore() => [];
        protected override string? GetItemStatusCore() => AutomationProperties.GetItemStatus(_owner) ??
            (State is null ? null : _owner.OptionDescription?.Invoke(State.TargetOption) ?? ScrollFieldNumber.Format(State.TargetOption, 1));
        protected override object? GetProviderCore(Type type) => type == typeof(IRangeValueProvider) ? this : base.GetProviderCore(type);
        private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == StateProperty || e.Property == OptionDescriptionProperty) Publish();
        }
        private void Publish()
        {
            var value = Value; var description = GetItemStatus();
            if (value != _lastValue) RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, _lastValue, value);
            if (description != _lastDescription) RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, _lastDescription, description);
            _lastValue = value; _lastDescription = description;
        }
        public bool IsReadOnly => !_owner.IsEffectivelyEnabled || State is null;
        public double Value => State?.TargetPage ?? 0;
        public double Minimum => 0;
        public double Maximum => ScrollFieldState.PageCount - 1;
        public double SmallChange => 1;
        public double LargeChange => 1;
        public void SetValue(double value)
        {
            EnsureEnabled();
            if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            if (State is null) return;
            var target = (int)Math.Clamp(Math.Floor(value + .5), Minimum, Maximum);
            if (target != State.TargetPage) _owner.AnimatePage(target);
        }
    }
}

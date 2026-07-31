// D004 maps every custom-drawn progress shape to the same UIA contract. Determinate indicators
// expose read-only RangeValue 0..1; indeterminate indicators expose status instead of a fake value.

using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialProgressIndicatorAutomationPeer :
    ControlAutomationPeer,
    IRangeValueProvider
{
    private double _lastValue;
    private volatile bool _lastIndeterminate;
    private string? _lastItemStatus;

    public MaterialProgressIndicatorAutomationPeer(Control owner)
        : base(owner)
    {
        _lastValue = ProgressValue;
        _lastIndeterminate = IsIndeterminate;
        _lastItemStatus = ResolveItemStatus(_lastIndeterminate);
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.ProgressBar;

    protected override string GetClassNameCore() => "ProgressBar";

    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

    protected override bool IsKeyboardFocusableCore() => false;

    protected override void SetFocusCore()
    {
    }

    protected override string? GetItemStatusCore() =>
        ResolveItemStatus(IsIndeterminate);

    protected override object? GetProviderCore(Type providerType) =>
        providerType == typeof(IRangeValueProvider)
            // Avalonia's Win32 bridge asks for provider availability on its COM thread rather
            // than through InvokeSync. Use the UI-thread-maintained snapshot instead of reading
            // an Avalonia property from that thread.
            ? _lastIndeterminate ? null : this
            : base.GetProviderCore(providerType);

    bool IRangeValueProvider.IsReadOnly => true;

    double IRangeValueProvider.Minimum => 0;

    double IRangeValueProvider.Maximum => 1;

    double IRangeValueProvider.Value => ProgressValue;

    double IRangeValueProvider.LargeChange => double.NaN;

    double IRangeValueProvider.SmallChange => double.NaN;

    void IRangeValueProvider.SetValue(double value) =>
        throw new InvalidOperationException("ProgressBar is read-only.");

    private double ProgressValue
    {
        get
        {
            var value = Owner switch
            {
                MaterialLinearProgressIndicator linear => linear.Value,
                MaterialCircularProgressIndicator circular => circular.Value,
                _ => 0
            };
            return double.IsNaN(value) ? 0 : Math.Clamp(value, 0, 1);
        }
    }

    private bool IsIndeterminate => Owner switch
    {
        MaterialLinearProgressIndicator linear => linear.IsIndeterminate,
        MaterialCircularProgressIndicator circular => circular.IsIndeterminate,
        _ => false
    };

    private string? ResolveItemStatus(bool isIndeterminate) =>
        AutomationProperties.GetItemStatus(Owner)
        ?? (isIndeterminate ? "In progress" : null);

    private bool IsValueProperty(AvaloniaProperty property) =>
        Owner switch
        {
            MaterialLinearProgressIndicator =>
                property == MaterialLinearProgressIndicator.ValueProperty,
            MaterialCircularProgressIndicator =>
                property == MaterialCircularProgressIndicator.ValueProperty,
            _ => false
        };

    private bool IsIndeterminateProperty(AvaloniaProperty property) =>
        Owner switch
        {
            MaterialLinearProgressIndicator =>
                property == MaterialLinearProgressIndicator.IsIndeterminateProperty,
            MaterialCircularProgressIndicator =>
                property == MaterialCircularProgressIndicator.IsIndeterminateProperty,
            _ => false
        };

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == AutomationProperties.ItemStatusProperty)
        {
            var explicitStatus = ResolveItemStatus(IsIndeterminate);
            if (!string.Equals(_lastItemStatus, explicitStatus, StringComparison.Ordinal))
            {
                RaisePropertyChangedEvent(
                    AutomationElementIdentifiers.ItemStatusProperty,
                    _lastItemStatus,
                    explicitStatus);
            }
            _lastItemStatus = explicitStatus;
            return;
        }

        if (IsValueProperty(e.Property))
        {
            var next = ProgressValue;
            if (!_lastIndeterminate && !next.Equals(_lastValue))
            {
                RaisePropertyChangedEvent(
                    RangeValuePatternIdentifiers.ValueProperty,
                    _lastValue,
                    next);
            }
            _lastValue = next;
            return;
        }

        if (!IsIndeterminateProperty(e.Property))
            return;

        var nextIndeterminate = IsIndeterminate;
        var nextStatus = ResolveItemStatus(nextIndeterminate);
        if (!string.Equals(_lastItemStatus, nextStatus, StringComparison.Ordinal))
        {
            RaisePropertyChangedEvent(
                AutomationElementIdentifiers.ItemStatusProperty,
                _lastItemStatus,
                nextStatus);
        }

        _lastIndeterminate = nextIndeterminate;
        _lastItemStatus = nextStatus;
        _lastValue = ProgressValue;
    }
}

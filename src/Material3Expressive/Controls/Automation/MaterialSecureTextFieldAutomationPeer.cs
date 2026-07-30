// Secure fields deliberately do not expose their value through UI Automation. Applications can
// still set the value through the standard Value pattern, matching a password-edit contract.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Material3Expressive.Controls.Automation;

internal sealed class MaterialSecureTextFieldAutomationPeer :
    ControlAutomationPeer,
    IValueProvider
{
    private string? _lastHelpText;
    private string? _lastItemStatus;

    public MaterialSecureTextFieldAutomationPeer(MaterialSecureTextField owner)
        : base(owner)
    {
        _lastHelpText = ResolveHelpText();
        _lastItemStatus = ResolveItemStatus();
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialSecureTextField Owner => (MaterialSecureTextField)base.Owner;

    public bool IsReadOnly => !Owner.IsEffectivelyEnabled;

    public string Value => string.Empty;

    public void SetValue(string? value)
    {
        EnsureEnabled();
        Owner.Text = value;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        AutomationControlType.Edit;

    protected override string GetClassNameCore() => nameof(MaterialSecureTextField);

    protected override string? GetNameCore() =>
        AutomationProperties.GetName(Owner)
        ?? Owner.Label
        ?? base.GetNameCore();

    protected override string? GetHelpTextCore() => ResolveHelpText();

    protected override string? GetItemStatusCore() => ResolveItemStatus();

    private string? ResolveHelpText() =>
        AutomationProperties.GetHelpText(Owner)
        ?? (Owner.IsError
            ? Owner.SupportingText ?? "Error"
            : Owner.SupportingText);

    private string? ResolveItemStatus() =>
        AutomationProperties.GetItemStatus(Owner)
        ?? (Owner.IsError ? "Invalid" : null);

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialTextField.LabelProperty
            && string.IsNullOrWhiteSpace(AutomationProperties.GetName(Owner)))
        {
            RaisePropertyChangedEvent(
                AutomationElementIdentifiers.NameProperty,
                e.GetOldValue<string?>(),
                GetName());
        }

        if (e.Property != MaterialTextField.IsErrorProperty
            && e.Property != MaterialTextField.SupportingTextProperty
            && e.Property != AutomationProperties.HelpTextProperty
            && e.Property != AutomationProperties.ItemStatusProperty)
        {
            return;
        }

        var helpText = ResolveHelpText();
        if (!string.Equals(_lastHelpText, helpText, StringComparison.Ordinal))
        {
            RaisePropertyChangedEvent(
                AutomationElementIdentifiers.HelpTextProperty,
                _lastHelpText,
                helpText);
            _lastHelpText = helpText;
        }

        var itemStatus = ResolveItemStatus();
        if (!string.Equals(_lastItemStatus, itemStatus, StringComparison.Ordinal))
        {
            RaisePropertyChangedEvent(
                AutomationElementIdentifiers.ItemStatusProperty,
                _lastItemStatus,
                itemStatus);
            _lastItemStatus = itemStatus;
        }
    }
}

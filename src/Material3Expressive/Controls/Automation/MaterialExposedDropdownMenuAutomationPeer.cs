using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;

namespace Material3Expressive.Controls.Automation;

/// <summary>
/// Retains Avalonia's native ComboBox automation patterns while adding Material field metadata.
/// </summary>
internal sealed class MaterialExposedDropdownMenuAutomationPeer : ComboBoxAutomationPeer
{
    private string? _lastHelpText;
    private string? _lastItemStatus;

    public MaterialExposedDropdownMenuAutomationPeer(MaterialExposedDropdownMenu owner)
        : base(owner)
    {
        _lastHelpText = ResolveHelpText();
        _lastItemStatus = ResolveItemStatus();
    }

    private new MaterialExposedDropdownMenu Owner =>
        (MaterialExposedDropdownMenu)base.Owner;

    protected override string GetClassNameCore() => nameof(MaterialExposedDropdownMenu);

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

    protected override void OwnerPropertyChanged(
        object? sender,
        AvaloniaPropertyChangedEventArgs e)
    {
        base.OwnerPropertyChanged(sender, e);

        if (e.Property == MaterialExposedDropdownMenu.LabelProperty
            && string.IsNullOrWhiteSpace(AutomationProperties.GetName(Owner)))
        {
            RaisePropertyChangedEvent(
                AutomationElementIdentifiers.NameProperty,
                e.GetOldValue<string?>(),
                GetName());
        }

        if (e.Property != MaterialExposedDropdownMenu.IsErrorProperty
            && e.Property != MaterialExposedDropdownMenu.SupportingTextProperty
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

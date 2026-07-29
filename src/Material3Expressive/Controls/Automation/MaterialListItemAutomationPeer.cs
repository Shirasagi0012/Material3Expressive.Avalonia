// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt.
// The executable reference is Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack alpha22:
// it can render the Click, SingleSelection, and MultiSelection semantics but not alpha25's
// modern non-interactive ListItem overload. None-mode parity is therefore unavailable.

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Material3Expressive.Controls.Automation;

/// <summary>
/// Exposes the semantic role of a <see cref="MaterialListItem"/> from its
/// <see cref="ListItemInteractionMode"/>, rather than from its ToggleButton implementation
/// detail.
/// </summary>
/// <remarks>
/// Upstream source: AndroidX <c>ff0a0a99</c>, Material3 <c>1.5.0-alpha25</c>,
/// <c>ListItem.kt</c>. The four ListItem overloads expose no action, a button action, radio
/// selection, and checkbox toggling respectively. The pinned alpha22 executable reference can
/// exercise the three interactive overloads, but not alpha25's modern non-interactive overload.
/// </remarks>
internal sealed class MaterialListItemAutomationPeer : ContentControlAutomationPeer,
    IInvokeProvider,
    IToggleProvider,
    ISelectionItemProvider
{
    public MaterialListItemAutomationPeer(MaterialListItem owner)
        : base(owner)
    {
        owner.PropertyChanged += OwnerPropertyChanged;
    }

    private new MaterialListItem Owner => (MaterialListItem)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner.InteractionMode switch
        {
            ListItemInteractionMode.Click => AutomationControlType.Button,
            ListItemInteractionMode.SingleSelection => AutomationControlType.RadioButton,
            ListItemInteractionMode.MultiSelection => AutomationControlType.CheckBox,
            _ => AutomationControlType.ListItem
        };

    protected override bool IsContentElementCore() => true;

    protected override bool IsControlElementCore() => true;

    /// <summary>
    /// Compose uses <c>semantics(mergeDescendants = true)</c> for ListItem. Keep the equivalent
    /// Avalonia peer flat so template presenters do not produce duplicate announcements.
    /// </summary>
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => null;

    protected override string? GetNameCore()
    {
        var explicitName = AutomationProperties.GetName(Owner);
        if (!string.IsNullOrWhiteSpace(explicitName))
            return explicitName;

        var text = new[]
            {
                DescribeContent(Owner.LeadingContent),
                DescribeContent(Owner.OverlineContent),
                DescribeContent(Owner.Content),
                DescribeContent(Owner.SupportingContent),
                DescribeContent(Owner.TrailingContent)
            }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return text.Length == 0 ? base.GetNameCore() : string.Join(" ", text);
    }

    /// <summary>
    /// Avalonia UI Automation has no secondary-invoke pattern corresponding to Compose's
    /// <c>onLongClickLabel</c>. Preserve an application-provided help text first; otherwise make
    /// the label discoverable as the desktop accessibility description of the holding action.
    /// </summary>
    protected override string? GetHelpTextCore()
    {
        var helpText = base.GetHelpTextCore();
        return !string.IsNullOrWhiteSpace(helpText) || !Owner.HasLongClickAction
            ? helpText
            : Owner.LongClickLabel;
    }

    protected override object? GetProviderCore(Type providerType)
    {
        if (providerType == typeof(IInvokeProvider))
            return Owner.InteractionMode == ListItemInteractionMode.Click ? this : null;

        if (providerType == typeof(ISelectionItemProvider))
            return Owner.InteractionMode == ListItemInteractionMode.SingleSelection ? this : null;

        if (providerType == typeof(IToggleProvider))
            return Owner.InteractionMode == ListItemInteractionMode.MultiSelection ? this : null;

        return base.GetProviderCore(providerType);
    }

    void IInvokeProvider.Invoke()
    {
        EnsureEnabled();
        Owner.InvokeFromAutomation();
    }

    ToggleState IToggleProvider.ToggleState => Owner.IsSelected ? ToggleState.On : ToggleState.Off;

    void IToggleProvider.Toggle()
    {
        EnsureEnabled();
        Owner.InvokeFromAutomation();
    }

    bool ISelectionItemProvider.IsSelected => Owner.IsSelected;

    ISelectionProvider? ISelectionItemProvider.SelectionContainer => null;

    void ISelectionItemProvider.AddToSelection()
    {
        EnsureEnabled();
        if (!Owner.IsSelected)
            throw new InvalidOperationException("The list item is not selected.");
    }

    void ISelectionItemProvider.RemoveFromSelection()
    {
        EnsureEnabled();
        if (Owner.IsSelected)
            throw new InvalidOperationException("The list item is selected.");
    }

    void ISelectionItemProvider.Select()
    {
        EnsureEnabled();
        Owner.InvokeFromAutomation();
    }

    private void OwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialListItem.IsSelectedProperty)
            return;

        var oldValue = e.GetOldValue<bool>();
        var newValue = e.GetNewValue<bool>();
        switch (Owner.InteractionMode)
        {
            case ListItemInteractionMode.SingleSelection:
                RaisePropertyChangedEvent(
                    SelectionItemPatternIdentifiers.IsSelectedProperty,
                    oldValue,
                    newValue);
                break;
            case ListItemInteractionMode.MultiSelection:
                RaisePropertyChangedEvent(
                    TogglePatternIdentifiers.ToggleStateProperty,
                    ToToggleState(oldValue),
                    ToToggleState(newValue));
                break;
        }
    }

    private static string? DescribeContent(object? content) => content switch
    {
        string text => text,
        TextBlock { Text: { } text } => text,
        Control control when !string.IsNullOrWhiteSpace(AutomationProperties.GetName(control)) =>
            AutomationProperties.GetName(control),
        _ => null
    };

    private static ToggleState ToToggleState(bool value) => value ? ToggleState.On : ToggleState.Off;
}

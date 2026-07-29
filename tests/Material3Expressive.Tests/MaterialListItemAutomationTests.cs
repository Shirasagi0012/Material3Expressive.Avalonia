using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Material3Expressive.Controls;
using Xunit;

namespace Material3Expressive.Tests;

public class MaterialListItemAutomationTests
{
    [AvaloniaTheory]
    [InlineData(ListItemInteractionMode.None, AutomationControlType.ListItem)]
    [InlineData(ListItemInteractionMode.Click, AutomationControlType.Button)]
    [InlineData(ListItemInteractionMode.SingleSelection, AutomationControlType.RadioButton)]
    [InlineData(ListItemInteractionMode.MultiSelection, AutomationControlType.CheckBox)]
    public void InteractionMode_ExposesOnlyItsMatchingAutomationRoleAndPattern(
        ListItemInteractionMode mode,
        AutomationControlType controlType)
    {
        var item = new MaterialListItem { InteractionMode = mode, Content = "Headline" };
        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(controlType, peer.GetAutomationControlType());
        Assert.Equal(mode == ListItemInteractionMode.Click, peer.GetProvider<IInvokeProvider>() is not null);
        Assert.Equal(mode == ListItemInteractionMode.SingleSelection, peer.GetProvider<ISelectionItemProvider>() is not null);
        Assert.Equal(mode == ListItemInteractionMode.MultiSelection, peer.GetProvider<IToggleProvider>() is not null);
    }

    [AvaloniaFact]
    public void InteractionMode_ChangesReevaluateTheExistingPeerRoleAndSupportedPattern()
    {
        var item = new MaterialListItem { InteractionMode = ListItemInteractionMode.Click };
        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
        Assert.NotNull(peer.GetProvider<IInvokeProvider>());

        item.InteractionMode = ListItemInteractionMode.MultiSelection;

        Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
        Assert.Null(peer.GetProvider<IInvokeProvider>());
        Assert.NotNull(peer.GetProvider<IToggleProvider>());

        item.InteractionMode = ListItemInteractionMode.None;

        Assert.Equal(AutomationControlType.ListItem, peer.GetAutomationControlType());
        Assert.Null(peer.GetProvider<IToggleProvider>());
    }

    [AvaloniaFact]
    public void Peer_MergesSlotTextIntoOneNameAndSuppressesTemplateChildren()
    {
        var item = new MaterialListItem
        {
            LeadingContent = "Leading",
            OverlineContent = "Overline",
            Content = new TextBlock { Text = "Headline" },
            SupportingContent = "Supporting",
            TrailingContent = "Trailing"
        };
        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal("Leading Overline Headline Supporting Trailing", peer.GetName());
        Assert.Empty(peer.GetChildren());

        AutomationProperties.SetName(item, "Explicit name");
        Assert.Equal("Explicit name", peer.GetName());
    }

    [AvaloniaFact]
    public void InvokeProvider_RaisesClickWithoutTurningAClickItemIntoASelection()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.Click
        };
        var invoked = 0;
        item.Click += (_, _) => invoked++;
        var provider = Assert.IsAssignableFrom<IInvokeProvider>(
            ControlAutomationPeer.CreatePeerForElement(item).GetProvider<IInvokeProvider>());

        provider.Invoke();

        Assert.Equal(1, invoked);
        Assert.False(item.IsSelected);
    }

    [AvaloniaFact]
    public void SelectionProvider_SelectsAndRaisesTheSelectionAutomationState()
    {
        var item = new MaterialListItem { InteractionMode = ListItemInteractionMode.SingleSelection };
        var peer = ControlAutomationPeer.CreatePeerForElement(item);
        var provider = Assert.IsAssignableFrom<ISelectionItemProvider>(peer.GetProvider<ISelectionItemProvider>());
        var clicks = 0;
        item.Click += (_, _) => clicks++;
        AutomationPropertyChangedEventArgs? change = null;
        peer.PropertyChanged += (_, args) =>
        {
            if (args.Property == SelectionItemPatternIdentifiers.IsSelectedProperty)
                change = args;
        };

        provider.Select();

        Assert.True(item.IsSelected);
        Assert.True(provider.IsSelected);
        Assert.Equal(1, clicks);
        Assert.Null(provider.SelectionContainer);
        Assert.NotNull(change);
        Assert.False(Assert.IsType<bool>(change.OldValue));
        Assert.True(Assert.IsType<bool>(change.NewValue));
    }

    [AvaloniaFact]
    public void ToggleProvider_TogglesAndRaisesTheToggleAutomationState()
    {
        var item = new MaterialListItem { InteractionMode = ListItemInteractionMode.MultiSelection };
        var peer = ControlAutomationPeer.CreatePeerForElement(item);
        var provider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetProvider<IToggleProvider>());
        var clicks = 0;
        item.Click += (_, _) => clicks++;
        AutomationPropertyChangedEventArgs? change = null;
        peer.PropertyChanged += (_, args) =>
        {
            if (args.Property == TogglePatternIdentifiers.ToggleStateProperty)
                change = args;
        };

        provider.Toggle();

        Assert.True(item.IsSelected);
        Assert.Equal(ToggleState.On, provider.ToggleState);
        Assert.Equal(1, clicks);
        Assert.NotNull(change);
        Assert.Equal(ToggleState.Off, Assert.IsType<ToggleState>(change.OldValue));
        Assert.Equal(ToggleState.On, Assert.IsType<ToggleState>(change.NewValue));
    }

    [AvaloniaFact]
    public void AutomationActions_RejectDisabledItems()
    {
        var multi = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.MultiSelection,
            IsEnabled = false
        };
        var toggle = Assert.IsAssignableFrom<IToggleProvider>(
            ControlAutomationPeer.CreatePeerForElement(multi).GetProvider<IToggleProvider>());

        Assert.Throws<ElementNotEnabledException>(toggle.Toggle);
        Assert.False(multi.IsSelected);
    }

    [AvaloniaFact]
    public void RadioSelectionProvider_RejectsAddAndRemoveWithoutASelectionContainer()
    {
        var item = new MaterialListItem { InteractionMode = ListItemInteractionMode.SingleSelection };
        var provider = Assert.IsAssignableFrom<ISelectionItemProvider>(
            ControlAutomationPeer.CreatePeerForElement(item).GetProvider<ISelectionItemProvider>());

        Assert.Throws<InvalidOperationException>(provider.AddToSelection);

        item.IsSelected = true;
        Assert.Throws<InvalidOperationException>(provider.RemoveFromSelection);
    }
}

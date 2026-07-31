using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Headless.XUnit;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialProgressIndicatorAutomationTests
{
    [AvaloniaFact]
    public void DeterminateLinearAndCircularVariantsExposeOneReadOnlyProgressBar()
    {
        AssertDeterminateContract(new MaterialLinearProgressIndicator { Value = .25 });
        AssertDeterminateContract(
            new MaterialLinearProgressIndicator { Value = .75, IsWavy = true });
        AssertDeterminateContract(new MaterialCircularProgressIndicator { Value = .4 });
        AssertDeterminateContract(
            new MaterialCircularProgressIndicator { Value = .6, IsWavy = true });
    }

    [AvaloniaFact]
    public void IndeterminateVariantsExposeStatusWithoutFabricatingRangeValue()
    {
        AssertIndeterminateContract(
            new MaterialLinearProgressIndicator { Value = .25, IsIndeterminate = true });
        AssertIndeterminateContract(
            new MaterialLinearProgressIndicator
            {
                Value = .75,
                IsIndeterminate = true,
                IsWavy = true
            });
        AssertIndeterminateContract(
            new MaterialCircularProgressIndicator { Value = .4, IsIndeterminate = true });
        AssertIndeterminateContract(
            new MaterialCircularProgressIndicator
            {
                Value = .6,
                IsIndeterminate = true,
                IsWavy = true
            });
    }

    [AvaloniaFact]
    public void DeterminateValueIsClampedAndRaisesRangeValueChanges()
    {
        var indicator = new MaterialCircularProgressIndicator { Value = .25, IsWavy = true };
        ThemeTestHelper.ShowWindow(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator);
        var changes = new List<AutomationPropertyChangedEventArgs>();
        peer.PropertyChanged += (_, change) => changes.Add(change);
        var provider = Assert.IsAssignableFrom<IRangeValueProvider>(
            peer.GetProvider<IRangeValueProvider>());

        indicator.Value = 2;

        Assert.Equal(1, provider.Value);
        Assert.Contains(changes, change =>
            change.Property == RangeValuePatternIdentifiers.ValueProperty
            && Equals(change.OldValue, .25d)
            && Equals(change.NewValue, 1d));

        indicator.Value = double.NaN;
        Assert.Equal(0, provider.Value);
        Assert.Throws<InvalidOperationException>(() => provider.SetValue(.5));
    }

    [AvaloniaFact]
    public void IndeterminateTransitionsRaiseStatusAndRestoreTheSameRangeProvider()
    {
        var indicator = new MaterialLinearProgressIndicator { Value = .5 };
        ThemeTestHelper.ShowWindow(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator);
        var provider = peer.GetProvider<IRangeValueProvider>();
        var changes = new List<AutomationPropertyChangedEventArgs>();
        peer.PropertyChanged += (_, change) => changes.Add(change);

        indicator.IsIndeterminate = true;

        Assert.Null(peer.GetProvider<IRangeValueProvider>());
        Assert.Equal("In progress", peer.GetItemStatus());
        Assert.Contains(changes, change =>
            change.Property == AutomationElementIdentifiers.ItemStatusProperty
            && change.OldValue is null
            && Equals(change.NewValue, "In progress"));

        indicator.Value = .75;
        Assert.DoesNotContain(changes, change =>
            change.Property == RangeValuePatternIdentifiers.ValueProperty);

        indicator.IsIndeterminate = false;

        Assert.Same(provider, peer.GetProvider<IRangeValueProvider>());
        Assert.Null(peer.GetItemStatus());
        Assert.Equal(.75, provider!.Value);
    }

    [AvaloniaFact]
    public void ExplicitItemStatusOverridesTheDefaultIndeterminateStatus()
    {
        var indicator = new MaterialCircularProgressIndicator
        {
            IsIndeterminate = true
        };
        AutomationProperties.SetItemStatus(indicator, "Downloading");
        ThemeTestHelper.ShowWindow(indicator);

        var peer = ControlAutomationPeer.CreatePeerForElement(indicator);

        Assert.Equal("Downloading", peer.GetItemStatus());
        indicator.IsIndeterminate = false;
        Assert.Equal("Downloading", peer.GetItemStatus());
    }

    private static void AssertDeterminateContract(Avalonia.Controls.Control indicator)
    {
        ThemeTestHelper.ShowWindow(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator);
        var provider = Assert.IsAssignableFrom<IRangeValueProvider>(
            peer.GetProvider<IRangeValueProvider>());

        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Equal("ProgressBar", peer.GetClassName());
        Assert.Empty(peer.GetChildren());
        Assert.False(peer.IsKeyboardFocusable());
        Assert.True(provider.IsReadOnly);
        Assert.Equal(0, provider.Minimum);
        Assert.Equal(1, provider.Maximum);
        Assert.True(double.IsNaN(provider.SmallChange));
        Assert.True(double.IsNaN(provider.LargeChange));
    }

    private static void AssertIndeterminateContract(Avalonia.Controls.Control indicator)
    {
        ThemeTestHelper.ShowWindow(indicator);
        var peer = ControlAutomationPeer.CreatePeerForElement(indicator);

        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Empty(peer.GetChildren());
        Assert.False(peer.IsKeyboardFocusable());
        Assert.Null(peer.GetProvider<IRangeValueProvider>());
        Assert.Equal("In progress", peer.GetItemStatus());
    }
}

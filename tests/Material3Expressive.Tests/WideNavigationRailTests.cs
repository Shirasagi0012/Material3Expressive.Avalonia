using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using System.Windows.Input;
using Xunit;

namespace Material3Expressive.Tests;

public class WideNavigationRailTests
{
    [AvaloniaTheory]
    [InlineData(false, true, 96, 52, 36, 4)]
    [InlineData(true, true, 116, 56, 36, 16)]
    [InlineData(false, false, 96, 56, 36, 16)]
    [InlineData(true, false, 96, 56, 36, 16)]
    public void StandaloneItem_UsesBothUpstreamMeasureBranches(bool expanded, bool label, double width, double height, double iconX, double iconY)
    {
        using var clock = new MotionTestClockScope();
        var item = Item(label); item.RailExpanded = expanded;
        var window = Show(item);
        try
        {
            Assert.Equal(new Size(width, height), item.Bounds.Size);
            var icon = Part<ContentPresenter>(item, "PART_Icon");
            Assert.Equal(new Rect(iconX, iconY, 24, 24), new Rect(icon.TranslatePoint(default, item)!.Value, icon.Bounds.Size));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false, 96, 64, 10)]
    [InlineData(true, 220, 56, 16)]
    public void InitialState_ControlsRailAndPropagatedItemConstraints(bool expanded, double width, double itemHeight, double iconY)
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail { State = new(expanded ? WideNavigationRailValue.Expanded : WideNavigationRailValue.Collapsed) };
        var item = Item(); rail.Items.Add(item);
        var window = Show(rail);
        try
        {
            Assert.Equal(new Size(width, 240), rail.Bounds.Size);
            Assert.Equal(itemHeight, item.Bounds.Height);
            Assert.Equal(44, item.TranslatePoint(default, rail)!.Value.Y);
            Assert.Equal(iconY, Part<ContentPresenter>(item, "PART_Icon").TranslatePoint(default, item)!.Value.Y);
            Assert.Equal(expanded, item.RailExpanded);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void HeaderAndCenterArrangement_UseWholeContainerCenter()
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail { Header = new Border { Width = 48, Height = 40 }, Arrangement = NavigationRailArrangement.Center };
        var item = Item(); rail.Items.Add(item);
        var window = Show(rail);
        try { Assert.Equal(88, item.TranslatePoint(default, rail)!.Value.Y); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StateReversal_CancelsPreviousOperationAndEndsAtRawTarget()
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail(); rail.Items.Add(Item());
        var window = Show(rail);
        try
        {
            var expanding = rail.State.ExpandAsync();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(64));
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
            Assert.True(rail.State.IsAnimating);
            Assert.Equal(WideNavigationRailValue.Collapsed, rail.State.CurrentValue);
            var collapsing = rail.State.CollapseAsync();
            for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.True(expanding.IsCanceled);
            Assert.True(collapsing.IsCompletedSuccessfully);
            Assert.Equal(WideNavigationRailValue.Collapsed, rail.State.CurrentValue);
            Assert.False(rail.State.IsAnimating);
            Assert.Equal(96, rail.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Reattachment_CancelsDetachedOperationsAndRestoresSavedTarget()
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail(); rail.Items.Add(Item());
        var window = Show(rail);
        try
        {
            var expanding = rail.State.ExpandAsync(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            var parent = (Grid)window.Content!; parent.Children.Remove(rail);
            Dispatcher.UIThread.RunJobs();
            Assert.True(expanding.IsCanceled);
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
            parent.Children.Add(rail); window.UpdateLayout();
            Assert.Equal(WideNavigationRailValue.Expanded, rail.State.CurrentValue);
            Assert.Equal(220, rail.Bounds.Width);
            Assert.Null(MotionTestClockScope.Error(rail));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClickAndAutomation_RequestNavigationWithoutOwningSelection()
    {
        using var clock = new MotionTestClockScope();
        var item = Item(); item.Label = "Home";
        var rail = new WideNavigationRail(); rail.Items.Add(item);
        var window = Show(rail); var clicks = 0;
        item.Click += (_, _) => clicks++;
        try
        {
            var position = item.TranslatePoint(new Point(48, 32), window)!.Value;
            window.MouseDown(position, MouseButton.Left); window.MouseUp(position, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.False(item.IsSelected);
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Equal(AutomationControlType.TabItem, peer.GetAutomationControlType());
            Assert.Equal("Home", peer.GetName());
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
            Assert.NotNull(selection.SelectionContainer);
            selection.Select(); Assert.Equal(2, clicks); Assert.False(item.IsSelected);
            item.IsSelected = true; Assert.Single(selection.SelectionContainer!.GetSelection());
            item.IsEnabled = false;
            window.MouseDown(position, MouseButton.Left); window.MouseUp(position, MouseButton.Left);
            Assert.Equal(2, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LiveTopSpaceToken_RemainsAContentPaddingDefault()
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail(); var item = Item(); rail.Items.Add(item);
        var window = Show(rail);
        try
        {
            window.Resources[MaterialTokens.Comp.WideNavigationRail.TopSpace] = 60d;
            window.UpdateLayout();
            Assert.Equal(new Thickness(0, 60, 0, 0), rail.ContentPadding);
            Assert.Equal(60, item.TranslatePoint(default, rail)!.Value.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void IndicatorPadding_DerivesFromLiveTokensAndHonorsLocalOverrides()
    {
        using var clock = new MotionTestClockScope();
        var item = Item(); var window = Show(item);
        try
        {
            window.Resources[MaterialTokens.Comp.WideNavigationRail.ActiveIndicatorWidth] = 64d;
            window.Resources[MaterialTokens.Comp.WideNavigationRail.TopIconIndicatorHeight] = 40d;
            window.Resources[MaterialTokens.Comp.WideNavigationRail.StartIconIndicatorHeight] = 60d;
            window.Resources[MaterialTokens.Comp.WideNavigationRail.StartIconIndicatorLeadingSpace] = 18d;
            window.Resources[MaterialTokens.Comp.WideNavigationRail.IconSize] = 20d;
            window.UpdateLayout();
            Assert.Equal(new Thickness(22, 10), item.CollapsedIndicatorPadding);
            Assert.Equal(new Thickness(18, 20), item.ExpandedIndicatorPadding);
            item.CollapsedIndicatorPadding = new Thickness(7, 3);
            window.Resources[MaterialTokens.Comp.WideNavigationRail.ActiveIndicatorWidth] = 72d;
            window.UpdateLayout();
            Assert.Equal(new Thickness(7, 3), item.CollapsedIndicatorPadding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DataTemplates_InheritExpansionAndProvideStringTypography()
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail
        {
            ItemsSource = new[] { "Home", "Search" }, Header = "Menu",
            HeaderTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text }),
            ItemTemplate = new FuncDataTemplate<string>((text, _) => new WideNavigationRailItem
            { Label = text, Icon = new Border { Width = 24, Height = 24 } })
        };
        var window = Show(rail);
        try
        {
            var items = rail.GetVisualDescendants().OfType<WideNavigationRailItem>().ToArray();
            Assert.Equal(2, items.Length);
            var label = Part<ContentPresenter>(items[0], "PART_Label").GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal("Home", label.Text); Assert.Equal(12, label.FontSize);
            Assert.Contains(rail.GetVisualDescendants().OfType<TextBlock>(), text => text.Text == "Menu");
            var task = rail.State.ExpandAsync();
            for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.True(task.IsCompletedSuccessfully);
            Assert.All(items, item => Assert.True(item.RailExpanded));
            Assert.Equal(14, label.FontSize);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void KeyboardCommand_InvokesOnReleaseAndHonorsCanExecute(Key key)
    {
        using var clock = new MotionTestClockScope();
        var command = new NavigationCommand(); var item = Item();
        item.Command = command; item.CommandParameter = "Home";
        var window = Show(item);
        try
        {
            item.Focus(NavigationMethod.Tab);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(0, command.Executions);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, command.Executions); Assert.Equal("Home", command.LastParameter);
            Assert.False(item.IsSelected);
            command.Allow = false;
            Assert.False(item.IsEffectivelyEnabled);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, command.Executions);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SharedState_RemainsActiveUntilLastConsumerDetaches()
    {
        using var clock = new MotionTestClockScope();
        var state = new WideNavigationRailState();
        var first = new WideNavigationRail { State = state }; first.Items.Add(Item());
        var second = new WideNavigationRail { State = state }; second.Items.Add(Item());
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Children = { first, second } };
        var window = Show(panel);
        try
        {
            var expanding = state.ExpandAsync(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            panel.Children.Remove(first); window.UpdateLayout();
            for (var i = 0; i < 60; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.True(expanding.IsCompletedSuccessfully);
            Assert.Equal(WideNavigationRailValue.Expanded, state.CurrentValue);
            var collapsing = state.CollapseAsync(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            panel.Children.Remove(second); Dispatcher.UIThread.RunJobs();
            Assert.True(collapsing.IsCanceled);
            Assert.False(state.IsAnimating);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void UnmountedState_SupportsSeedingAndRejectsAnimation()
    {
        var state = new WideNavigationRailState();
        Assert.Throws<InvalidOperationException>(() => { _ = state.ExpandAsync(); });
        Assert.True(state.SnapToAsync(WideNavigationRailValue.Expanded).IsCompletedSuccessfully);
        Assert.Equal(WideNavigationRailValue.Expanded, state.TargetValue);
        Assert.Equal(WideNavigationRailValue.Expanded, state.CurrentValue);
        Assert.False(state.IsAnimating);
    }

    [AvaloniaFact]
    public void TopPressMapping_RoundsTokenWidthToPhysicalPixels()
    {
        using var clock = new MotionTestClockScope();
        var item = Item(); var window = Show(item);
        try
        {
            window.Resources[MaterialTokens.Comp.WideNavigationRail.ActiveIndicatorWidth] = 56.25d;
            window.UpdateLayout();
            var mapped = MaterialInteraction.GetOrCreate(item).PressPositionMapper!(new Point(48, 32));
            Assert.Equal(new Point(28, 20), mapped);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ExpandingWideContent_UpdatesContainerWidthInTheSameLayoutPass()
    {
        using var clock = new MotionTestClockScope();
        var rail = new WideNavigationRail { Header = new Border { Width = 280, Height = 40 } };
        var item = Item(); item.Label = new Border { Width = 240, Height = 16 }; rail.Items.Add(item);
        var window = Show(rail);
        try
        {
            _ = rail.State.ExpandAsync();
            for (var i = 0; i < 12; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); }
            Assert.True(item.Bounds.Width > 280, $"Item: {item.Bounds}; rail: {rail.Bounds}");
            // The expanding content is wider than the header and below the animated
            // width limit: its measured width plus the upstream 20dp determines the rail.
            Assert.Equal(item.Bounds.Width + 20, rail.Bounds.Width);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PositionChange_RecreatesIndicationWithoutCancelingButtonActivation()
    {
        using var clock = new MotionTestClockScope();
        var item = Item(); var rail = new WideNavigationRail(); rail.Items.Add(item);
        var window = Show(rail); var clicks = 0; item.Click += (_, _) => clicks++;
        try
        {
            var point = item.TranslatePoint(new Point(48, 32), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left);
            var interaction = MaterialInteraction.GetOrCreate(item);
            Assert.Equal(1, interaction.RippleCount);
            Assert.True(item.IsPressed);
            _ = rail.State.ExpandAsync();
            window.UpdateLayout();
            Assert.Equal(0, interaction.RippleCount);
            Assert.True(item.IsPressed);
            window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.False(item.IsSelected);
            Assert.Null(MotionTestClockScope.Error(item));
        }
        finally { window.Close(); }
    }

    sealed class NavigationCommand : ICommand
    {
        private bool _allow = true;
        public bool Allow { get => _allow; set { _allow = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public int Executions { get; private set; }
        public object? LastParameter { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Allow;
        public void Execute(object? parameter) { Executions++; LastParameter = parameter; }
    }

    static WideNavigationRailItem Item(bool label = true) => new()
    {
        Icon = new Border { Width = 24, Height = 24 },
        Label = label ? new Border { Width = 32, Height = 16 } : null,
        HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
    };
    static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 520, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } };
        window.Show(); window.UpdateLayout();
        return window;
    }
    static T Part<T>(Control owner, string name) where T : Control => owner.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);
}


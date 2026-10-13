using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class NavigationDrawerTests
{
    [AvaloniaTheory]
    [InlineData("modal", true, 0, 520)]
    [InlineData("modal", false, 0, 520)]
    [InlineData("dismissible", true, 360, 520)]
    [InlineData("dismissible", false, 0, 520)]
    [InlineData("permanent", true, 360, 160)]
    public void Containers_PreserveOverlayPushAndRemainingWidth(string kind, bool open, double pageX, double pageWidth)
    {
        using var clock = new MotionTestClockScope();
        var drawer = Create(kind, open); var window = Show(drawer);
        try
        {
            var page = (Control)drawer.Content!; var sheet = (DrawerSheet)drawer.DrawerContent!;
            Assert.Empty(page.GetVisualAncestors().Prepend(page).OfType<Control>().Where(v => v.ClipToBounds && v != window).Select(v => v.GetType().Name));
            Assert.Equal(360, sheet.Bounds.Width); Assert.Equal(300, sheet.Bounds.Height);
            Assert.False(sheet.ClipToBounds); // Surface may stretch past the sheet when opening overshoots.
            Assert.Equal(pageX, page.TranslatePoint(default, window)!.Value.X); Assert.Equal(pageWidth, page.Bounds.Width);
            if (drawer is DismissibleNavigationDrawer d) Assert.Equal(open ? 0 : -360, d.State.CurrentOffset);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(null, 240)]
    [InlineData(100d, 100)]
    [InlineData(420d, 420)]
    public void Sheet_AutoMinimumRespectsFixedIncomingConstraints(double? width, double expected)
    {
        using var clock = new MotionTestClockScope();
        var sheet = new ModalDrawerSheet { HorizontalAlignment = HorizontalAlignment.Left };
        if (width is { } w) sheet.Width = w;
        var window = Show(sheet);
        try { Assert.Equal(expected, sheet.Bounds.Width); Assert.Equal(300, sheet.Bounds.Height); }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData("modal", 360)]
    [InlineData("dismissible", 100)]
    [InlineData("permanent", 460)]
    public void UnboundedMeasure_PreservesBoxContentAndRowWidths(string kind, double expected)
    {
        using var clock = new MotionTestClockScope();
        var drawer = Create(kind, true); drawer.Content = new Border { Width = 100, Height = 80 };
        var window = Show(new StackPanel { Orientation = Orientation.Horizontal, Children = { drawer } });
        try { Assert.Equal(expected, drawer.DesiredSize.Width); }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void HoistedState_IsIndependentAndInitiallyUnmeasured()
    {
        var first = new ModalNavigationDrawer(); var second = new ModalNavigationDrawer();
        Assert.NotSame(first.State, second.State); Assert.True(double.IsNaN(first.State.CurrentOffset));
        first.State.SnapToAsync(DrawerValue.Open).GetAwaiter().GetResult(); Assert.True(first.State.IsOpen); Assert.True(second.State.IsClosed);
    }
    [AvaloniaFact]
    public void ProgrammaticReversal_CancelsPreviousAndPreservesSettledStateUntilCompletion()
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", false); var window = Show(drawer);
        try
        {
            var opening = drawer.State.OpenAsync(); Advance(clock, window, 5);
            Assert.True(drawer.State.IsClosed); Assert.Equal(DrawerValue.Open, drawer.State.TargetValue); Assert.True(drawer.State.IsAnimationRunning);
            var closing = drawer.State.CloseAsync(); Advance(clock, window, 70);
            Assert.True(opening.IsCanceled); Assert.True(closing.IsCompletedSuccessfully);
            Assert.True(drawer.State.IsClosed); Assert.Equal(-360, drawer.State.CurrentOffset); Assert.False(drawer.State.IsAnimationRunning);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void AnchorChanges_RestartActiveMutationWithClosestNewAnchor()
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", false); var window = Show(drawer);
        try
        {
            var operation = drawer.State.OpenAsync(); Advance(clock, window, 9);
            ((DrawerSheet)drawer.DrawerContent!).Width = 260; window.UpdateLayout(); Advance(clock, window, 70);
            Assert.True(operation.IsCompletedSuccessfully); Assert.Equal(0, drawer.State.CurrentOffset); Assert.True(drawer.State.IsOpen);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Unload_CancelsStateAndSheetSubscriptionsThenCanReattach()
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", false); var window = Show(drawer);
        var opening = drawer.State.OpenAsync(); Advance(clock, window, 4);
        window.Content = null; Dispatcher.UIThread.RunJobs();
        Assert.True(opening.IsCanceled); Assert.False(drawer.State.IsAnimationRunning);
        Assert.Throws<InvalidOperationException>(() => { _ = drawer.State.OpenAsync(); });
        window.Content = drawer; window.UpdateLayout(); var closing = drawer.State.CloseAsync(); Advance(clock, window, 70);
        Assert.True(closing.IsCompletedSuccessfully); Assert.Equal(-360, drawer.State.CurrentOffset); window.Close();
    }
    [AvaloniaTheory]
    [InlineData(60, 0, DrawerValue.Open)]
    [InlineData(240, 0, DrawerValue.Closed)]
    [InlineData(60, -500, DrawerValue.Closed)]
    [InlineData(240, 500, DrawerValue.Open)]
    public void DragSettlement_UsesPositionAndVelocityBranches(double distance, float velocity, DrawerValue target)
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("dismissible", true); var window = Show(drawer);
        try
        {
            drawer.State.BeginDrag(); drawer.State.DragBy(-(float)distance);
            Assert.Equal(DrawerValue.Open, drawer.State.CurrentValue); Assert.False(drawer.State.IsAnimationRunning);
            var settled = drawer.State.EndDrag(velocity); Assert.Equal(target, drawer.State.TargetValue); Advance(clock, window, 70);
            Assert.True(settled.IsCompletedSuccessfully); Assert.Equal(target, drawer.State.CurrentValue); Assert.Equal(target == DrawerValue.Open ? 0 : -360, drawer.State.CurrentOffset);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Veto_PreservesUpstreamProgrammaticOffsetAndSettledValueDistinction()
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", false); drawer.State = new(DrawerValue.Closed, _ => false);
        var window = Show(drawer);
        try
        {
            var opening = drawer.State.OpenAsync(); Advance(clock, window, 70);
            Assert.True(opening.IsCompletedSuccessfully); Assert.True(drawer.State.IsClosed);
            Assert.Equal(0, drawer.State.CurrentOffset); Assert.Equal(DrawerValue.Open, drawer.State.TargetValue);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void BackHandling_IsOptInThroughSheetState(bool stateful, bool handled)
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", true);
        var sheet = (ModalDrawerSheet)drawer.DrawerContent!; sheet.State = stateful ? drawer.State : null;
        var window = Show(drawer);
        try
        {
            var back = new RoutedEventArgs(TopLevel.BackRequestedEvent); window.RaiseEvent(back);
            Assert.Equal(handled, back.Handled); Advance(clock, window, 70); Assert.Equal(!stateful, drawer.State.IsOpen);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ClosedDrawer_IsExcludedFromTabNavigation_AndOpenMovesFocusToDestination()
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", false); var window = Show(drawer);
        try
        {
            var presenter = drawer.GetVisualDescendants().OfType<ContentPresenter>().Single(p => p.Name == "PART_Drawer");
            Assert.Equal(KeyboardNavigationMode.None, KeyboardNavigation.GetTabNavigation(presenter));
            _ = drawer.State.OpenAsync(); Advance(clock, window, 70);
            Assert.Equal(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(presenter));
            Assert.True(drawer.GetVisualDescendants().OfType<NavigationDrawerItem>().Single().IsFocused);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void SnapOpen_FocusRunsAfterCompositionFrame()
    {
        using var clock = new MotionTestClockScope();
        var drawer = (DismissibleNavigationDrawer)Create("modal", false); var window = Show(drawer);
        try
        {
            var item = drawer.GetVisualDescendants().OfType<NavigationDrawerItem>().Single();
            drawer.State.SnapToAsync(DrawerValue.Open).GetAwaiter().GetResult(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.False(item.IsFocused); Advance(clock, window, 1); Assert.True(item.IsFocused);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Item_UsesSeparateSlotsAndExternalSelectionForClickAndAutomation()
    {
        using var clock = new MotionTestClockScope();
        var drawer = Create("permanent", true); var item = ((StackPanel)((DrawerSheet)drawer.DrawerContent!).Content!).Children.OfType<NavigationDrawerItem>().Single();
        var clicks = 0; item.Click += (_, _) => clicks++;
        var window = Show(drawer);
        try
        {
            var p = item.TranslatePoint(new Point(80, 28), window)!.Value;
            window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); Assert.Equal(1, clicks); Assert.False(item.IsSelected);
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!; Assert.Equal(AutomationControlType.TabItem, peer.GetAutomationControlType());
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer); selection.Select(); Assert.Equal(2, clicks); Assert.False(item.IsSelected);
            item.IsSelected = true; Assert.Single(selection.SelectionContainer!.GetSelection());
            item.Focus(NavigationMethod.Tab); window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); Assert.Equal(2, clicks);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null); Assert.Equal(3, clicks);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void TokenOverrides_AreLiveAndBadgeDefaultsFollowTextColor()
    {
        using var clock = new MotionTestClockScope();
        var drawer = Create("permanent", true); var window = Show(drawer);
        try
        {
            var item = drawer.GetVisualDescendants().OfType<NavigationDrawerItem>().Single(); item.IsSelected = true;
            item.SelectedTextColor = Colors.Red; Assert.Equal(Colors.Red, item.SelectedBadgeColor);
            var surface = item.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.True(TokenResolver.TryResolve(window, MaterialTokens.Sys.Color.OnSecondaryContainer, window.ActualThemeVariant, out var ink));
            Assert.Equal((Color)ink!, ((ISolidColorBrush)surface.StateLayerBrush!).Color);
            item.IsSelected = false;
            Assert.Equal(((ISolidColorBrush)((DrawerSheet)drawer.DrawerContent!).Foreground!).Color, ((ISolidColorBrush)surface.StateLayerBrush!).Color);
            window.Resources[MaterialTokens.Comp.NavigationDrawer.ContainerWidth] = 300d; window.UpdateLayout();
            Assert.Equal(300, ((DrawerSheet)drawer.DrawerContent!).Bounds.Width);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ItemHeldPress_KeepsItsCaptureUntilChildProcessesRelease()
    {
        using var clock = new MotionTestClockScope();
        var drawer = Create("modal", true); var window = Show(drawer);
        try
        {
            var item = drawer.GetVisualDescendants().OfType<NavigationDrawerItem>().Single();
            var clicks = 0; item.Click += (_, _) => clicks++;
            var p = item.TranslatePoint(new Point(80, 28), window)!.Value;
            window.MouseMove(p); window.MouseDown(p, MouseButton.Left); Advance(clock, window, 10);
            window.MouseUp(p, MouseButton.Left); Assert.Equal(1, clicks); Assert.False(item.IsSelected);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void BothTemplates_ConsumeDataAndKeepInheritedResourcesWhenContentChanges()
    {
        using var clock = new MotionTestClockScope();
        var drawer = new PermanentNavigationDrawer {
            Content = "First page", ContentTemplate = new FuncDataTemplate<string>((text, _) => new TextBlock { Text = text }),
            DrawerContent = "Inbox", DrawerContentTemplate = new FuncDataTemplate<string>((text, _) => new PermanentDrawerSheet {
                Content = new StackPanel { Children = { new NavigationDrawerItem { Content = text } } }
            })
        };
        var window = Show(drawer);
        try
        {
            Assert.Contains(drawer.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "First page");
            Assert.Equal("Inbox", drawer.GetVisualDescendants().OfType<NavigationDrawerItem>().Single().Content);
            window.Resources[MaterialTokens.Comp.NavigationDrawer.ContainerWidth] = 280d;
            drawer.Content = "Second page"; drawer.DrawerContent = "Sent"; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.Contains(drawer.GetVisualDescendants().OfType<TextBlock>(), t => t.Text == "Second page");
            Assert.Equal("Sent", drawer.GetVisualDescendants().OfType<NavigationDrawerItem>().Single().Content);
            Assert.Equal(280, drawer.GetVisualDescendants().OfType<PermanentDrawerSheet>().Single().Bounds.Width);
        }
        finally { window.Close(); }
    }
    private static NavigationDrawer Create(string kind, bool open)
    {
        var state = new DrawerState(open ? DrawerValue.Open : DrawerValue.Closed);
        NavigationDrawer drawer = kind switch { "modal" => new ModalNavigationDrawer { State = state }, "dismissible" => new DismissibleNavigationDrawer { State = state }, _ => new PermanentNavigationDrawer() };
        DrawerSheet sheet = kind switch { "modal" => new ModalDrawerSheet { State = state }, "dismissible" => new DismissibleDrawerSheet { State = state }, _ => new PermanentDrawerSheet() };
        sheet.Content = new StackPanel { Children = { new NavigationDrawerItem { Content = "Home", Icon = new Border { Width = 24, Height = 24 }, Badge = "8" } } };
        drawer.DrawerContent = sheet; drawer.Content = new Fill(); return drawer;
    }
    private sealed class Fill : Control { protected override Size MeasureOverride(Size size) => size; }
    private static MotionTestWindow Show(Control control) => DialogTests.Show(control);
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    }
}

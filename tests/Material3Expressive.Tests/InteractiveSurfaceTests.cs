using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class InteractiveSurfaceTests
{
    [AvaloniaTheory]
    [InlineData("click", false)]
    [InlineData("select", false)]
    [InlineData("toggle", false)]
    [InlineData("click", true)]
    [InlineData("select", true)]
    [InlineData("toggle", true)]
    public void SimultaneousActivationKeysReleaseIndependently(string kind, bool reverseRelease)
    {
        using var clock = new MotionTestClockScope();
        var surface = Create(kind); surface.Content = "Surface";
        var source = new MaterialInteractionSource(); surface.InteractionSource = source;
        var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var window = DialogTests.Show(surface); var clicks = 0;
        surface.Click += (_, _) => clicks++;
        try
        {
            surface.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(0, clicks); Assert.Equal(2, events.OfType<MaterialInteractionEvent.Press>().Count());
            window.KeyRelease(reverseRelease ? Key.Enter : Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, clicks); Assert.True(surface.IsPressed);
            window.KeyRelease(reverseRelease ? Key.Space : Key.Enter, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(2, clicks); Assert.False(surface.IsPressed);
            Assert.Equal(2, events.OfType<MaterialInteractionEvent.Release>().Count());
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(2, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void SourceReplacementCancelsPendingKeyboardActivation(Key key)
    {
        using var clock = new MotionTestClockScope();
        var surface = new ClickableSurface { Content = "Surface", InteractionSource = new() };
        var window = DialogTests.Show(surface); var clicks = 0; surface.Click += (_, _) => clicks++;
        try
        {
            surface.Focus(NavigationMethod.Tab);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            surface.InteractionSource = new();
            Assert.True(surface.IsFocused); Assert.False(surface.IsPressed);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(0, clicks);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("click")]
    [InlineData("select")]
    [InlineData("toggle")]
    public void ChildButtonOwnsPointerAndKeyboardActivation(string kind)
    {
        using var clock = new MotionTestClockScope();
        var child = new Button { Content = "Child" };
        var surface = Create(kind); surface.Content = child; surface.Padding = new Thickness(16);
        var activations = 0; var childActivations = 0;
        surface.Command = new RelayCommand(() => activations++);
        child.Command = new RelayCommand(() => childActivations++);
        var window = DialogTests.Show(surface);
        try
        {
            Click(window, child); Assert.Equal(1, childActivations); Assert.Equal(0, activations);
            Assert.True(child.Focus(NavigationMethod.Tab));
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(2, childActivations); Assert.Equal(0, activations); Assert.False(surface.IsPressed);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void LosingFocusCancelsKeyboardActivationAndPairedPress(Key key)
    {
        using var clock = new MotionTestClockScope();
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var surface = new ClickableSurface { Content = "Surface", InteractionSource = source };
        var other = new Button { Content = "Other" };
        var window = DialogTests.Show(new StackPanel { Children = { surface, other } });
        var activations = 0; surface.Click += (_, _) => activations++;
        try
        {
            Assert.True(surface.Focus(NavigationMethod.Tab));
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            Assert.True(other.Focus(NavigationMethod.Tab)); Flush(window);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
            Assert.True(surface.Focus(NavigationMethod.Tab));
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(0, activations); Assert.False(surface.IsPressed);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("click")]
    [InlineData("select")]
    [InlineData("toggle")]
    public void DefaultsKeepContentSizeAndReserveMinimumInteractionSpace(string kind)
    {
        using var clock = new MotionTestClockScope();
        var surface = Create(kind); surface.Content = new Border { Width = 20, Height = 12 };
        var window = DialogTests.Show(surface); Flush(window);
        try
        {
            var container = Container(surface);
            Assert.Equal(new Size(48, 48), surface.Bounds.Size);
            Assert.Equal(new Size(20, 12), container.Bounds.Size);
            Assert.Equal(new Thickness(0), surface.Padding);
            Assert.Equal(default, surface.CornerRadius);
            Assert.Equal(0, container.ShadowElevation); Assert.Equal(0, container.TonalElevation);
            surface.IsEnabled = false; Flush(window); Assert.Equal(new Size(48, 48), surface.Bounds.Size);
            MaterialInteractiveSize.SetMinimum(surface, 64); Flush(window);
            Assert.Equal(new Size(64, 64), surface.Bounds.Size);
            Assert.Equal(new Size(20, 12), container.Bounds.Size);
            MaterialInteractiveSize.SetMinimum(surface, 0); Flush(window);
            Assert.Equal(new Size(20, 12), surface.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BorderDoesNotConsumeSpaceAndExplicitSizePropagatesToContent()
    {
        using var clock = new MotionTestClockScope();
        var child = new Border();
        var surface = new ClickableSurface { Width = 120, Height = 64, Content = child,
            BorderThickness = new Thickness(4), BorderBrush = Brushes.Red, Padding = new Thickness(8),
            CornerRadius = new CornerRadius(16), TonalElevation = 3, ShadowElevation = 6 };
        var window = DialogTests.Show(surface); Flush(window);
        try
        {
            Assert.Equal(new Size(120, 64), Container(surface).Bounds.Size);
            Assert.Equal(new Size(104, 48), child.Bounds.Size);
            Assert.Equal(3, Container(surface).TonalElevation); Assert.Equal(6, Container(surface).ShadowElevation);
            surface.BorderThickness = new Thickness(12); Flush(window);
            Assert.Equal(new Size(104, 48), child.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContentColorTracksThemeRolesAmbientFallbackAndExplicitForeground()
    {
        using var clock = new MotionTestClockScope();
        var surface = new ClickableSurface { Content = new Border { Width = 80, Height = 40 } };
        var parent = new StackPanel { Children = { surface } };
        Avalonia.Controls.Documents.TextElement.SetForeground(parent, Brushes.Magenta);
        var window = DialogTests.Show(parent);
        try
        {
            window.Resources[MaterialTokens.Sys.Color.Primary] = Colors.Red;
            window.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Yellow;
            surface.Background = Brushes.Red; Flush(window);
            Assert.Equal(Colors.Yellow, Assert.IsAssignableFrom<ISolidColorBrush>(surface.Foreground).Color);
            surface.Background = Brushes.Lime; Flush(window);
            Assert.Equal(Colors.Magenta, Assert.IsAssignableFrom<ISolidColorBrush>(surface.Foreground).Color);
            surface.Foreground = Brushes.Cyan; surface.Background = Brushes.Red; Flush(window);
            Assert.Equal(Colors.Cyan, Assert.IsAssignableFrom<ISolidColorBrush>(surface.Foreground).Color);
            surface.ClearValue(ClickableSurface.ForegroundProperty); Flush(window);
            Assert.Equal(Colors.Yellow, Assert.IsAssignableFrom<ISolidColorBrush>(surface.Foreground).Color);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("click", Key.Enter)]
    [InlineData("click", Key.Space)]
    [InlineData("select", Key.Enter)]
    [InlineData("toggle", Key.Enter)]
    [InlineData("toggle", Key.Space)]
    public void KeyboardActivatesOnceOnReleaseAndCancelsOnDisable(string kind, Key key)
    {
        using var clock = new MotionTestClockScope();
        var surface = Create(kind); surface.Content = new Border { Width = 80, Height = 40 };
        var window = DialogTests.Show(surface); var clicks = 0; var requests = 0;
        try
        {
            surface.Click += (_, _) => clicks++;
            if (surface is ToggleableSurface toggle) toggle.CheckedChangeRequested += (_, _) => requests++;
            surface.Focus(); window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(0, clicks); Assert.Equal(0, requests);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Flush(window);
            Assert.Equal(1, clicks); Assert.Equal(kind == "toggle" ? 1 : 0, requests);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); surface.IsEnabled = false; surface.IsEnabled = true;
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Flush(window); Assert.Equal(1, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SelectionAndToggleRequestsPreserveBindingsUntilApplicationWritesBack()
    {
        using var clock = new MotionTestClockScope();
        var selected = new SelectableSurface { Content = new Border { Width = 80, Height = 40 } };
        var toggled = new ToggleableSurface { Content = new Border { Width = 80, Height = 40 } };
        var model = new CheckBox { IsChecked = false };
        selected.Bind(SelectableSurface.IsSelectedProperty, new Binding("IsChecked") { Source = model });
        toggled.Bind(ToggleableSurface.IsCheckedProperty, new Binding("IsChecked") { Source = model });
        var window = DialogTests.Show(new StackPanel { Children = { selected, toggled } });
        try
        {
            var requests = new List<bool>(); toggled.CheckedChangeRequested += (_, e) => requests.Add(e.Value);
            Click(window, selected); Click(window, toggled);
            Assert.False(selected.IsSelected); Assert.False(toggled.IsChecked); Assert.Equal(new[] { true }, requests);
            model.IsChecked = true; Flush(window);
            Click(window, toggled); Assert.True(toggled.IsChecked); Assert.Equal(new[] { true, false }, requests);
            Assert.Contains(":selected", selected.Classes); Assert.Contains(":checked", toggled.Classes);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void AutomationExposesStateAndRequestsWithoutInventingARole()
    {
        using var clock = new MotionTestClockScope();
        var selected = new SelectableSurface { Content = new Border { Width = 80, Height = 40 } };
        var toggled = new ToggleableSurface { Content = new Border { Width = 80, Height = 40 } };
        var window = DialogTests.Show(new StackPanel { Children = { selected, toggled } });
        try
        {
            var selectPeer = ControlAutomationPeer.CreatePeerForElement(selected)!;
            var togglePeer = ControlAutomationPeer.CreatePeerForElement(toggled)!;
            Assert.Equal(AutomationControlType.Custom, selectPeer.GetAutomationControlType());
            Assert.Equal(AutomationControlType.Custom, togglePeer.GetAutomationControlType());
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(selectPeer);
            var toggle = Assert.IsAssignableFrom<IToggleProvider>(togglePeer);
            var clicks = 0; selected.Click += (_, _) => clicks++;
            var requests = 0; toggled.CheckedChangeRequested += (_, _) => requests++;
            selection.Select(); toggle.Toggle(); Flush(window);
            Assert.Equal(1, clicks); Assert.Equal(1, requests);
            Assert.False(selection.IsSelected); Assert.Equal(ToggleState.Off, toggle.ToggleState);
            selected.IsSelected = true; toggled.IsChecked = true;
            Assert.True(selection.IsSelected); Assert.Equal(ToggleState.On, toggle.ToggleState);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CommandsAndDisabledInputRemainNativeAndExternalEventsOnlyDriveFeedback()
    {
        using var clock = new MotionTestClockScope();
        var executions = 0; var enabled = true;
        var command = new RelayCommand(() => executions++, () => enabled);
        var source = new MaterialInteractionSource();
        var events = new List<MaterialInteractionEvent>(); source.Interaction += events.Add;
        var surface = new ClickableSurface { Command = command, InteractionSource = source,
            Content = new Border { Width = 80, Height = 40 } };
        var window = DialogTests.Show(surface); Click(window, surface);
        try
        {
            Assert.Equal(1, executions);
            var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.Release>()).Start);
            enabled = false; command.NotifyCanExecuteChanged(); Flush(window);
            Assert.False(surface.IsEffectivelyEnabled); Click(window, surface); Assert.Equal(1, executions);
            source.Emit(new MaterialInteractionEvent.Press(default)); Flush(window);
            Assert.Equal(1, executions);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SourceReplacementAndDetachCancelOwnInputAndUnsubscribeOldFeedback()
    {
        using var clock = new MotionTestClockScope();
        var first = new MaterialInteractionSource(); var second = new MaterialInteractionSource();
        var events = new List<MaterialInteractionEvent>(); first.Interaction += events.Add;
        var surface = new ClickableSurface { InteractionSource = first, Content = new Border { Width = 80, Height = 40 } };
        var window = DialogTests.Show(surface);
        try
        {
            var point = Center(window, surface); window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Flush(window);
            var press = Assert.Single(events.OfType<MaterialInteractionEvent.Press>());
            surface.InteractionSource = second; Flush(window);
            Assert.Same(press, Assert.Single(events.OfType<MaterialInteractionEvent.CancelPress>()).Start);
            first.Emit(new MaterialInteractionEvent.Press(default)); Flush(window);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(surface).RippleCount);
            window.MouseUp(point, MouseButton.Left);
            window.Content = null; Flush(window);
            second.Emit(new MaterialInteractionEvent.Press(default));
            window.Content = surface; Flush(window);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(surface).RippleCount);
        }
        finally { window.Close(); }
    }

    private static ClickableSurface Create(string kind) => kind switch { "select" => new SelectableSurface(), "toggle" => new ToggleableSurface(), _ => new ClickableSurface() };
    private static MaterialSurface Container(ClickableSurface owner) => owner.GetVisualDescendants().OfType<MaterialSurface>().First();
    private static Point Center(Window window, Control control) => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
    private static void Click(Window window, Control control) { var p = Center(window, control); window.MouseMove(p); window.MouseDown(p, MouseButton.Left); window.MouseUp(p, MouseButton.Left); Flush(window); }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
}

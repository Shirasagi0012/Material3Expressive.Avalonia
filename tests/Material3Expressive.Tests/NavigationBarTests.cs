using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Documents;
using Avalonia.Automation;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Tokens;
using Xunit;
using System.Windows.Input;

namespace Material3Expressive.Tests;

public class NavigationBarTests
{
    [AvaloniaTheory]
    [InlineData(false, true, false, 80, 28)]
    [InlineData(true, true, false, 80, 16)]
    [InlineData(true, false, false, 80, 28)]
    [InlineData(true, false, true, 80, 16)]
    public void DefaultItem_UsesUpstreamLayout(bool label, bool always, bool selected, double height, double iconY)
    {
        var item = new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 },
            Label = label ? new Border { Width = 32, Height = 16 } : null, AlwaysShowLabel = always, IsSelected = selected };
        item.Width = 112;
        var window = Show(item);
        try
        {
            Assert.Equal(new Size(112, height), item.Bounds.Size);
            Assert.Equal(new Rect(44, iconY, 24, 24), Part<ContentPresenter>(item, "PART_Icon").Bounds);
            Assert.Equal(32, Part<MaterialSurface>(item, "PART_IndicatorRipple").Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TallLabel_GrowsItemAndKeepsMinimumVerticalPadding()
    {
        var item = new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 100, Height = 48 } };
        item.Width = 112;
        var window = Show(item);
        try { Assert.Equal(new Size(112, 88), item.Bounds.Size); Assert.Equal(4, Part<ContentPresenter>(item, "PART_Icon").Bounds.Y); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Bar_FillsWidthAndDistributesRemainderPixels()
    {
        var bar = new NavigationBar();
        var items = Enumerable.Range(0, 3).Select(_ => new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 } }).ToArray();
        foreach (var item in items) bar.Items.Add(item);
        var window = Show(bar);
        try
        {
            Assert.Equal(new Size(240, 80), bar.Bounds.Size);
            Assert.Equal(new[] { 74d, 75d, 75d }, items.Select(item => item.Bounds.Width));
            Assert.Equal(new[] { 0d, 82d, 165d }, items.Select(item => item.TranslatePoint(default, bar)!.Value.X));
            bar.WindowInsets = new Thickness(13, 7, 5, 11); window.UpdateLayout();
            Assert.Equal(98, bar.Bounds.Height);
            Assert.Equal(new Point(13, 7), items[0].TranslatePoint(default, bar)!.Value);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DataTemplates_UseDestinationWeights()
    {
        var bar = new NavigationBar
        {
            ItemsSource = new[] { "one", "two", "three" },
            ItemTemplate = new FuncDataTemplate<string>((value, _) => new NavigationBarItem { Label = value, Icon = new Border { Width = 24, Height = 24 } })
        };
        var window = Show(bar);
        try
        {
            var items = bar.GetVisualDescendants().OfType<NavigationBarItem>().ToArray();
            Assert.Equal(3, items.Length);
            Assert.Equal(new[] { 74d, 75d, 75d }, items.Select(item => item.Bounds.Width));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClickAndAutomation_RequestNavigationWithoutChangingSelection()
    {
        var item = new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = "Home" };
        var rail = new NavigationBar(); rail.Items.Add(item);
        var window = Show(rail); var clicks = 0;
        item.Click += (_, _) => clicks++;
        try
        {
            var position = item.TranslatePoint(new Point(40, 28), window)!.Value;
            window.MouseDown(position, MouseButton.Left); window.MouseUp(position, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.False(item.IsSelected);
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Equal(AutomationControlType.TabItem, peer.GetAutomationControlType());
            Assert.Equal("Home", peer.GetName());
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
            Assert.False(selection.IsSelected); Assert.NotNull(selection.SelectionContainer);
            selection.Select(); Assert.Equal(2, clicks); Assert.False(item.IsSelected);
            item.IsSelected = true;
            Assert.Single(selection.SelectionContainer!.GetSelection());
            item.IsEnabled = false;
            window.MouseDown(position, MouseButton.Left); window.MouseUp(position, MouseButton.Left);
            Assert.Equal(2, clicks);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Reattachment_CleansSelectionAnimations()
    {
        using var clock = new MotionTestClockScope();
        var item = new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 32, Height = 16 }, AlwaysShowLabel = false };
        item.Width = 112;
        var window = Show(item);
        try
        {
            clock.SendFrame(TimeSpan.FromSeconds(1));
            item.IsSelected = true;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            item.IsSelected = false;
            ((Grid)window.Content!).Children.Clear();
            window.Content = null;
            window.Content = new Grid { Children = { item } }; window.UpdateLayout();
            Assert.Equal(0, Part<Border>(item, "PART_Indicator").Opacity);
            Assert.Null(((MotionTestWindow)window).MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateReplacement_CancelsSelectionAndRippleTasks()
    {
        using var clock = new MotionTestClockScope();
        var item = new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = "Home" };
        item.Width = 112;
        var window = Show(item);
        try
        {
            clock.SendFrame(TimeSpan.FromSeconds(1));
            item.IsSelected = true;
            window.MouseDown(item.TranslatePoint(new Point(40, 28), window)!.Value, MouseButton.Left);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            item.Template = new FuncControlTemplate<NavigationBarItem>((_, _) => new Border());
            item.ApplyTemplate(); window.UpdateLayout();
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(0, clock.PendingFrameCount);
            Assert.Null(((MotionTestWindow)window).MotionRoot.Diagnostics.LastError);
            item.ClearValue(TemplatedControl.TemplateProperty); item.ApplyTemplate(); window.UpdateLayout();
            Assert.Equal(1, Part<Border>(item, "PART_Indicator").Opacity);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(Key.Space)]
    [InlineData(Key.Enter)]
    public void Keyboard_ActivatesOnReleaseAndHonorsCommandCanExecute(Key key)
    {
        var command = new NavigationCommand();
        var item = new NavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Command = command, CommandParameter = "home" };
        item.Width = 112;
        var window = Show(item);
        try
        {
            item.Focus(NavigationMethod.Tab);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(0, command.Executions); Assert.True(item.IsPressed);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, command.Executions); Assert.Equal("home", command.LastParameter);
            Assert.False(item.IsPressed); Assert.False(item.IsSelected);
            command.Allow = false;
            Assert.False(item.IsEffectivelyEnabled);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, command.Executions);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ContainerContentColor_MatchesThemeRoleOrInheritsAmbientAndAllowsOverride()
    {
        var rail = new NavigationBar();
        var window = Show(rail);
        try
        {
            var root = (Grid)window.Content!;
            TextElement.SetForeground(root, Brushes.Red);
            window.Resources[MaterialTokens.Sys.Color.Primary] = Colors.Blue;
            window.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Yellow;
            rail.Background = Brushes.Blue;
            Assert.Equal(Colors.Yellow, Assert.IsAssignableFrom<ISolidColorBrush>(rail.Foreground).Color);
            window.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Green;
            Assert.Equal(Colors.Green, Assert.IsAssignableFrom<ISolidColorBrush>(rail.Foreground).Color);
            rail.Background = Brushes.Magenta;
            Assert.Equal(Colors.Red, Assert.IsAssignableFrom<ISolidColorBrush>(rail.Foreground).Color);
            TextElement.SetForeground(root, Brushes.Orange);
            Assert.Equal(Colors.Orange, Assert.IsAssignableFrom<ISolidColorBrush>(rail.Foreground).Color);
            rail.Foreground = Brushes.Cyan; rail.Background = Brushes.Blue;
            Assert.Equal(Colors.Cyan, Assert.IsAssignableFrom<ISolidColorBrush>(rail.Foreground).Color);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Automation_HidesIconDescriptionWhenLabelIsShown()
    {
        var icon = new Border { Width = 24, Height = 24 };
        AutomationProperties.SetName(icon, "House icon");
        var item = new NavigationBarItem { Icon = icon, Label = "Home", AlwaysShowLabel = false };
        item.Width = 112;
        var window = Show(item);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Contains(peer.GetChildren()!, child => child.GetName() == "House icon");
            item.IsSelected = true;
            Assert.DoesNotContain(peer.GetChildren()!, child => child.GetName() == "House icon");
            Assert.Equal("Home", peer.GetName());
        }
        finally { window.Close(); }
    }

    [Fact]
    public void Tokens_AnswerAllDeclaredKeys() => TokenScan.AssertComplete(new NavigationBarTokenSource(), typeof(MaterialTokens.Comp.NavigationBar));

    [AvaloniaFact]
    public void OutgoingLabel_KeepsSemanticsUntilItIsNoLongerPlaced()
    {
        using var clock = new MotionTestClockScope();
        var icon = new Border { Width = 24, Height = 24 };
        var label = new Border { Width = 32, Height = 16 };
        AutomationProperties.SetName(icon, "House icon");
        AutomationProperties.SetName(label, "Home label");
        var item = new NavigationBarItem { Icon = icon, Label = label, IsSelected = true, AlwaysShowLabel = false };
        item.Width = 112;
        var window = Show(item);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Contains(peer.GetChildren()!, child => child.GetName() == "Home label");
            Assert.DoesNotContain(peer.GetChildren()!, child => child.GetName() == "House icon");
            item.IsSelected = false;
            Assert.Contains(peer.GetChildren()!, child => child.GetName() == "Home label");
            Assert.Contains(peer.GetChildren()!, child => child.GetName() == "House icon");
            for (var frame = 0; frame < 60; frame++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.DoesNotContain(peer.GetChildren()!, child => child.GetName() == "Home label");
            Assert.Contains(peer.GetChildren()!, child => child.GetName() == "House icon");
        }
        finally { window.Close(); }
    }

    static Window Show(Control content)
    {
        var window = new MotionTestWindow { Width = 240, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { content } };
        window.Show(); window.UpdateLayout();
        return window;
    }
    static T Part<T>(Control item, string name) where T : Control => item.GetVisualDescendants().OfType<T>().Single(control => control.Name == name);

    private sealed class NavigationCommand : ICommand
    {
        private bool _allow = true;
        public bool Allow { get => _allow; set { _allow = value; CanExecuteChanged?.Invoke(this, EventArgs.Empty); } }
        public int Executions { get; private set; }
        public object? LastParameter { get; private set; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => Allow;
        public void Execute(object? parameter) { Executions++; LastParameter = parameter; }
    }
}

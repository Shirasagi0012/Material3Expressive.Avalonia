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

public class ShortNavigationBarTests
{
    [AvaloniaTheory]
    [InlineData(NavigationItemIconPosition.Top, true, 64, 10, 32)]
    [InlineData(NavigationItemIconPosition.Top, false, 48, 12, 32)]
    [InlineData(NavigationItemIconPosition.Start, true, 48, 12, 40)]
    [InlineData(NavigationItemIconPosition.Start, false, 48, 12, 40)]
    public void Item_UsesNonAnimatedNavigationItemLayout(NavigationItemIconPosition position, bool label, double height, double iconY, double pillHeight)
    {
        var item = new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = label ? new Border { Width = 32, Height = 16 } : null,
            IconPosition = position, Width = 112, VerticalAlignment = VerticalAlignment.Top };
        var window = Show(item);
        try
        {
            Assert.Equal(height, item.Bounds.Height);
            Assert.Equal(iconY, Part<ContentPresenter>(item, "PART_Icon").TranslatePoint(default, item)!.Value.Y);
            Assert.Equal(pillHeight, Part<MaterialSurface>(item, "PART_IndicatorRipple").Bounds.Height);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(ShortNavigationBarArrangement.EqualWeight, 0, 80)]
    [InlineData(ShortNavigationBarArrangement.Centered, 48, 48)]
    public void Bar_UsesArrangementAndUniformHeight(ShortNavigationBarArrangement arrangement, double x, double width)
    {
        var bar = new ShortNavigationBar { Arrangement = arrangement };
        var items = Enumerable.Range(0,3).Select(_ => new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 16, Height = 16 } }).ToArray();
        foreach(var item in items) bar.Items.Add(item);
        var window = Show(bar);
        try
        {
            Assert.Equal(new Size(240,64), bar.Bounds.Size);
            Assert.Equal(width, items[0].Bounds.Width);
            Assert.Equal(x, items[0].TranslatePoint(default,bar)!.Value.X);
            items[0].Label = new Border { Width = 16, Height = 48 }; window.UpdateLayout();
            Assert.Equal(96, bar.Bounds.Height);
            Assert.All(items, item => Assert.Equal(96, item.Bounds.Height));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ExistingContentSizeChange_RequeriesIntrinsicHeight()
    {
        var label = new Border { Width = 16, Height = 16 };
        var item = new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = label };
        var bar = new ShortNavigationBar(); bar.Items.Add(item);
        var window = Show(bar);
        try
        {
            label.Height = 48; window.UpdateLayout();
            Assert.Equal(96, bar.Bounds.Height);
            label.Height = 80; window.UpdateLayout();
            Assert.Equal(128, bar.Bounds.Height);
            label.Height = 16; window.UpdateLayout();
            Assert.Equal(64, bar.Bounds.Height);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void DataTemplates_PreserveAllocation()
    {
        var bar = new ShortNavigationBar { ItemsSource = new[] { 1,2,3 }, ItemTemplate = new FuncDataTemplate<int>((_,_) => new ShortNavigationBarItem { Icon = new Border { Width=24, Height=24 } }) };
        var window = Show(bar);
        try { Assert.Equal(new[] {80d,80d,80d},bar.GetVisualDescendants().OfType<ShortNavigationBarItem>().Select(item=>item.Bounds.Width)); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ClickAndAutomation_RequestNavigationWithoutChangingSelection()
    {
        var item = new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = "Home" };
        var rail = new ShortNavigationBar(); rail.Items.Add(item);
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
        var item = new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = new Border { Width = 32, Height = 16 } };
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
        var item = new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Label = "Home" };
        item.Width = 112;
        var window = Show(item);
        try
        {
            clock.SendFrame(TimeSpan.FromSeconds(1));
            item.IsSelected = true;
            window.MouseDown(item.TranslatePoint(new Point(40, 28), window)!.Value, MouseButton.Left);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            item.Template = new FuncControlTemplate<ShortNavigationBarItem>((_, _) => new Border());
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
        var item = new ShortNavigationBarItem { Icon = new Border { Width = 24, Height = 24 }, Command = command, CommandParameter = "home" };
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

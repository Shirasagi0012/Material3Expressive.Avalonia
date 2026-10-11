using System.Collections.ObjectModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarOverflowTests
{
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void IndicatorSmallerConstraint_PreservesReservedExtent(bool vertical)
    {
        using var clock = new MotionTestClockScope();
        AppBarRow bar = vertical ? new AppBarColumn { MaxHeight = 20 } : new AppBarRow { MaxWidth = 20 };
        bar.OverflowIndicatorTemplate = new FuncDataTemplate<AppBarMenuState>((_, _) => new Button
        { Template = new FuncControlTemplate<Button>((_, _) => new PreferredSizeControl()) });
        for (var i = 0; i < 3; i++) bar.Items.Add(new AppBarCustomItem { Content = new PreferredSizeControl() });
        var window = DialogTests.Show(bar); Flush(window);
        var indicator = (AppBarSlot)bar.Layout!.Children[1];
        Assert.False(bar.ClipToBounds);
        var marker = indicator.GetVisualDescendants().OfType<PreferredSizeControl>().Single();
        Assert.True((vertical ? marker.Bounds.Height : marker.Bounds.Width) == 48,
            string.Join("; ", bar.GetVisualDescendants().Select(v => $"{v.GetType().Name}={v.Bounds}")));
        Assert.True((vertical ? indicator.Child!.Bounds.Height : indicator.Child!.Bounds.Width) == 48,
            $"reserved={bar.Layout.ReservedSpace}; remaining={bar.Layout.RemainingSpace}; root={bar.Layout.Bounds}; slot={indicator.Bounds}/{indicator.DesiredSize}; child={indicator.Child!.Bounds}/{indicator.Child.DesiredSize}");
        window.Close();
    }

    private sealed class PreferredSizeControl : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(48, 48).Constrain(availableSize);
    }

    [AvaloniaFact]
    public void Xaml_IconSlots_Label_AndControlledToggleAreRealTemplateInputs()
    {
        using var clock = new MotionTestClockScope();
        var root = (StackPanel)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/AppBarOverflow.axaml"));
        var window = DialogTests.Show(root); Flush(window);
        var row = (AppBarRow)root.Children[0];
        var action = (AppBarAction)row.Items[0]!;
        var button = action.GetVisualDescendants().OfType<IconButton>().Single();
        Assert.Same(action.Icon, button.Content);
        Assert.Equal("Save", Avalonia.Automation.AutomationProperties.GetName(button));
        Assert.Equal("Save", action.GetVisualDescendants().OfType<TooltipBox>().Single().Tooltip is PlainTooltip tooltip ? tooltip.Content : null);
        var column = (AppBarColumn)root.Children[1];
        var symbol = column.GetVisualDescendants().OfType<MaterialSymbol>().First(s => s.Symbol == MaterialSymbols.MaterialSymbolCodepoint.Add);
        Assert.Equal(new Size(24, 24), symbol.Bounds.Size);
        var toggle = (IconToggleButton)root.Children[2];
        var peer = (IToggleProvider)ControlAutomationPeer.CreatePeerForElement(toggle)!;
        Assert.Equal(ToggleState.On, peer.ToggleState);
        var requested = false; toggle.CheckedChangeRequested += (_, e) => { Assert.False(e.Value); requested = true; };
        peer.Toggle(); Flush(window); Assert.True(requested); Assert.True(toggle.IsChecked);
        window.Close();
    }

    [AvaloniaFact]
    public void DefaultIndicator_RealPointer_Escape_AndRetemplateReleaseOldPopup()
    {
        using var clock = new MotionTestClockScope();
        var bar = new AppBarRow { MaxItemCount = 1, Items = { new AppBarAction { Label = "A" }, new AppBarAction { Label = "B" } } };
        var window = DialogTests.Show(bar); Flush(window);
        var button = bar.GetVisualDescendants().OfType<AppBarOverflowButton>().Single();
        var point = button.TranslatePoint(new Point(24, 24), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Advance(clock, window, 80);
        Assert.True(bar.MenuState.IsShowing); Assert.True(bar.Layout!.Menu.IsPresented);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null); Advance(clock, window, 80); Assert.False(bar.MenuState.IsShowing);
        bar.MenuState.Show(); Advance(clock, window, 80);
        var old = bar.Layout.Menu;
        var template = bar.Template; bar.Template = null; Flush(window);
        Assert.False(old.IsPresented);
        bar.Template = template; Advance(clock, window, 80);
        Assert.True(bar.Layout!.Menu.IsPresented);
        window.Content = null; Flush(window); Assert.False(bar.Layout.Menu.IsPresented);
        window.Content = bar; Advance(clock, window, 80); Assert.True(bar.Layout.Menu.IsPresented);
        window.Close(); Advance(clock, window, 1); Assert.Equal(0, window.MotionRoot.ActiveChildCount);
    }

    [AvaloniaFact]
    public void IconToggle_UsesAmbientSelectedAndDisabledColors_AndPreservesUserStyle()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new IconToggleButton { Symbol = MaterialSymbols.MaterialSymbolCodepoint.Favorite };
        var parent = new StackPanel { Children = { toggle } };
        Avalonia.Controls.Documents.TextElement.SetForeground(parent, Brushes.Red);
        var window = DialogTests.Show(parent); Flush(window);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)toggle.Foreground!).Color);
        toggle.IsChecked = true; Flush(window); Assert.NotEqual(Colors.Red, ((ISolidColorBrush)toggle.Foreground!).Color);
        Assert.Equal(Colors.Red, ((ISolidColorBrush)toggle.GetVisualDescendants().OfType<MaterialSurface>().Single().StateLayerBrush!).Color);
        toggle.IsEnabled = false; Flush(window); Assert.Equal(Color.FromArgb(97, 255, 0, 0), ((ISolidColorBrush)toggle.Foreground!).Color);
        toggle.Styles.Add(new Avalonia.Styling.Style(s => s.OfType<IconToggleButton>()) { Setters = { new Avalonia.Styling.Setter(IconToggleButton.ForegroundProperty, Brushes.Blue) } });
        Flush(window); Assert.Equal(Colors.Blue, ((ISolidColorBrush)toggle.Foreground!).Color);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(false, 144, 3)]
    [InlineData(false, 143, 1)]
    [InlineData(false, 96, 1)]
    [InlineData(false, 48, 0)]
    [InlineData(true, 144, 3)]
    [InlineData(true, 143, 1)]
    [InlineData(true, 96, 1)]
    [InlineData(true, 48, 0)]
    public void Constraints_ReserveIndicator_AndLastItemReclaimsIt(bool vertical, int space, int visible)
    {
        using var clock = new MotionTestClockScope();
        var bar = MakeBar(vertical, space);
        var window = DialogTests.Show(bar);
        Flush(window);
        Assert.Equal(visible, bar.Layout!.VisibleCount);
        Assert.Equal(3 - visible, bar.Layout.Menu.Items.Count);
        Assert.Equal(visible == 3 ? 144 : (visible + 1) * 48, vertical ? bar.Bounds.Height : bar.Bounds.Width);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(0, 3)]
    [InlineData(-1, 3)]
    public void MaxCount_PreservesUpstreamExactBoundaryIncludingNonpositive(int maximum, int expected)
    {
        using var clock = new MotionTestClockScope();
        var bar = MakeBar(false, 400); bar.MaxItemCount = maximum;
        var window = DialogTests.Show(bar); Flush(window);
        Assert.Equal(expected, bar.Layout!.VisibleCount);
        window.Close();
    }

    [AvaloniaFact]
    public void Resize_RevealsExistingContent_HidesFocusTargets_AndUpdatesOpenMenu()
    {
        using var clock = new MotionTestClockScope();
        var bar = MakeBar(false, 96);
        var window = DialogTests.Show(bar); Flush(window);
        var hidden = (AppBarCustomItem)bar.Items[2]!;
        Assert.False(hidden.IsEffectivelyVisible);
        bar.MenuState.Show(); Advance(clock, window, 80);
        Assert.True(bar.Layout!.Menu.IsPresented);
        bar.MaxWidth = 144; Flush(window);
        Assert.Equal(3, bar.Layout.VisibleCount);
        Assert.True(hidden.IsEffectivelyVisible);
        Assert.Empty(bar.Layout.Menu.Items);
        bar.MaxWidth = 48; Flush(window);
        Assert.Equal(0, bar.Layout.VisibleCount);
        Assert.Equal(3, bar.Layout.Menu.Items.Count);
        window.Close(); Advance(clock, window, 1);
        Assert.Equal(0, window.MotionRoot.ActiveChildCount);
    }

    [AvaloniaFact]
    public void Actions_ExecuteOnceBeforeDismiss_AndCanExecuteTracksChanges()
    {
        using var clock = new MotionTestClockScope();
        var bar = new AppBarRow { MaxItemCount = 1 };
        var calls = 0;
        var command = new TestCommand(() => { Assert.True(bar.MenuState.IsShowing); calls++; });
        var action = new AppBarAction { Label = "Save", Icon = new Border { Width = 24, Height = 24 }, Command = command };
        bar.Items.Add(new AppBarAction { Label = "Other" }); bar.Items.Add(action);
        var window = DialogTests.Show(bar); Flush(window);
        bar.MenuState.Show(); Advance(clock, window, 80);
        var menuItem = Assert.IsType<DropdownMenuItem>(bar.Layout!.Menu.Items[1]);
        command.Enabled = false; command.Notify(); Flush(window);
        Assert.False(menuItem.IsEffectivelyEnabled);
        command.Enabled = true; command.Notify(); Flush(window);
        Invoke(menuItem); Flush(window);
        Assert.Equal(1, calls); Assert.False(bar.MenuState.IsShowing);
        window.Close();
    }

    [AvaloniaFact]
    public void Toggle_RequestsWithoutMutating_InBothPresentations_AndMenuIsPlain()
    {
        using var clock = new MotionTestClockScope();
        var item = new AppBarToggleAction { Label = "Favorite", Icon = new Border { Width = 24, Height = 24 } };
        var bar = new AppBarRow { Items = { item, new AppBarAction { Label = "Other" } } };
        var requests = new List<bool>(); item.CheckedChangeRequested += (_, e) => requests.Add(e.Value);
        var window = DialogTests.Show(bar); Flush(window);
        var button = item.GetVisualDescendants().OfType<IconToggleButton>().Single();
        Invoke(button); Flush(window);
        Assert.Equal([true], requests); Assert.False(item.IsChecked);
        item.IsChecked = true; Flush(window); Assert.True(button.IsChecked);
        bar.MaxItemCount = 1; Flush(window); bar.MenuState.Show(); Advance(clock, window, 80);
        var menuItem = Assert.IsType<DropdownMenuItem>(bar.Layout!.Menu.Items[0]);
        Invoke(menuItem); Flush(window);
        Assert.Equal([true, false], requests); Assert.True(item.IsChecked); Assert.False(bar.MenuState.IsShowing);
        window.Close();
    }

    [AvaloniaFact]
    public void DataTemplate_CollectionMutation_AndCustomMenuKeepDataContext()
    {
        using var clock = new MotionTestClockScope();
        var data = new ObservableCollection<string> { "first", "second", "third" };
        var bar = new AppBarRow
        {
            MaxItemCount = 1, ItemsSource = data,
            ItemTemplate = new FuncDataTemplate<string>((value, _) => new AppBarCustomItem
            {
                Content = new Border { Width = 40, Height = 24 },
                MenuContentTemplate = new FuncDataTemplate<object?>((_, _) => new DropdownMenuItem { Content = value })
            })
        };
        var window = DialogTests.Show(bar); Flush(window);
        Assert.Equal(3, bar.Layout!.Menu.Items.Count);
        data.RemoveAt(0); Flush(window); Assert.Equal(2, bar.Layout.Menu.Items.Count);
        bar.MenuState.Show(); Advance(clock, window, 80);
        var custom = (ContentControl)bar.Layout.Menu.Items[0]!;
        Assert.Equal("second", custom.DataContext);
        Invoke(custom.GetVisualDescendants().OfType<DropdownMenuItem>().Single()); Flush(window);
        Assert.True(bar.MenuState.IsShowing);
        bar.MenuState.Dismiss(); window.Close();
    }

    private static AppBarRow MakeBar(bool vertical, double space)
    {
        AppBarRow bar = vertical ? new AppBarColumn { MaxHeight = space } : new AppBarRow { MaxWidth = space };
        bar.OverflowIndicatorTemplate = new FuncDataTemplate<AppBarMenuState>((_, _) => new Border { Width = 48, Height = 48 });
        for (var i = 0; i < 3; i++) bar.Items.Add(new AppBarCustomItem
        {
            Content = new Border { Width = 48, Height = 48, Background = Brushes.Red },
            MenuContent = new DropdownMenuItem { Content = $"Item {i}" }
        });
        return bar;
    }
    private static void Invoke(Control control) => ((IInvokeProvider)ControlAutomationPeer.CreatePeerForElement(control)!).Invoke();
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    { for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
    private sealed class TestCommand(Action action) : ICommand
    {
        public bool Enabled { get; set; } = true;
        public bool CanExecute(object? parameter) => Enabled;
        public void Execute(object? parameter) => action();
        public event EventHandler? CanExecuteChanged;
        public void Notify() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}

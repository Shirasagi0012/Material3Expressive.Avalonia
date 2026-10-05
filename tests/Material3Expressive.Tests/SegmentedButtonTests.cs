using Avalonia;
using Avalonia.Automation;
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
using Material3Expressive.Tokens;
using CommunityToolkit.Mvvm.Input;
using Xunit;

namespace Material3Expressive.Tests;

public class SegmentedButtonTests
{
    [AvaloniaFact]
    public void ResourcesAndRtl_UpdateDimensionsMotionAndContentDirection()
    {
        var row = new SingleChoiceSegmentedButtonRow { FlowDirection = FlowDirection.RightToLeft };
        var item = Item(); item.IsSelected = true; row.Items.Add(item);
        var window = Show(row);
        try
        {
            Assert.Equal(FlowDirection.RightToLeft, ((Control)item.Content!).FlowDirection);
            var visibility = item.GetVisualDescendants().OfType<ComposeAnimation.AnimatedVisibility>().Single();
            var enter = visibility.Enter;
            item.Resources[MaterialTokens.Sys.Motion.Spring.SpatialFast] = new Material3Expressive.Foundation.Motion.SpringSpec(0.8, 200);
            Assert.NotSame(enter, visibility.Enter);
            row.Resources[MaterialTokens.Comp.SegmentedButton.ContainerHeight] = 64d;
            row.Resources[MaterialTokens.Comp.SegmentedButton.IconSize] = 24d;
            window.UpdateLayout();
            Assert.Equal(64, row.Bounds.Height);
            Assert.Equal(64, Part<MaterialSurface>(item, "PART_Container").Bounds.Height);
            Assert.Equal(88, row.Bounds.Width);
            row.Items.Clear(); window.UpdateLayout(); Assert.Equal(64, row.Bounds.Height);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ConfigurationAndIconTemplates_AreUsableFromCompiledXaml()
    {
        using var clock = new MotionTestClockScope();
        var row = (SingleChoiceSegmentedButtonRow)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/SegmentedButtonConfiguration.axaml"));
        var window = Show(row);
        try
        {
            var item = (SegmentedButton)row.Items[0]!;
            Assert.Equal(Color.Parse("#126E60"), Assert.IsType<SolidColorBrush>(item.Background).Color);
            Assert.Equal(new CornerRadius(8, 0, 0, 8), item.CornerRadius);
            Assert.NotNull(item.ActiveIconTemplate); Assert.NotNull(item.InactiveIconTemplate);
            item.IsSelected = false; window.UpdateLayout(); clock.SendFrame(TimeSpan.FromMilliseconds(1));
            for (var time = 16; time <= 800; time += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(time)); window.UpdateLayout(); }
            Assert.Null(MotionTestClockScope.Error(item));
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void TemplatedItems_ReceiveShapesSelectionRoleAndSiblingLayerOrder()
    {
        var row = new SingleChoiceSegmentedButtonRow { ItemsSource = new[] { "Day", "Week" },
            ItemTemplate = new FuncDataTemplate<string>((_, _) => Item()) };
        var window = Show(row);
        try
        {
            var items = row.GetVisualDescendants().OfType<SegmentedButton>().ToArray();
            Assert.Equal(2, items.Length); Assert.Equal(163, row.Bounds.Width);
            items[0].IsSelected = true;
            Assert.Equal(5, Assert.IsType<ContentPresenter>(items[0].GetVisualParent()).ZIndex);
            Assert.Equal(AutomationControlType.RadioButton, ControlAutomationPeer.CreatePeerForElement(items[0])!.GetAutomationControlType());
            Assert.Equal(new CornerRadius(1000, 0, 0, 1000), items[0].CornerRadius);
            items[0].IsSelected = false; items[0].InactiveIcon = new Border { Width = 36, Height = 16 }; window.UpdateLayout();
            Assert.Equal(199, row.Bounds.Width);
        }
        finally { window.Close(); }
    }
    [AvaloniaTheory]
    [InlineData(Key.Space)] [InlineData(Key.Enter)]
    public void Keyboard_InvokesCommandOnReleaseAndCancelsWhenFocusIsLost(Key key)
    {
        var count = 0; var row = new MultiChoiceSegmentedButtonRow(); var item = Item(); var other = Item();
        item.Command = new RelayCommand(() => count++); row.Items.Add(item); row.Items.Add(other); var window = Show(row);
        try
        {
            item.Focus(NavigationMethod.Tab); window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(0, count); Assert.True(item.IsPressed);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(1, count); Assert.False(item.IsSelected);
            window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null); other.Focus(NavigationMethod.Tab); item.Focus(NavigationMethod.Tab);
            window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, count);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void SelectionIcon_PreservesSpatialOvershootAndReleasesClock()
    {
        using var clock = new MotionTestClockScope();
        var row = new MultiChoiceSegmentedButtonRow(); var item = Item();
        item.ActiveIcon = new Border { Width = 18, Height = 16, Background = Brushes.Black };
        row.Items.Add(item); var window = Show(row);
        clock.SendFrame(TimeSpan.Zero); item.IsSelected = true; window.UpdateLayout();
        clock.SendFrame(TimeSpan.FromMilliseconds(1));
        for (var time = 16; time <= 160; time += 16) { clock.SendFrame(TimeSpan.FromMilliseconds(time)); window.UpdateLayout(); }
        Assert.False(item.GetVisualDescendants().OfType<ComposeAnimation.AnimatedVisibility>().Single().ClipToBounds);
        var surface = item.GetVisualDescendants().Single(v => v.GetType().Name == "VisibilitySurface");
        Assert.True(surface.RenderTransform!.Value.M11 > 1);
        Assert.Null(MotionTestClockScope.Error(item));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }
    [AvaloniaTheory]
    [InlineData(false)] [InlineData(true)]
    public void DefaultRow_UsesLargestIntrinsicWidthAndOverlappingEqualSegments(bool multi)
    {
        SegmentedButtonRow row = multi ? new MultiChoiceSegmentedButtonRow() : new SingleChoiceSegmentedButtonRow();
        var items = new[] { Item(32), Item(48), Item(16) };
        foreach (var item in items) row.Items.Add(item);
        var window = Show(row);
        try
        {
            Assert.Equal(new Size(292, 48), row.Bounds.Size);
            Assert.All(items, item => Assert.Equal(new Size(98, 48), item.Bounds.Size));
            Assert.All(items, item => Assert.Equal(new Size(98, 40), Part<MaterialSurface>(item, "PART_Container").Bounds.Size));
            Assert.Equal(new CornerRadius(1000, 0, 0, 1000), items[0].CornerRadius);
            Assert.Equal(default, Part<MaterialSurface>(items[1], "PART_Container").EffectiveCornerRadius);
            Assert.Equal(new CornerRadius(0, 1000, 1000, 0), items[2].CornerRadius);
            Assert.Equal(97, items[1].Bounds.X - items[0].Bounds.X);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void ActivationAndAutomation_KeepSelectionExternalAndUseRowRole()
    {
        var single = new SingleChoiceSegmentedButtonRow(); var item = Item(); single.Items.Add(item);
        var window = Show(single); var clicks = 0; item.Click += (_, _) => clicks++;
        try
        {
            var center = item.TranslatePoint(new Point(item.Bounds.Width / 2, 24), window)!.Value;
            window.MouseDown(center, MouseButton.Left); window.MouseUp(center, MouseButton.Left);
            Assert.Equal(1, clicks); Assert.False(item.IsSelected);
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Equal(AutomationControlType.RadioButton, peer.GetAutomationControlType());
            var selection = Assert.IsAssignableFrom<ISelectionItemProvider>(peer);
            selection.Select(); Assert.Equal(2, clicks); Assert.False(item.IsSelected);
            item.IsSelected = true; Assert.True(selection.IsSelected);
            Assert.Single(Assert.IsAssignableFrom<ISelectionProvider>(selection.SelectionContainer).GetSelection());
        }
        finally { window.Close(); }
        var multi = new MultiChoiceSegmentedButtonRow(); item = Item(); multi.Items.Add(item); window = Show(multi);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
            var toggle = Assert.IsAssignableFrom<IToggleProvider>(peer); toggle.Toggle(); Assert.False(item.IsSelected);
            item.IsSelected = true; Assert.Equal(ToggleState.On, toggle.ToggleState);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Colors_TrackTokensAndPreserveNativeOverrides()
    {
        var row = new MultiChoiceSegmentedButtonRow(); var item = Item(); row.Items.Add(item); var window = Show(row);
        try
        {
            var outline = Color.Parse("#126E60"); item.Resources[MaterialTokens.Sys.Color.Outline] = outline;
            item.IsSelected = true; item.IsEnabled = false;
            Assert.Equal(Color.FromArgb(31, outline.R, outline.G, outline.B), Assert.IsAssignableFrom<ISolidColorBrush>(item.BorderBrush).Color);
            Assert.Equal(97, Assert.IsAssignableFrom<ISolidColorBrush>(item.Foreground).Color.A);
            Assert.Equal((Color)window.FindResource(MaterialTokens.Sys.Color.SecondaryContainer)!, Assert.IsAssignableFrom<ISolidColorBrush>(item.Background).Color);
            item.Resources[MaterialTokens.Comp.SegmentedButton.DisabledActiveContentColor] = Colors.Blue;
            item.Resources[MaterialTokens.Comp.SegmentedButton.DisabledActiveContainerColor] = Colors.Red;
            Assert.Equal(Colors.Blue, Assert.IsAssignableFrom<ISolidColorBrush>(item.Foreground).Color);
            item.Foreground = Brushes.Green; item.IsEnabled = true; Assert.Same(Brushes.Green, item.Foreground);
            Assert.Same(Brushes.Green, Part<MaterialSurface>(item, "PART_Container").StateLayerBrush);
            item.Padding = new Thickness(4); item.BorderThickness = new Thickness(2); item.CornerRadius = new CornerRadius(8);
            row.BaseShape = new CornerRadius(12); window.UpdateLayout(); Assert.Equal(new CornerRadius(8), item.CornerRadius);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void RowUpdates_RecomputeShapesAndIntrinsicContent()
    {
        var row = new MultiChoiceSegmentedButtonRow(); var item = Item(); row.Items.Add(item); var window = Show(row);
        try
        {
            Assert.Equal(82, row.Bounds.Width);
            row.Items.Add(Item(64)); window.UpdateLayout(); Assert.Equal(227, row.Bounds.Width);
            row.Space = 2; row.BaseShape = new CornerRadius(8); window.UpdateLayout(); Assert.Equal(226, row.Bounds.Width);
            Assert.Equal(new CornerRadius(8, 0, 0, 8), item.CornerRadius);
            ((Border)item.Content!).Width = 80; window.UpdateLayout(); Assert.Equal(258, row.Bounds.Width);
            row.Items.RemoveAt(1); window.UpdateLayout(); Assert.Equal(new CornerRadius(8), item.CornerRadius);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void LayerOrder_CountsSelectionPressAndFocusWithoutHover()
    {
        var row = new MultiChoiceSegmentedButtonRow(); var item = Item(); row.Items.Add(item); var window = Show(row);
        try
        {
            item.IsSelected = true; Assert.Equal(5, item.ZIndex);
            item.Focus(NavigationMethod.Tab); Assert.Equal(6, item.ZIndex);
            var center = item.TranslatePoint(new Point(30, 24), window)!.Value;
            window.MouseMove(center); Assert.Equal(6, item.ZIndex);
            window.MouseDown(center, MouseButton.Left); Assert.Equal(7, item.ZIndex);
            window.MouseUp(center, MouseButton.Left); Assert.Equal(6, item.ZIndex);
            item.IsEnabled = false; Assert.Equal(5, item.ZIndex);
        }
        finally { window.Close(); }
    }
    [AvaloniaFact]
    public void Icons_TemplateReplacementAndDetachReleaseMotionAndCallerContent()
    {
        using var clock = new MotionTestClockScope();
        var row = new MultiChoiceSegmentedButtonRow();
        var active = new Border { Width = 18, Height = 18 }; var inactive = new Border { Width = 24, Height = 24 };
        var item = Item(); item.ActiveIcon = active; item.InactiveIcon = inactive; row.Items.Add(item);
        var window = Show(row);
        item.IsSelected = true; clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var template = item.Template; item.Template = null; window.UpdateLayout();
        item.Template = template; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        Assert.Contains(active, item.GetVisualDescendants());
        row.Items.Remove(item); window.UpdateLayout(); row.Items.Add(item); window.UpdateLayout();
        item.IsSelected = false; clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Null(MotionTestClockScope.Error(item));
        window.Close(); Assert.Equal(0, clock.PendingFrameCount);
    }
    private static SegmentedButton Item(double width = 32) => new() { Content = new Border { Width = width, Height = 16 } };
    private static T Part<T>(Control owner, string name) where T : Control => owner.GetVisualDescendants().OfType<T>().Single(c => c.Name == name);
    private static Window Show(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center; control.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 480, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); return window;
    }
}

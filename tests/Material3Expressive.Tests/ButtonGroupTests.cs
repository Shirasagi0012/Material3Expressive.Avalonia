using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.Input;
using Material3Expressive.Components;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class ButtonGroupTests
{
    [AvaloniaFact]
    public void CompressedActionAndToggleLabelsRemainOnOneLine()
    {
        using var clock = new MotionTestClockScope();
        ButtonGroupAction[] actions =
        [
            new() { Label = "Another action", CompressionLimit = 80 },
            new() { Label = "Press this action" },
            new ButtonGroupToggleAction { Label = "More actions", CompressionLimit = 80 }
        ];
        var group = new ButtonGroup { ExpandedRatio = 1 };
        foreach (var action in actions)
            group.Items.Add(action);
        var window = DialogTests.Show(group);
        try
        {
            Flush(window);
            var originalWidths = actions.Select(action => action.Bounds.Width).ToArray();
            var pressedButton = actions[1].ActionButton!;
            var center = pressedButton.TranslatePoint(
                new Point(pressedButton.Bounds.Width / 2, pressedButton.Bounds.Height / 2), window)!.Value;
            window.MouseMove(center);
            window.MouseDown(center, MouseButton.Left);
            Advance(clock, window, 40);

            Assert.True(actions[0].Bounds.Width < originalWidths[0]);
            Assert.True(actions[2].Bounds.Width < originalWidths[2]);
            foreach (var action in actions)
            {
                var label = Assert.Single(action.ActionButton!.GetVisualDescendants().OfType<TextBlock>());
                Assert.Single(label.TextLayout.TextLines);
            }
            window.MouseUp(center, MouseButton.Left);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CommandExecutesOnceAndCanExecuteUpdatesBothPresentations()
    {
        using var clock = new MotionTestClockScope();
        var executions = 0;
        var enabled = true;
        var command = new RelayCommand(() => executions++, () => enabled);
        var action = new ButtonGroupAction { Label = "Run", Command = command };
        var group = new ButtonGroup { Items = { action } };
        var window = DialogTests.Show(group);
        Flush(window);
        Click(window, action.ActionButton!);
        Assert.Equal(1, executions);

        enabled = false;
        command.NotifyCanExecuteChanged();
        Flush(window);
        Assert.False(action.ActionButton!.IsEffectivelyEnabled);
        Click(window, action.ActionButton);
        Assert.Equal(1, executions);

        group.MaxWidth = 20;
        Flush(window);
        group.MenuState.Show();
        Advance(clock, window, 20);
        var menuItem = Assert.Single(group.Layout!.Menu.Items.OfType<DropdownMenuItem>());
        Assert.False(menuItem.IsEffectivelyEnabled);
        enabled = true;
        command.NotifyCanExecuteChanged();
        Flush(window);
        Assert.True(menuItem.IsEffectivelyEnabled);
        Click(window, menuItem);
        Assert.Equal(2, executions);
        Assert.False(group.MenuState.IsShowing);
        window.Close();
    }

    [AvaloniaFact]
    public void DefaultIndicatorOpensTheMenuAndRetemplateReleasesTheOldPopup()
    {
        using var clock = new MotionTestClockScope();
        var group = new ButtonGroup
        {
            MaxWidth = 160,
            Items =
            {
                new ButtonGroupAction { Label = "Alpha" },
                new ButtonGroupAction { Label = "Beta" },
                new ButtonGroupAction { Label = "Gamma" }
            }
        };
        var window = DialogTests.Show(group);
        Flush(window);
        var indicator = Assert.Single(group.GetVisualDescendants().OfType<ButtonGroupOverflowButton>());
        Click(window, indicator);
        Advance(clock, window, 20);
        Assert.True(group.MenuState.IsShowing);
        Assert.True(group.Layout!.Menu.IsPresented);

        var oldMenu = group.Layout.Menu;
        var template = group.Template;
        group.Template = null;
        Flush(window);
        Assert.False(oldMenu.IsPresented);
        group.Template = template;
        Advance(clock, window, 20);
        Assert.True(group.Layout!.Menu.IsPresented);
        window.Content = null;
        Advance(clock, window, 2);
        Assert.False(group.Layout.Menu.IsPresented);
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
        window.Close();
    }

    [AvaloniaFact]
    public void DetachWhileRepressIsJoiningTheReturnDoesNotRestartInAnEndedScope()
    {
        using var clock = new MotionTestClockScope();
        var button = new Button { Content = "Press" };
        var group = new ButtonGroup
        {
            Items =
            {
                new ButtonGroupCustomItem { Content = button, PressSource = button },
                new ButtonGroupCustomItem { Content = new Button { Content = "Neighbour" } }
            }
        };
        var window = DialogTests.Show(group);
        Flush(window);
        var center = button.TranslatePoint(new Point(button.Bounds.Width / 2, button.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        Advance(clock, window, 15);
        window.MouseUp(center, MouseButton.Left);
        Advance(clock, window, 2);
        window.MouseDown(center, MouseButton.Left);
        window.Content = null;
        Advance(clock, window, 2);
        Assert.Null(window.MotionRoot.Diagnostics.LastError);
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
        window.Close();
    }

    [AvaloniaFact]
    public void MixedWeightsDistributeRemainingWidthAndRoundingRemainder()
    {
        using var clock = new MotionTestClockScope();
        var group = Blocks(241);
        ((ButtonGroupItem)group.Items[0]!).Weight = 1;
        ((ButtonGroupItem)group.Items[2]!).Weight = 3;
        var window = DialogTests.Show(group);
        Flush(window);
        Assert.Equal(new[] { 40d, 56d, 121d }, group.Items.Cast<ButtonGroupItem>().Select(item => item.Bounds.Width));
        window.Close();
    }

    [AvaloniaFact]
    public void EmptyGroupHasNoIndicatorOrMenuEntries()
    {
        using var clock = new MotionTestClockScope();
        var group = new ButtonGroup();
        var window = DialogTests.Show(group);
        Flush(window);
        Assert.Equal(default, group.Layout!.Row.Bounds.Size);
        Assert.Empty(group.Layout.Menu.Items);
        window.Close();
    }

    [AvaloniaFact]
    public void ChangingPressSourceRetainsProgressAndDisconnectsTheOldInput()
    {
        using var clock = new MotionTestClockScope();
        var first = new Button { Content = "First" };
        var second = new Button { Content = "Second" };
        var item = new ButtonGroupCustomItem { Content = first, PressSource = first };
        var group = new ButtonGroup { Items = { item, new ButtonGroupCustomItem { Content = second } } };
        var window = DialogTests.Show(group);
        Flush(window);
        var center = first.TranslatePoint(new Point(first.Bounds.Width / 2, first.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        Advance(clock, window, 5);
        var progress = item.PressProgress;
        Assert.True(progress > 0);

        item.PressSource = second;
        Assert.Equal(progress, item.PressProgress);
        window.MouseUp(center, MouseButton.Left);
        Advance(clock, window, 5);
        Assert.Equal(progress, item.PressProgress);
        Click(window, second);
        Advance(clock, window, 80);
        Assert.Equal(0, item.PressProgress);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData(180, 3)]
    [InlineData(140, 2)]
    [InlineData(100, 1)]
    [InlineData(20, 0)]
    public void OverflowRetainsTheFittingPrefix(int constraint, int visibleCount)
    {
        using var clock = new MotionTestClockScope();
        var group = Blocks(constraint);
        var window = DialogTests.Show(group);
        Flush(window);

        Assert.Equal(visibleCount, group.Layout!.VisibleCount);
        Assert.Equal(3 - visibleCount, group.Layout.Menu.Items.Count);
        Assert.All(group.Items.Cast<ButtonGroupItem>(), item => Assert.True(item.IsVisible));
        window.Close();
    }

    [AvaloniaFact]
    public void TokenBindingRespectsStyleLocalValueAndClearValue()
    {
        using var clock = new MotionTestClockScope();
        var group = Blocks(400);
        var window = DialogTests.Show(group);
        Flush(window);
        Assert.Equal(12, group.Spacing);
        Assert.Equal(.15, group.ExpandedRatio);

        group.Resources[Comp.ButtonGroup.Spacing] = 20d;
        Flush(window);
        Assert.Equal(20, group.Spacing);
        group.Styles.Add(new Style(selector => selector.OfType<ButtonGroup>())
        {
            Setters = { new Setter(ButtonGroup.SpacingProperty, 6d) }
        });
        Flush(window);
        Assert.Equal(6, group.Spacing);
        group.Spacing = 3;
        Flush(window);
        Assert.Equal(3, group.Spacing);
        group.ClearValue(ButtonGroup.SpacingProperty);
        Flush(window);
        Assert.Equal(6, group.Spacing);
        group.Styles.Clear();
        Flush(window);
        Assert.Equal(20, group.Spacing);
        window.Close();
    }

    [AvaloniaFact]
    public void ToggleRequestsStateWithoutChangingItAndMenuDismissesAfterCallback()
    {
        using var clock = new MotionTestClockScope();
        var toggle = new ButtonGroupToggleAction { Label = "Pinned", IsChecked = true };
        var group = new ButtonGroup { Items = { toggle } };
        var window = DialogTests.Show(group);
        Flush(window);
        var requested = new List<bool>();
        toggle.CheckedChangeRequested += (_, args) => requested.Add(args.Value);
        Click(window, toggle.ActionButton!);
        Assert.Equal([false], requested);
        Assert.True(toggle.IsChecked);

        var menuItem = (DropdownMenuItem)toggle.CreateMenuContent(group.MenuState);
        group.MenuState.Show();
        toggle.CheckedChangeRequested += (_, _) => Assert.True(group.MenuState.IsShowing);
        menuItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        Assert.Equal([false, false], requested);
        Assert.False(group.MenuState.IsShowing);
        window.Close();
    }

    [AvaloniaFact]
    public void ConnectedThemeConsumesTokensAndKeepsUserShape()
    {
        using var clock = new MotionTestClockScope();
        var button = new ToggleButton { Content = "Leading" };
        var window = DialogTests.Show(button);
        button.Theme = (ControlTheme)button.FindResource("MaterialConnectedLeadingToggleButtonTheme")!;
        Flush(window);
        Assert.Equal(new CornerRadius(1000, 8, 8, 1000), button.CornerRadius);
        Assert.Equal(new CornerRadius(1000, 4, 4, 1000), MaterialButton.GetPressedCornerRadius(button));
        button.Resources[Comp.ConnectedButtonGroup.InnerCornerRadius] = new CornerRadius(10);
        Flush(window);
        Assert.Equal(new CornerRadius(1000, 10, 10, 1000), button.CornerRadius);
        button.CornerRadius = new CornerRadius(2);
        button.IsChecked = true;
        Flush(window);
        Assert.Equal(new CornerRadius(2), button.CornerRadius);
        window.Close();
    }

    [AvaloniaFact]
    public void ShortPressExpandsBeforeReturningAndDetachReleasesAnimation()
    {
        using var clock = new MotionTestClockScope();
        var buttons = Enumerable.Range(0, 3).Select(_ => new Button { Content = new Marker(48, 16) }).ToArray();
        var items = buttons.Select(button => new ButtonGroupCustomItem { Content = button, PressSource = button }).ToArray();
        var group = new ButtonGroup { ExpandedRatio = .5 };
        foreach (var item in items)
            group.Items.Add(item);
        var window = DialogTests.Show(group);
        Flush(window);
        var initialWidth = items[1].Bounds.Width;
        Click(window, buttons[1]);
        for (var frame = 0; frame <= 8; frame++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Flush(window);
        }

        Assert.True(items[1].Bounds.Width > initialWidth);
        for (var frame = 0; frame < 80; frame++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Flush(window);
        }

        Assert.Equal(initialWidth, items[1].Bounds.Width);
        Assert.Equal(0, items[1].PressProgress);
        window.Content = null;
        Flush(window);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
        window.Close();
    }

    [AvaloniaFact]
    public void IconTemplateCreatesIndependentInstancesAndResizePreservesIntrinsicWidth()
    {
        using var clock = new MotionTestClockScope();
        var action = new ButtonGroupAction
        {
            Label = "Second",
            Icon = "edit",
            IconTemplate = new FuncDataTemplate<string>((_, _) => new Marker(20, 20))
        };
        var group = new ButtonGroup { MaxWidth = 400, Items = { new ButtonGroupAction { Label = "First" }, action } };
        var window = DialogTests.Show(group);
        Flush(window);
        Assert.Equal(2, group.Layout!.VisibleCount);
        var barIcon = Assert.Single(action.ActionButton!.GetVisualDescendants().OfType<Marker>());
        group.MaxWidth = 80;
        Flush(window);
        group.MenuState.Show();
        Advance(clock, window, 40);
        Assert.Contains(group.Layout.Menu.Items.OfType<DropdownMenuItem>(), item => Equals(item.LeadingIcon, "edit"));
        Assert.True(MaterialButton.GetIconTemplate(action.ActionButton!) is not null,
            $"Icon={MaterialButton.GetIcon(action.ActionButton!)}; template={MaterialButton.GetIconTemplate(action.ActionButton!)}; action template={action.IconTemplate}");
        var iconMenuItem = group.Layout.Menu.Items.OfType<DropdownMenuItem>().Single(item => Equals(item.LeadingIcon, "edit"));
        var menuIcon = Assert.Single(iconMenuItem.GetVisualDescendants().OfType<Marker>());
        Assert.NotSame(barIcon, menuIcon);
        group.MenuState.Dismiss();
        Advance(clock, window, 40);
        group.MaxWidth = 400;
        Flush(window);
        Assert.Equal(2, group.Layout.VisibleCount);
        Assert.Equal("edit", MaterialButton.GetIcon(action.ActionButton!));
        Assert.Throws<ArgumentException>(() => action.Icon = new Marker(20, 20));
        window.Close();
    }

    private static ButtonGroup Blocks(double maxWidth)
    {
        var group = new ButtonGroup
        {
            MaxWidth = maxWidth,
            OverflowIndicatorTemplate = new FuncDataTemplate<ButtonGroupMenuState>((_, _) => new Marker(32, 40))
        };
        foreach (var width in new[] { 40, 56, 32 })
            group.Items.Add(new ButtonGroupCustomItem { Content = new Marker(width, 24), MenuContent = new DropdownMenuItem { Content = "Overflow" } });
        return group;
    }

    private static void Click(Window window, Control control)
    {
        var center = control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;
        window.MouseMove(center);
        window.MouseDown(center, MouseButton.Left);
        window.MouseUp(center, MouseButton.Left);
        Flush(window);
    }

    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var frame = 0; frame < frames; frame++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Flush(window);
        }
    }

    private static void Flush(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
    }

    private sealed class Marker(double width, double height) : Control
    {
        protected override Size MeasureOverride(Size availableSize) => new Size(width, height).Constrain(availableSize);
        public override void Render(DrawingContext context) => context.FillRectangle(Brushes.Blue, new Rect(Bounds.Size));
    }
}


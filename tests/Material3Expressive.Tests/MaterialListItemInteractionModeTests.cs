// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:262-299,
// 1450-1603. A non-interactive ListItem passes onClick = null and therefore installs neither
// combinedClickable nor an interaction source. The alpha22 executable reference cannot render
// that alpha25 overload, so these are deterministic Avalonia behavior tests (parity unavailable).

using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Locks the one-control InteractionMode adaptation to the input contract of Compose's four
/// ListItem overloads. In particular, the ToggleButton base class must not make None rows behave
/// like inert buttons that still consume pointer and keyboard input.
/// </summary>
public sealed class MaterialListItemInteractionModeTests
{
    [AvaloniaFact]
    public void NoneMode_DoesNotClaimPointerOrKeyboardInput()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.None,
            Content = "Non-interactive row",
            Width = 240
        };
        var host = new Border { Child = item };
        var parentPresses = 0;
        var parentReleases = 0;
        var clicks = 0;
        host.PointerPressed += (_, _) => parentPresses++;
        host.PointerReleased += (_, _) => parentReleases++;
        item.Click += (_, _) => clicks++;

        var window = ThemeTestHelper.ShowWindow(host);
        var point = item.TranslatePoint(
                        new Point(item.Bounds.Width / 2, item.Bounds.Height / 2),
                        window)
                    ?? throw new InvalidOperationException("List item was not mounted.");

        window.MouseMove(point);
        Assert.False(InteractionOf(item).Interaction.HasFlag(InteractionState.Hovered));

        window.MouseDown(point, MouseButton.Left);

        Assert.False(item.IsPressed);
        Assert.False(InteractionOf(item).Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, parentPresses);
        Assert.Equal(0, clicks);

        window.MouseUp(point, MouseButton.Left);

        Assert.False(item.IsPressed);
        Assert.False(InteractionOf(item).Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, parentReleases);
        Assert.Equal(0, clicks);

        var enter = new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Key = Key.Enter
        };
        item.RaiseEvent(enter);

        Assert.False(enter.Handled);
        Assert.False(item.IsPressed);
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void InteractiveModes_KeepTheButtonInputRoute()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.Click,
            Content = "Action row",
            Width = 240
        };
        var host = new Border { Child = item };
        var parentPresses = 0;
        var clicks = 0;
        host.PointerPressed += (_, _) => parentPresses++;
        item.Click += (_, _) => clicks++;

        var window = ThemeTestHelper.ShowWindow(host);
        var point = item.TranslatePoint(
                        new Point(item.Bounds.Width / 2, item.Bounds.Height / 2),
                        window)
                    ?? throw new InvalidOperationException("List item was not mounted.");

        window.MouseDown(point, MouseButton.Left);

        Assert.True(item.IsPressed);
        Assert.True(InteractionOf(item).Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(0, parentPresses);

        window.MouseUp(point, MouseButton.Left);

        Assert.False(item.IsPressed);
        Assert.False(InteractionOf(item).Interaction.HasFlag(InteractionState.Pressed));
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void NoneMode_DoesNotLetTheInheritedCommandDisableTheRow()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.None,
            Content = "Status row",
            Command = new DisabledCommand()
        };

        ThemeTestHelper.ShowWindow(item);

        Assert.True(item.IsEnabled);
        Assert.True(item.IsEffectivelyEnabled);

        item.InteractionMode = ListItemInteractionMode.Click;

        Assert.False(item.IsEffectivelyEnabled);

        item.InteractionMode = ListItemInteractionMode.None;

        Assert.True(item.IsEffectivelyEnabled);
    }

    [AvaloniaFact]
    public void ChangingToNone_ClearsTheInheritedCheckedAndMaterialInputStates()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.MultiSelection,
            IsSelected = true,
            Content = "Selected row",
            Width = 240
        };
        var clicks = 0;
        item.Click += (_, _) => clicks++;

        var window = ThemeTestHelper.ShowWindow(item);
        var point = item.TranslatePoint(
                        new Point(item.Bounds.Width / 2, item.Bounds.Height / 2),
                        window)
                    ?? throw new InvalidOperationException("List item was not mounted.");

        Assert.True(item.IsChecked);
        window.MouseDown(point, MouseButton.Left);
        Assert.True(item.IsPressed);
        Assert.True(InteractionOf(item).Interaction.HasFlag(InteractionState.Pressed));

        item.InteractionMode = ListItemInteractionMode.None;

        Assert.True(item.IsSelected);
        Assert.False(item.IsChecked);
        Assert.False(InteractionOf(item).Interaction.HasFlag(InteractionState.Pressed));

        // Finish the button gesture that began before the live mode switch. It clears Button's
        // private pressed field without activating a row that is now static.
        window.MouseUp(point, MouseButton.Left);
        Assert.False(item.IsPressed);
        Assert.Equal(0, clicks);

        item.IsChecked = true;
        Assert.False(item.IsChecked);

        item.InteractionMode = ListItemInteractionMode.MultiSelection;
        Assert.True(item.IsChecked);
    }

    private static MaterialInteractionController InteractionOf(MaterialListItem item) =>
        MaterialElementContext.Ensure(item).Interaction;

    private sealed class DisabledCommand : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => false;

        public void Execute(object? parameter) => throw new InvalidOperationException("A disabled command cannot execute.");
    }
}

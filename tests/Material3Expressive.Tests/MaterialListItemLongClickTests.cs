// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:325-372,
// 403-454, 487-538, 642-687, 722-771, 806-855, and 1450-1602. Every interactive
// ListItem/SegmentedListItem overload accepts onLongClick/onLongClickLabel and delegates to
// combinedClickable. Avalonia's Holding gesture is the intentional desktop adaptation; the
// pinned alpha22 executable reference has no deterministic long-press capture checkpoint, so
// this file supplies deterministic Avalonia behavior and accessibility evidence.

using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialListItemLongClickTests
{
    [AvaloniaTheory]
    [InlineData(ListItemInteractionMode.Click)]
    [InlineData(ListItemInteractionMode.SingleSelection)]
    [InlineData(ListItemInteractionMode.MultiSelection)]
    public void InteractiveModes_ExposeOneUniformLongClickActionWithoutChangingSelection(
        ListItemInteractionMode mode)
    {
        var command = new RecordingCommand();
        var item = new MaterialListItem
        {
            InteractionMode = mode,
            LongClickCommand = command,
            LongClickCommandParameter = "secondary",
            IsSelected = false
        };
        var raised = 0;
        item.LongClick += (_, _) => raised++;

        Assert.True(InputElement.GetIsHoldingEnabled(item));
        Assert.True(InputElement.GetIsHoldWithMouseEnabled(item));

        item.InvokeLongClick();

        Assert.Equal(1, raised);
        Assert.Equal(1, command.ExecuteCount);
        Assert.Equal("secondary", command.LastParameter);
        Assert.False(item.IsSelected);
    }

    [AvaloniaFact]
    public void NoneMode_DoesNotEnableOrInvokeLongClickEvenWhenACommandIsSupplied()
    {
        var command = new RecordingCommand();
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.None,
            LongClickCommand = command
        };
        var raised = 0;
        item.LongClick += (_, _) => raised++;

        Assert.False(InputElement.GetIsHoldingEnabled(item));
        Assert.False(InputElement.GetIsHoldWithMouseEnabled(item));

        item.InvokeLongClick();

        Assert.Equal(0, raised);
        Assert.Equal(0, command.ExecuteCount);
    }

    [AvaloniaFact]
    public void HoldingGesture_ConsumesTheFollowingPrimaryClick()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.MultiSelection,
            Content = "Hold to organize",
            Width = 240
        };
        var longClicks = 0;
        var clicks = 0;
        item.LongClick += (_, _) => longClicks++;
        item.Click += (_, _) => clicks++;

        var window = ThemeTestHelper.ShowWindow(item);
        var point = item.TranslatePoint(
                        new Point(item.Bounds.Width / 2, item.Bounds.Height / 2),
                        window)
                    ?? throw new InvalidOperationException("List item was not mounted.");

        window.MouseDown(point, MouseButton.Left);
        Assert.True(item.IsPressed);

        var holding = CreateHoldingStartedEvent();
        item.RaiseEvent(holding);

        Assert.True(holding.Handled);
        Assert.Equal(1, longClicks);
        Assert.False(item.IsSelected);

        window.MouseUp(point, MouseButton.Left);

        Assert.False(item.IsPressed);
        Assert.False(item.IsSelected);
        Assert.Equal(0, clicks);
    }

    [AvaloniaFact]
    public void LongClickLabel_SupplementsHelpTextWithoutReplacingAnExplicitAutomationDescription()
    {
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.Click,
            LongClickCommand = new RecordingCommand(),
            LongClickLabel = "Show row actions"
        };
        var peer = ControlAutomationPeer.CreatePeerForElement(item);

        Assert.Equal("Show row actions", peer.GetHelpText());

        AutomationProperties.SetHelpText(item, "Application guidance");

        Assert.Equal("Application guidance", peer.GetHelpText());
    }

    [AvaloniaFact]
    public void HandledLongClickEvent_DoesNotAlsoExecuteTheCommand()
    {
        var command = new RecordingCommand();
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.Click,
            LongClickCommand = command
        };
        item.LongClick += (_, args) => args.Handled = true;

        item.InvokeLongClick();

        Assert.Equal(0, command.ExecuteCount);
    }

    [AvaloniaFact]
    public void DisabledLongClickCommand_DoesNotConsumePrimaryClick_AndTracksCanExecuteChanges()
    {
        var command = new RecordingCommand { IsEnabled = false };
        var item = new MaterialListItem
        {
            InteractionMode = ListItemInteractionMode.Click,
            LongClickCommand = command,
            Content = "Open"
        };
        var clicks = 0;
        item.Click += (_, _) => clicks++;
        ThemeTestHelper.ShowWindow(item);

        Assert.False(InputElement.GetIsHoldingEnabled(item));

        item.InvokeLongClick();
        item.InvokeFromAutomation();

        Assert.Equal(0, command.ExecuteCount);
        Assert.Equal(1, clicks);

        command.IsEnabled = true;

        Assert.True(InputElement.GetIsHoldingEnabled(item));
        Assert.True(InputElement.GetIsHoldWithMouseEnabled(item));
    }

    private static HoldingRoutedEventArgs CreateHoldingStartedEvent() =>
        Assert.IsType<HoldingRoutedEventArgs>(Activator.CreateInstance(
            typeof(HoldingRoutedEventArgs),
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            args: [HoldingState.Started, new Point(), PointerType.Mouse, null],
            culture: null));

    private sealed class RecordingCommand : ICommand
    {
        private bool _isEnabled = true;

        public int ExecuteCount { get; private set; }

        public object? LastParameter { get; private set; }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled == value)
                    return;

                _isEnabled = value;
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => IsEnabled;

        public void Execute(object? parameter)
        {
            ExecuteCount++;
            LastParameter = parameter;
        }
    }
}

using System.Windows.Input;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public class ListItemLongClickAutomationTests
{
    [AvaloniaTheory]
    [InlineData(false, ListItemInteractionMode.Click)]
    [InlineData(false, ListItemInteractionMode.SingleSelection)]
    [InlineData(false, ListItemInteractionMode.MultipleSelection)]
    [InlineData(true, ListItemInteractionMode.Click)]
    [InlineData(true, ListItemInteractionMode.SingleSelection)]
    [InlineData(true, ListItemInteractionMode.MultipleSelection)]
    public void NamedLongClickIsIndependentOfClickAndControlledSelection(bool segmented, ListItemInteractionMode mode)
    {
        using var clock = new MotionTestClockScope();
        var item = Create(segmented, mode);
        var clicks = 0;
        var requests = 0;
        var longClicks = 0;
        item.Click += (_, _) => clicks++;
        item.CheckedChangeRequested += (_, _) => requests++;
        item.LongClick += (_, _) => longClicks++;
        var window = Show(item);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            var action = Action(peer)!;
            Assert.Equal("More actions", action.GetName());
            Assert.Equal("More actions", peer.GetHelpText());
            Assert.Same(peer, action.GetParent());
            Assert.False(action.IsKeyboardFocusable());
            Assert.False(action.HasKeyboardFocus());
            Assert.Equal(AutomationControlType.Button, action.GetAutomationControlType());
            var original = item.Bounds;
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, longClicks);
            Assert.Equal(0, clicks);
            Assert.Equal(0, requests);
            Assert.True(item.IsSelected);
            Assert.True(item.IsChecked);
            Assert.False(item.IsPressed);
            Assert.False(item.IsFocused);
            Assert.Equal(original, item.Bounds);

            if (mode == ListItemInteractionMode.MultipleSelection)
                peer.GetProvider<IToggleProvider>()!.Toggle();
            else if (mode == ListItemInteractionMode.SingleSelection)
                peer.GetProvider<ISelectionItemProvider>()!.Select();
            else
                peer.GetProvider<IInvokeProvider>()!.Invoke();
            Assert.Equal(mode == ListItemInteractionMode.MultipleSelection ? 1 : 0, requests);
            Assert.Equal(mode == ListItemInteractionMode.MultipleSelection ? 0 : 1, clicks);
            Assert.Equal(1, longClicks);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void EventLabelAndModeChangesUpdateTheActionAndInvalidateStaleReferences()
    {
        using var clock = new MotionTestClockScope();
        var item = Create(false, ListItemInteractionMode.Click);
        var calls = 0;
        EventHandler handler = (_, _) => calls++;
        var window = Show(item);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            Assert.Null(Action(peer)); // A description without an operation is not an action.
            item.LongClick += handler;
            var action = Action(peer)!;
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, calls);
            item.LongClickLabel = "Archive";
            Assert.Same(action, Action(peer));
            Assert.Equal("Archive", action.GetName());
            item.LongClickLabel = null;
            Assert.Null(Action(peer));
            Assert.False(action.IsEnabled());
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, calls);
            item.LongClickLabel = "Archive";
            item.InteractionMode = ListItemInteractionMode.None;
            Assert.Null(Action(peer));
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, calls);
            item.InteractionMode = ListItemInteractionMode.MultipleSelection;
            Assert.Same(action, Action(peer));
            item.LongClick -= handler;
            Assert.Null(Action(peer));
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, calls);
            item.LongClick += handler;
            window.Content = null;
            Assert.Null(Action(peer));
            Assert.False(action.IsEnabled());
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, calls);
            window.Content = item;
            window.UpdateLayout();
            Assert.Same(action, Action(peer));
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(2, calls);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void DisabledRowsRetainDisabledActionsAndDoNotDisableNestedControls()
    {
        using var clock = new MotionTestClockScope();
        var child = new Button { Content = "Help" };
        var item = Create(true, ListItemInteractionMode.Click);
        item.TrailingContent = child;
        var calls = 0;
        item.LongClick += (_, _) => calls++;
        var window = Show(item);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            var childPeer = ControlAutomationPeer.CreatePeerForElement(child)!;
            var action = Action(peer)!;
            Assert.Contains(childPeer, Descendants(peer));
            item.IsInteractionEnabled = false;
            Assert.Same(action, Action(peer));
            Assert.False(action.IsEnabled());
            Assert.True(childPeer.IsEnabled());
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(0, calls);
            item.IsInteractionEnabled = true;
            item.IsEnabled = false;
            Assert.False(action.IsEnabled());
            Assert.False(childPeer.IsEnabled());
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(0, calls);
            item.IsEnabled = true;
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, calls);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CommandAvailabilityParameterAndReplacementAreObservedWithoutRetainingDetachedOwners()
    {
        using var clock = new MotionTestClockScope();
        var first = new TestCommand();
        var second = new TestCommand();
        var item = Create(false, ListItemInteractionMode.Click);
        item.LongClickCommand = first;
        item.LongClickCommandParameter = "one";
        var window = Show(item);
        try
        {
            Assert.Equal(1, first.Subscribers);
            var peer = ControlAutomationPeer.CreatePeerForElement(item)!;
            var action = Action(peer)!;
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(new object?[] { "one" }, first.Parameters);
            first.Enabled = false;
            Assert.False(action.IsEnabled());
            ((IInvokeProvider)action).Invoke();
            Assert.Single(first.Parameters);
            var events = 0;
            EventHandler handler = (_, _) => events++;
            item.LongClick += handler;
            Assert.True(action.IsEnabled()); // An independently registered event still executes.
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(1, events);
            Assert.Single(first.Parameters);
            item.LongClick -= handler;
            item.LongClickCommand = second;
            item.LongClickCommandParameter = "two";
            Assert.Equal(0, first.Subscribers);
            Assert.Equal(1, second.Subscribers);
            ((IInvokeProvider)action).Invoke();
            Assert.Equal(new object?[] { "two" }, second.Parameters);
            window.Content = null;
            Assert.Equal(0, second.Subscribers);
            ((IInvokeProvider)action).Invoke();
            Assert.Single(second.Parameters);
            window.Content = item;
            window.UpdateLayout();
            Assert.Equal(1, second.Subscribers);
            item.LongClickCommand = null;
            Assert.Equal(0, second.Subscribers);
            Assert.Null(Action(peer));
        }
        finally { window.Close(); }
        Assert.Equal(0, second.Subscribers);
    }

    private static ListItem Create(bool segmented, ListItemInteractionMode mode)
    {
        ListItem item = segmented ? new SegmentedListItem() : new ListItem();
        item.Content = new Border { Width = 64, Height = 24 };
        item.Width = 280;
        item.InteractionMode = mode;
        item.LongClickLabel = "More actions";
        item.IsSelected = item.IsChecked = true;
        return item;
    }
    private static AutomationPeer? Action(AutomationPeer peer) => peer.GetChildren().SingleOrDefault(p => p.GetAutomationId() == "ListItem.LongClick");
    private static IEnumerable<AutomationPeer> Descendants(AutomationPeer peer) => peer.GetChildren().SelectMany(child => new[] { child }.Concat(Descendants(child)));
    private static MotionTestWindow Show(Control content)
    {
        var window = new MotionTestWindow { Width = 320, Height = 240 };
        window.Styles.Add(new MaterialTheme());
        window.Content = content;
        window.Show();
        window.UpdateLayout();
        return window;
    }
    private sealed class TestCommand : ICommand
    {
        private EventHandler? _changed;
        private bool _enabled = true;
        internal int Subscribers { get; private set; }
        internal List<object?> Parameters { get; } = [];
        internal bool Enabled { get => _enabled; set { _enabled = value; _changed?.Invoke(this, EventArgs.Empty); } }
        public bool CanExecute(object? parameter) => _enabled;
        public void Execute(object? parameter) => Parameters.Add(parameter);
        public event EventHandler? CanExecuteChanged
        {
            add { _changed += value; Subscribers++; }
            remove { _changed -= value; Subscribers--; }
        }
    }
}

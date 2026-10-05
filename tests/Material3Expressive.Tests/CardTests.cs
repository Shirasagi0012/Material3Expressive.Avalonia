using System.Windows.Input;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public class CardTests
{
    [AvaloniaFact]
    public void PassiveElevation_RemembersInitialHeight_UntilModeOrMountChanges()
    {
        using var clock = new MotionTestClockScope();
        var card = new ElevatedCard { Elevation = new CardElevation { Default = 2 },
            Content = new Border { Width = 64, Height = 24 } };
        var window = Show(card); var parent = (Grid)window.Content!;
        try
        {
            Assert.Equal(2, Surface(card).ShadowElevation);
            card.Elevation = new CardElevation { Default = 1 };
            Assert.Equal(2, Surface(card).ShadowElevation);
            var template = card.Template; card.Template = null; window.UpdateLayout();
            card.Template = template; window.UpdateLayout();
            Assert.Equal(2, Surface(card).ShadowElevation);
            // Return template selection to the theme before testing its two mode templates.
            card.ClearValue(Card.TemplateProperty); window.UpdateLayout();
            card.IsClickable = true; window.UpdateLayout();
            Assert.Equal(1, Surface(card).ShadowElevation);
            card.IsClickable = false; window.UpdateLayout();
            Assert.Equal(1, Surface(card).ShadowElevation);
            parent.Children.Remove(card);
            card.Elevation = new CardElevation { Default = 4 };
            parent.Children.Add(card); window.UpdateLayout();
            Assert.Equal(4, Surface(card).ShadowElevation);
            Assert.Null(MotionTestClockScope.Error(card));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ClickableCard_ChildButtonOwnsItsPointerAndKeyboardFeedback()
    {
        using var clock = new MotionTestClockScope();
        var child = new Button { Width = 80, Height = 48, Content = "Child" };
        var card = new Card { IsClickable = true, Width = 160, Height = 96,
            Content = new CardColumn { Children = { child } } };
        var window = Show(card); var ownClicks = 0; var childClicks = 0;
        card.Click += (_, e) => { if (ReferenceEquals(e.Source, card)) ownClicks++; };
        child.Click += (_, _) => childClicks++;
        try
        {
            var point = Center(child, window);
            window.MouseDown(point, MouseButton.Left);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(card).RippleCount);
            window.MouseUp(point, MouseButton.Left);
            Assert.Equal(1, childClicks); Assert.Equal(0, ownClicks);
            child.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(0, MaterialInteraction.GetOrCreate(card).RippleCount);
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
            Assert.Equal(2, childClicks); Assert.Equal(0, ownClicks);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void CompiledXaml_ConfiguresColorsElevationAndColumn()
    {
        var card = (OutlinedCard)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive.Tests/Fixtures/CardConfiguration.axaml"));
        var window = Show(card);
        try
        {
            Assert.True(card.IsClickable);
            Assert.Equal(Colors.White, ((ISolidColorBrush)card.Foreground!).Color);
            Assert.Equal(2, Surface(card).ShadowElevation);
            Assert.Equal(9, card.Elevation!.Dragged);
            Assert.Equal(2, ((CardColumn)card.Content!).Children.Count);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("filled", false, 32, 16, 0)]
    [InlineData("elevated", false, 32, 16, 1)]
    [InlineData("outlined", false, 32, 16, 0)]
    [InlineData("filled", true, 48, 48, 0)]
    [InlineData("elevated", true, 48, 48, 1)]
    [InlineData("outlined", true, 48, 48, 0)]
    public void AutomaticSize_SeparatesContentSurfaceAndInteractiveSpace(string kind, bool clickable, double width, double height, double elevation)
    {
        var card = Create(kind); card.IsClickable = clickable;
        var window = Show(card);
        try
        {
            Assert.Equal(new Size(width, height), card.Bounds.Size);
            Assert.Equal(new Size(32, 16), Surface(card).Bounds.Size);
            Assert.Equal(elevation, Surface(card).ShadowElevation);
            Assert.Equal(new CornerRadius(12), card.CornerRadius);
            Assert.Equal(clickable, card.Focusable);
            Assert.Equal(new Thickness(kind == "outlined" ? 1 : 0), card.BorderThickness);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PassiveCommand_DoesNotDisableInteractiveContent()
    {
        var command = new CountingCommand { CanRun = false };
        var child = new Button { Content = "Child" };
        var card = new Card { Command = command, Content = child };
        var window = Show(card);
        try
        {
            Assert.True(card.IsEffectivelyEnabled); Assert.True(child.IsEffectivelyEnabled);
            card.IsClickable = true; window.UpdateLayout();
            Assert.False(card.IsEffectivelyEnabled); Assert.False(child.IsEffectivelyEnabled);
            card.IsClickable = false; window.UpdateLayout();
            Assert.True(card.IsEffectivelyEnabled); Assert.True(child.IsEffectivelyEnabled);
            Assert.Equal(0, command.Count);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void Column_AllocatesRemainingHeight_IncludingZero(bool clickable)
    {
        var first = new SizedContent(new Size(64, 24));
        var second = new SizedContent(new Size(32, 12));
        var card = new OutlinedCard { IsClickable = clickable, Width = 160, Height = 12,
            Content = new CardColumn { Children = { first, second } } };
        var window = Show(card);
        try
        {
            Assert.Equal(new Size(64, 12), first.Bounds.Size);
            Assert.Equal(new Size(32, 0), second.Bounds.Size);
            Assert.Equal(12, second.Bounds.Y);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PassiveCard_DoesNotConsumeChildInputOrExposeInvoke_AndModeChangesUpdateSamePeer()
    {
        var child = new Button { Content = "Child" };
        var card = new Card { Content = child, Width = 160, Height = 80 };
        var window = Show(card); var childClicks = 0; var ownClicks = 0;
        child.Click += (_, _) => childClicks++;
        card.Click += (_, e) => { if (ReferenceEquals(e.Source, card)) ownClicks++; };
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(card);
            Assert.Null(peer.GetProvider<IInvokeProvider>());
            Click(child, window); Assert.Equal(1, childClicks); Assert.Equal(0, ownClicks);
            card.IsClickable = true; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.NotNull(peer.GetProvider<IInvokeProvider>());
            peer.GetProvider<IInvokeProvider>()!.Invoke(); Assert.Equal(1, ownClicks);
            card.IsClickable = false; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.Null(peer.GetProvider<IInvokeProvider>());
            Assert.False(card.IsFocused); Assert.False(card.Focusable);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NativeActivation_ExecutesCommandOnce_AndDisabledInputDoesNothing()
    {
        var command = new CountingCommand();
        var card = new ElevatedCard { IsClickable = true, Command = command, Content = new Border { Width = 80, Height = 64 } };
        var window = Show(card);
        try
        {
            Click(card, window); Assert.Equal(1, command.Count);
            card.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(1, command.Count);
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(2, command.Count);
            window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(3, command.Count);
            window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Assert.Equal(3, command.Count);
            card.IsEnabled = false; Click(card, window); Assert.Equal(3, command.Count);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void PassiveDisabledContainer_KeepsNormalAppearance_AndExplicitPaintSurvivesUpdates()
    {
        var card = new OutlinedCard { Content = new Border { Width = 80, Height = 64 } };
        var window = Show(card);
        try
        {
            var background = ((ISolidColorBrush)card.Background!).Color;
            var ink = ((ISolidColorBrush)card.Foreground!).Color;
            var border = ((ISolidColorBrush)card.BorderBrush!).Color;
            card.IsEnabled = false;
            Assert.Equal(background, ((ISolidColorBrush)card.Background!).Color);
            Assert.Equal(ink, ((ISolidColorBrush)card.Foreground!).Color);
            Assert.Equal(border, ((ISolidColorBrush)card.BorderBrush!).Color);
            card.IsClickable = true; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(97, ((ISolidColorBrush)card.Foreground!).Color.A);
            card.Background = Brushes.Red; card.Foreground = Brushes.Blue; card.BorderBrush = Brushes.Green; card.BorderThickness = new Thickness(3);
            card.Resources[MaterialTokens.Sys.Color.Surface] = Colors.Black;
            card.IsEnabled = true; card.ContainerColor = Colors.Yellow;
            Assert.Same(Brushes.Red, card.Background); Assert.Same(Brushes.Blue, card.Foreground);
            Assert.Same(Brushes.Green, card.BorderBrush); Assert.Equal(new Thickness(3), card.BorderThickness);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Colors_DeriveContentFromEnabledContainer_AndTrackAmbientAndTokens()
    {
        var card = new Card { ContainerColor = Colors.Red, Content = new Border { Width = 80, Height = 64 } };
        var window = Show(card); var parent = (Grid)window.Content!;
        try
        {
            Avalonia.Controls.Documents.TextElement.SetForeground(parent, Brushes.Blue);
            Assert.Equal(Colors.Blue, ((ISolidColorBrush)card.Foreground!).Color);
            card.ContainerColor = (Color)window.FindResource(MaterialTokens.Sys.Color.Primary)!;
            Assert.Equal((Color)window.FindResource(MaterialTokens.Sys.Color.OnPrimary)!, ((ISolidColorBrush)card.Foreground!).Color);
            card.Resources[MaterialTokens.Sys.Color.OnPrimary] = Colors.Green;
            Assert.Equal(Colors.Green, ((ISolidColorBrush)card.Foreground!).Color);
            card.IsClickable = true; card.IsEnabled = false; window.UpdateLayout();
            Assert.Equal(97, ((ISolidColorBrush)card.Foreground!).Color.A);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void Elevation_LastInteractionAndHeightInference_AndDisableSnapFollowCardPolicy()
    {
        using var clock = new MotionTestClockScope();
        var card = new OutlinedCard { IsClickable = true, Content = new Border { Width = 80, Height = 64 },
            Elevation = new CardElevation { Default = 2, Pressed = 3, Focused = 4, Hovered = 5, Dragged = 9, Disabled = 1 } };
        var window = Show(card);
        try
        {
            Assert.Equal(2, Surface(card).ShadowElevation);
            card.Focus(NavigationMethod.Tab); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
            Assert.Equal(4, Surface(card).ShadowElevation);
            var center = Center(card, window); window.MouseMove(center);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(120));
            Assert.Equal(5, Surface(card).ShadowElevation); // Later hover wins over existing focus.
            window.MouseDown(center, MouseButton.Left);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); clock.AdvanceBy(TimeSpan.FromMilliseconds(60));
            Assert.InRange(Surface(card).ShadowElevation, 3, 5);
            card.IsEnabled = false; clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(1, Surface(card).ShadowElevation);
            Assert.Null(MotionTestClockScope.Error(card));
            Assert.Equal(9, Card.Target(new CardElevation { Default = 2, Pressed = 3, Focused = 4, Hovered = 5, Dragged = 9, Disabled = 1 }, InteractionState.Dragged, true));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void ModeAndTemplateReplacementDuringPress_ReleaseScopesAndDoNotActivate()
    {
        using var clock = new MotionTestClockScope();
        var card = new Card { IsClickable = true, Content = new Border { Width = 80, Height = 64 } };
        var window = Show(card); var clicks = 0; card.Click += (_, _) => clicks++;
        try
        {
            var point = Center(card, window); window.MouseDown(point, MouseButton.Left);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            card.IsClickable = false; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            window.MouseUp(point, MouseButton.Left); Assert.Equal(0, clicks); Assert.False(card.IsPressed);
            window.MouseMove(point);
            Assert.Equal(InteractionState.None, MaterialInteraction.GetOrCreate(card).LastInteraction);
            card.IsClickable = true; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.Equal(InteractionState.None, MaterialInteraction.GetOrCreate(card).LastInteraction);
            window.MouseMove(point);
            Assert.Equal(InteractionState.Hovered, MaterialInteraction.GetOrCreate(card).LastInteraction);
            var old = Surface(card); var template = card.Template;
            card.Template = null; window.UpdateLayout(); card.Template = template; window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
            Assert.NotSame(old, Surface(card)); Click(card, window); Assert.Equal(1, clicks);
            Assert.Null(MotionTestClockScope.Error(card));
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    static Card Create(string kind) => kind switch
    {
        "elevated" => new ElevatedCard { Content = new Border { Width = 32, Height = 16 } },
        "outlined" => new OutlinedCard { Content = new Border { Width = 32, Height = 16 } },
        _ => new Card { Content = new Border { Width = 32, Height = 16 } },
    };
    static MaterialSurface Surface(Card card) => card.GetVisualDescendants().OfType<MaterialSurface>().Single();
    static Point Center(Control c, Window w) => c.TranslatePoint(new Point(c.Bounds.Width / 2, c.Bounds.Height / 2), w)!.Value;
    static void Click(Control c, Window w) { var point = Center(c, w); w.MouseDown(point, MouseButton.Left); w.MouseUp(point, MouseButton.Left); }
    static Window Show(Card card)
    {
        card.HorizontalAlignment = HorizontalAlignment.Center; card.VerticalAlignment = VerticalAlignment.Center;
        var window = new MotionTestWindow { Width = 320, Height = 160 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { card } }; window.Show(); window.UpdateLayout(); return window;
    }
    sealed class CountingCommand : ICommand
    {
        public int Count { get; private set; }
        public bool CanRun { get; init; } = true;
        public bool CanExecute(object? parameter) => CanRun;
        public void Execute(object? parameter) => Count++;
        public event EventHandler? CanExecuteChanged { add { } remove { } }
    }

    sealed class SizedContent(Size preferred) : Control
    {
        protected override Size MeasureOverride(Size available) => new(Math.Min(preferred.Width, available.Width), Math.Min(preferred.Height, available.Height));
    }
}

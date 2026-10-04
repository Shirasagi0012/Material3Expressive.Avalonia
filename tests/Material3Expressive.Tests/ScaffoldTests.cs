using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class ScaffoldTests
{
    [AvaloniaFact]
    public void BodyReceivesFullConstraints_AndPaddingBeforeMeasurement()
    {
        var scaffold = new Scaffold { TopBar = new Border { Height = 40 }, BottomBar = new Border { Height = 32 },
            ContentWindowInsets = new Thickness(8, 24, 12, 16) };
        var body = new MeasuringBody(scaffold); scaffold.Content = body;
        var window = Show(scaffold);
        try
        {
            Assert.Single(scaffold.GetVisualDescendants().OfType<ScaffoldLayout>());
            Assert.Equal(new Size(240, 240), body.LastAvailable);
            Assert.Equal(new Thickness(8, 40, 12, 32), body.PaddingAtMeasure);
            Assert.Equal(new Point(0, 0), body.TranslatePoint(default, scaffold));
            Assert.Equal(new Size(240, 240), body.Bounds.Size);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(FabPosition.Start, false, 24, 136)]
    [InlineData(FabPosition.Center, false, 90, 136)]
    [InlineData(FabPosition.End, false, 156, 136)]
    [InlineData(FabPosition.EndOverlay, false, 156, 152)]
    [InlineData(FabPosition.Start, true, 156, 136)]
    [InlineData(FabPosition.Center, true, 90, 136)]
    [InlineData(FabPosition.End, true, 24, 136)]
    [InlineData(FabPosition.EndOverlay, true, 24, 152)]
    public void FabPlacement_UsesPhysicalInsetsAndDirection(FabPosition position, bool rtl, double x, double y)
    {
        var fab = new Border { Width = 56, Height = 56 };
        var snackbar = new Border { Width = 120, Height = 24 };
        var scaffold = new Scaffold { FloatingActionButton = fab, SnackbarHost = snackbar,
            BottomBar = new Border { Height = 32 }, ContentWindowInsets = new Thickness(8, 24, 12, 16),
            FloatingActionButtonPosition = position, FlowDirection = rtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
        var window = Show(scaffold);
        try
        {
            // Window coordinates are physical; Scaffold-local coordinates carry its RTL mirror.
            Assert.Equal(new Point(x, y), PhysicalOrigin(fab, window));
            Assert.Equal(new Point(58, y - 24), PhysicalOrigin(snackbar, window));
            Assert.Equal(new Thickness(rtl ? 12 : 8, 24, rtl ? 8 : 12, 32), scaffold.ContentPadding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void EmptyAndCollapsedBars_AreDifferentAndUpdatePadding()
    {
        var scaffold = new Scaffold { ContentWindowInsets = new Thickness(8, 24, 12, 16) };
        var body = new Border(); body.Bind(Border.PaddingProperty, scaffold.GetObservable(Scaffold.ContentPaddingProperty)); scaffold.Content = body;
        var window = Show(scaffold);
        try
        {
            Assert.Equal(new Thickness(8, 24, 12, 16), body.Padding);
            scaffold.TopBar = new Border { Height = 0 }; scaffold.BottomBar = new Border { Height = 0 }; window.UpdateLayout();
            Assert.Equal(new Thickness(8, 0, 12, 0), body.Padding);
            ((Border)scaffold.TopBar).Height = 48; ((Border)scaffold.BottomBar).Height = 28; window.UpdateLayout();
            Assert.Equal(new Thickness(8, 48, 12, 28), body.Padding);
            scaffold.TopBar = scaffold.BottomBar = null; window.UpdateLayout();
            Assert.Equal(new Thickness(8, 24, 12, 16), body.Padding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void InsetsConstrainFabAndSnackbar_ButNotBarsOrBody()
    {
        var fab = new MeasuringBody(null); var snackbar = new MeasuringBody(null); var top = new MeasuringBody(null);
        var scaffold = new Scaffold { FloatingActionButton = fab, SnackbarHost = snackbar, TopBar = top,
            ContentWindowInsets = new Thickness(20, 50, 30, 40) };
        var window = Show(scaffold);
        try
        {
            Assert.Equal(new Size(190, 200), fab.LastAvailable);
            Assert.Equal(fab.LastAvailable, snackbar.LastAvailable);
            Assert.Equal(new Size(240, 240), top.LastAvailable);
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData(0, 56)]
    [InlineData(56, 0)]
    public void ZeroDimensionFab_DoesNotMoveSnackbar(double width, double height)
    {
        var snack = new Border { Width = 120, Height = 24 };
        var scaffold = new Scaffold { FloatingActionButton = new Border { Width = width, Height = height },
            SnackbarHost = snack, ContentWindowInsets = new Thickness(0, 0, 0, 16) };
        var window = Show(scaffold);
        try { Assert.Equal(200, snack.TranslatePoint(default, scaffold)!.Value.Y); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void NestedConsumption_AddsPadding_ClampsExclusion_AndFollowsReparenting()
    {
        var inner = new Scaffold { ContentWindowInsets = new Thickness(20, 30, 40, 50) };
        var first = new Border { Child = inner };
        var outer = new Border { Child = first };
        MaterialWindowInsets.SetConsumePadding(outer, new Thickness(8, 12, 16, 20));
        MaterialWindowInsets.SetConsumePadding(first, new Thickness(4, 6, 8, 10));
        var window = Show(outer);
        try
        {
            Assert.Equal(new Thickness(8, 12, 16, 20), inner.ContentPadding);
            MaterialWindowInsets.SetConsumePadding(outer, new Thickness(30, 40, 50, 60)); window.UpdateLayout();
            Assert.Equal(default, inner.ContentPadding);
            first.Child = null; window.Content = null;
            var newParent = new Border { Child = inner, FlowDirection = FlowDirection.RightToLeft };
            MaterialWindowInsets.SetConsumePadding(newParent, new Thickness(3, 5, 7, 9));
            window.Content = newParent; window.UpdateLayout();
            Assert.Equal(new Thickness(37, 25, 13, 41), inner.ContentPadding);
            // The old provider cannot retain a subscription into the detached subtree.
            MaterialWindowInsets.SetConsumePadding(outer, new Thickness(100)); window.UpdateLayout();
            Assert.Equal(new Thickness(37, 25, 13, 41), inner.ContentPadding);
            newParent.ClearValue(MaterialWindowInsets.ConsumePaddingProperty); window.UpdateLayout();
            Assert.Equal(new Thickness(40, 30, 20, 50), inner.ContentPadding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ConsumingBodyPadding_PreventsNestedScaffoldDoubleInsets()
    {
        var outer = new Scaffold { ContentWindowInsets = new Thickness(8, 24, 12, 16) };
        var inner = new Scaffold { ContentWindowInsets = outer.ContentWindowInsets };
        var body = new Border { Child = inner };
        body.Bind(Border.PaddingProperty, outer.GetObservable(Scaffold.ContentPaddingProperty));
        body.Bind(MaterialWindowInsets.ConsumePaddingProperty, outer.GetObservable(Scaffold.ContentPaddingProperty));
        outer.Content = body;
        var window = Show(outer);
        try
        {
            Assert.Equal(new Size(220, 200), inner.Bounds.Size);
            Assert.Equal(default, inner.ContentPadding);
            outer.TopBar = new Border { Height = 48 }; window.UpdateLayout();
            Assert.Equal(new Size(220, 176), inner.Bounds.Size);
            Assert.Equal(default, inner.ContentPadding);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void SlotTemplatesAndReplacement_PreserveContentAndLogicalParenting()
    {
        var scaffold = new Scaffold { TopBar = "bar", TopBarTemplate = new FuncDataTemplate<string>((_, _) => new Border { Height = 40 }),
            Content = "body", ContentTemplate = new FuncDataTemplate<string>((_, _) => new Border()),
            BottomBar = new Border { Height = 20 } };
        var old = (Control)scaffold.BottomBar;
        var window = Show(scaffold);
        try
        {
            Assert.Equal(40, scaffold.ContentPadding.Top);
            Assert.Same(scaffold, old.Parent);
            scaffold.BottomBar = new Border { Height = 32 }; window.UpdateLayout();
            Assert.Null(old.Parent);
            Assert.Equal(32, scaffold.ContentPadding.Bottom);
            scaffold.TopBarTemplate = new FuncDataTemplate<string>((_, _) => new Border { Height = 12 }); window.UpdateLayout();
            Assert.Equal(12, scaffold.ContentPadding.Top);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void ZOrderPlacesFabAboveBottomBar_AndBodyBelowTopBar()
    {
        var body = new Border { Background = Brushes.Red };
        var top = new Border { Width = 240, Height = 240, Background = Brushes.Blue };
        var bottom = new Border { Width = 240, Height = 240, Background = Brushes.Green };
        var fab = new Border { Width = 56, Height = 56, Background = Brushes.Yellow };
        var scaffold = new Scaffold { Content = body, TopBar = top, BottomBar = bottom, FloatingActionButton = fab,
            FloatingActionButtonPosition = FabPosition.EndOverlay };
        var window = Show(scaffold);
        try
        {
            window.MouseMove(new Point(200, 200));
            Assert.True(fab.IsPointerOver);
            Assert.False(bottom.IsPointerOver);
            window.MouseMove(new Point(20, 20)); Assert.True(bottom.IsPointerOver); Assert.False(top.IsPointerOver);
            scaffold.BottomBar = null; window.UpdateLayout();
            window.MouseMove(new Point(21, 21)); Assert.True(top.IsPointerOver); Assert.False(body.IsPointerOver);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TemplateReplacementAndReattachment_ContinuePublishingPadding()
    {
        var scaffold = new Scaffold { TopBar = new Border { Height = 40 } };
        var window = Show(scaffold);
        try
        {
            var template = scaffold.Template;
            scaffold.Template = new FuncControlTemplate<Scaffold>((_, _) => new Border()); scaffold.ApplyTemplate(); window.UpdateLayout();
            scaffold.Template = template; scaffold.ApplyTemplate(); window.UpdateLayout();
            Assert.Equal(40, scaffold.ContentPadding.Top);
            ((Grid)window.Content!).Children.Clear(); window.Content = null;
            scaffold.TopBar = new Border { Height = 60 }; window.Content = new Grid { Children = { scaffold } }; window.UpdateLayout();
            Assert.Equal(60, scaffold.ContentPadding.Top);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void KeyboardTraversalAndAccessibility_PreserveAllFiveSlots()
    {
        var top = new Button { Content = "top" }; var body = new Button { Content = "body" };
        var snackbar = new Button { Content = "snackbar" }; var fab = new Button { Content = "fab" };
        var bottom = new Button { Content = "bottom" };
        var scaffold = new Scaffold { TopBar = top, Content = body, SnackbarHost = snackbar, FloatingActionButton = fab, BottomBar = bottom };
        var window = Show(scaffold);
        try
        {
            var peer = ControlAutomationPeer.CreatePeerForElement(scaffold)!;
            Assert.Equal("Scaffold", peer.GetClassName()); Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
            IEnumerable<AutomationPeer> Descendants(AutomationPeer parent) => (parent.GetChildren() ?? []).SelectMany(child => new[] { child }.Concat(Descendants(child)));
            var children = Descendants(peer).ToArray();
            foreach (var name in new[] { "top", "body", "snackbar", "fab", "bottom" })
                Assert.Contains(children, child => child.GetName() == name);
            top.Focus();
            foreach (var next in new[] { body, snackbar, fab, bottom })
            {
                window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
                window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
                Assert.True(next.IsFocused);
            }
        }
        finally { window.Close(); }
    }

    private sealed class MeasuringBody(Scaffold? scaffold) : Control
    {
        public Size LastAvailable { get; private set; }
        public Thickness PaddingAtMeasure { get; private set; }
        protected override Size MeasureOverride(Size availableSize)
        {
            LastAvailable = availableSize; PaddingAtMeasure = scaffold?.ContentPadding ?? default;
            return availableSize;
        }
    }
    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 240, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = new Grid { Children = { control } };
        window.Show(); window.UpdateLayout(); return window;
    }
    private static Point PhysicalOrigin(Control control, Window window)
    {
        var first = control.TranslatePoint(default, window)!.Value;
        var second = control.TranslatePoint(new Point(control.Bounds.Width, control.Bounds.Height), window)!.Value;
        return new Point(Math.Min(first.X, second.X), Math.Min(first.Y, second.Y));
    }
}

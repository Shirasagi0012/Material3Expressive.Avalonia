using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Overlays;
using Material3Expressive.Components;
using Xunit;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;

namespace Material3Expressive.Tests;

public class DialogTests
{
    [AvaloniaFact]
    public void EntranceOpacity_CompositesOverlappingContentAsOneLayer()
    {
        using var clock = new MotionTestClockScope();
        var content = new Grid { Width = 120, Height = 80, Background = Brushes.Red };
        content.Children.Add(new Border { Width = 60, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, Background = Brushes.Red });
        var dialog = new Dialog { Content = content };
        var window = Show(new Grid { Children = { dialog } });
        window.Background = Brushes.White;
        dialog.IsOpen = true; Advance(clock, window, 5);
        Assert.InRange(dialog.AppearanceProgress, .1f, .9f);
        using var frame = window.CaptureRenderedFrame()!;
        using var buffer = frame.Lock();
        var overlap = new byte[4]; var background = new byte[4];
        Marshal.Copy(buffer.Address + 150 * buffer.RowBytes + 240 * 4, overlap, 0, 4);
        Marshal.Copy(buffer.Address + 150 * buffer.RowBytes + 280 * 4, background, 0, 4);
        Assert.Equal(background, overlap);
    }

    [AvaloniaTheory]
    [InlineData(1)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(2)]
    [InlineData(4)]
    public void ExitSnapshot_PreservesWholeContentAtRenderScaling(double scale)
    {
        using var clock = new MotionTestClockScope();
        var content = new Grid { Width = 120, Height = 80, ColumnDefinitions = new("*,*"), RowDefinitions = new("*,*") };
        foreach (var (brush, row, column) in new[] { (Brushes.Red, 0, 0), (Brushes.Green, 0, 1), (Brushes.Blue, 1, 0), (Brushes.Yellow, 1, 1) })
        {
            var quadrant = new Border { Background = brush };
            Grid.SetRow(quadrant, row); Grid.SetColumn(quadrant, column); content.Children.Add(quadrant);
        }
        var dialog = new Dialog { Content = content };
        var window = Show(new Grid { Children = { dialog } });
        window.SetRenderScaling(scale); window.Background = Brushes.White;
        dialog.IsOpen = true; Advance(clock, window, 20);
        var expected = new[] { (255, 0, 0), (0, 128, 0), (0, 0, 255), (255, 255, 0) };
        using (var frame = window.CaptureRenderedFrame()!) Assert.Equal(expected, Quadrants(frame));
        dialog.IsOpen = false; Flush(window);
        var snapshot = window.GetVisualDescendants().OfType<DialogSnapshot>().Single();
        Assert.Equal(new PixelSize((int)Math.Ceiling(120 * scale), (int)Math.Ceiling(80 * scale)), snapshot.Bitmap!.PixelSize);
        Assert.Equal(new Size(120, 80), snapshot.Bounds.Size);
        using (var frame = window.CaptureRenderedFrame()!) Assert.Equal(expected, Quadrants(frame));

        (int, int, int)[] Quadrants(WriteableBitmap frame)
        {
            using var buffer = frame.Lock();
            return new[] { (230, 130), (290, 130), (230, 170), (290, 170) }.Select(point =>
            {
                var pixel = new byte[4];
                Marshal.Copy(buffer.Address + (int)(point.Item2 * scale) * buffer.RowBytes + (int)(point.Item1 * scale) * 4, pixel, 0, 4);
                return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
                    ? ((int)pixel[0], (int)pixel[1], (int)pixel[2]) : ((int)pixel[2], (int)pixel[1], (int)pixel[0]);
            }).ToArray();
        }
    }

    [AvaloniaFact]
    public void RepeatedReopen_DetachDisposesAllFrozenLayers()
    {
        using var clock = new MotionTestClockScope();
        var dialog = new Dialog { Content = new Border { Width = 120, Height = 80, Background = Brushes.Red } };
        var window = Show(new Grid { Children = { dialog } });
        dialog.IsOpen = true; Advance(clock, window, 20);
        dialog.IsOpen = false; dialog.IsOpen = true; dialog.IsOpen = false; Flush(window);
        Assert.Equal(2, dialog.ExitingPresentationCount); Assert.True(dialog.IsPresented);
        window.Content = null; Flush(window);
        Assert.Equal(0, dialog.ExitingPresentationCount); Assert.False(dialog.IsPresented);
        Assert.Equal(0, window.MotionRoot.ActiveChildCount); Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void ExitSnapshot_RendersOldContent_AndReopenRendersNewContent()
    {
        using var clock = new MotionTestClockScope();
        var content = new Border { Background = Brushes.Red, Width = 120, Height = 80 };
        var dialog = new Dialog { Content = content };
        var window = Show(new Grid { Children = { dialog } });
        window.Background = Brushes.White;
        dialog.IsOpen = true; Advance(clock, window, 20);
        using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((255, 0, 0), PixelAt(frame, 260, 150));
        dialog.IsOpen = false; Flush(window);
        content.Background = Brushes.Blue; Flush(window);
        using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((255, 0, 0), PixelAt(frame, 260, 150));
        dialog.IsOpen = true; Advance(clock, window, 20);
        using (var frame = window.CaptureRenderedFrame()!) Assert.Equal((0, 0, 255), PixelAt(frame, 260, 150));

        static (int R, int G, int B) PixelAt(WriteableBitmap frame, int x, int y)
        {
            using var buffer = frame.Lock();
            var pixel = new byte[4];
            Marshal.Copy(buffer.Address + y * buffer.RowBytes + x * 4, pixel, 0, 4);
            return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
                ? (pixel[0], pixel[1], pixel[2]) : (pixel[2], pixel[1], pixel[0]);
        }
    }

    [AvaloniaFact]
    public void NativeTextBox_ContextMenu_TabOrder_AndHandledEscapeRemainUsable()
    {
        using var clock = new MotionTestClockScope();
        var input = new TextBox { Text = "hello", Width = 200 };
        var next = new Button { Content = "Next" };
        KeyboardNavigation.SetTabIndex(input, 1); KeyboardNavigation.SetTabIndex(next, 2);
        var dialog = new Dialog { Content = new StackPanel { Children = { input, next } }, AnimateTransition = false };
        var window = Show(new Grid { Children = { dialog } }, fluent: true);
        dialog.IsOpen = true; Flush(window);
        input.Focus(); input.SelectAll(); window.KeyTextInput("中文"); Assert.Equal("中文", input.Text);
        var menuItem = new MenuItem { Header = "Action" };
        var menu = new ContextMenu { Items = { menuItem } };
        input.ContextMenu = menu;
        menu.Open(input); Flush(window);
        menuItem.Focus(); Assert.True(menuItem.IsFocused);
        menu.Close(); input.Focus();
        window.KeyPress(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t"); Assert.True(next.IsFocused);
        window.KeyRelease(Key.Tab, RawInputModifiers.None, PhysicalKey.Tab, "\t");
        window.KeyPress(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, "\t"); Assert.True(input.IsFocused);
        window.KeyRelease(Key.Tab, RawInputModifiers.Shift, PhysicalKey.Tab, "\t");
        var requests = 0; dialog.DismissRequested += (_, _) => requests++;
        var handleDown = false; var handleUp = true;
        input.AddHandler(InputElement.KeyDownEvent, (_, e) => { if (e.Key == Key.Escape && handleDown) e.Handled = true; });
        input.AddHandler(InputElement.KeyUpEvent, (_, e) => { if (e.Key == Key.Escape && handleUp) e.Handled = true; });
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        Assert.Equal(1, requests); // Core BackNavigationEventInput handles unconsumed KeyDown.
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"); Assert.Equal(1, requests);
        handleDown = true; handleUp = false;
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"); Assert.Equal(1, requests);
    }

    [AvaloniaFact]
    public void DialogAboveRail_RestoresRailThenPage_WithoutChangingRailState()
    {
        using var clock = new MotionTestClockScope();
        var page = new Button(); var openDialog = new Button { Content = "Dialog" };
        var dialog = new Dialog { Content = new Button { Content = "Close" }, AnimateTransition = false };
        var rail = new ModalWideNavigationRail { HideOnCollapse = true, ExpandedHeader = new StackPanel { Children = { openDialog, dialog } } };
        rail.Items.Add(new WideNavigationRailItem { Label = "Item" });
        var window = Show(new Grid { Children = { page, rail } });
        page.Focus(); _ = rail.State.ExpandAsync(); Advance(clock, window, 80); openDialog.Focus();
        dialog.IsOpen = true; Flush(window);
        dialog.IsOpen = false; Flush(window);
        Assert.True(openDialog.IsFocused); Assert.True(rail.IsModalVisible);
        Assert.Equal(WideNavigationRailValue.Expanded, rail.State.TargetValue);
        _ = rail.State.CollapseAsync(); Advance(clock, window, 80); Assert.True(page.IsFocused);
    }

    [AvaloniaFact]
    public void ClosingOlderSibling_DoesNotStealFocus_AndHostDetachClosesChildren()
    {
        using var clock = new MotionTestClockScope();
        var first = new Dialog { Content = new Button(), AnimateTransition = false };
        var current = new Button(); var second = new Dialog { Content = current, AnimateTransition = false };
        var root = new Grid { Children = { first, second } }; var window = Show(root);
        first.IsOpen = true; Flush(window); second.IsOpen = true; Flush(window); current.Focus();
        first.IsOpen = false; Flush(window); Assert.True(current.IsFocused);
        window.Content = null; Flush(window); Assert.False(first.IsPresented); Assert.False(second.IsPresented);
        Assert.Empty(window.GetVisualDescendants().OfType<ModalPresenter>());
    }

    [AvaloniaFact]
    public void ContentTemplate_AndLocalResourceChangesSurvivePopupHosting()
    {
        using var clock = new MotionTestClockScope();
        var data = new object(); var context = new object(); Border? child = null;
        var dialog = new Dialog
        {
            Content = data, DataContext = context, AnimateTransition = false,
            ContentTemplate = new FuncDataTemplate<object>((_, _) =>
            {
                child = new Border { Width = 80, Height = 40 };
                child.Bind(Border.BackgroundProperty, child.GetResourceObservable("DialogColor"));
                return child;
            }),
        };
        dialog.Resources["DialogColor"] = Brushes.Red;
        var window = Show(new Grid { Children = { dialog } });
        dialog.IsOpen = true; Flush(window);
        Assert.Same(data, child!.DataContext); Assert.Same(Brushes.Red, child.Background);
        dialog.Resources["DialogColor"] = Brushes.Blue; Flush(window); Assert.Same(Brushes.Blue, child.Background);
        dialog.IsOpen = false; Flush(window); dialog.IsOpen = true; Flush(window);
        Assert.Same(Brushes.Blue, child.Background);
    }

    [AvaloniaFact]
    public void Dialog_UsesPopupLayer_TracksClientSize_AndRestoresFocus()
    {
        using var clock = new MotionTestClockScope();
        var page = new Button { Content = "Page" };
        var input = new Button { Content = "Input" };
        var dialog = new Dialog { Content = input, Width = 180, Height = 100, AnimateTransition = false };
        var root = new Grid { Children = { page, dialog } };
        var window = Show(root);
        page.Focus();
        dialog.IsOpen = true; Flush(window);
        Assert.True(dialog.IsPresented);
        Assert.True(dialog.Popup.IsUsingOverlayLayer);
        Assert.Equal(default, dialog.DesiredSize);
        var modal = window.GetVisualDescendants().OfType<ModalPresenter>().Single();
        Assert.Equal(window.ClientSize, modal.Bounds.Size);
        Assert.Equal("PopupOverlayLayer", modal.GetVisualAncestors().OfType<OverlayPopupHost>().Single().GetVisualParent()!.GetType().Name);
        input.Focus(); page.Focus(); Assert.True(input.IsFocused);
        window.Width = 640; window.Height = 360; Flush(window);
        Assert.Equal(window.ClientSize, modal.Bounds.Size);
        Assert.Equal(new Point(230, 130), window.GetVisualDescendants().OfType<Grid>().Single(g => g.Name == "PART_Surface").TranslatePoint(default, window));
        dialog.IsOpen = false; Flush(window);
        Assert.False(dialog.IsPresented);
        Assert.True(page.IsFocused);
        Assert.Null(window.MotionRoot.LastError);
    }

    [AvaloniaFact]
    public void NestedDialog_AndNativePopup_DoNotFightForFocusOrDismissTheirParent()
    {
        using var clock = new MotionTestClockScope();
        var page = new Button { Content = "Page" };
        var input = new Button { Content = "Input" };
        var innerInput = new Button { Content = "Inner" };
        var inner = new Dialog { Content = innerInput, Width = 100, Height = 80, AnimateTransition = false };
        var outer = new Dialog { Content = new StackPanel { Children = { input, inner } }, Width = 240, Height = 160, AnimateTransition = false };
        var window = Show(new Grid { Children = { page, outer } });
        page.Focus(); outer.IsOpen = true; Flush(window); input.Focus();
        var menuItem = new Button { Content = "Choice" };
        var popup = new Popup { PlacementTarget = input, Child = menuItem, ShouldUseOverlayLayer = true };
        ((StackPanel)outer.Content!).Children.Add(popup); popup.Open(); Flush(window);
        Assert.True(menuItem.Focus(), $"menu attached={menuItem.IsAttachedToVisualTree()} host={popup.IsUsingOverlayLayer}"); Assert.True(menuItem.IsFocused);
        popup.Close(); input.Focus();
        var outerRequests = 0; outer.DismissRequested += (_, _) => outerRequests++;
        inner.DismissRequested += (_, _) => inner.IsOpen = false;
        inner.IsOpen = true; Flush(window); innerInput.Focus();
        Assert.True(innerInput.IsFocused);
        input.Focus(); Assert.True(innerInput.IsFocused);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        Assert.False(inner.IsOpen);
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"); Flush(window);
        Assert.False(inner.IsPresented); Assert.True(outer.IsPresented); Assert.Equal(0, outerRequests);
        Assert.True(input.IsFocused);
        outer.IsOpen = false; Flush(window); Assert.True(page.IsFocused);
    }

    [AvaloniaFact]
    public void OutsideAndEscape_OnlyRequestDismissal_AndDoNotClickThrough()
    {
        using var clock = new MotionTestClockScope();
        var page = new Button(); var clicks = 0; page.Click += (_, _) => clicks++;
        var dialog = new Dialog { Content = new Border { Background = Brushes.White }, Width = 160, Height = 100, AnimateTransition = false };
        var window = Show(new Grid { Children = { page, dialog } });
        var reasons = new List<DismissReason>(); dialog.DismissRequested += (_, e) => reasons.Add(e.Reason);
        dialog.IsOpen = true; Flush(window);
        window.MouseDown(new Point(20, 20), MouseButton.Left); Assert.Empty(reasons);
        window.MouseUp(new Point(20, 20), MouseButton.Left);
        Assert.Equal(new[] { DismissReason.PointerOutside }, reasons);
        Assert.True(dialog.IsOpen); Assert.Equal(0, clicks);
        // A gesture starting inside is not a scrim click even if it ends outside.
        window.MouseDown(new Point(250, 150), MouseButton.Left);
        window.MouseUp(new Point(20, 20), MouseButton.Left); Assert.Single(reasons);
        dialog.DismissOnEscape = false;
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape"); Assert.Single(reasons);
        dialog.DismissOnEscape = true;
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, "Escape");
        Assert.Equal(DismissReason.Escape, reasons.Last());
    }

    [AvaloniaFact]
    public void ExitRetainsFrozenPresentation_ReopenKeepsOldExit_DetachCleansUp()
    {
        using var clock = new MotionTestClockScope();
        var dialog = new Dialog { Content = new Border { Background = Brushes.Red, Width = 120, Height = 80 } };
        var root = new Grid { Children = { dialog } }; var window = Show(root);
        dialog.IsOpen = true; Advance(clock, window, 20);
        Assert.Equal(1, dialog.AppearanceProgress);
        dialog.IsOpen = false; Flush(window);
        Assert.True(dialog.IsPresented);
        Assert.NotNull(window.GetVisualDescendants().OfType<DialogSnapshot>().Single().Bitmap);
        Advance(clock, window, 3);
        dialog.IsOpen = true;
        Assert.Equal(1, dialog.ExitingPresentationCount); Assert.Equal(0, dialog.AppearanceProgress);
        Advance(clock, window, 20);
        Assert.Equal(0, dialog.ExitingPresentationCount);
        Assert.True(dialog.IsPresented); Assert.Equal(1, dialog.AppearanceProgress);
        dialog.IsOpen = false; Advance(clock, window, 20); Assert.False(dialog.IsPresented);
        dialog.IsOpen = true; Advance(clock, window, 3);
        window.Content = null; Flush(window);
        Assert.False(dialog.IsPresented); Assert.False(dialog.Popup.IsOpen);
        window.Content = root; Advance(clock, window, 20); Assert.True(dialog.IsPresented);
        Assert.Null(window.MotionRoot.LastError);
    }

    internal static MotionTestWindow Show(Control content, double height = 300, bool fluent = false)
    {
        var window = new MotionTestWindow { Width = 520, Height = height };
        // A real Window template enables PopupOverlayLayer through OnApplyTemplate; an arbitrary
        // VisualLayerManager in Content only supplies the ordinary OverlayLayer.
        if (fluent) window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        else window.Template = new FuncControlTemplate<Window>((owner, scope) =>
        {
            var presenter = new ContentPresenter();
            presenter.Bind(ContentPresenter.ContentProperty, owner.GetObservable(ContentControl.ContentProperty));
            presenter.Bind(ContentPresenter.BackgroundProperty, owner.GetObservable(TemplatedControl.BackgroundProperty));
            var layers = new VisualLayerManager { Name = "PART_VisualLayerManager", Child = presenter };
            scope.Register(layers.Name, layers);
            return layers;
        });
        window.Styles.Add((Avalonia.Styling.Styles)AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml")));
        window.Content = content; window.Show(); Flush(window); return window;
    }

    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private static void Advance(MotionTestClockScope clock, Window window, int frames)
    {
        for (var i = 0; i < frames; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); }
    }
}

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using static Material3Expressive.Tokens.MaterialTokens;
using Xunit;

namespace Material3Expressive.Tests;

public class ExposedDropdownMenuTests
{
    [AvaloniaFact]
    public void NestedBoxDoesNotPublishRequestsForTheInnerAnchor()
    {
        using var clock = new MotionTestClockScope();
        var (inner, field, menu) = Create(true);
        var outer = new ExposedDropdownMenuBox { Content = inner, Menu = new ExposedDropdownMenu() };
        var outerRequests = 0; var innerRequests = 0;
        outer.ExpandedChangeRequested += (_, _) => outerRequests++;
        inner.ExpandedChangeRequested += (_, e) => { innerRequests++; inner.IsExpanded = e.Expanded; };
        var window = DialogTests.Show(outer);
        var point = field.TranslatePoint(new Point(60, 24), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Advance(clock, window);
        Assert.Equal(0, outerRequests); Assert.Equal(1, innerRequests);
        Assert.False(outer.IsExpanded); Assert.True(menu.IsPresented);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Advance(clock, window);
        Assert.Equal(0, outerRequests); Assert.Equal(2, innerRequests); Assert.False(menu.IsPresented);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultFieldPaletteKeepsDisabledAffixesOnSurface_AndFilledLabelOpaque(bool outlined)
    {
        using var clock = new MotionTestClockScope();
        var field = new TextBox { IsEnabled = false, Height = 96 };
        MaterialTextField.SetLabel(field, "Label"); MaterialTextField.SetPrefix(field, "Prefix"); MaterialTextField.SetSuffix(field, "Suffix");
        MaterialTextField.SetAlwaysMinimizeLabel(field, true);
        var window = DialogTests.Show(field);
        field.Theme = (ControlTheme)window.FindResource(outlined ? "MaterialOutlinedExposedDropdownTextFieldTheme" : "MaterialFilledExposedDropdownTextFieldTheme")!;
        window.Resources[Sys.Color.OnSurface] = Colors.Red; Flush(window); Advance(clock, window);
        var layout = field.GetVisualDescendants().OfType<TextFieldLayout>().Single();
        Assert.Equal(Color.FromArgb(outlined ? (byte)97 : (byte)255, 255, 0, 0), ((ISolidColorBrush)layout.LabelBrush!).Color);
        foreach (var name in new[] { "Prefix", "Suffix" })
            Assert.Equal(Color.FromArgb(97, 255, 0, 0), ((ISolidColorBrush)field.GetVisualDescendants().OfType<ContentPresenter>().Single(c => c.Name == name).Foreground!).Color);
        window.Resources[Sys.Color.OnSurface] = Colors.Blue; Flush(window); Advance(clock, window);
        Assert.Equal(Color.FromArgb(outlined ? (byte)97 : (byte)255, 0, 0, 255), ((ISolidColorBrush)layout.LabelBrush!).Color);
    }

    [AvaloniaFact]
    public void TrailingIconInsideTextFieldActsAsSecondaryAnchorWithoutBecomingFocusable()
    {
        using var clock = new MotionTestClockScope();
        var (box, field, menu) = Create(true);
        var icon = new ExposedDropdownMenuTrailingIcon();
        ExposedDropdownMenuBox.SetAnchorType(icon, ExposedDropdownMenuAnchorType.SecondaryEditable);
        field.InnerRightContent = icon;
        box.ExpandedChangeRequested += (_, e) => { box.IsExpanded = e.Expanded; icon.IsExpanded = e.Expanded; };
        var window = DialogTests.Show(box); Advance(clock, window);
        var point = icon.TranslatePoint(new Point(12, 12), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Advance(clock, window);
        Assert.True(icon.IsExpanded); Assert.True(box.IsExpanded); Assert.False(icon.Focusable);
        Assert.Equal(new Size(24, 24), icon.Bounds.Size); Assert.True(field.IsFocused); Assert.False(menu.IsPopupFocusable);
    }

    [AvaloniaFact]
    public void SecondaryGestureCancelsAfterLeavingBounds_AndUnmountEndsRetainedFocus()
    {
        using var clock = new MotionTestClockScope();
        var (box, field, menu) = Create(false);
        var icon = new ExposedDropdownMenuTrailingIcon();
        ExposedDropdownMenuBox.SetAnchorType(icon, ExposedDropdownMenuAnchorType.SecondaryEditable); field.InnerRightContent = icon;
        var root = new Grid { Children = { box } }; var window = DialogTests.Show(root); var requests = 0;
        box.ExpandedChangeRequested += (_, _) => requests++;
        var point = icon.TranslatePoint(new Point(12, 12), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseMove(new Point(400, 250)); window.MouseMove(point); window.MouseUp(point, MouseButton.Left);
        Assert.Equal(0, requests);
        field.Focus(); box.IsExpanded = true; Advance(clock, window);
        Assert.True(field.GetValue(TextFieldThemeDefaults.RetainPopupFocusProperty));
        root.Children.Clear(); Flush(window); Advance(clock, window);
        Assert.False(field.GetValue(TextFieldThemeDefaults.RetainPopupFocusProperty)); Assert.False(menu.IsPresented);
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void PopupFocusHandoffRetainsTheSameFieldInteraction_AndReleasesOnRealBlur(bool editable)
    {
        using var clock = new MotionTestClockScope();
        var (box, field, menu) = Create(editable);
        var source = new MaterialInteractionSource(); var events = new List<MaterialInteractionEvent>();
        source.Interaction += events.Add; MaterialTextField.SetInteractionSource(field, source);
        var target = new Button { Content = "Other", Width = 80, Height = 32, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom };
        var window = DialogTests.Show(new Grid { Children = { box, target } });
        field.Focus(); Advance(clock, window); var start = Assert.Single(events.OfType<MaterialInteractionEvent.Focus>());
        box.IsExpanded = true; Advance(clock, window);
        if (editable) { window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null); Advance(clock, window); }
        Assert.True(menu.IsPopupFocusable); Assert.False(field.IsFocused);
        Assert.True(TextFieldThemeDefaults.GetIsFocused(field)); Assert.Empty(events.OfType<MaterialInteractionEvent.Unfocus>());
        box.IsExpanded = false; Advance(clock, window);
        Assert.True(field.IsFocused); Assert.Empty(events.OfType<MaterialInteractionEvent.Unfocus>());
        Assert.Single(events.OfType<MaterialInteractionEvent.Focus>());
        target.Focus(); Advance(clock, window);
        Assert.Same(start, Assert.Single(events.OfType<MaterialInteractionEvent.Unfocus>()).Start);
        Assert.False(TextFieldThemeDefaults.GetIsFocused(field));
    }

    [Theory]
    [InlineData(false, 100)]
    [InlineData(true, 20)]
    public void PositionUsesAnchorEdgesBeforeWindowMargins(bool rtl, int x)
    {
        var provider = new ExposedDropdownMenuPositionProvider();
        Assert.Equal(new PixelPoint(x, 40), provider.CalculatePosition(new(100, 8, 40, 32), new(320, 240), rtl, new(120, 60)));
        Assert.Equal(0, provider.TransformOrigin.Point.Y);
        Assert.Equal(new PixelPoint(200, 132), provider.CalculatePosition(new(280, 192, 40, 32), new(320, 240), false, new(120, 60)));
    }

    [AvaloniaFact]
    public void EditableAnchorRetainsInputFocus_ThenKeyboardNavigationEntersMenu()
    {
        using var clock = new MotionTestClockScope();
        var (box, field, menu) = Create(true);
        var window = DialogTests.Show(box);
        var requests = new List<bool>(); box.ExpandedChangeRequested += (_, e) => { requests.Add(e.Expanded); box.IsExpanded = e.Expanded; };
        var point = field.TranslatePoint(new Point(60, 24), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Advance(clock, window);
        Assert.Equal([true], requests); Assert.True(menu.IsPresented); Assert.False(menu.IsPopupFocusable); Assert.True(field.IsFocused);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.None, null); window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.None, null);
        Assert.Single(requests);
        window.KeyPress(Key.Down, RawInputModifiers.None, PhysicalKey.None, null); Advance(clock, window);
        Assert.True(menu.IsPopupFocusable); Assert.False(field.IsFocused);
        window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.None, null); Advance(clock, window);
        Assert.False(box.IsExpanded); Assert.False(menu.IsPresented); Assert.True(field.IsFocused);
    }

    [AvaloniaFact]
    public void RequestsAreControlled_AndDisablingAnchorKeepsTextEditable()
    {
        using var clock = new MotionTestClockScope();
        var (box, field, menu) = Create(true); var window = DialogTests.Show(box);
        var requests = 0; box.ExpandedChangeRequested += (_, _) => requests++;
        field.Focus(); window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); Advance(clock, window);
        Assert.Equal(1, requests); Assert.False(box.IsExpanded); Assert.False(menu.IsPresented);
        ExposedDropdownMenuBox.SetIsAnchorEnabled(field, false);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.None, null); window.KeyTextInput("query");
        Assert.Equal(1, requests); Assert.Contains("query", field.Text);
        var peer = Assert.IsAssignableFrom<IExpandCollapseProvider>(ControlAutomationPeer.CreatePeerForElement(box));
        peer.Expand(); Assert.Equal(1, requests);
    }

    [AvaloniaFact]
    public void WidthAndHeightFollowAnchorWindowAndMatchOption_WithoutLosingExitAnimation()
    {
        using var clock = new MotionTestClockScope();
        var (box, _, menu) = Create(false);
        box.Width = 240; box.Height = 56;
        box.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left; box.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top;
        box.Margin = new Thickness(40, 80, 0, 0);
        for (var i = 0; i < 8; i++) menu.Items.Add(new DropdownMenuItem { Content = new Border { Width = 40, Height = 16 } });
        var window = DialogTests.Show(box); box.IsExpanded = true; Advance(clock, window);
        Assert.Equal(240, menu.Presentation!.Bounds.Width);
        Assert.Equal(116, menu.Presentation.Bounds.Height); // 300 - 48 - (80 + 56)
        box.Width = 180; window.UpdateLayout(); Advance(clock, window);
        Assert.Equal(180, menu.Presentation.Bounds.Width);
        menu.MatchAnchorWidth = false; window.UpdateLayout(); Advance(clock, window);
        Assert.Equal(112, menu.Presentation.Bounds.Width);
        box.IsExpanded = false; Flush(window);
        Assert.True(menu.IsPresented); Advance(clock, window); Assert.False(menu.IsPresented);
        window.Close(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaFact]
    public void SecondaryAnchorConsumesInput_AndUsesEditableFocusPolicy()
    {
        using var clock = new MotionTestClockScope();
        var (box, field, menu) = Create(true);
        var secondary = new Button { Content = "Open", Width = 40, Height = 40 };
        ExposedDropdownMenuBox.SetAnchorType(secondary, ExposedDropdownMenuAnchorType.SecondaryEditable);
        box.Content = null;
        box.Content = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { field, secondary } };
        var window = DialogTests.Show(box); var clicks = 0; secondary.Click += (_, _) => clicks++;
        box.ExpandedChangeRequested += (_, e) => box.IsExpanded = e.Expanded;
        var point = secondary.TranslatePoint(new Point(20, 20), window)!.Value;
        window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Advance(clock, window);
        Assert.True(box.IsExpanded); Assert.False(menu.IsPopupFocusable); Assert.True(field.IsFocused);
        Assert.Equal(ExposedDropdownMenuAnchorType.SecondaryEditable, box.ActiveAnchorType); Assert.Equal(0, clicks);
    }

    private static (ExposedDropdownMenuBox Box, TextBox Field, ExposedDropdownMenu Menu) Create(bool editable)
    {
        var field = new TextBox { Width = 200, Height = 56, IsReadOnly = !editable };
        ExposedDropdownMenuBox.SetAnchorType(field, editable ? ExposedDropdownMenuAnchorType.PrimaryEditable : ExposedDropdownMenuAnchorType.PrimaryNotEditable);
        var menu = new ExposedDropdownMenu { Items = { new DropdownMenuItem { Content = new Border { Width = 40, Height = 16, Background = Brushes.Red } } } };
        return (new ExposedDropdownMenuBox { Content = field, Menu = menu, HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top, Margin = new Thickness(40, 60, 0, 0) }, field, menu);
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); Dispatcher.UIThread.RunJobs(); }
    private static void Advance(MotionTestClockScope clock, Window window)
    { for (var i = 0; i < 80; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
}

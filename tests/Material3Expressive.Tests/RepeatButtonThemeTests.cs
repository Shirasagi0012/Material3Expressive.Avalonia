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
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation.TestSupport;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Styles;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class RepeatButtonThemeTests
{
    [AvaloniaFact]
    public void ImplicitTheme_IsTheFilledButton()
    {
        var repeat = new RepeatButton { Content = "Repeat" };
        var button = new Button { Content = "Repeat" };
        var window = Show(repeat, button);
        Assert.Same(window.FindResource("MaterialFilledButtonTheme"), ((ControlTheme)window.FindResource(typeof(RepeatButton))!).BasedOn);
        Assert.True(MaterialInteraction.GetIsInteractive(repeat));
        Assert.NotNull(Container(repeat));
        AssertLooksLike(button, repeat);
        window.Close();
    }

    [AvaloniaTheory]
    [InlineData("MaterialFilledButtonTheme", null)]
    [InlineData("MaterialElevatedButtonTheme", null)]
    [InlineData("MaterialTonalButtonTheme", "medium")]
    [InlineData("MaterialOutlinedButtonTheme", "xsmall")]
    [InlineData("MaterialTextButtonTheme", null)]
    [InlineData("MaterialOutlinedButtonTheme", "large")]
    [InlineData("MaterialTonalButtonTheme", "xlarge")]
    public void KeyedButtonThemesAndSizeClasses_ApplyToRepeatButton(string themeKey, string? sizeClass)
    {
        var repeat = new RepeatButton { Content = "Repeat" };
        var button = new Button { Content = "Repeat" };
        var window = Show(repeat, button);
        var theme = (ControlTheme)window.FindResource(themeKey)!;
        repeat.Theme = theme; button.Theme = theme;
        if (sizeClass is not null) { repeat.Classes.Add(sizeClass); button.Classes.Add(sizeClass); }
        Draw(window);
        AssertLooksLike(button, repeat);
        window.Close();
    }

    [AvaloniaFact]
    public async Task PointerHold_RepeatsWhilePressedAndReleasesCleanly()
    {
        var repeat = new RepeatButton { Content = "Repeat", Delay = 20, Interval = 10 };
        var window = Show(repeat);
        var clicks = 0;
        repeat.Click += (_, _) => clicks++;
        var center = Center(repeat, window);
        window.MouseMove(center); window.MouseDown(center, MouseButton.Left);
        await Pump(window, 200);
        Assert.True(clicks >= 3, $"Expected repeated clicks while held, got {clicks}.");
        Assert.True(repeat.IsPressed);
        Assert.True(IsPressedFeedback(repeat));
        Assert.Contains(":pressed", repeat.Classes);
        window.MouseUp(center, MouseButton.Left); Draw(window);
        // Release adds the native ClickMode.Release click; repetition then stops.
        var released = clicks;
        await Pump(window, 100);
        Assert.Equal(released, clicks);
        Assert.False(repeat.IsPressed);
        Assert.False(IsPressedFeedback(repeat));
        window.Close();
    }

    [AvaloniaFact]
    public async Task PointerLeavingWhileHeld_CancelsWithoutStuckPressedState()
    {
        var repeat = new RepeatButton { Content = "Repeat", Delay = 20, Interval = 10 };
        var window = Show(repeat);
        var clicks = 0;
        repeat.Click += (_, _) => clicks++;
        var center = Center(repeat, window);
        window.MouseMove(center); window.MouseDown(center, MouseButton.Left);
        await Pump(window, 80);
        Assert.True(clicks >= 1);
        var outside = repeat.TranslatePoint(new Point(repeat.Bounds.Width + 60, repeat.Bounds.Height / 2), window)!.Value;
        window.MouseMove(outside, RawInputModifiers.LeftMouseButton); Draw(window);
        Assert.False(repeat.IsPressed);
        Assert.False(IsPressedFeedback(repeat));
        var afterLeave = clicks;
        await Pump(window, 100);
        Assert.Equal(afterLeave, clicks);
        window.MouseUp(outside, MouseButton.Left); Draw(window);
        Assert.Equal(afterLeave, clicks);
        Assert.DoesNotContain(":pressed", repeat.Classes);

        // The next press works normally.
        window.MouseMove(center); window.MouseDown(center, MouseButton.Left); Draw(window);
        Assert.True(repeat.IsPressed); Assert.True(IsPressedFeedback(repeat));
        window.MouseUp(center, MouseButton.Left); Draw(window);
        Assert.False(repeat.IsPressed); Assert.False(IsPressedFeedback(repeat));
        window.Close();
    }

    [AvaloniaFact]
    public async Task Keyboard_SpaceRepeatsWhileHeldAndEnterClicksPerKeyDown()
    {
        var repeat = new RepeatButton { Content = "Repeat", Delay = 20, Interval = 10 };
        var window = Show(repeat);
        var clicks = 0;
        repeat.Click += (_, _) => clicks++;
        repeat.Focus(NavigationMethod.Tab); Draw(window);
        window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
        Assert.True(repeat.IsPressed); Assert.True(IsPressedFeedback(repeat));
        await Pump(window, 200);
        Assert.True(clicks >= 3, $"Expected Space to repeat, got {clicks}.");
        window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " "); Draw(window);
        var released = clicks;
        await Pump(window, 100);
        Assert.Equal(released, clicks);
        Assert.False(repeat.IsPressed); Assert.False(IsPressedFeedback(repeat));

        clicks = 0;
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        await Pump(window, 80);
        Assert.Equal(2, clicks);
        window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r"); Draw(window);
        Assert.False(IsPressedFeedback(repeat));
        window.Close();
    }

    [AvaloniaFact]
    public void DisabledDarkRtlAndFocusVisible_MatchButton()
    {
        var repeat = new RepeatButton { Content = "Repeat", FlowDirection = FlowDirection.RightToLeft };
        var button = new Button { Content = "Repeat", FlowDirection = FlowDirection.RightToLeft };
        MaterialButton.SetIcon(repeat, new Border { Width = 18, Height = 18 });
        MaterialButton.SetIcon(button, new Border { Width = 18, Height = 18 });
        var window = Show(repeat, button);
        window.RequestedThemeVariant = ThemeVariant.Dark; Draw(window);
        AssertLooksLike(button, repeat);
        // RTL: the leading icon is mirrored exactly like Button's.
        var icon = (Control)MaterialButton.GetIcon(repeat)!;
        var buttonIcon = (Control)MaterialButton.GetIcon(button)!;
        Assert.Equal(buttonIcon.TranslatePoint(new Point(4, 4), button), icon.TranslatePoint(new Point(4, 4), repeat));
        Assert.Equal(FlowDirection.RightToLeft, icon.FlowDirection);

        repeat.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Contains(":focus-visible", repeat.Classes);
        var focusedLayer = Brush(Container(repeat).StateLayerBrush);
        button.Focus(NavigationMethod.Tab); Draw(window);
        Assert.Equal(Brush(Container(button).StateLayerBrush), focusedLayer);

        repeat.IsEnabled = false; button.IsEnabled = false; Draw(window);
        AssertLooksLike(button, repeat);
        window.Close();
    }

    [AvaloniaFact]
    public void InternalKeyedRepeatButtons_KeepTheirThemes()
    {
        var scrollBar = new ScrollBar { Orientation = Orientation.Vertical, Height = 200, Maximum = 100, ViewportSize = 20, Value = 40, AllowAutoHide = false };
        var nud = new NumericUpDown { Value = 5, Width = 280 };
        var time = new TimePicker();
        var window = Show(scrollBar, nud, fluent: true);
        ((StackPanel)window.Content!).Children.Add(time); Draw(window);

        var pageButtons = scrollBar.GetVisualDescendants().OfType<RepeatButton>().Where(b => b.Name is "PART_PageUpButton" or "PART_PageDownButton").ToList();
        Assert.Equal(2, pageButtons.Count);
        Assert.All(pageButtons, b => Assert.Same(window.FindResource("MaterialScrollBarPageButtonTheme"), b.Theme));
        Assert.All(pageButtons, b => Assert.Null(Container(b, required: false)));

        var steppers = nud.GetVisualDescendants().OfType<RepeatButton>().ToList();
        Assert.Equal(2, steppers.Count);
        Assert.All(steppers, b => Assert.Same(window.FindResource("MaterialSpinnerRepeatButtonTheme"), b.Theme));
        // Pixels: a stepper's container stays transparent over the field (no filled-button primary).
        var field = nud.GetVisualDescendants().OfType<TextBox>().Single();
        var increase = steppers.Single(b => b.Name == "PART_IncreaseButton");
        using (var frame = window.CaptureRenderedFrame()!)
        {
            var corner = increase.TranslatePoint(new Point(12, 12), window)!.Value;
            var fieldColor = Brush(field.Background)!.Value;
            Assert.Equal((fieldColor.R, fieldColor.G, fieldColor.B), PixelAt(frame, (int)corner.X, (int)corner.Y));
        }

        var pickerButtons = time.GetVisualDescendants().OfType<RepeatButton>().ToList();
        Assert.All(pickerButtons, b => Assert.NotNull(b.Theme));
        Assert.All(pickerButtons, b => Assert.False(MaterialInteraction.GetIsInteractive(b)));
        window.Close();
    }

    static MaterialSurface Container(Button button, bool required = true)
    {
        var surface = button.GetTemplateDescendants().OfType<MaterialSurface>().SingleOrDefault(s => s.Name == "PART_Container");
        if (required) Assert.NotNull(surface);
        return surface!;
    }

    static bool IsPressedFeedback(Button button) => MaterialInteraction.GetOrCreate(button).Interaction.HasFlag(InteractionState.Pressed);

    static void AssertLooksLike(Button expected, RepeatButton actual)
    {
        Assert.Equal(expected.Bounds.Size, actual.Bounds.Size);
        Assert.Equal(expected.CornerRadius, actual.CornerRadius);
        Assert.Equal(expected.Padding, actual.Padding);
        Assert.Equal(expected.BorderThickness, actual.BorderThickness);
        Assert.Equal(Brush(expected.Background), Brush(actual.Background));
        Assert.Equal(Brush(expected.Foreground), Brush(actual.Foreground));
        Assert.Equal(Brush(expected.BorderBrush), Brush(actual.BorderBrush));
        Assert.Equal(Container(expected).Bounds, Container(actual).Bounds);
        Assert.Equal(MaterialButton.GetElevation(expected), MaterialButton.GetElevation(actual));
    }

    static Color? Brush(IBrush? brush) => (brush as ISolidColorBrush)?.Color;

    static Point Center(Control control, Window window) =>
        control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    // RepeatButton's DispatcherTimer only fires while the headless dispatcher runs its loop.
    static async Task Pump(Window window, int milliseconds)
    {
        await Task.Delay(milliseconds, TestContext.Current.CancellationToken);
        Draw(window);
    }

    static (int R, int G, int B) PixelAt(Avalonia.Media.Imaging.WriteableBitmap frame, int x, int y)
    {
        using var buffer = frame.Lock();
        var row = new byte[buffer.RowBytes];
        System.Runtime.InteropServices.Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, buffer.RowBytes);
        return buffer.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? (row[x * 4], row[x * 4 + 1], row[x * 4 + 2])
            : (row[x * 4 + 2], row[x * 4 + 1], row[x * 4]);
    }

    static AnimationTestWindow Show(Control control, Control? sibling = null, bool fluent = false)
    {
        var clock = new ManualFrameSource(Dispatcher.UIThread.VerifyAccess);
        var window = new AnimationTestWindow(clock) { Width = 480, Height = 480 };
        if (fluent) window.Styles.Add(new FluentTheme());
        window.Styles.Add(new MaterialTheme());
        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 16, Children = { control } };
        if (sibling is not null) panel.Children.Add(sibling);
        window.Content = panel;
        window.Show(); Draw(window); return window;
    }

    static void Draw(Window window)
    {
        window.UpdateLayout(); Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick(); Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
    }
}

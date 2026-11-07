using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Styles;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Interaction;
using Xunit;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Tests;

public class IconToggleButtonTests
{
    [AvaloniaTheory]
    [InlineData("Standard")]
    [InlineData("Filled")]
    [InlineData("Tonal")]
    [InlineData("Outlined")]
    public void ExplicitCutsKeepPressedPriorityAndCaptureAThirdDestination(string variant)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconToggleButton { Width = 64, Height = 64, CheckedShape = new CutCornerShape(16) };
        MaterialButton.SetShape(button, new CutCornerShape(24));
        MaterialButton.SetPressedShape(button, new CutCornerShape(4));
        var window = DialogTests.Show(new Grid { Children = { button } });
        try
        {
            button.Theme = (ControlTheme)window.FindResource($"Material{variant}IconToggleButtonTheme")!;
            window.UpdateLayout(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            double Cut() => ((PathGeometry)surface.ContainerGeometry!).Figures![0].StartPoint.Y;
            button.IsChecked = true;
            for (var i = 0; i < 4; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.InRange(Cut(), 16.01, 23.99);
            var before = Cut(); button.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.Equal(before, Cut());
            for (var i = 0; i < 30; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(4, Cut());
            button.IsChecked = false;
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(4, Cut());
            window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            for (var i = 0; i < 30; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.False(button.IsChecked); Assert.Equal(24, Cut());
            button.IsChecked = true;
            for (var i = 0; i < 30; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            ((CutCornerShape)button.CheckedShape!).TopStart = new(12, RelativeUnit.Absolute);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(12, Cut());
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }
    [AvaloniaTheory]
    [InlineData("Filled")]
    [InlineData("Tonal")]
    public void Colors_FollowCheckedDisabledThemeAndLocalOverrides_WithoutAnimation(string variant)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconToggleButton();
        var window = Show(button, variant);
        try
        {
            var filled = variant == "Filled";
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => filled ? s.SurfaceContainer : s.SecondaryContainer), Color(button.Background));
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => filled ? s.OnSurfaceVariant : s.OnSecondaryContainer), Color(button.Foreground));
            button.IsChecked = true; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => filled ? s.Primary : s.Secondary), Color(button.Background));
            Assert.Equal(ButtonThemeFixture.SchemeColor(false, s => filled ? s.OnPrimary : s.OnSecondary), Color(button.Foreground));
            Assert.Equal(Color(button.Foreground), Color(surface.StateLayerBrush));
            var point = button.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseMove(point); Flush(window);
            Assert.Equal(Color(button.Foreground), Color(surface.StateLayerBrush));
            window.MouseDown(point, MouseButton.Left); Flush(window);
            Assert.Equal(Color(button.Foreground), Color(surface.StateLayerBrush));
            window.MouseUp(point, MouseButton.Left); Flush(window);
            window.RequestedThemeVariant = ThemeVariant.Dark; Flush(window);
            Assert.Equal(ButtonThemeFixture.SchemeColor(true, s => filled ? s.Primary : s.Secondary), Color(button.Background));
            button.IsEnabled = false; Flush(window);
            var onSurface = ButtonThemeFixture.SchemeColor(true, s => s.OnSurface);
            Assert.Equal(Avalonia.Media.Color.FromArgb(26, onSurface.R, onSurface.G, onSurface.B), Color(button.Background));
            Assert.Equal(Avalonia.Media.Color.FromArgb(97, onSurface.R, onSurface.G, onSurface.B), Color(button.Foreground));
            button.IsChecked = false; Flush(window);
            Assert.Equal(Avalonia.Media.Color.FromArgb(97, onSurface.R, onSurface.G, onSurface.B), Color(button.Foreground));
            button.Resources[filled ? Comp.FilledIconToggleButton.CheckedContentColor : Comp.TonalIconToggleButton.CheckedContentColor] = Colors.Magenta;
            button.IsChecked = true; button.IsEnabled = true; Flush(window);
            Assert.Equal(Colors.Magenta, Color(button.Foreground));
            var userStyle = new Style { Setters = { new Setter(IconToggleButton.ForegroundProperty, Brushes.Blue) } };
            button.Styles.Add(userStyle); button.ApplyStyling(); button.IsEnabled = false; Flush(window);
            Assert.Equal(Colors.Blue, Color(button.Foreground));
            button.Styles.Remove(userStyle); button.IsEnabled = true; Flush(window);
            Assert.Equal(Colors.Magenta, Color(button.Foreground));
        }
        finally { window.Close(); }
    }

    [AvaloniaTheory]
    [InlineData("Outlined")]
    [InlineData("Standard")]
    [InlineData("Filled")]
    [InlineData("Tonal")]
    [InlineData("Vibrant")]
    [InlineData("OutlinedVibrant")]
    public void PointerAndAutomation_RequestState_AndRespectDisabled(string variant)
    {
        using var clock = new MotionTestClockScope();
        var button = new IconToggleButton();
        var window = Show(button, variant);
        try
        {
            var requests = new List<bool>();
            button.CheckedChangeRequested += (_, e) => requests.Add(e.Value);
            var point = button.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal([true], requests); Assert.False(button.IsChecked);
            button.IsChecked = true;
            var peer = ControlAutomationPeer.CreatePeerForElement(button)!;
            Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
            ((IToggleProvider)peer).Toggle(); Flush(window);
            Assert.Equal([true, false], requests); Assert.True(button.IsChecked);
            button.IsEnabled = false;
            window.MouseDown(point, MouseButton.Left); window.MouseUp(point, MouseButton.Left); Flush(window);
            Assert.Equal([true, false], requests);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CheckedShape_AnimatesReversesAndReattaches_AndPressedShapeHasPriority()
    {
        using var clock = new MotionTestClockScope();
        var button = new IconToggleButton { CornerRadius = new CornerRadius(20), CheckedCornerRadius = new CornerRadius(12) };
        MaterialButton.SetPressedCornerRadius(button, new CornerRadius(8));
        var window = Show(button, "Filled");
        void Frames(int count) { for (var i = 0; i < count; i++) { clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); Flush(window); } }
        try
        {
            var surface = button.GetVisualDescendants().OfType<MaterialSurface>().Single();
            Frames(2); Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft);
            button.IsChecked = true; Frames(4);
            Assert.InRange(surface.EffectiveCornerRadius.TopLeft, 12.001, 19.999);
            button.IsChecked = false; Frames(70); Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft);
            button.IsChecked = true; Frames(70); Assert.Equal(12, surface.EffectiveCornerRadius.TopLeft);
            var point = button.TranslatePoint(new Point(24, 24), window)!.Value;
            window.MouseMove(point); window.MouseDown(point, MouseButton.Left); Frames(70);
            Assert.Equal(8, surface.EffectiveCornerRadius.TopLeft);
            window.MouseUp(point, MouseButton.Left); Frames(70);
            Assert.Equal(12, surface.EffectiveCornerRadius.TopLeft);
            button.IsChecked = false; Frames(3); window.Content = null; Frames(2);
            Assert.Equal(0, window.MotionRoot.Diagnostics.ActiveChildCount);
            window.Content = button; Frames(70);
            Assert.Equal(20, surface.EffectiveCornerRadius.TopLeft);
            Assert.Null(window.MotionRoot.Diagnostics.LastError);
        }
        finally { window.Close(); }
    }

    private static Color Color(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;
    private static MotionTestWindow Show(IconToggleButton button, string variant)
    {
        var window = new MotionTestWindow { Width = 240, Height = 160 };
        window.Styles.Add(new MaterialTheme());
        button.Theme = (ControlTheme)window.FindResource($"Material{variant}IconToggleButtonTheme")!;
        window.Content = button; window.Show(); Flush(window); return window;
    }
    private static void Flush(Window window) { Dispatcher.UIThread.RunJobs(); window.UpdateLayout(); }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonShapeApiTests
{
    [AvaloniaTheory]
    [InlineData("Filled")]
    [InlineData("Elevated")]
    [InlineData("Tonal")]
    [InlineData("Outlined")]
    [InlineData("Text")]
    [InlineData("StandardIcon")]
    [InlineData("FilledIcon")]
    [InlineData("TonalIcon")]
    [InlineData("OutlinedIcon")]
    [InlineData("VibrantIcon")]
    [InlineData("OutlinedVibrantIcon")]
    public void CutShapesMorphReverseResizeAndRestoreTheNativeCornerApi(string variant)
    {
        using var clock = new MotionTestClockScope();
        Button button = variant.Contains("Icon", StringComparison.Ordinal) ? new IconButton() : new Button();
        var window = DialogTests.Show(new Grid { Children = { button } });
        try
        {
            button.Theme = (ControlTheme)button.FindResource($"Material{variant}ButtonTheme")!;
            button.Width = 120; button.Height = 64;
            var rest = new CutCornerShape(24);
            var pressed = new CutCornerShape(new RelativeScalar(.125, RelativeUnit.Relative));
            MaterialButton.SetShape(button, rest); MaterialButton.SetPressedShape(button, pressed);
            window.UpdateLayout(); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            var surface = Surface(button);
            double Cut() => ((PathGeometry)surface.ContainerGeometry!).Figures![0].StartPoint.Y;
            Assert.Same(rest, surface.Shape); Assert.Same(pressed, surface.PressedShape);
            Assert.Equal(24, Cut());
            button.Focus(NavigationMethod.Tab);
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            for (var i = 0; i < 4; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.True(ButtonSurfaceState.GetIsPressed(surface));
            Assert.InRange(Cut(), 8.01, 23.99);
            var beforeRelease = Cut(); window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            Assert.Equal(beforeRelease, Cut()); // no pre-composition target snap
            for (var i = 0; i < 35; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(24, Cut());
            window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
            for (var i = 0; i < 35; i++) clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(8, Cut());
            button.Height = 48; window.UpdateLayout();
            Assert.Equal(6, Cut());
            pressed.TopStart = new(.25, RelativeUnit.Relative);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(12, Cut()); // configuration replacement keys a fresh AnimatedShape
            MaterialButton.SetPressedShape(button, null); clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(24, Cut());
            MaterialButton.SetShape(button, null); button.CornerRadius = new(7);
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Null(surface.ContainerGeometry); Assert.Equal(new CornerRadius(7), surface.EffectiveCornerRadius);
        }
        finally { window.Close(); }
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void DefaultPressRetainsOwnerShapeAndStillPlaysRipple(bool icon)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(icon, out var button);
        button.CornerRadius = new CornerRadius(13, 9, 5, 2);
        Dispatcher.UIThread.RunJobs();
        var surface = Surface(button);
        Assert.Null(MaterialButton.GetPressedCornerRadius(button));
        Assert.Null(surface.PressedCornerRadius);
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.StartPress(new Point(8, 8));
        for (var i = 0; i < 20; i++)
        {
            clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
            Assert.Equal(button.CornerRadius, surface.EffectiveCornerRadius);
        }
        Assert.Equal(1, controller.RippleCount);
        controller.EndPress();
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(500));
        Assert.Equal(button.CornerRadius, surface.EffectiveCornerRadius);
        Assert.Equal(0, controller.RippleCount);
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitPressedShapeMorphsAndClearingItWhileHeldRestoresFixedShape(bool icon)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(icon, out var button);
        button.CornerRadius = new CornerRadius(20);
        MaterialButton.SetPressedCornerRadius(button, new CornerRadius(8));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        var surface = Surface(button);
        var controller = MaterialInteraction.GetOrCreate(button);
        controller.StartPress(new Point(8, 8));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        clock.AdvanceBy(TimeSpan.FromMilliseconds(32));
        Assert.InRange(surface.EffectiveCornerRadius.TopLeft, 8.01, 19.99);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(400));
        Assert.Equal(new CornerRadius(8), surface.EffectiveCornerRadius);

        MaterialButton.SetPressedCornerRadius(button, null);
        clock.AdvanceBy(TimeSpan.FromMilliseconds(16));
        Assert.Null(surface.PressedCornerRadius);
        Assert.Equal(button.CornerRadius, surface.EffectiveCornerRadius);
        button.CornerRadius = new CornerRadius(11);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(button.CornerRadius, surface.EffectiveCornerRadius);
        controller.EndPress();
        window.Close();
        Assert.Equal(0, clock.PendingFrameCount);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ShapeStyleBindingsReachTemplateAndLocalOverridesSurviveSizeChanges(bool icon)
    {
        using var clock = new MotionTestClockScope();
        var window = Show(icon, out var button);
        button.Styles.Add(new Style(selector => icon ? selector.OfType<IconButton>() : selector.OfType<Button>())
        {
            Setters = { new Setter(MaterialButton.PressedCornerRadiusProperty, new CornerRadius(6)) }
        });
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new CornerRadius(6), Surface(button).PressedCornerRadius);
        button.CornerRadius = new CornerRadius(17);
        MaterialButton.SetPressedCornerRadius(button, new CornerRadius(3));
        foreach (var size in new[] { "xsmall", "medium", "large", "xlarge" })
        {
            button.Classes.Clear();
            button.Classes.Add(size);
            Dispatcher.UIThread.RunJobs();
            Assert.Equal(new CornerRadius(17), Surface(button).CornerRadius);
            Assert.Equal(new CornerRadius(3), Surface(button).PressedCornerRadius);
        }
        button.ClearValue(Button.CornerRadiusProperty);
        MaterialButton.SetPressedCornerRadius(button, null);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new CornerRadius(1000), Surface(button).CornerRadius);
        Assert.Null(Surface(button).PressedCornerRadius);
        window.Close();
    }

    static MaterialSurface Surface(Button button) => button.GetVisualDescendants().OfType<MaterialSurface>().First();
    static Window Show(bool icon, out Button button)
    {
        if (icon)
        {
            var window = IconButtonThemeFixture.ShowWindow(out var value);
            button = value;
            return window;
        }
        return ButtonThemeFixture.ShowWindow(out button);
    }
}

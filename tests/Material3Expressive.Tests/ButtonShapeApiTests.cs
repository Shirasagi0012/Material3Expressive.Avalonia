using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;
using Xunit;

namespace Material3Expressive.Tests;

public class ButtonShapeApiTests
{
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

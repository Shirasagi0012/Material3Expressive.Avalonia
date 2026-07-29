using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;
using Material3Expressive.VisualTesting.Rendering;
using Xunit;

namespace Material3Expressive.VisualTests;

/// <summary>Frame-level motion coverage for the alpha25 circular wavy indicator.</summary>
public sealed class CircularWavyProgressMotionTests
{
    [AvaloniaFact]
    public void DefaultWaveSpeed_MovesTheDeterminateWaveBetweenFrames()
    {
        var renderer = new VisualRenderer();
        var initial = renderer.Render(Case(TimeSpan.Zero));
        // The default advances one of the nine resolved wavelengths in one second. At exactly one
        // second the regular nine-point star returns to an equivalent orientation, so use a
        // half-wavelength frame to prove the intervening motion rather than testing a symmetry.
        var afterHalfSecond = renderer.Render(Case(TimeSpan.FromSeconds(.5)));

        var changedPixels = initial.Pixels
            .Where((value, index) => value != afterHalfSecond.Pixels[index])
            .Count();

        Assert.True(changedPixels > 0, "The default circular wave must visibly advance between deterministic frames.");
    }

    private static VisualCase Case(TimeSpan elapsed) => new(
        "progress/circular-wavy/motion/light",
        new PixelSize(112, 112),
        VisualTheme.Light,
        _ => new Grid
        {
            Children =
            {
                new MaterialCircularProgressIndicator
                {
                    Name = "component",
                    Value = .5,
                    IsWavy = true,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        },
        Prepare: context =>
        {
            context.UseManualMotionClock();
            context.AdvanceAnimations(elapsed);
        });
}

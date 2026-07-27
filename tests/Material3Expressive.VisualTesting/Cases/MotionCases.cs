using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>
/// Exact intermediate frames driven by real input and a deterministic manual animation clock.
/// </summary>
internal static class MotionCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        HoverFrame(0),
        HoverFrame(7),
        HoverFrame(15),
        PressFrame(0),
        PressFrame(45),
        PressFrame(90),
        PressFrame(225)
    ];

    private static VisualCase HoverFrame(int elapsedMilliseconds) =>
        ButtonFrame(
            $"motion/button/hover/{elapsedMilliseconds:000}ms/light",
            elapsedMilliseconds,
            (context, target) =>
                context.Window.MouseMove(PointIn(target, context.Window)));

    private static VisualCase PressFrame(int elapsedMilliseconds) =>
        ButtonFrame(
            $"motion/button/press/{elapsedMilliseconds:000}ms/light",
            elapsedMilliseconds,
            (context, target) =>
            {
                var point = PointIn(target, context.Window, target.Bounds.Width * 0.3);
                context.Window.MouseMove(point);
                context.Window.MouseDown(point, MouseButton.Left);
                if (!target.IsPressed)
                    throw new InvalidOperationException("Headless pointer input did not press the button.");
            });

    private static VisualCase ButtonFrame(
        string id,
        int elapsedMilliseconds,
        Action<VisualContext, MaterialButton> input) =>
        new(
            id,
            new PixelSize(360, 180),
            VisualTheme.Light,
            _ => Center(
                new MaterialButton
                {
                    Name = "target",
                    Content = "Animate",
                    Variant = ButtonVariant.Filled,
                    Size = ButtonSize.Medium
                }),
            ComparisonProfile.Exact,
            ["full", "motion", "button"],
            Prepare: context =>
            {
                var target = context.Require<MaterialButton>("target");
                context.UseManualMotionClock();
                input(context, target);
                context.AdvanceAnimations(TimeSpan.FromMilliseconds(elapsedMilliseconds));
            });

    private static Point PointIn(
        Control target,
        Visual relativeTo,
        double? x = null,
        double? y = null) =>
        target.TranslatePoint(
            new Point(x ?? target.Bounds.Width / 2, y ?? target.Bounds.Height / 2),
            relativeTo)
        ?? throw new InvalidOperationException($"'{target.Name}' is not mounted in the visual tree.");

    private static Grid Center(Control control)
    {
        control.HorizontalAlignment = HorizontalAlignment.Center;
        control.VerticalAlignment = VerticalAlignment.Center;
        return new Grid
        {
            Children = { control }
        };
    }
}

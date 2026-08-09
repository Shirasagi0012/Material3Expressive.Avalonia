using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Renderer-neutral probes for the deterministic motion replay contract.</summary>
internal static class MotionParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        LinearTween("start"),
        LinearTween("mid"),
        LinearTween("end")
    ];

    private static VisualCase LinearTween(string checkpointId) => new(
        $"foundation/motion/linear-tween/{checkpointId}/light",
        new PixelSize(112, 64),
        VisualTheme.Light,
        _ => new Grid
        {
            Children =
            {
                new LinearTweenProbe
                {
                    Name = "component",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: "foundation/motion/linear-tween/light",
            CheckpointId: checkpointId,
            TriggerAction: "start-tween",
            Trigger: context => context.Require<LinearTweenProbe>("component").Start(),
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(usesComposeCoroutineClock: false)));

    private sealed class LinearTweenProbe : Control, IMaterialMotionConsumer
    {
        private static readonly TimeSpan Duration = TimeSpan.FromMilliseconds(200);
        private readonly MaterialMotionController _motion;
        private readonly TweenAnimation _progress;

        public LinearTweenProbe()
        {
            Width = 60;
            Height = 20;
            _motion = MaterialElementContext.Ensure(this).Motion;
            _progress = _motion.AddTween(0, Duration, Easings.Linear);
        }

        public void Start()
        {
            _progress.AnimateTo(1, Duration, Easings.Linear);
            _motion.RequestFrame();
        }

        void IMaterialMotionConsumer.OnMaterialMotionFrame() => InvalidateVisual();

        public override void Render(DrawingContext context)
        {
            context.DrawRectangle(
                Brushes.Black,
                null,
                new Rect(_progress.Value * 40, 0, 20, 20));
        }
    }
}

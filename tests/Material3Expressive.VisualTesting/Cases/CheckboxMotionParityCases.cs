using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Material3Expressive.Controls;
using Material3Expressive.VisualTesting.Model;

namespace Material3Expressive.VisualTesting.Cases;

/// <summary>Checkbox state-transition checkpoints driven by the Compose-produced replay schedule.</summary>
internal static class CheckboxMotionParityCases
{
    public static IReadOnlyList<VisualCase> All { get; } =
    [
        Scene("off-to-on", "spatial-settled", isChecked: false, target: true),
        Scene("on-to-indeterminate", "morph-mid", isChecked: true, target: null),
        Scene("indeterminate-to-off", "after-clear", isChecked: null, target: false)
    ];

    private static VisualCase Scene(string scenario, string checkpoint, bool? isChecked, bool? target) => new(
        $"selection/checkbox/motion/{scenario}/{checkpoint}/light",
        new PixelSize(96, 96),
        VisualTheme.Light,
        _ => new Grid
        {
            Children =
            {
                new MaterialCheckbox
                {
                    Name = "component",
                    IsThreeState = true,
                    IsChecked = isChecked,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        },
        RenderScaling: 1,
        Motion: new VisualMotionSpec(
            ScenarioId: $"selection/checkbox/motion/{scenario}/light",
            CheckpointId: checkpoint,
            TriggerAction: $"set-{StateName(target)}",
            Trigger: context => context.Require<MaterialCheckbox>("component").IsChecked = target,
            ClockCoverage: VisualMotionClockCoverage.ComponentOwned(usesComposeCoroutineClock: false)));

    private static string StateName(bool? value) => value switch
    {
        true => "on",
        false => "off",
        null => "indeterminate"
    };
}

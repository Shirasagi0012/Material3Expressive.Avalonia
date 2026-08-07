using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Material3Expressive.Controls;

namespace Material3Expressive.VisualTesting.Rendering;

/// <summary>
/// Test-only bridge that settles the public motion primitives owned privately by controls.
/// The interaction that chose each target must still be produced by real input.
/// </summary>
internal static class VisualMotionSettler
{
    public static void Settle(Visual root)
    {
        foreach (var coordinator in FindCoordinators(root))
            coordinator.SettleAll();
    }

    public static void UseManualClock(Visual root)
    {
        foreach (var coordinator in FindCoordinators(root))
            coordinator.UseManualClock();
    }

    public static void AdvanceBy(Visual root, TimeSpan delta)
    {
        foreach (var coordinator in FindCoordinators(root))
            coordinator.AdvanceBy(delta);
    }

    private static HashSet<MaterialMotionCoordinator> FindCoordinators(Visual root)
    {
        var coordinators = new HashSet<MaterialMotionCoordinator>();
        foreach (var visual in root.GetVisualDescendants().Prepend(root))
        {
            if (visual is Control control
                && MaterialElementContext.TryGet(control, out var context)
                && context.MotionCoordinatorIfCreated is { } coordinator)
                coordinators.Add(coordinator);
        }

        return coordinators;
    }
}

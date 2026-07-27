using System.Reflection;
using Avalonia;
using Avalonia.VisualTree;
using Material3Expressive.Controls;

namespace Material3Expressive.VisualTesting.Rendering;

/// <summary>
/// Test-only bridge that settles the public motion primitives owned privately by controls.
/// The interaction that chose each target must still be produced by real input.
/// </summary>
internal static class VisualMotionSettler
{
    private const BindingFlags Fields =
        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    public static void Settle(Visual root)
    {
        foreach (var group in FindGroups(root))
            group.SettleAll();
    }

    public static void UseManualClock(Visual root)
    {
        foreach (var group in FindGroups(root))
            group.UseManualClock();
    }

    public static void AdvanceBy(Visual root, TimeSpan delta)
    {
        foreach (var group in FindGroups(root))
            group.AdvanceBy(delta);
    }

    private static HashSet<MotionGroup> FindGroups(Visual root)
    {
        var groups = new HashSet<MotionGroup>();
        foreach (var visual in root.GetVisualDescendants().Prepend(root))
        {
            for (var type = visual.GetType(); type is not null; type = type.BaseType)
            {
                foreach (var field in type.GetFields(Fields))
                {
                    if (field.GetValue(visual) is MotionGroup group)
                    {
                        groups.Add(group);
                    }
                    else if (field.GetValue(visual) is MaterialInteractionHost interaction)
                    {
                        groups.Add(interaction.Motion);
                    }
                }
            }
        }

        return groups;
    }
}

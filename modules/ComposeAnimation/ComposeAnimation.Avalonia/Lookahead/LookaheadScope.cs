using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace ComposeAnimation;

internal static class LookaheadScope
{
    internal static LookaheadContext? FindContext(Visual visual)
    {
        visual.VerifyAccess();
        return LookaheadEnvironment.GetContext(visual);
    }

    /// <summary>Returns the inherited environment only when this control is an actual domain member.</summary>
    internal static LookaheadContext? FindParticipatingContext(Control control)
    {
        var context = FindContext(control);
        return context?.Contains(control) == true ? context : null;
    }

    internal static LookaheadBoundary? FindEnclosingBoundary(Visual visual)
    {
        visual.VerifyAccess();
        for (var parent = visual.GetVisualParent(); parent is not null; parent = parent.GetVisualParent())
        {
            if (LookaheadEnvironment.GetContext(parent)?.Owner == parent) return null;
            if (parent is LookaheadBoundary boundary) return boundary;
        }
        return null;
    }
}

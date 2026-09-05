using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Media;

namespace ComposeAnimationRuntime.Avalonia;

internal static class TransformComposition
{
    private sealed class State(Transform? original)
    {
        public Transform? Original { get; } = original;
        public List<Transform> Parts { get; } = [];
    }

    private static readonly ConditionalWeakTable<Control, State> States = new();

    public static void Begin(Control control)
    {
        var state = States.GetValue(control, static item => new State(item.RenderTransform as Transform));
        state.Parts.Clear();
        control.RenderTransform = state.Original;
    }

    public static void Add(Control control, Transform transform)
    {
        var state = States.GetValue(control, static item => new State(item.RenderTransform as Transform));
        state.Parts.Add(transform);
        var group = new TransformGroup();
        if (state.Original is not null)
            group.Children.Add(state.Original);
        foreach (var part in state.Parts)
            group.Children.Add(part);
        control.RenderTransform = group;
    }

    public static void End(Control control)
    {
        if (!States.TryGetValue(control, out var state))
            return;
        control.RenderTransform = state.Original;
        States.Remove(control);
    }
}

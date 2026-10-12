using System.Runtime.CompilerServices;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>View-owned adapter for an explicit state or an externally driven parent transition.</summary>
public abstract class VisibilitySource
{
    private protected VisibilitySource() { }

    public static VisibilitySource From(MutableTransitionState<bool> state)
        => new StateSource(state ?? throw new ArgumentNullException(nameof(state)));

    public static VisibilitySource From<T>(Transition<T> transition, Func<T, bool> visible)
        => new ParentSource<T>(transition ?? throw new ArgumentNullException(nameof(transition)),
            visible ?? throw new ArgumentNullException(nameof(visible)));

    internal abstract VisibilityConnection Connect(IFrameSource clock, string? label);

    private sealed class StateSource(MutableTransitionState<bool> state) : VisibilitySource
    {
        private static readonly ConditionalWeakTable<MutableTransitionState<bool>, object> Owners = new();
        internal override VisibilityConnection Connect(IFrameSource clock, string? label)
        {
            if (!Owners.TryAdd(state, new object()))
                throw new InvalidOperationException("A visibility state cannot drive multiple active hosts. Share a parent Transition instead.");
            try { return new(new Transition<bool>(state, clock, label), true, () => Owners.Remove(state)); }
            catch { Owners.Remove(state); throw; }
        }
    }

    private sealed class ParentSource<T>(Transition<T> transition, Func<T, bool> visible) : VisibilitySource
    {
        internal override VisibilityConnection Connect(IFrameSource clock, string? label)
        {
            transition.VerifyAccess();
            return new(transition.CreateChildTransition(visible, label ?? "AnimatedVisibility"), false);
        }
    }
}

internal sealed class VisibilityConnection(Transition<bool> transition, bool ownsLoop, Action? release = null) : IDisposable
{
    public Transition<bool> Transition { get; } = transition;
    public bool OwnsLoop { get; } = ownsLoop;
    public void Dispose()
    {
        try { Transition.Dispose(); }
        finally { release?.Invoke(); }
    }
}

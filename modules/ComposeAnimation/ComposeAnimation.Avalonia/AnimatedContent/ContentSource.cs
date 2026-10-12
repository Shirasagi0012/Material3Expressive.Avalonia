using System.Runtime.CompilerServices;
using ComposeAnimation.Runtime;

namespace ComposeAnimation;

/// <summary>View-owned adapter for an explicit state or an externally driven parent transition.</summary>
public abstract class ContentSource
{
    private protected ContentSource() { }

    public static ContentSource From(MutableTransitionState<object?> state)
        => new ObjectStateSource(state ?? throw new ArgumentNullException(nameof(state)));

    public static ContentSource From<T>(MutableTransitionState<T> state, Func<T, object?>? content = null)
        => new TypedStateSource<T>(state ?? throw new ArgumentNullException(nameof(state)),
            content ?? (s => s!));

    public static ContentSource From<T>(Transition<T> transition, Func<T, object?> content)
        => new ParentSource<T>(transition ?? throw new ArgumentNullException(nameof(transition)),
            content ?? throw new ArgumentNullException(nameof(content)));

    public static ContentSource From<T>(Transition<T> transition)
        => From(transition, s => s!);

    internal abstract ContentConnection Connect(string? label);

    private sealed class ObjectStateSource(MutableTransitionState<object?> state) : ContentSource
    {
        private static readonly ConditionalWeakTable<MutableTransitionState<object?>, object> Owners = new();
        internal override ContentConnection Connect(string? label)
        {
            if (!Owners.TryAdd(state, new object()))
                throw new InvalidOperationException("A content state cannot drive multiple active hosts. Share a parent Transition instead.");
            try { return new(new Transition<object?>(state, label), true, null, () => Owners.Remove(state)); }
            catch { Owners.Remove(state); throw; }
        }
    }

    private sealed class TypedStateSource<T>(MutableTransitionState<T> state, Func<T, object?> content) : ContentSource
    {
        private static readonly ConditionalWeakTable<object, object> Owners = new();
        internal override ContentConnection Connect(string? label)
        {
            if (!Owners.TryAdd(state, new object()))
                throw new InvalidOperationException("A content state cannot drive multiple active hosts. Share a parent Transition instead.");
            try
            {
                var root = new Transition<T>(state, label);
                var child = root.CreateChildTransition(content, "AnimatedContent");
                return new(child, true, root, () => Owners.Remove(state));
            }
            catch { Owners.Remove(state); throw; }
        }
    }

    private sealed class ParentSource<T>(Transition<T> transition, Func<T, object?> content) : ContentSource
    {
        internal override ContentConnection Connect(string? label)
        {
            transition.VerifyAccess();
            return new(transition.CreateChildTransition(content, label ?? "AnimatedContent"), false);
        }
    }
}

internal sealed class ContentConnection : IDisposable
{
    private readonly Transition? _loopRoot;
    private readonly Action? _release;
    public ContentConnection(Transition<object?> transition, bool ownsLoop, Transition? loopRoot = null,
        Action? release = null)
    {
        Transition = transition;
        OwnsLoop = ownsLoop;
        _loopRoot = loopRoot;
        _release = release;
    }
    public Transition<object?> Transition { get; }
    public bool OwnsLoop { get; }
    public Transition Loop => _loopRoot ?? Transition;
    public void Dispose()
    {
        try { Loop.Dispose(); }
        finally { _release?.Invoke(); }
    }
}

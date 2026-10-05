using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Creates scroll behaviors. Visual configuration comes from the AppBar theme and tokens.</summary>
public static class TopAppBarDefaults
{
    public static TopAppBarScrollBehavior PinnedScrollBehavior(
        TopAppBarState? state = null,
        Func<bool>? canScroll = null,
        Func<bool>? isScrollingContentAtStart = null) =>
        new DefaultTopAppBarScrollBehavior(
            TopScrollKind.Pinned, state ?? new(), canScroll, isScrollingContentAtStart, null, null);

    public static TopAppBarScrollBehavior EnterAlwaysScrollBehavior(
        TopAppBarState? state = null,
        Func<bool>? canScroll = null,
        Func<bool>? isScrollingContentAtStart = null,
        float? density = null) =>
        new DefaultTopAppBarScrollBehavior(
            TopScrollKind.EnterAlways, state ?? new(), canScroll, isScrollingContentAtStart,
            null, null, true, density);

    public static TopAppBarScrollBehavior EnterAlwaysScrollBehavior(
        TopAppBarState state,
        IAnimationSpec<float>? snapAnimationSpec,
        IDecayAnimationSpec<float>? flingAnimationSpec,
        Func<bool>? canScroll = null,
        Func<bool>? isScrollingContentAtStart = null) =>
        new DefaultTopAppBarScrollBehavior(
            TopScrollKind.EnterAlways, state, canScroll, isScrollingContentAtStart,
            snapAnimationSpec, flingAnimationSpec);

    public static TopAppBarScrollBehavior ExitUntilCollapsedScrollBehavior(
        TopAppBarState? state = null,
        Func<bool>? canScroll = null,
        float? density = null) =>
        new DefaultTopAppBarScrollBehavior(
            TopScrollKind.ExitUntilCollapsed, state ?? new(), canScroll, null, null, null, true, density);

    public static TopAppBarScrollBehavior ExitUntilCollapsedScrollBehavior(
        TopAppBarState state,
        IAnimationSpec<float>? snapAnimationSpec,
        IDecayAnimationSpec<float>? flingAnimationSpec,
        Func<bool>? canScroll = null) =>
        new DefaultTopAppBarScrollBehavior(
            TopScrollKind.ExitUntilCollapsed, state, canScroll, null, snapAnimationSpec, flingAnimationSpec);
}

/// <summary>Creates scroll behaviors. Visual configuration comes from the AppBar theme and tokens.</summary>
public static class BottomAppBarDefaults
{
    public static BottomAppBarScrollBehavior ExitAlwaysScrollBehavior(
        BottomAppBarState? state = null,
        Func<bool>? canScroll = null,
        float? density = null) =>
        new DefaultBottomAppBarScrollBehavior(state ?? new(), canScroll, null, null, true, density);

    public static BottomAppBarScrollBehavior ExitAlwaysScrollBehavior(
        BottomAppBarState state,
        IAnimationSpec<float>? snapAnimationSpec,
        IDecayAnimationSpec<float>? flingAnimationSpec,
        Func<bool>? canScroll = null) =>
        new DefaultBottomAppBarScrollBehavior(state, canScroll, snapAnimationSpec, flingAnimationSpec);
}

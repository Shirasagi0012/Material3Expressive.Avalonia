using Avalonia;
using Avalonia.Media;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public sealed record TopAppBarColors(Color? ContainerColor = null, Color? ScrolledContainerColor = null,
    Color? NavigationIconContentColor = null, Color? TitleContentColor = null,
    Color? ActionIconContentColor = null, Color? SubtitleContentColor = null);

public static class TopAppBarDefaults
{
    public const double TopAppBarExpandedHeight = 64;
    public const double MediumAppBarCollapsedHeight = 64;
    public const double MediumAppBarExpandedHeight = 112;
    public const double LargeAppBarCollapsedHeight = 64;
    public const double LargeAppBarExpandedHeight = 152;
    public const double MediumFlexibleAppBarWithoutSubtitleExpandedHeight = 112;
    public const double MediumFlexibleAppBarWithSubtitleExpandedHeight = 136;
    public const double LargeFlexibleAppBarWithoutSubtitleExpandedHeight = 120;
    public const double LargeFlexibleAppBarWithSubtitleExpandedHeight = 152;
    public static Thickness ContentPadding => default;
    public static Thickness WindowInsets => default;
    public static IAnimationSpec<float> SnapAnimationSpec => new SpringSpec<float>(1, 1600);
    public static IDecayAnimationSpec<float> FlingAnimationSpec(float density = 1) => DecayAnimationSpec.SplineBasedDecay<float>(density);
    public static TopAppBarColors Colors() => new();
    public static TopAppBarScrollBehavior PinnedScrollBehavior(TopAppBarState? state = null, Func<bool>? canScroll = null, Func<bool>? isScrollingContentAtStart = null) =>
        new DefaultTopAppBarScrollBehavior(TopScrollKind.Pinned, state ?? new(), canScroll, isScrollingContentAtStart, null, null);
    public static TopAppBarScrollBehavior EnterAlwaysScrollBehavior(TopAppBarState? state = null, Func<bool>? canScroll = null, Func<bool>? isScrollingContentAtStart = null, float? density = null) =>
        new DefaultTopAppBarScrollBehavior(TopScrollKind.EnterAlways, state ?? new(), canScroll, isScrollingContentAtStart, null, null, true, density);
    public static TopAppBarScrollBehavior EnterAlwaysScrollBehavior(TopAppBarState state, IAnimationSpec<float>? snapAnimationSpec,
        IDecayAnimationSpec<float>? flingAnimationSpec, Func<bool>? canScroll = null, Func<bool>? isScrollingContentAtStart = null) =>
        new DefaultTopAppBarScrollBehavior(TopScrollKind.EnterAlways, state, canScroll, isScrollingContentAtStart, snapAnimationSpec, flingAnimationSpec);
    public static TopAppBarScrollBehavior ExitUntilCollapsedScrollBehavior(TopAppBarState? state = null, Func<bool>? canScroll = null, float? density = null) =>
        new DefaultTopAppBarScrollBehavior(TopScrollKind.ExitUntilCollapsed, state ?? new(), canScroll, null, null, null, true, density);
    public static TopAppBarScrollBehavior ExitUntilCollapsedScrollBehavior(TopAppBarState state, IAnimationSpec<float>? snapAnimationSpec,
        IDecayAnimationSpec<float>? flingAnimationSpec, Func<bool>? canScroll = null) =>
        new DefaultTopAppBarScrollBehavior(TopScrollKind.ExitUntilCollapsed, state, canScroll, null, snapAnimationSpec, flingAnimationSpec);
}

public static class BottomAppBarDefaults
{
    public const double ContainerHeight = 80;
    public const double FlexibleBottomAppBarHeight = 64;
    public const double ContainerElevation = 0;
    public const double FlexibleContainerElevation = 0;
    public static Thickness ContentPadding => new(4, 4, 4, 0);
    public static Thickness FlexibleContentPadding => new(16, 0, 16, 0);
    public static Thickness WindowInsets => default;
    public static IAnimationSpec<float> SnapAnimationSpec => new SpringSpec<float>(.6f, 800);
    public static IDecayAnimationSpec<float> FlingAnimationSpec(float density = 1) => DecayAnimationSpec.SplineBasedDecay<float>(density);
    public static BottomAppBarScrollBehavior ExitAlwaysScrollBehavior(BottomAppBarState? state = null, Func<bool>? canScroll = null, float? density = null) =>
        new DefaultBottomAppBarScrollBehavior(state ?? new(), canScroll, null, null, true, density);
    public static BottomAppBarScrollBehavior ExitAlwaysScrollBehavior(BottomAppBarState state, IAnimationSpec<float>? snapAnimationSpec,
        IDecayAnimationSpec<float>? flingAnimationSpec, Func<bool>? canScroll = null) =>
        new DefaultBottomAppBarScrollBehavior(state, canScroll, snapAnimationSpec, flingAnimationSpec);
}

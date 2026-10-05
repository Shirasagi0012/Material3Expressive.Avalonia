using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ComposeAnimation.Runtime;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarScrollBehaviorTests
{
    [AvaloniaFact]
    public void CompiledXaml_SupportsLiteralsBindingsAndObjectElements()
    {
        var root = (StackPanel)AvaloniaXamlLoader.Load(new Uri(
            "avares://Material3Expressive.Tests/Fixtures/AppBarScrollBehavior.axaml"));
        var literal = root.FindControl<TopAppBar>("Literal")!.ScrollBehavior!;
        var named = root.FindControl<TopAppBar>("NamedLiteral")!.ScrollBehavior!;
        var positional = root.FindControl<TopAppBar>("PositionalBinding")!.ScrollBehavior!;
        var compiled = root.FindControl<TopAppBar>("CompiledBinding")!.ScrollBehavior!;
        var objectBehavior = root.FindControl<TopAppBar>("Object")!.ScrollBehavior!;
        var bottom = root.FindControl<BottomAppBar>("BottomMarkup")!.ScrollBehavior!;
        var bottomObject = root.FindControl<BottomAppBar>("BottomObject")!.ScrollBehavior!;
        var positionalDefault = positional.State;
        var compiledDefault = compiled.State;
        var ownState = objectBehavior.State;
        var bottomDefault = bottom.State;
        Assert.Equal(TopAppBarScrollMode.EnterAlways, literal.Mode);
        Assert.Equal(TopAppBarScrollMode.ExitUntilCollapsed, named.Mode);
        Assert.NotSame(positionalDefault, compiledDefault);
        Assert.NotNull(compiled.SnapAnimationSpec);

        var first = new AppBarBindingData
        {
            Mode = TopAppBarScrollMode.EnterAlways, CanScroll = false, AtStart = false,
            TopState = new(), BottomState = new(), Snap = new SpringSpec<float>(.5, 500),
        };
        root.DataContext = first;
        Dispatcher.UIThread.RunJobs();
        foreach (var behavior in new[] { positional, compiled, objectBehavior })
        {
            Assert.Equal(first.Mode, behavior.Mode);
            Assert.False(behavior.CanScroll);
            Assert.Same(first, behavior.DataContext);
        }
        Assert.Same(first.TopState, positional.State);
        Assert.Same(first.TopState, compiled.State);
        Assert.Same(first.BottomState, bottom.State);
        Assert.False(bottom.CanScroll);
        Assert.False(bottomObject.CanScroll);
        Assert.False(positional.State.IsScrollingContentAtStart);
        Assert.Same(ownState, objectBehavior.State);
        Assert.Equal(-10, ownState.ContentOffset);
        Assert.Same(first.Snap, compiled.GetSnapAnimationSpec());
        Assert.Null(compiled.GetFlingAnimationSpec());

        first.Mode = TopAppBarScrollMode.ExitUntilCollapsed;
        first.CanScroll = true;
        first.Snap = null;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(first.Mode, positional.Mode);
        Assert.Equal(first.Mode, compiled.Mode);
        Assert.True(objectBehavior.CanScroll);
        Assert.True(bottom.CanScroll);
        Assert.Null(compiled.GetSnapAnimationSpec());

        root.DataContext = new AppBarBindingData { Mode = TopAppBarScrollMode.EnterAlways };
        Dispatcher.UIThread.RunJobs();
        Assert.Same(positionalDefault, positional.State);
        Assert.Same(compiledDefault, compiled.State);
        Assert.Same(bottomDefault, bottom.State);
        Assert.Same(positional, root.FindControl<TopAppBar>("PositionalBinding")!.ScrollBehavior);
        Assert.Same(compiled, root.FindControl<TopAppBar>("CompiledBinding")!.ScrollBehavior);
        Assert.Same(ownState, objectBehavior.State);
    }

    [AvaloniaFact]
    public void AnimationProperties_ResolveIndependentlyAndClearRestoresDefaults()
    {
        var top = new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways);
        var bottom = new BottomAppBarScrollBehavior();
        var snap = new SpringSpec<float>(.8, 700);
        var fling = DecayAnimationSpec.SplineBasedDecay<float>(2);
        top.SnapAnimationSpec = null;
        Assert.Null(top.GetSnapAnimationSpec());
        Assert.NotNull(top.GetFlingAnimationSpec());
        top.ClearValue(TopAppBarScrollBehavior.SnapAnimationSpecProperty);
        top.FlingAnimationSpec = null;
        Assert.NotNull(top.GetSnapAnimationSpec());
        Assert.Null(top.GetFlingAnimationSpec());
        top.SnapAnimationSpec = snap;
        top.FlingAnimationSpec = fling;
        Assert.Same(snap, top.GetSnapAnimationSpec());
        Assert.Same(fling, top.GetFlingAnimationSpec());
        top.Mode = TopAppBarScrollMode.Pinned;
        Assert.True(top.IsPinned);
        Assert.Null(top.GetSnapAnimationSpec());
        Assert.Null(top.GetFlingAnimationSpec());
        top.Mode = TopAppBarScrollMode.EnterAlways;
        Assert.Same(snap, top.GetSnapAnimationSpec());

        bottom.SnapAnimationSpec = null;
        Assert.Null(bottom.GetSnapAnimationSpec());
        Assert.NotNull(bottom.GetFlingAnimationSpec());
        bottom.ClearValue(BottomAppBarScrollBehavior.SnapAnimationSpecProperty);
        bottom.FlingAnimationSpec = null;
        Assert.NotNull(bottom.GetSnapAnimationSpec());
        Assert.Null(bottom.GetFlingAnimationSpec());
        bottom.ClearValue(BottomAppBarScrollBehavior.FlingAnimationSpecProperty);
        Assert.NotNull(bottom.GetFlingAnimationSpec());

        top.ClearValue(TopAppBarScrollBehavior.SnapAnimationSpecProperty);
        using var binding = top.Bind(TopAppBarScrollBehavior.SnapAnimationSpecProperty,
            new Binding("Snap"));
        Assert.NotNull(top.GetSnapAnimationSpec());
        top.DataContext = new AppBarBindingData();
        Assert.Null(top.GetSnapAnimationSpec());
        top.DataContext = null;
        Assert.NotNull(top.GetSnapAnimationSpec());
        top.DataContext = new AppBarBindingData();
        Assert.Null(top.GetSnapAnimationSpec());
        top.ClearValue(TopAppBarScrollBehavior.SnapAnimationSpecProperty);
        Assert.NotNull(top.GetSnapAnimationSpec());
    }

    [AvaloniaFact]
    public void Behavior_OwnershipTransfersAndRejectsSharing()
    {
        var behavior = new TopAppBarScrollBehavior(TopAppBarScrollMode.EnterAlways);
        var first = new TopAppBar { ScrollBehavior = behavior };
        var second = new TopAppBar();
        Assert.Same(first, behavior.Parent);
        Assert.Throws<InvalidOperationException>(() => second.ScrollBehavior = behavior);
        first.ScrollBehavior = null;
        second.ScrollBehavior = behavior;
        Assert.Same(second, behavior.Parent);
    }
}

// Avalonia properties give the XAML fixture observable inputs without a test-only binding adapter.
public class AppBarBindingData : AvaloniaObject
{
    public static readonly StyledProperty<TopAppBarScrollMode> ModeProperty =
        AvaloniaProperty.Register<AppBarBindingData, TopAppBarScrollMode>(nameof(Mode));
    public static readonly StyledProperty<bool> CanScrollProperty =
        AvaloniaProperty.Register<AppBarBindingData, bool>(nameof(CanScroll), true);
    public static readonly StyledProperty<bool?> AtStartProperty =
        AvaloniaProperty.Register<AppBarBindingData, bool?>(nameof(AtStart));
    public static readonly StyledProperty<TopAppBarState?> TopStateProperty =
        AvaloniaProperty.Register<AppBarBindingData, TopAppBarState?>(nameof(TopState));
    public static readonly StyledProperty<BottomAppBarState?> BottomStateProperty =
        AvaloniaProperty.Register<AppBarBindingData, BottomAppBarState?>(nameof(BottomState));
    public static readonly StyledProperty<IAnimationSpec<float>?> SnapProperty =
        AvaloniaProperty.Register<AppBarBindingData, IAnimationSpec<float>?>(nameof(Snap));

    public TopAppBarScrollMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }
    public bool CanScroll { get => GetValue(CanScrollProperty); set => SetValue(CanScrollProperty, value); }
    public bool? AtStart { get => GetValue(AtStartProperty); set => SetValue(AtStartProperty, value); }
    public TopAppBarState? TopState { get => GetValue(TopStateProperty); set => SetValue(TopStateProperty, value); }
    public BottomAppBarState? BottomState { get => GetValue(BottomStateProperty); set => SetValue(BottomStateProperty, value); }
    public IAnimationSpec<float>? Snap { get => GetValue(SnapProperty); set => SetValue(SnapProperty, value); }
}

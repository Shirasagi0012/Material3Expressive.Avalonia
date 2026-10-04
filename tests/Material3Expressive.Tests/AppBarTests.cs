using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Components;
using Xunit;

namespace Material3Expressive.Tests;

public class AppBarTests
{
    [AvaloniaTheory]
    [InlineData("small", 64)] [InlineData("center", 64)] [InlineData("medium", 112)]
    [InlineData("large", 152)] [InlineData("mediumFlexible", 112)] [InlineData("largeFlexible", 120)]
    [InlineData("twoRows", 112)]
    public void DefaultTopHeights(string variant, double height)
    {
        TopAppBar bar = variant switch { "center" => new CenterAlignedTopAppBar(), "medium" => new MediumTopAppBar(), "large" => new LargeTopAppBar(),
            "mediumFlexible" => new MediumFlexibleTopAppBar(), "largeFlexible" => new LargeFlexibleTopAppBar(), "twoRows" => new TwoRowsTopAppBar(), _ => new TopAppBar() };
        using var clock = new MotionTestClockScope();
        var window = Show(bar);
        try { Assert.True(bar.Bounds.Height == height, $"height={bar.Bounds.Height}, desired={bar.DesiredSize}, template={bar.Template}, theme={bar.Theme}, hasTheme={bar.TryFindResource(typeof(TopAppBar), out var found)}, resource={found}, setters={(found as ControlTheme)?.Setters.Count}, key={bar.StyleKey}, initialized={bar.IsInitialized}"); }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CollapsedTitle_ExplicitNull_Styles_Binding_ClearValue()
    {
        var bar = new MediumTopAppBar { Title = "expanded" };
        using var clock = new MotionTestClockScope();
        var window = Show(bar);
        try
        {
            Assert.Equal("expanded", bar.ResolveTitle(true));
            bar.CollapsedTitle = null; window.UpdateLayout(); Assert.Null(bar.ResolveTitle(true));
            bar.ClearValue(TopAppBar.CollapsedTitleProperty); window.UpdateLayout(); Assert.Equal("expanded", bar.ResolveTitle(true));
            var style = new Style { Setters = { new Setter(TopAppBar.CollapsedTitleProperty, null) } };
            bar.Styles.Add(style); bar.ApplyStyling(); window.UpdateLayout(); Assert.Null(bar.ResolveTitle(true));
            bar.SetValue(TopAppBar.CollapsedTitleProperty, "local"); Assert.Equal("local", bar.ResolveTitle(true));
            bar.ClearValue(TopAppBar.CollapsedTitleProperty); Assert.Null(bar.ResolveTitle(true));
            bar.Styles.Remove(style); window.UpdateLayout(); Assert.Equal("expanded", bar.ResolveTitle(true));
            var source = new TextBlock { Tag = null };
            using (bar.Bind(TopAppBar.CollapsedTitleProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                Assert.Null(bar.ResolveTitle(true)); source.Tag = "bound"; Assert.Equal("bound", bar.ResolveTitle(true));
                source.Tag = null; Assert.Null(bar.ResolveTitle(true));
            }
            window.UpdateLayout(); Assert.Equal("expanded", bar.ResolveTitle(true));
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void CollapsedTemplates_ExplicitNull_Binding_Styles_ClearValue_UpdateActualTree()
    {
        var template = new FuncDataTemplate<object>((data, _) => new Border { Tag = data, Width = 80, Height = 20 });
        var bar = new MediumFlexibleTopAppBar { Title = "title", Subtitle = "subtitle", TitleTemplate = template, SubtitleTemplate = template };
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            var collapsed = bar.GetVisualDescendants().OfType<AppBarTitle>().First();
            Assert.Equal(2, collapsed.GetVisualDescendants().OfType<Border>().Count());
            bar.CollapsedTitle = null; bar.CollapsedSubtitle = null; window.UpdateLayout();
            Assert.Empty(collapsed.GetVisualDescendants().OfType<Border>());
            bar.ClearValue(TopAppBar.CollapsedTitleProperty); bar.ClearValue(TopAppBar.CollapsedSubtitleProperty); window.UpdateLayout();
            Assert.Equal(2, collapsed.GetVisualDescendants().OfType<Border>().Count());
            var source = new Border { Tag = null };
            using (bar.Bind(TopAppBar.CollapsedTitleTemplateProperty, new Binding(nameof(Control.Tag)) { Source = source }))
            {
                window.UpdateLayout(); Assert.Single(collapsed.GetVisualDescendants().OfType<Border>());
                source.Tag = template; window.UpdateLayout(); Assert.Equal(2, collapsed.GetVisualDescendants().OfType<Border>().Count());
                source.Tag = null; window.UpdateLayout(); Assert.Single(collapsed.GetVisualDescendants().OfType<Border>());
            }
            var style = new Style { Setters = { new Setter(TopAppBar.CollapsedSubtitleTemplateProperty, null) } };
            bar.Styles.Add(style); bar.ApplyStyling(); window.UpdateLayout(); Assert.Single(collapsed.GetVisualDescendants().OfType<Border>());
            bar.CollapsedSubtitleTemplate = template; window.UpdateLayout(); Assert.Equal(2, collapsed.GetVisualDescendants().OfType<Border>().Count());
            bar.ClearValue(TopAppBar.CollapsedSubtitleTemplateProperty); window.UpdateLayout(); Assert.Single(collapsed.GetVisualDescendants().OfType<Border>());
            bar.Styles.Remove(style); window.UpdateLayout(); Assert.Equal(2, collapsed.GetVisualDescendants().OfType<Border>().Count());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void TwoTitleTrees_AndOnlyActiveTitleSemantics()
    {
        var state = new TopAppBarState();
        var bar = new MediumTopAppBar { Title = new object(), TitleTemplate = new FuncDataTemplate<object>((_, _) => new Border { Width = 80, Height = 20 }),
            NavigationIcon = new Border { Width = 48, Height = 48 }, ScrollBehavior = TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(state) };
        using var clock = new MotionTestClockScope();
        var window = Show(bar);
        try
        {
            var titles = bar.GetVisualDescendants().OfType<AppBarTitle>().ToArray(); Assert.Equal(2, titles.Length);
            clock.SendFrame(TimeSpan.Zero);
            var controls = titles.Select(t => t.GetVisualDescendants().OfType<Border>().Single()).ToArray(); Assert.NotSame(controls[0], controls[1]);
            Assert.Empty(ControlAutomationPeer.CreatePeerForElement(titles[0]).GetChildren()!);
            Assert.NotEmpty(ControlAutomationPeer.CreatePeerForElement(titles[1]).GetChildren()!);
            state.HeightOffset = state.HeightOffsetLimit * .5f; window.UpdateLayout();
            Assert.False(titles[0].HiddenSemantics, $"fraction={state.CollapsedFraction}, offset={state.HeightOffset}, limit={state.HeightOffsetLimit}"); Assert.NotEmpty(ControlAutomationPeer.CreatePeerForElement(titles[0]).GetChildren()!);
            Assert.Empty(ControlAutomationPeer.CreatePeerForElement(titles[1]).GetChildren()!);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BottomCustomContent_ReplacesItemsAndFab_AndRestoresThem()
    {
        var action = new Border { Width = 48, Height = 48 };
        var fab = new Border { Width = 56, Height = 56 };
        var custom = new Border();
        var bar = new BottomAppBar { FloatingActionButton = fab };
        bar.Items.Add(action);
        using var clock = new MotionTestClockScope();
        var window = Show(bar);
        try
        {
            Assert.Equal(80, bar.Bounds.Height); Assert.True(action.IsEffectivelyVisible);
            bar.CustomContent = custom; window.UpdateLayout();
            Assert.False(action.IsEffectivelyVisible); Assert.Null(fab.GetVisualParent());
            Assert.Equal(new Size(292, 76), custom.Bounds.Size);
            bar.CustomContent = null; window.UpdateLayout(); Assert.True(action.IsEffectivelyVisible); Assert.NotNull(fab.GetVisualParent());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void LimitWrite_DoesNotRefreshTitleLayers_UntilObservedOffsetChanges()
    {
        var state = new TopAppBarState();
        var bar = new MediumTopAppBar { Title = "title", ScrollBehavior = TopAppBarDefaults.ExitUntilCollapsedScrollBehavior(state) };
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        try
        {
            state.HeightOffset = -24; window.UpdateLayout();
            var collapsed = bar.GetVisualDescendants().OfType<AppBarTitle>().First(); var alpha = collapsed.Opacity;
            bar.ExpandedHeight = 160; window.UpdateLayout();
            Assert.Equal(-96, state.HeightOffsetLimit); Assert.Equal(alpha, collapsed.Opacity);
            clock.SendFrame(TimeSpan.Zero); clock.AdvanceBy(TimeSpan.FromMilliseconds(16)); window.UpdateLayout();
            Assert.Equal(alpha, collapsed.Opacity); // upstream limit is not snapshot state
            state.ContentOffset = -1; window.UpdateLayout(); Assert.True(collapsed.Opacity < alpha);
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void BottomReplacement_RemovesInactiveFocusAndAutomation_AndRestoresSlots()
    {
        var action = new Border { Width = 48, Height = 48, Focusable = true };
        var fab = new Border { Width = 56, Height = 56, Focusable = true };
        var custom = new Border { Focusable = true };
        var bar = new BottomAppBar { FloatingActionButton = fab }; bar.Items.Add(action);
        using var clock = new MotionTestClockScope(); var window = Show(bar);
        IEnumerable<Control> AutomationControls(AutomationPeer peer)
        {
            if (peer is ControlAutomationPeer control) yield return control.Owner;
            foreach (var child in peer.GetChildren() ?? []) foreach (var descendant in AutomationControls(child)) yield return descendant;
        }
        try
        {
            Assert.True(action.Focus()); bar.CustomContent = custom; window.UpdateLayout();
            Assert.False(action.Focus()); Assert.False(fab.Focus()); Assert.True(custom.Focus());
            var controls = AutomationControls(ControlAutomationPeer.CreatePeerForElement(bar)).ToArray();
            Assert.DoesNotContain(action, controls); Assert.DoesNotContain(fab, controls); Assert.Contains(custom, controls);
            bar.CustomContent = null; window.UpdateLayout(); Assert.True(action.Focus()); Assert.True(fab.Focus());
        }
        finally { window.Close(); }
    }

    [AvaloniaFact]
    public void StateAndScrollConsumption_FollowFixedBaseline()
    {
        var state = new TopAppBarState(-48, -24);
        var connection = TopAppBarDefaults.EnterAlwaysScrollBehavior(state, snapAnimationSpec: null, flingAnimationSpec: null).NestedScrollConnection;
        Assert.Equal(new Vector(0, -50), connection.OnPreScroll(new Vector(3, -50), AppBarScrollSource.UserInput)); Assert.Equal(-48, state.HeightOffset);
        Assert.Equal(default, connection.OnPreScroll(new Vector(0, -50), AppBarScrollSource.UserInput));
        connection.OnPostScroll(new Vector(0, -15), default, AppBarScrollSource.UserInput); Assert.Equal(-15, state.ContentOffset);
        state.HeightOffsetLimit = -8; Assert.Equal(-48, state.HeightOffset); // changing the limit does not clamp the offset
        state.HeightOffset = -48; Assert.Equal(-8, state.HeightOffset);
    }

    private static Window Show(Control control)
    {
        var window = new MotionTestWindow { Width = 300, Height = 240 };
        window.Styles.Add((Avalonia.Styling.Styles)Avalonia.Markup.Xaml.AvaloniaXamlLoader.Load(new Uri("avares://Material3Expressive/MaterialTheme.axaml"))); window.Content = new Grid { Children = { control } }; window.Show(); window.UpdateLayout(); return window;
    }
}

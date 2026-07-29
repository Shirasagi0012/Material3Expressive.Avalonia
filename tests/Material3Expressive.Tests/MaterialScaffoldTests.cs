// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Scaffold.kt:87-380
// and androidDeviceTest/.../ScaffoldTest.kt. The pinned alpha22 executable reference has matching
// slots, padding, FAB/snackbar geometry, and resulting draw order, but alpha25 alone makes the
// 0/1/2/3/4 z-indices explicit. These deterministic tests also cover the Avalonia adaptation in
// which null ContentWindowInsets observes TopLevel safe area and explicit Thickness overrides it.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Tests.TestUtils;
using MaterialColorUtilities.Avalonia.Tokens;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialScaffoldTests
{
    [AvaloniaFact]
    public void BodyFillsScaffoldAndBarsReplaceOnlyVerticalInsets()
    {
        var body = Box("body");
        var top = Box("top", height: 48);
        var bottom = Box("bottom", height: 80);
        var scaffold = Scaffold(body, top: top, bottom: bottom);

        ThemeTestHelper.ShowWindow(scaffold);

        AssertRect(new Rect(0, 0, 300, 400), BoundsIn(body, scaffold));
        AssertRect(new Rect(0, 0, 300, 48), BoundsIn(top, scaffold));
        AssertRect(new Rect(0, 320, 300, 80), BoundsIn(bottom, scaffold));
        Assert.Equal(new Thickness(10, 48, 14, 80), scaffold.ContentPadding);
    }

    [AvaloniaFact]
    public void MissingBarsUseInsetsButPresentZeroHeightBarReplacesItsInset()
    {
        var scaffold = Scaffold(Box("body"));
        ThemeTestHelper.ShowWindow(scaffold);
        Assert.Equal(new Thickness(10, 12, 14, 16), scaffold.ContentPadding);

        scaffold.TopBar = Box("top", height: 0);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(10, 0, 14, 16), scaffold.ContentPadding);
    }

    [AvaloniaTheory]
    [InlineData(ScaffoldFabPosition.Start, FlowDirection.LeftToRight, 26, 248)]
    [InlineData(ScaffoldFabPosition.Start, FlowDirection.RightToLeft, 30, 248)]
    [InlineData(ScaffoldFabPosition.Center, FlowDirection.LeftToRight, 150, 248)]
    [InlineData(ScaffoldFabPosition.End, FlowDirection.LeftToRight, 274, 248)]
    [InlineData(ScaffoldFabPosition.End, FlowDirection.RightToLeft, 278, 248)]
    [InlineData(ScaffoldFabPosition.EndOverlay, FlowDirection.LeftToRight, 274, 312)]
    public void FabPositionUsesLogicalDirectionBottomBarAndOverlayContract(
        ScaffoldFabPosition position,
        FlowDirection flowDirection,
        double expectedX,
        double expectedY)
    {
        var fab = Box("fab", width: 56, height: 56);
        var scaffold = Scaffold(
            Box("body"),
            bottom: Box("bottom", height: 80),
            fab: fab,
            width: 360);
        scaffold.FloatingActionButtonPosition = position;
        scaffold.FlowDirection = flowDirection;

        ThemeTestHelper.ShowWindow(scaffold);

        // RTL FlowDirection mirrors this logical rectangle at the scaffold boundary. The parity
        // case asserts the resulting root-relative physical rectangle against Compose.
        AssertRect(new Rect(expectedX, expectedY, 56, 56), BoundsIn(fab, scaffold));
    }

    [AvaloniaFact]
    public void SnackbarCentersInsideHorizontalInsetsAndStacksAboveFab()
    {
        var snackbar = Box("snackbar", width: 240, height: 48);
        var fab = Box("fab", width: 56, height: 56);
        var scaffold = Scaffold(
            Box("body"),
            bottom: Box("bottom", height: 80),
            snackbar: snackbar,
            fab: fab,
            width: 360);

        ThemeTestHelper.ShowWindow(scaffold);

        AssertRect(new Rect(58, 200, 240, 48), BoundsIn(snackbar, scaffold));
        AssertRect(new Rect(274, 248, 56, 56), BoundsIn(fab, scaffold));
    }

    [AvaloniaFact]
    public void ContentPaddingIsReadOnlyAndCanDriveCallerOwnedBodyPadding()
    {
        var body = Box("body");
        body.Bind(
            Layoutable.MarginProperty,
            new Binding(nameof(MaterialScaffold.ContentPadding)));
        var scaffold = Scaffold(
            body,
            top: Box("top", height: 48),
            bottom: Box("bottom", height: 80));
        body.DataContext = scaffold;

        ThemeTestHelper.ShowWindow(scaffold);

        Assert.Equal(new Thickness(10, 48, 14, 80), body.Margin);
        scaffold.FlowDirection = FlowDirection.RightToLeft;
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(new Thickness(14, 48, 10, 80), body.Margin);
        Assert.Null(typeof(MaterialScaffold).GetProperty(nameof(MaterialScaffold.ContentPadding))!.SetMethod);
    }

    [AvaloniaFact]
    public void RtlContentPaddingUsesLogicalThicknessForMirroredBodyLayout()
    {
        var body = Box("body");
        body.Bind(
            Layoutable.MarginProperty,
            new Binding(nameof(MaterialScaffold.ContentPadding)));
        var scaffold = Scaffold(
            body,
            top: Box("top", height: 48),
            bottom: Box("bottom", height: 80));
        scaffold.FlowDirection = FlowDirection.RightToLeft;
        body.DataContext = scaffold;

        ThemeTestHelper.ShowWindow(scaffold);

        Assert.Equal(new Thickness(14, 48, 10, 80), scaffold.ContentPadding);
        AssertRect(new Rect(14, 48, 276, 272), BoundsIn(body, scaffold));
    }

    [AvaloniaFact]
    public void TemplateUsesAlpha25ZOrderWhileKeepingFocusPlacementOrder()
    {
        var scaffold = Scaffold(
            Box("body"),
            top: Box("top", height: 48),
            bottom: Box("bottom", height: 80),
            snackbar: Box("snackbar", width: 240, height: 48),
            fab: Box("fab", width: 56, height: 56));
        ThemeTestHelper.ShowWindow(scaffold);

        var panel = scaffold.GetVisualDescendants().OfType<Panel>()
            .Single(candidate => candidate.GetType().Name == "MaterialScaffoldPanel");
        var presenters = panel.Children.OfType<ContentPresenter>().ToArray();

        Assert.Equal(
            ["PART_TopBar", "PART_Content", "PART_SnackbarHost", "PART_FloatingActionButton", "PART_BottomBar"],
            presenters.Select(static presenter => presenter.Name));
        Assert.Equal([1, 0, 2, 4, 3], presenters.Select(static presenter => presenter.ZIndex));
    }

    [AvaloniaFact]
    public void UsesNeutralPaneAutomationAndDoesNotInventHostStateProviders()
    {
        var scaffold = Scaffold(Box("body"));
        ThemeTestHelper.ShowWindow(scaffold);

        var peer = ControlAutomationPeer.CreatePeerForElement(scaffold);

        Assert.Equal(AutomationControlType.Pane, peer.GetAutomationControlType());
        Assert.NotNull(peer.GetChildren());
    }

    [AvaloniaFact]
    public void DefaultContainerUsesBackgroundAndOnBackgroundRoles()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Color.Parse("#6750A4"));
        using var scope = ThemeTestHelper.AppTheme(theme);
        var scaffold = Scaffold(Box("body"));

        ThemeTestHelper.ShowWindow(scaffold);

        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.Background),
            ThemeTestHelper.ColorOf(scaffold.Background));
        Assert.Equal(
            ThemeTestHelper.Expected(theme, SysColorToken.OnBackground),
            ThemeTestHelper.ColorOf(scaffold.Foreground));
    }

    [AvaloniaFact]
    public void NullInsetsOwnTopLevelSafeAreaWhileExplicitZeroIsAValidOverride()
    {
        var scaffold = new MaterialScaffold();

        Assert.Null(scaffold.ContentWindowInsets);
        Assert.False(TopLevel.GetAutoSafeAreaPadding(scaffold));

        scaffold.ContentWindowInsets = default(Thickness);
        Assert.Equal(default(Thickness), scaffold.ContentWindowInsets);
    }

    [AvaloniaFact]
    public void InvalidInsetsAreRejected()
    {
        var scaffold = new MaterialScaffold();

        Assert.Throws<ArgumentException>(() => scaffold.ContentWindowInsets = new Thickness(-1));
        Assert.Throws<ArgumentException>(
            () => scaffold.ContentWindowInsets = new Thickness(double.NaN));
    }

    private static MaterialScaffold Scaffold(
        Control body,
        Control? top = null,
        Control? bottom = null,
        Control? snackbar = null,
        Control? fab = null,
        double width = 300) =>
        new()
        {
            Width = width,
            Height = 400,
            ContentWindowInsets = new Thickness(10, 12, 14, 16),
            Content = body,
            TopBar = top,
            BottomBar = bottom,
            SnackbarHost = snackbar,
            FloatingActionButton = fab
        };

    private static Border Box(
        string name,
        double width = double.NaN,
        double height = double.NaN) =>
        new()
        {
            Name = name,
            Width = width,
            Height = height
        };

    private static Rect BoundsIn(Control child, Visual ancestor)
    {
        var origin = child.TranslatePoint(default, ancestor)
            ?? throw new InvalidOperationException($"'{child.Name}' is not attached below '{ancestor}'.");
        return new Rect(origin, child.Bounds.Size);
    }

    private static void AssertRect(Rect expected, Rect actual)
    {
        Assert.Equal(expected.X, actual.X, precision: 6);
        Assert.Equal(expected.Y, actual.Y, precision: 6);
        Assert.Equal(expected.Width, actual.Width, precision: 6);
        Assert.Equal(expected.Height, actual.Height, precision: 6);
    }
}

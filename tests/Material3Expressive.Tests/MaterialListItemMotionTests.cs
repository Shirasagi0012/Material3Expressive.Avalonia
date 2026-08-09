// Source baseline: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:1492-1588
// (InteractiveListItem): its ListColor transition uses DefaultEffects for every resolved colour
// and animateDpAsState uses FastSpatial for dragged elevation. ListItemDefaults.kt:750-835 resolves
// disabled before dragged before selected; :1135-1151 resolves shape independently of enabled;
// ListItem.kt:1580-1587 raises every positive animated shadow elevation and clips content after
// leaving the shadow unclipped. Colour/elevation targets come from ListItemDefaults.kt and
// tokens/{ListTokens,ReorderListTokens}.kt (VERSION 29.0.0).
// Visual-reference difference: Compose Multiplatform Material3 1.12.0-alpha03 / Jetpack Material3
// 1.5.0-alpha22 supports these interactive selected/disabled/dragged mechanics, which matching
// static parity scenes exercise. Only alpha25's non-interactive overload and text-line-aware
// height policy remain parity unavailable; these deterministic tests additionally cover motion,
// z-order, and template clipping contracts.

using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

public sealed class MaterialListItemMotionTests
{
    [AvaloniaFact]
    public void SelectionColors_UseDefaultEffectsAndSettleOnResolvedTokens()
    {
        using var clock = new MotionTestClockScope();
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var item = CreateInteractiveItem(ListItemInteractionMode.MultiSelection, theme);
        var colors = ListItemDefaults.Colors(theme.Resolved, isDark: false);
        var tokens = ListItemDefaults.Metrics();

        // Theme attachment can itself retarget the initially-default brushes. Establish a known
        // rest point before testing the user-visible selection transition.
        clock.SettleAll();

        var animation = ContainerColorAnimation(item);
        var start = ThemeTestHelper.ColorOf(animation.Brush);
        var target = ThemeTestHelper.ColorOf(colors.SelectedContainerColor);

        Assert.Equal(ThemeTestHelper.ColorOf(colors.ContainerColor), start);
        Assert.NotEqual(start, target);
        Assert.Equal(MotionRole.DefaultEffects, tokens.ColorMotion);

        item.IsSelected = true;

        Assert.Equal(
            theme.Resolved.Motion.Get(tokens.ColorMotion),
            ColorAlpha(animation).Spec);
        Assert.True(animation.IsRunning);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(32));

        var middle = ThemeTestHelper.ColorOf(animation.Brush);
        Assert.NotEqual(start, middle);
        Assert.NotEqual(target, middle);

        clock.SettleAll();

        Assert.False(animation.IsRunning);
        Assert.Equal(target, ThemeTestHelper.ColorOf(animation.Brush));
    }

    [AvaloniaFact]
    public void DraggedElevation_UsesFastSpatialRetargetingAndSettlesOnResolvedTokens()
    {
        using var clock = new MotionTestClockScope();
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var item = CreateInteractiveItem(ListItemInteractionMode.Click, theme);

        clock.SettleAll();

        var elevation = ElevationAnimation(item);
        var tokens = ListItemDefaults.Metrics();
        var resting = tokens.ContainerElevation;
        var dragged = tokens.DraggedContainerElevation;

        Assert.Equal(MotionRole.FastSpatial, tokens.ElevationMotion);
        Assert.Equal(resting, elevation.Value);
        Assert.Equal(resting, elevation.Target);

        item.IsDragged = true;

        Assert.Same(elevation, ElevationAnimation(item));
        Assert.Equal(dragged, elevation.Target);
        Assert.Equal(theme.Resolved.Motion.Get(tokens.ElevationMotion), elevation.Spec);
        Assert.True(elevation.IsRunning);

        clock.AdvanceBy(TimeSpan.FromMilliseconds(32));

        Assert.NotEqual(resting, elevation.Value);
        Assert.NotEqual(dragged, elevation.Value);
        Assert.True(elevation.Velocity > 0, "the elevation should be rising before it is retargeted");

        item.IsDragged = false;

        Assert.Same(elevation, ElevationAnimation(item));
        Assert.Equal(resting, elevation.Target);

        // Retargeting preserves velocity, just as Compose's Animatable-backed spring does. A
        // one-millisecond tick must not reset the motion to rest before it turns around.
        clock.AdvanceBy(TimeSpan.FromMilliseconds(1));
        Assert.True(elevation.Velocity > 0, "retargeting should retain the in-flight velocity initially");

        clock.SettleAll();

        Assert.False(elevation.IsRunning);
        Assert.Equal(resting, elevation.Value);
        Assert.Equal(resting, elevation.Target);
    }

    [AvaloniaFact]
    public void DisabledSelectedItem_UsesDisabledBrushesWhileRetainingTheSelectedShape()
    {
        using var clock = new MotionTestClockScope();
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var item = CreateInteractiveItem(ListItemInteractionMode.MultiSelection, theme);
        var colors = ListItemDefaults.Colors(theme.Resolved, isDark: false);
        var tokens = ListItemDefaults.Metrics();

        clock.SettleAll();
        item.IsSelected = true;
        clock.SettleAll();
        item.IsEnabled = false;
        clock.SettleAll();

        // Disabled takes precedence over selected in ListItemDefaults' state resolution.
        Assert.NotEqual(
            ThemeTestHelper.ColorOf(colors.SelectedContainerColor),
            ThemeTestHelper.ColorOf(colors.DisabledContainerColor));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledContainerColor), ThemeTestHelper.ColorOf(ContainerColorAnimation(item).Brush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledContentColor), ThemeTestHelper.ColorOf(item.Foreground));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledLeadingContentColor), ThemeTestHelper.ColorOf(item.LeadingBrush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledTrailingContentColor), ThemeTestHelper.ColorOf(item.TrailingBrush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledOverlineContentColor), ThemeTestHelper.ColorOf(item.OverlineBrush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledSupportingContentColor), ThemeTestHelper.ColorOf(item.SupportingBrush));

        Assert.Equal(
            theme.Resolved.Shapes.GetCornerRadius(
                tokens.SelectedShape,
                item.Bounds.Size),
            ContentClip(item).CornerRadius);
    }

    [AvaloniaFact]
    public void DisabledDraggedItem_RetainsDraggedShapeAndElevationUntilTheAnimatedShadowSettles()
    {
        using var clock = new MotionTestClockScope();
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var item = CreateInteractiveItem(ListItemInteractionMode.MultiSelection, theme);
        var tokens = ListItemDefaults.Metrics();
        var colors = ListItemDefaults.Colors(theme.Resolved, isDark: false);

        clock.SettleAll();
        item.IsSelected = true;
        item.IsEnabled = false;
        item.IsDragged = true;
        clock.SettleAll();

        var elevation = ElevationAnimation(item);
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledContainerColor), ThemeTestHelper.ColorOf(ContainerColorAnimation(item).Brush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledContentColor), ThemeTestHelper.ColorOf(item.Foreground));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledLeadingContentColor), ThemeTestHelper.ColorOf(item.LeadingBrush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledTrailingContentColor), ThemeTestHelper.ColorOf(item.TrailingBrush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledOverlineContentColor), ThemeTestHelper.ColorOf(item.OverlineBrush));
        Assert.Equal(ThemeTestHelper.ColorOf(colors.DisabledSupportingContentColor), ThemeTestHelper.ColorOf(item.SupportingBrush));
        Assert.Equal(tokens.DraggedContainerElevation, elevation.Target);
        Assert.Equal(tokens.DraggedContainerElevation, elevation.Value);
        Assert.Equal(
            theme.Resolved.Shapes.GetCornerRadius(tokens.DraggedShape, item.Bounds.Size),
            ContentClip(item).CornerRadius);
        Assert.Equal(InteractionState.Dragged, InteractionController(item).Interaction & InteractionState.Dragged);
        Assert.Equal(StateLayer.Opacity(InteractionState.Dragged), InteractionController(item).StateLayerOpacity);
        Assert.Equal(1, item.ZIndex);

        item.IsDragged = false;

        // zIndex follows the animated elevation rather than the raw drag boolean, matching the
        // Compose zIndexLambda. The row stays above an overlapping sibling during the return
        // spring and relinquishes that lease only after the shadow reaches zero.
        Assert.Equal(1, item.ZIndex);
        clock.SettleAll();
        Assert.Equal(tokens.ContainerElevation, elevation.Value);
        Assert.Equal(0, item.ZIndex);
    }

    [AvaloniaFact]
    public void DraggedElevation_DoesNotOverrideAnApplicationLocalZIndex()
    {
        using var clock = new MotionTestClockScope();
        var item = CreateInteractiveItem(ListItemInteractionMode.Click, ThemeTestHelper.ThemeSeededWith(Colors.Teal));
        item.ZIndex = 7;

        clock.SettleAll();
        item.IsDragged = true;
        clock.SettleAll();

        // The control's z-index is a template-priority implementation detail. A caller who
        // deliberately owns Visual.ZIndex keeps that local/bound ordering while still receiving
        // the same elevated shadow and shape treatment.
        Assert.Equal(7, item.ZIndex);

        item.IsDragged = false;
        clock.SettleAll();
        Assert.Equal(7, item.ZIndex);
    }

    [AvaloniaFact]
    public void Template_ClipsSlotsToTheSameAnimatedShapeWithoutClippingTheOuterShadow()
    {
        using var clock = new MotionTestClockScope();
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Teal);
        var item = CreateInteractiveItem(ListItemInteractionMode.MultiSelection, theme);
        var contentClip = Assert.Single(
            item.GetVisualDescendants().OfType<Border>(),
            border => border.Name == "PART_ContentClip");

        Assert.False(item.ClipToBounds);
        Assert.True(contentClip.ClipToBounds);
        var tokens = ListItemDefaults.Metrics();
        Assert.Equal(
            theme.Resolved.Shapes.GetCornerRadius(
                tokens.RestingShape,
                item.Bounds.Size),
            contentClip.CornerRadius);

        item.IsSelected = true;
        clock.SettleAll();
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(
            theme.Resolved.Shapes.GetCornerRadius(
                tokens.SelectedShape,
                item.Bounds.Size),
            contentClip.CornerRadius);
    }

    private static MaterialListItem CreateInteractiveItem(
        ListItemInteractionMode mode,
        MaterialTheme theme)
    {
        var item = new MaterialListItem
        {
            InteractionMode = mode,
            Content = "Headline",
            Width = 240
        };
        var host = new Border { Child = item };
        MaterialThemeScope.SetTheme(host, theme);
        ThemeTestHelper.ShowWindow(host);
        return item;
    }

    private static AnimatedBrush ContainerColorAnimation(MaterialListItem item) =>
        Assert.IsType<AnimatedBrush>(PrivateField(item, "_containerBrushAnimation").GetValue(item));

    private static SpringAnimation ColorAlpha(AnimatedBrush animation) =>
        Assert.IsType<SpringAnimation>(PrivateField(animation, "_alpha").GetValue(animation));

    private static SpringAnimation ElevationAnimation(MaterialListItem item) =>
        Assert.IsType<SpringAnimation>(PrivateField(item, "_elevation").GetValue(item));

    private static MaterialInteractionController InteractionController(MaterialListItem item) =>
        MaterialElementContext.GetOrCreate(item).Interaction;

    private static Border ContentClip(MaterialListItem item) =>
        Assert.Single(
            item.GetVisualDescendants().OfType<Border>(),
            border => border.Name == "PART_ContentClip");

    private static FieldInfo PrivateField(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new InvalidOperationException($"Missing private test probe field '{name}'.");
}

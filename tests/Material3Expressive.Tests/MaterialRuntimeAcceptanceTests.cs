using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Material3Expressive.Controls;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tests.TestUtils;
using Xunit;

namespace Material3Expressive.Tests;

/// <summary>
/// Focused acceptance coverage for the owner-local runtime. Component tests prove the rendered
/// result; these tests prove the ownership and lifetime invariants that make the result reliable
/// when a template is rebuilt or an element changes scope.
/// </summary>
public sealed class MaterialElementContextAcceptanceTests
{
    [Fact]
    public void Context_IsStableAndDoesNotInheritToChildren()
    {
        var owner = new Border();
        var child = new Border();
        owner.Child = child;

        var first = MaterialElementContext.Ensure(owner);

        Assert.Same(first, MaterialElementContext.Ensure(owner));
        Assert.True(MaterialElementContext.TryGet(owner, out var stored));
        Assert.Same(first, stored);
        Assert.False(MaterialElementContext.TryGet(child, out _));
        Assert.Null(first.MotionIfCreated);
        Assert.Null(first.InteractionIfCreated);
    }

    [AvaloniaFact]
    public void FirstDefaultResolution_IsPublishedExactlyOnce()
    {
        var probe = new ThemeProbe();
        Assert.Equal(1, probe.ChangeCount);
        ThemeTestHelper.ShowWindow(probe);

        Assert.Equal(1, probe.ChangeCount);
        Assert.Same(ResolvedTheme.Default, probe.ObservedTheme);
        Assert.False(probe.ObservedIsDark);
    }

    [AvaloniaFact]
    public void ContextCreatedAfterAttachment_PublishesAfterAttachedPropertyPublication()
    {
        var owner = new Border();
        var window = ThemeTestHelper.ShowWindow(owner);

        var context = MaterialElementContext.Ensure(owner);

        Assert.Same(ResolvedTheme.Default, context.Theme.Current.Theme);
        Assert.Same(context, MaterialElementContext.Ensure(owner));
        window.Close();
    }

    [Fact]
    public void ReentrantThemeChanges_AreQueuedAndCommitInOrder()
    {
        var first = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var second = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var probe = new ReentrantThemeProbe(second);

        MaterialThemeScope.SetTheme(probe, first);
        probe.Activate();

        Assert.Equal(2, probe.Themes.Count);
        Assert.Same(first.Resolved, probe.Themes[0]);
        Assert.Same(second.Resolved, probe.Themes[1]);
    }

    [AvaloniaFact]
    public void DetachPublishesTheEffectiveDefaultAndReattachReusesTheContext()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var probe = new ThemeProbe();
        var host = new Border { Child = probe };
        var window = ThemeTestHelper.ShowWindow(host);
        var context = MaterialElementContext.Ensure(probe);

        MaterialThemeScope.SetTheme(host, theme);
        Assert.Same(theme.Resolved, probe.ObservedTheme);
        var afterTheme = probe.ChangeCount;

        host.Child = null;
        Assert.Same(ResolvedTheme.Default, probe.ObservedTheme);
        Assert.Equal(afterTheme + 1, probe.ChangeCount);

        host.Child = probe;
        Assert.Same(context, MaterialElementContext.Ensure(probe));
        Assert.Same(theme.Resolved, probe.ObservedTheme);

        var beforeReplacement = probe.ChangeCount;
        var replacement = ThemeTestHelper.ThemeSeededWith(Colors.Green);
        MaterialThemeScope.SetTheme(host, replacement);
        Assert.Same(replacement.Resolved, probe.ObservedTheme);
        Assert.Equal(beforeReplacement + 1, probe.ChangeCount);
        window.Close();
    }

    [Fact]
    public void RepeatedThemeValuesAndVariantValues_AreDeduplicatedAfterTheFirstPublication()
    {
        var theme = ThemeTestHelper.ThemeSeededWith(Colors.Green);
        var probe = new ThemeProbe();
        var initialChanges = probe.ChangeCount;
        MaterialThemeScope.SetTheme(probe, theme);

        Assert.Equal(initialChanges + 1, probe.ChangeCount);
        MaterialThemeScope.SetTheme(probe, theme);
        Assert.Equal(initialChanges + 1, probe.ChangeCount);

        var beforeVariant = probe.ChangeCount;
        probe.SetValue(ThemeVariantScope.RequestedThemeVariantProperty, ThemeVariant.Dark);
        var afterVariant = probe.ChangeCount;
        probe.SetValue(ThemeVariantScope.RequestedThemeVariantProperty, ThemeVariant.Dark);

        // RequestedThemeVariant is inherited through the normal Avalonia theme pipeline; a direct
        // detached owner may not expose ActualThemeVariant until it joins a TopLevel. The key
        // invariant here is that a repeated effective value cannot dispatch twice.
        Assert.Equal(beforeVariant + 1, afterVariant);
        Assert.Equal(afterVariant, probe.ChangeCount);
    }

    [Fact]
    public void OneControllerIsSharedByComponentAndInteraction()
    {
        var context = MaterialElementContext.Ensure(new Border());
        var component = context.Motion;
        var interaction = context.Interaction.Motion;

        Assert.Same(component, interaction);
        Assert.Same(context.MotionIfCreated, component);
        Assert.Same(context.InteractionIfCreated, context.Interaction);
    }

    [Fact]
    public void SettleAll_SettlesEveryRegisteredTrack()
    {
        var context = MaterialElementContext.Ensure(new Border());
        var controller = context.Motion;
        var firstAnimation = controller.AddTween(0, TimeSpan.FromSeconds(1), Easings.Linear);
        var secondAnimation = controller.AddTween(0, TimeSpan.FromSeconds(1), Easings.Linear);

        controller.UseManualClock();
        firstAnimation.AnimateTo(1);
        secondAnimation.AnimateTo(1);
        controller.AdvanceBy(TimeSpan.FromMilliseconds(100));
        controller.SettleAll();

        Assert.Equal(1, firstAnimation.Value);
        Assert.Equal(1, secondAnimation.Value);
        Assert.False(secondAnimation.IsRunning);
    }

    [Fact]
    public void FrameCallback_CanStartAnotherAnimationWithoutLosingTheNextFrame()
    {
        var context = MaterialElementContext.Ensure(new Border());
        var controller = context.Motion;
        var first = controller.AddTween(0, TimeSpan.FromMilliseconds(10), Easings.Linear);
        var secondStarted = false;
        TweenAnimation? second = null;
        controller.AfterOwnerFrame += () =>
        {
            if (secondStarted)
                return;

            secondStarted = true;
            second = controller.AddTween(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
            second.AnimateTo(1);
        };

        controller.UseManualClock();
        first.AnimateTo(1);
        controller.AdvanceBy(TimeSpan.FromMilliseconds(10));

        Assert.True(secondStarted);
        Assert.NotNull(second);
        Assert.True(second!.IsRunning);
        Assert.True(controller.IsRunning);
    }

    [Fact]
    public void LifecycleSettle_FinishesAnimationsStartedByAFrameCallback()
    {
        var context = MaterialElementContext.Ensure(new Border());
        var controller = context.Motion;
        var first = controller.AddTween(0, TimeSpan.FromMilliseconds(10), Easings.Linear);
        TweenAnimation? second = null;

        controller.AfterOwnerFrame += () =>
        {
            if (second is not null)
                return;

            second = controller.AddTween(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
            second.AnimateTo(1);
        };

        first.AnimateTo(1);
        controller.SettleAllForLifecycle();

        Assert.NotNull(second);
        Assert.Equal(1, first.Value);
        Assert.Equal(1, second!.Value);
        Assert.False(controller.IsRunning);
    }
}

public sealed class MaterialInteractionRuntimeAcceptanceTests
{
    [Fact]
    public void LocalVariantChanges_ReapplyLoadingIndicatorTokensWithoutAThemeDispatch()
    {
        var indicator = new MaterialLoadingIndicator();

        Assert.False(indicator.HasResolvedContainer);
        indicator.Variant = LoadingIndicatorVariant.Contained;

        Assert.True(indicator.HasResolvedContainer);
    }

    [Fact]
    public void InputDisable_ClearsPhysicalStateButPreservesExternalDraggedState()
    {
        var owner = new Border { Width = 80, Height = 40 };
        var interaction = MaterialElementContext.Ensure(owner).Interaction;

        interaction.Set(InteractionState.Dragged, true);
        interaction.Set(InteractionState.Hovered, true);
        interaction.Set(InteractionState.Focused, true);
        interaction.StartPress(new Point(20, 10));
        interaction.SetInputEnabled(false);

        var state = interaction.Interaction;
        Assert.True(state.HasFlag(InteractionState.Dragged));
        Assert.False(state.HasFlag(InteractionState.Hovered));
        Assert.False(state.HasFlag(InteractionState.Focused));
        Assert.False(state.HasFlag(InteractionState.Pressed));
        Assert.Equal(0, interaction.DebugSnapshot.RippleCount);
    }

    [Fact]
    public void InputDisable_ClearsReleasedRipplesThatOutlivePressed()
    {
        var owner = new Border { Width = 80, Height = 40 };
        var interaction = MaterialElementContext.Ensure(owner).Interaction;
        using var presentation = interaction.AttachPresentation(owner);
        presentation.UpdateGeometry(
            new MaterialInteractionGeometry(new Rect(0, 0, 80, 40), null, null));

        interaction.StartPress(new Point(20, 10));
        interaction.EndPress();

        Assert.Equal(1, interaction.DebugSnapshot.RippleCount);

        interaction.SetInputEnabled(false);

        Assert.Equal(0, interaction.DebugSnapshot.RippleCount);
        Assert.False(interaction.Interaction.HasFlag(InteractionState.Pressed));
    }

    [Fact]
    public void PresentationGeometry_IsLiveAndClearsWhenItsLeaseIsDisposed()
    {
        var owner = new Border { Width = 100, Height = 40 };
        var context = MaterialElementContext.Ensure(owner);
        var interaction = context.Interaction;
        var lease = interaction.AttachPresentation(new Border());
        var geometry = new MaterialInteractionGeometry(
            new Rect(12, 4, 48, 32),
            new Rect(12, 4, 48, 32),
            new CornerRadius(8));

        lease.UpdateGeometry(geometry);
        Assert.Equal(geometry, interaction.Geometry);

        lease.Dispose();
        Assert.Equal(interaction.DefaultGeometry, interaction.Geometry);

        // A stale presenter cannot write geometry back after template replacement.
        lease.UpdateGeometry(new MaterialInteractionGeometry(new Rect(1, 1, 2, 2), null, null));
        Assert.Equal(interaction.DefaultGeometry, interaction.Geometry);
    }

    [Fact]
    public void ObserverLeases_DisposeIndependently()
    {
        var interaction = MaterialElementContext.Ensure(new Border()).Interaction;
        var observer = new RecordingInteractionObserver();
        using var first = interaction.Subscribe(observer);
        using var second = interaction.Subscribe(observer);

        first.Dispose();
        interaction.Set(InteractionState.Hovered, true);

        Assert.Equal(1, observer.StateChanges);
        second.Dispose();
        interaction.Set(InteractionState.Hovered, false);
        Assert.Equal(1, observer.StateChanges);
    }

    [Fact]
    public void PolicyChangesRefreshActiveStateWithoutChangingTheUnderlyingState()
    {
        var interaction = MaterialElementContext.Ensure(new Border()).Interaction;
        var coordinator = interaction.Motion;
        coordinator.UseManualClock();
        interaction.Set(InteractionState.Hovered, true);
        coordinator.AdvanceBy(TimeSpan.FromMilliseconds(20));
        Assert.True(interaction.StateLayerOpacity > 0);

        interaction.UpdateIndicationPolicy(new MaterialInteractionPolicy(IsHoverIndicationEnabled: false));
        coordinator.AdvanceBy(TimeSpan.FromMilliseconds(100));

        Assert.True(interaction.Interaction.HasFlag(InteractionState.Hovered));
        Assert.Equal(0, interaction.StateLayerOpacity);

        interaction.UpdateIndicationPolicy(new MaterialInteractionPolicy(IsHoverIndicationEnabled: true));
        coordinator.AdvanceBy(TimeSpan.FromMilliseconds(100));
        Assert.Equal(StateTokens.HoverStateLayerOpacity, interaction.StateLayerOpacity, 6);
    }

    private sealed class RecordingInteractionObserver : IMaterialInteractionObserver
    {
        public int StateChanges { get; private set; }

        public void OnMaterialInteractionChanged(InteractionState state) => StateChanges++;

        public void OnMaterialInteractionFrame()
        {
        }
    }
}

internal sealed class ReentrantThemeProbe : TemplatedControl
{
    private readonly MaterialTheme _replacement;
    private readonly MaterialThemeController _theme;
    private bool _reentered;

    public ReentrantThemeProbe(MaterialTheme replacement)
    {
        _replacement = replacement;
        _theme = MaterialElementContext.Ensure(this).Theme;
    }

    public List<ResolvedTheme> Themes { get; } = [];

    internal void Activate() => _theme.Subscribe(OnThemeChanged);

    private void OnThemeChanged(MaterialThemeState state)
    {
        Themes.Add(state.Theme);
        if (_reentered)
            return;

        _reentered = true;
        MaterialThemeScope.SetTheme(this, _replacement);
    }
}

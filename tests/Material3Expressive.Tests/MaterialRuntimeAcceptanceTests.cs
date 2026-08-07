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

        var first = MaterialElementContext.GetOrCreate(owner);

        Assert.Same(first, MaterialElementContext.GetOrCreate(owner));
        Assert.True(MaterialElementContext.TryGet(owner, out var stored));
        Assert.Same(first, stored);
        Assert.False(MaterialElementContext.TryGet(child, out _));
        Assert.Null(first.MotionCoordinatorIfCreated);
        Assert.Null(first.InteractionIfCreated);
    }

    [AvaloniaFact]
    public void FirstDefaultResolution_IsPublishedExactlyOnce()
    {
        var probe = new ThemeProbe();
        Assert.Equal(0, probe.ChangeCount);
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

        var context = MaterialElementContext.GetOrCreate(owner);

        Assert.True(context.HasPublishedTheme);
        Assert.Same(context, MaterialElementContext.GetOrCreate(owner));
        window.Close();
    }

    [Fact]
    public void ReentrantThemeChanges_AreQueuedAndCommitInOrder()
    {
        var first = ThemeTestHelper.ThemeSeededWith(Colors.Red);
        var second = ThemeTestHelper.ThemeSeededWith(Colors.Blue);
        var probe = new ReentrantThemeProbe(second);

        MaterialThemeScope.SetTheme(probe, first);

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
        var context = MaterialElementContext.GetOrCreate(probe);

        MaterialThemeScope.SetTheme(host, theme);
        Assert.Same(theme.Resolved, probe.ObservedTheme);
        var afterTheme = probe.ChangeCount;

        host.Child = null;
        Assert.Same(ResolvedTheme.Default, probe.ObservedTheme);
        Assert.Equal(afterTheme + 1, probe.ChangeCount);

        host.Child = probe;
        Assert.Same(context, MaterialElementContext.GetOrCreate(probe));
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
        MaterialThemeScope.SetTheme(probe, theme);

        Assert.Equal(1, probe.ChangeCount);
        MaterialThemeScope.SetTheme(probe, theme);
        Assert.Equal(1, probe.ChangeCount);

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
    public void OneCoordinatorOwnsIndependentComponentAndInteractionScopes()
    {
        var context = MaterialElementContext.GetOrCreate(new Border());
        var component = context.Motion;
        var interaction = context.Interaction.Motion;

        Assert.Same(component.Coordinator, interaction.Coordinator);
        Assert.Equal(2, component.Coordinator.ScopeCount);
        Assert.Same(context.MotionCoordinatorIfCreated, component.Coordinator);
        Assert.Same(context.InteractionIfCreated, context.Interaction);
    }

    [Fact]
    public void ScopeSettle_DoesNotSettleASiblingScope()
    {
        var context = MaterialElementContext.GetOrCreate(new Border());
        var coordinator = context.EnsureMotionCoordinator();
        var first = coordinator.CreateScope("first");
        var second = coordinator.CreateScope("second");
        var firstAnimation = first.AddTween(0, TimeSpan.FromSeconds(1), Easings.Linear);
        var secondAnimation = second.AddTween(0, TimeSpan.FromSeconds(1), Easings.Linear);

        coordinator.UseManualClock();
        firstAnimation.AnimateTo(1);
        secondAnimation.AnimateTo(1);
        coordinator.AdvanceBy(TimeSpan.FromMilliseconds(100));
        first.Settle();

        Assert.Equal(1, firstAnimation.Value);
        Assert.InRange(secondAnimation.Value, 0.099, 0.101);
        Assert.True(secondAnimation.IsRunning);
    }

    [Fact]
    public void FrameCallback_CanStartAnotherAnimationWithoutLosingTheNextFrame()
    {
        var context = MaterialElementContext.GetOrCreate(new Border());
        var coordinator = context.EnsureMotionCoordinator();
        var scope = coordinator.CreateScope("callback");
        var first = scope.AddTween(0, TimeSpan.FromMilliseconds(10), Easings.Linear);
        var secondStarted = false;
        TweenAnimation? second = null;
        scope.Ticked += (_, _) =>
        {
            if (secondStarted)
                return;

            secondStarted = true;
            second = scope.AddTween(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
            second.AnimateTo(1);
        };

        coordinator.UseManualClock();
        first.AnimateTo(1);
        coordinator.AdvanceBy(TimeSpan.FromMilliseconds(10));

        Assert.True(secondStarted);
        Assert.NotNull(second);
        Assert.True(second!.IsRunning);
        Assert.True(coordinator.IsRunning);
    }

    [Fact]
    public void LifecycleSettle_FinishesAnimationsStartedByASettledScopeCallback()
    {
        var context = MaterialElementContext.GetOrCreate(new Border());
        var coordinator = context.EnsureMotionCoordinator();
        var scope = coordinator.CreateScope("lifecycle-reentrant");
        var first = scope.AddTween(0, TimeSpan.FromMilliseconds(10), Easings.Linear);
        TweenAnimation? second = null;

        scope.Ticked += (_, _) =>
        {
            if (second is not null)
                return;

            second = scope.AddTween(0, TimeSpan.FromMilliseconds(100), Easings.Linear);
            second.AnimateTo(1);
        };

        first.AnimateTo(1);
        coordinator.SettleAllForLifecycle();

        Assert.NotNull(second);
        Assert.Equal(1, first.Value);
        Assert.Equal(1, second!.Value);
        Assert.False(coordinator.IsRunning);
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
        var interaction = MaterialElementContext.GetOrCreate(owner).Interaction;

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
        var interaction = MaterialElementContext.GetOrCreate(owner).Interaction;
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
        var context = MaterialElementContext.GetOrCreate(owner);
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
        var interaction = MaterialElementContext.GetOrCreate(new Border()).Interaction;
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
        var interaction = MaterialElementContext.GetOrCreate(new Border()).Interaction;
        var coordinator = interaction.Motion.Coordinator;
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

internal sealed class ReentrantThemeProbe : TemplatedControl, IMaterialThemeConsumer
{
    private readonly MaterialTheme _replacement;
    private bool _reentered;

    public ReentrantThemeProbe(MaterialTheme replacement)
    {
        _replacement = replacement;
        MaterialElementContext.GetOrCreate(this);
    }

    public List<ResolvedTheme> Themes { get; } = [];

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state)
    {
        Themes.Add(state.Theme);
        if (_reentered)
            return;

        _reentered = true;
        MaterialThemeScope.SetTheme(this, _replacement);
    }
}

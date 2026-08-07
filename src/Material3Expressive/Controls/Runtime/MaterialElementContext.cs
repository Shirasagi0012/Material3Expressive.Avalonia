using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Owner-local runtime state for a Material element.
/// </summary>
/// <remarks>
/// Theme values still travel through the inherited <see cref="MaterialThemeScope.ResolvedThemeProperty" />.
/// This context is deliberately private storage and lifecycle glue: it is not a public behavior and
/// it is never inherited by template children.
/// </remarks>
internal sealed class MaterialElementContext
{
    private static readonly AttachedProperty<MaterialElementContext?> ContextProperty =
        AvaloniaProperty.RegisterAttached<MaterialElementContext, Control, MaterialElementContext?>(
            "__MaterialElementContext",
            defaultValue: null,
            inherits: false);

    private readonly Control _owner;
    private MaterialThemeState _theme = MaterialThemeState.Default;
    private bool _hasPublishedTheme;
    private bool _active;
    private bool _dispatchingTheme;
    private bool _queuedTheme;
    private MaterialThemeState _queuedThemeState;
    private MaterialMotionCoordinator? _motionCoordinator;
    private MaterialMotionScope? _motionScope;
    private MaterialInteractionController? _interaction;
    private MaterialInteractionPresentationLease? _interactionPresentation;

    private MaterialElementContext(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    internal Control Owner => _owner;
    internal ResolvedTheme Theme => _theme.Theme;
    internal bool IsDark => _theme.IsDark;
    internal MaterialThemeState ThemeState => _theme;
    internal bool HasPublishedTheme => _hasPublishedTheme;

    internal MaterialMotionCoordinator? MotionCoordinatorIfCreated => _motionCoordinator;
    internal MaterialInteractionController? InteractionIfCreated => _interaction;

    /// <summary>The component-owned motion scope, created lazily on first use.</summary>
    internal MaterialMotionScope Motion =>
        _motionScope ??= EnsureMotionCoordinator().CreateScope("component");

    internal MaterialInteractionController Interaction =>
        _interaction ??= CreateInteraction();

    /// <summary>The owner-level presentation lease used by controls that draw their own layers.</summary>
    internal MaterialInteractionPresentationLease InteractionPresentation =>
        _interactionPresentation is { IsDisposed: false } existing
            ? existing
            : _interactionPresentation = Interaction.AttachPresentation(_owner);

    internal SpringSpec Spring(MotionRole role) => Theme.Motion.Get(role);

    internal MaterialMotionCoordinator EnsureMotionCoordinator()
    {
        _owner.VerifyAccess();
        return _motionCoordinator ??= new MaterialMotionCoordinator(
            _owner,
            OnMotionFrame);
    }

    internal void AttachInteraction(MaterialInteractionController controller)
    {
        if (_interaction is not null && !ReferenceEquals(_interaction, controller))
            throw new InvalidOperationException("A material owner can have only one interaction controller.");

        _interaction = controller;
    }

    internal static MaterialElementContext GetOrCreate(Control owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();

        if (owner.GetValue(ContextProperty) is { } existing)
            return existing;

        // Publication and activation are intentionally separate. Avalonia property notifications
        // can re-enter GetOrCreate, and the attached property must already contain the instance
        // before activation can observe the owner.
        var created = new MaterialElementContext(owner);
        try
        {
            owner.SetValue(ContextProperty, created);
        }
        catch
        {
            // A property callback may throw while the two-phase publication is in progress. Do
            // not leave a half-created context discoverable by the next caller; the attached
            // value is cleared only for this failed construction path, never on detach.
            if (ReferenceEquals(owner.GetValue(ContextProperty), created))
                owner.ClearValue(ContextProperty);
            throw;
        }

        // Keep a successfully published context discoverable even if a consumer throws during
        // activation. Activate marks its subscriptions before the initial dispatch, so retaining
        // that instance is what prevents a retry from installing duplicate handlers.
        created.Activate();

        return created;
    }

    internal static bool TryGet(
        Control owner,
        [NotNullWhen(true)] out MaterialElementContext? context)
    {
        ArgumentNullException.ThrowIfNull(owner);
        owner.VerifyAccess();
        context = owner.GetValue(ContextProperty);
        return context is not null;
    }

    private void Activate()
    {
        if (_active)
            return;

        _active = true;
        _owner.AttachedToLogicalTree += OnAttachedToLogicalTree;
        _owner.DetachedFromLogicalTree += OnDetachedFromLogicalTree;
        _owner.AttachedToVisualTree += OnAttachedToVisualTree;
        _owner.DetachedFromVisualTree += OnDetachedFromVisualTree;
        _owner.PropertyChanged += OnPropertyChanged;

        // A template presenter can be materialized after the owner has already attached. Its
        // context must still publish exactly one initial snapshot.
        if (((ILogical)_owner).IsAttachedToLogicalTree)
            PublishCurrentTheme();
    }

    private void OnAttachedToLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        PublishCurrentTheme();
    }

    private void OnDetachedFromLogicalTree(object? sender, LogicalTreeAttachmentEventArgs e)
    {
        // Inherited values have already fallen back by the time this event is raised. Re-read and
        // publish the effective value explicitly so detach cannot miss the Default transition when
        // a custom inheritance parent does not emit a separate property notification. Clear only
        // physical interaction state; external state such as Dragged belongs to the component and
        // is intentionally left intact.
        PublishCurrentTheme();
        _interaction?.ClearInputInteractionsOnDetach();
        _motionCoordinator?.SettleAllForLifecycle();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _motionCoordinator?.StartIfNeeded();
    }

    private void OnDetachedFromVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _motionCoordinator?.CancelScheduledCallbacksAndSettle();
    }

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != MaterialThemeScope.ResolvedThemeProperty
            && e.Property != ThemeVariantScope.ActualThemeVariantProperty)
            return;

        PublishCurrentTheme();
    }

    private void PublishCurrentTheme()
    {
        var next = new MaterialThemeState(
            MaterialThemeScope.GetResolvedTheme(_owner),
            ColorScheme.IsDark(_owner.ActualThemeVariant ?? ThemeVariant.Default));

        if (_hasPublishedTheme
            && ReferenceEquals(next.Theme, _theme.Theme)
            && next.IsDark == _theme.IsDark)
            return;

        if (_dispatchingTheme)
        {
            _queuedTheme = true;
            _queuedThemeState = next;
            return;
        }

        DispatchTheme(next);

        while (_queuedTheme)
        {
            _queuedTheme = false;
            var queued = _queuedThemeState;
            if (_hasPublishedTheme
                && ReferenceEquals(queued.Theme, _theme.Theme)
                && queued.IsDark == _theme.IsDark)
                continue;

            DispatchTheme(queued);
        }
    }

    private void DispatchTheme(MaterialThemeState next)
    {
        _theme = next;
        _hasPublishedTheme = true;
        _dispatchingTheme = true;
        try
        {
            if (_owner is IMaterialThemeConsumer ownerConsumer)
                ownerConsumer.OnMaterialThemeChanged(next);

            // The interaction controller is context-owned, so it receives theme commits through
            // an explicit capability call rather than a broad context event. This keeps owner
            // theme dispatch deterministic and prevents components from accumulating anonymous
            // Changed subscriptions.
            _interaction?.OnMaterialThemeChanged(next);
        }
        finally
        {
            _dispatchingTheme = false;
        }
    }

    private MaterialInteractionController CreateInteraction()
    {
        var controller = new MaterialInteractionController(this, EnsureMotionCoordinator());
        AttachInteraction(controller);

        // A controller requested after the context's first publication needs the current theme,
        // but a controller created reentrantly from the owner consumer is included in the current
        // dispatch below and must not receive a duplicate notification.
        if (_hasPublishedTheme && !_dispatchingTheme)
            controller.OnMaterialThemeChanged(_theme);

        return controller;
    }

    private void OnMotionFrame()
    {
        if (_owner is IMaterialMotionConsumer consumer)
            consumer.OnMaterialMotionFrame();

        // Interaction observers are secondary consumers. They must see the synchronized owner
        // state, but they must run before the single owner invalidation at the end of the frame.
        _interaction?.PublishInteractionFrame();
        _owner.InvalidateVisual();
    }

}

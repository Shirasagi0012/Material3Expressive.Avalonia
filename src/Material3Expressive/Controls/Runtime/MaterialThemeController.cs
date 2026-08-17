using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>
/// Owns the effective Material theme snapshot for one control.
/// </summary>
/// <remarks>
/// The controller deliberately publishes data only.  Token lookup and component-default
/// projection belong to their respective consumers, rather than turning this type into a
/// runtime service locator.
/// </remarks>
[Obsolete(
    "Legacy whole-theme snapshot controller. Consume typed MaterialValueKey<T> resources.",
    DiagnosticId = "M3E1004")]
internal sealed class MaterialThemeController
{
    private readonly Control _owner;
    private readonly List<Action<MaterialThemeState>> _observers = [];
    private Action<MaterialThemeState>? _ownerHandler;
    private MaterialThemeState _current = MaterialThemeState.Default;
    private MaterialThemeState _queued;
    private bool _active;
    private bool _dispatching;
    private bool _hasQueued;

    internal MaterialThemeController(Control owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    /// <summary>Gets the current effective theme. It is always usable, including before attach.</summary>
    internal MaterialThemeState Current => _current;

    /// <summary>
    /// Installs the one owner callback. It receives the current snapshot immediately so an owner
    /// materialized after its controller can initialize without an attachment transition.
    /// </summary>
    internal void SetOwnerHandler(Action<MaterialThemeState> handler, bool replayCurrent = true)
    {
        ArgumentNullException.ThrowIfNull(handler);
        if (_ownerHandler is not null)
            throw new InvalidOperationException("The material theme owner handler can be set only once.");

        _ownerHandler = handler;
        if (replayCurrent)
            handler(_current);
    }

    /// <summary>
    /// Subscribes to committed snapshots and replays the current one immediately.
    /// </summary>
    internal IDisposable Subscribe(Action<MaterialThemeState> observer)
    {
        ArgumentNullException.ThrowIfNull(observer);
        _observers.Add(observer);
        observer(_current);
        return new MaterialThemeSubscription(this, observer);
    }

    /// <summary>Starts observing the owner. Safe to call repeatedly.</summary>
    internal void Activate()
    {
        if (_active)
            return;

        _active = true;
        _owner.AttachedToLogicalTree += OnLogicalTreeChanged;
        _owner.DetachedFromLogicalTree += OnLogicalTreeChanged;
        _owner.PropertyChanged += OnPropertyChanged;

        // Compatibility bridge while controls finish their explicit-subscription migration. It
        // deliberately does not replay during controller creation: an older owner commonly asks
        // for its Theme from a field initializer and is not yet safe to receive a callback.
        if (_ownerHandler is null && _owner is IMaterialThemeConsumer consumer)
            SetOwnerHandler(consumer.OnMaterialThemeChanged, replayCurrent: false);
        Refresh();
    }

    private void OnLogicalTreeChanged(object? sender, LogicalTreeAttachmentEventArgs e) => Refresh();

    private void OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == MaterialThemeScope.ResolvedThemeProperty
            || e.Property == ThemeVariantScope.ActualThemeVariantProperty)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        var next = new MaterialThemeState(
            MaterialThemeScope.GetResolvedTheme(_owner),
            ColorScheme.IsDark(_owner.ActualThemeVariant ?? ThemeVariant.Default));

        if (Same(next, _current))
            return;

        if (_dispatching)
        {
            _queued = next;
            _hasQueued = true;
            return;
        }

        Dispatch(next);
        while (_hasQueued)
        {
            _hasQueued = false;
            var queued = _queued;
            if (!Same(queued, _current))
                Dispatch(queued);
        }
    }

    private void Dispatch(MaterialThemeState next)
    {
        // Commit before notification. A reentrant consumer therefore always reads a coherent
        // snapshot, and a later reentrant update can be coalesced rather than recursed.
        _current = next;
        _dispatching = true;
        try
        {
            _ownerHandler?.Invoke(next);
            var observers = _observers.ToArray();
            for (var index = 0; index < observers.Length; index++)
                observers[index](next);
        }
        finally
        {
            _dispatching = false;
        }
    }

    private void Remove(Action<MaterialThemeState> observer)
    {
        for (var index = _observers.Count - 1; index >= 0; index--)
        {
            if (_observers[index] == observer)
            {
                _observers.RemoveAt(index);
                return;
            }
        }
    }

    private static bool Same(MaterialThemeState left, MaterialThemeState right) =>
        ReferenceEquals(left.Theme, right.Theme) && left.IsDark == right.IsDark;

    private sealed class MaterialThemeSubscription : IDisposable
    {
        private MaterialThemeController? _controller;
        private readonly Action<MaterialThemeState> _observer;

        internal MaterialThemeSubscription(
            MaterialThemeController controller,
            Action<MaterialThemeState> observer)
        {
            _controller = controller;
            _observer = observer;
        }

        public void Dispose()
        {
            var controller = _controller;
            if (controller is null)
                return;

            _controller = null;
            controller.Remove(_observer);
        }
    }
}

// FadeInFadeOutWithScale from the fixed AndroidX SnackbarHost.kt (Apache-2.0).
using System.ComponentModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

/// <summary>Displays the current data, retains exiting items, and applies the Core desktop timeout.</summary>
public class SnackbarHost : TemplatedControl
{
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> ScaleAnimationProperty =
        AvaloniaProperty.Register<SnackbarHost, Foundation.Motion.SpringSpec?>(nameof(ScaleAnimation));
    internal Foundation.Motion.SpringSpec? ScaleAnimation { get => GetValue(ScaleAnimationProperty); set => SetValue(ScaleAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> OpacityAnimationProperty =
        AvaloniaProperty.Register<SnackbarHost, Foundation.Motion.SpringSpec?>(nameof(OpacityAnimation));
    internal Foundation.Motion.SpringSpec? OpacityAnimation { get => GetValue(OpacityAnimationProperty); set => SetValue(OpacityAnimationProperty, value); }


    public static readonly StyledProperty<SnackbarHostState> StateProperty = AvaloniaProperty.Register<SnackbarHost, SnackbarHostState>(nameof(State));
    public static readonly StyledProperty<IDataTemplate?> SnackbarTemplateProperty = AvaloniaProperty.Register<SnackbarHost, IDataTemplate?>(nameof(SnackbarTemplate));
    public SnackbarHostState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public IDataTemplate? SnackbarTemplate { get => GetValue(SnackbarTemplateProperty); set => SetValue(SnackbarTemplateProperty, value); }

    private readonly List<Item> _items = [];
    private Panel? _panel;
    private AnimationScope? _scope;
    private AnimationChildScope? _timeout;
    private SnackbarHostState? _observedState;
    private ISnackbarData? _current;
    private bool _updatePending;

    public SnackbarHost() => SetCurrentValue(StateProperty, new SnackbarHostState());
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _panel?.Children.Clear();
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<Panel>("PART_Items");
        if (_panel is not null)
        {
            // Avalonia reads RenderOptions from the parent before pushing a child's opacity.
            // Compose graphicsLayer applies alpha once to the complete Snackbar subtree.
            RenderOptions.SetRequiresFullOpacityHandling(_panel, true);
            foreach (var item in _items) _panel.Children.Add(item.Presenter);
        }
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scope = AnimationScope.Get(this);
        ObserveState(initial: true);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observedState is not null) _observedState.PropertyChanged -= StateChanged;
        _observedState = null;
        _scope = null; _timeout = null;
        foreach (var item in _items) item.Effect?.Dispose();
        _items.Clear(); _panel?.Children.Clear(); _current = null; _updatePending = false;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty && _scope is not null) ObserveState();
        if (change.Property == SnackbarTemplateProperty || change.Property == ScaleAnimationProperty || change.Property == OpacityAnimationProperty) RequestUpdate();
    }
    private void ObserveState(bool initial = false)
    {
        if (_observedState is not null) _observedState.PropertyChanged -= StateChanged;
        _observedState = State;
        _observedState.PropertyChanged += StateChanged;
        if (initial) UpdateCurrent();
        else RequestUpdate();
    }
    private void StateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (null or "" or nameof(SnackbarHostState.CurrentSnackbarData))) return;
        if (Dispatcher.UIThread.CheckAccess()) RequestUpdate();
        else Dispatcher.UIThread.Post(() => { if (ReferenceEquals(sender, _observedState)) RequestUpdate(); });
    }
    private void RequestUpdate()
    {
        if (!this.IsAttachedToVisualTree() || _scope is null || _updatePending) return;
        var motion = _scope = AnimationScope.Get(this);
        _updatePending = true;
        // Compose samples the old animations, then recomposes SnackbarHost on this frame.
        // State changes immediately; presentation/effects commit at the next frame boundary.
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            _updatePending = false;
            foreach (var item in _items) item.Presenter.ContentTemplate = SnackbarTemplate;
            UpdateCurrent();
        }, "Snackbar composition frame");
    }
    private void UpdateCurrent()
    {
        if (!this.IsAttachedToVisualTree() || _scope is null || ScaleAnimation is not { } scaleSpec || OpacityAnimation is not { } opacitySpec) return;
        var scope = _scope = AnimationScope.Get(this);
        var current = State.CurrentSnackbarData;
        if (ReferenceEquals(_current, current)) return;
        _current = current;
        _timeout?.Dispose(); _timeout = null;
        if (current is not null)
        {
            var duration = current.Visuals.Duration switch
            {
                SnackbarDuration.Short => TimeSpan.FromSeconds(4),
                SnackbarDuration.Long => TimeSpan.FromSeconds(10),
                SnackbarDuration.Indefinite => Timeout.InfiniteTimeSpan,
                _ => throw new ArgumentOutOfRangeException(nameof(current.Visuals.Duration))
            };
            var timeout = _timeout = scope.CreateChild("Snackbar timeout");
            _ = timeout.Launch(async ct => { await Task.Delay(duration, ct); current.Dismiss(); }, "Display duration");
            if (!_items.Any(item => ReferenceEquals(item.Data, current)))
            {
                var itemScope = scope.CreateChild("Snackbar item");
                var presenter = new SnackbarPresenter { Content = current, ContentTemplate = SnackbarTemplate };
                var item = new Item(current, presenter, itemScope,
                    new Animatable<float>(new FloatConverter(), 0f, VisibilityThresholds.Float, "Snackbar opacity"),
                    new Animatable<float>(new FloatConverter(), .8f, VisibilityThresholds.Float, "Snackbar scale"));
                _items.Add(item); _panel?.Children.Add(presenter);
                itemScope.SubscribeValue(item.Opacity, value => presenter.Opacity = value);
                itemScope.SubscribeValue(item.Scale, value => presenter.RenderTransform = new ScaleTransform(value, value));
            }
        }
        foreach (var item in _items.ToArray())
        {
            var visible = ReferenceEquals(item.Data, current);
            if (item.Visible == visible) continue;
            item.Visible = visible;
            AutomationProperties.SetLiveSetting(item.Presenter, visible ? AutomationLiveSetting.Polite : AutomationLiveSetting.Off);
            item.Effect?.Cancel(); item.Effect?.Dispose();
            item.Effect = CancellationTokenSource.CreateLinkedTokenSource(item.Scope.Lifetime);
            var token = item.Effect.Token;
            _ = item.Scope.Launch(async _ =>
            {
                var alpha = AnimateOpacity();
                var scale = item.Scale.AnimateToAsync(item.Scope, visible ? 1f : .8f,
                    Spec(scaleSpec), cancellationToken: token);
                await Task.WhenAll(alpha, scale);

                async Task AnimateOpacity()
                {
                    await item.Opacity.AnimateToAsync(item.Scope, visible ? 1f : 0f,
                        Spec(opacitySpec), cancellationToken: token);
                    if (!ReferenceEquals(item.Data, _current))
                    {
                        _items.Remove(item); _panel?.Children.Remove(item.Presenter);
                        item.Scope.Dispose(); item.Effect?.Dispose();
                    }
                }
            }, visible ? "Snackbar enter" : "Snackbar exit");
        }
    }
    private static SpringSpec<float> Spec(Foundation.Motion.SpringSpec spec) =>
        new(spec.DampingRatio, spec.Stiffness, .01f);
    private sealed class Item(ISnackbarData data, SnackbarPresenter presenter, AnimationChildScope scope,
        Animatable<float> opacity, Animatable<float> scale)
    {
        public ISnackbarData Data { get; } = data;
        public SnackbarPresenter Presenter { get; } = presenter;
        public AnimationChildScope Scope { get; } = scope;
        public Animatable<float> Opacity { get; } = opacity;
        public Animatable<float> Scale { get; } = scale;
        public bool? Visible;
        public CancellationTokenSource? Effect;
    }
}

internal sealed class SnackbarPresenter : ContentPresenter
{
    public SnackbarPresenter()
    {
        RenderTransformOrigin = RelativePoint.Center;
        AutomationProperties.SetName(this, "Snackbar");
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new Peer(this);
    private sealed class Peer(SnackbarPresenter owner) : ControlAutomationPeer(owner), IInvokeProvider
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
        // Avalonia has no Compose dismiss semantic; Invoke exposes the same operation.
        public void Invoke() => (owner.Content as ISnackbarData)?.Dismiss();
    }
}

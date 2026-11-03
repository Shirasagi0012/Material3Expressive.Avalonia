using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>Card.kt's filled container; opt in to the clickable overload with IsClickable.</summary>
public class Card : Button
{
    public static readonly StyledProperty<bool> IsClickableProperty = AvaloniaProperty.Register<Card, bool>(nameof(IsClickable));
    public bool IsClickable { get => GetValue(IsClickableProperty); set => SetValue(IsClickableProperty, value); }
    public static readonly StyledProperty<Color> ContainerColorProperty = AvaloniaProperty.Register<Card, Color>(nameof(ContainerColor));
    public Color ContainerColor { get => GetValue(ContainerColorProperty); set => SetValue(ContainerColorProperty, value); }
    public static readonly StyledProperty<IBrush?> ContentColorProperty = AvaloniaProperty.Register<Card, IBrush?>(nameof(ContentColor));
    public IBrush? ContentColor { get => GetValue(ContentColorProperty); set => SetValue(ContentColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledContainerColorProperty = AvaloniaProperty.Register<Card, Color>(nameof(DisabledContainerColor));
    public Color DisabledContainerColor { get => GetValue(DisabledContainerColorProperty); set => SetValue(DisabledContainerColorProperty, value); }
    public static readonly StyledProperty<IBrush?> DisabledContentColorProperty = AvaloniaProperty.Register<Card, IBrush?>(nameof(DisabledContentColor));
    public IBrush? DisabledContentColor { get => GetValue(DisabledContentColorProperty); set => SetValue(DisabledContentColorProperty, value); }
    public static readonly StyledProperty<CardElevation> ElevationProperty = AvaloniaProperty.Register<Card, CardElevation>(nameof(Elevation), new CardElevation(), validate: value => value is not null);
    public CardElevation Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = AvaloniaProperty.Register<Card, MaterialInteractionSource?>(nameof(InteractionSource));
    /// <summary>The clickable card's shared interaction stream. Null uses a private source.</summary>
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }

    internal static readonly StyledProperty<IControlTemplate?> DefaultTemplateProperty = AvaloniaProperty.Register<Card, IControlTemplate?>(nameof(DefaultTemplate));
    internal IControlTemplate? DefaultTemplate { get => GetValue(DefaultTemplateProperty); set => SetValue(DefaultTemplateProperty, value); }
    internal static readonly StyledProperty<Cursor?> DefaultCursorProperty = AvaloniaProperty.Register<Card, Cursor?>(nameof(DefaultCursor));
    internal Cursor? DefaultCursor { get => GetValue(DefaultCursorProperty); set => SetValue(DefaultCursorProperty, value); }

    private MaterialSurface? _surface;
    private MaterialInteractionController? _interaction;
    private AnimationChildScope? _attachScope;
    private Animatable<double>? _elevation;
    private CancellationTokenSource? _elevationEffect;
    private Task? _elevationTask;
    private IDisposable? _elevationBinding;
    private CancellationTokenSource? _elevationCommit;
    private double? _passiveElevation;
    private bool _pending;
    private readonly MaterialInteractionSource _localSource = new();
    private MaterialInteractionSource? _source;
    // CardElevation's remember list is independent of the indication node: it survives source replacement.
    private readonly List<MaterialInteractionEvent> _elevationInteractions = [];
    private InteractionState ElevationInteraction => _elevationInteractions.LastOrDefault() switch
    {
        MaterialInteractionEvent.Press => InteractionState.Pressed,
        MaterialInteractionEvent.HoverEnter => InteractionState.Hovered,
        MaterialInteractionEvent.Focus => InteractionState.Focused,
        MaterialInteractionEvent.DragStart => InteractionState.Dragged,
        _ => InteractionState.None,
    };

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        DisposeMotion();
        base.OnApplyTemplate(e);
        _surface = e.NameScope.Find<MaterialSurface>("PART_Container");
        Refresh();
        InitializeMotion();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attachScope = AnimationScope.Get(this).CreateChild("Card");
        _passiveElevation = null;
        ConnectSource();
        Refresh();
        InitializeMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _passiveElevation = null;
        DisconnectSource();
        DisposeMotion();
        _attachScope?.Dispose(); _attachScope = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsClickableProperty)
        {
            _passiveElevation = null;
            // The two templates mirror Surface's passive/clickable overloads. Replacing the
            // presenter releases its feedback lifetime, including any press in progress.
            MaterialInteraction.SetIsInteractive(this, IsClickable);
            PseudoClasses.Set(":clickable", IsClickable);
            UpdateIsEffectivelyEnabled();
            if (!IsClickable)
            {
                // Reuse Button's pressed-state cleanup without dispatching a synthetic event.
                base.OnLostFocus(new FocusChangedEventArgs(LostFocusEvent));
                if (IsFocused) TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
                DisposeMotion();
            }
            if (IsClickable) ConnectSource();
            else DisconnectSource();
            Refresh();
        }
        else if (change.Property == InteractionSourceProperty) ConnectSource();
        else if (change.Property == ElevationProperty
                 || change.Property == IsEffectivelyEnabledProperty) Refresh();
    }

    private void ConnectSource()
    {
        if (!IsClickable || !this.IsAttachedToVisualTree()) return;
        var source = InteractionSource ?? _localSource;
        if (ReferenceEquals(source, _source)) return;
        var replacing = _source is not null;
        // Native hover/press cancellation belongs to the old stream and must reach its collector.
        _interaction ??= MaterialInteraction.GetOrCreate(this);
        _interaction.ConnectSource(null);
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = source;
        _source.Interaction += ReceiveInteraction;
        _interaction.ConnectSource(source);
        if (replacing) _interaction.ResetSourceIndication();
    }

    private void DisconnectSource()
    {
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = null;
        _interaction?.ConnectSource(null);
        _elevationInteractions.Clear();
    }

    private void ReceiveInteraction(MaterialInteractionEvent interaction)
    {
        MaterialInteractionEvent? start = interaction switch
        {
            MaterialInteractionEvent.Release e => e.Start,
            MaterialInteractionEvent.CancelPress e => e.Start,
            MaterialInteractionEvent.HoverExit e => e.Start,
            MaterialInteractionEvent.Unfocus e => e.Start,
            MaterialInteractionEvent.DragStop e => e.Start,
            MaterialInteractionEvent.DragCancel e => e.Start,
            _ => null,
        };
        if (start is null) _elevationInteractions.Add(interaction);
        else _elevationInteractions.Remove(start);
        QueueElevation();
    }

    private void Refresh()
    {
        if (_surface is null || !this.IsAttachedToVisualTree()) return;
        // With no InteractionSource, the upstream elevation is remembered without a value key.
        // Keep the initial effective configuration until the mode or visual mount changes.
        if (!IsClickable) _surface.ShadowElevation = _passiveElevation ??= Elevation.Default;
        else QueueElevation();
    }

    internal static double Target(CardElevation values, InteractionState interaction, bool enabled) =>
        !enabled ? values.Disabled : interaction switch
        {
            InteractionState.Pressed => values.Pressed, InteractionState.Hovered => values.Hovered,
            InteractionState.Focused => values.Focused, InteractionState.Dragged => values.Dragged, _ => values.Default,
        };

    private void InitializeMotion()
    {
        if (!IsClickable || _surface is null || !this.IsAttachedToVisualTree() || _attachScope is not { } motion || _elevation is not null) return;
        _elevation = new Animatable<double>(new DoubleConverter(), Target(Elevation, ElevationInteraction, IsEffectivelyEnabled), VisibilityThresholds.Double);
        _elevationBinding = motion.BindAnimatedValue(_surface, MaterialSurface.ShadowElevationProperty, _elevation);
    }

    private void QueueElevation()
    {
        if (!IsClickable || !this.IsAttachedToVisualTree() || _attachScope is not { } motion || _elevation is null || _pending) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            // A recomposition commits effects after existing animation waiters sample this
            // frame. In particular, replacing CardElevation must retain that sampled value.
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            _pending = false;
            var values = Elevation;
            var interaction = ElevationInteraction;
            var target = Target(values, interaction, IsEffectivelyEnabled);
            if (_elevation is not { } elevation || elevation.TargetValue == target) return;
            // CardElevation infers the outgoing interaction from target height, in this exact order.
            var from = elevation.TargetValue == values.Pressed ? InteractionState.Pressed
                : elevation.TargetValue == values.Hovered ? InteractionState.Hovered
                : elevation.TargetValue == values.Focused ? InteractionState.Focused
                : elevation.TargetValue == values.Dragged ? InteractionState.Dragged : InteractionState.None;
            // LaunchedEffect(target) cancels its previous coroutine before the replacement
            // calls animateTo. An Animatable interruption alone would retain its old frame
            // time, incorrectly advancing the replacement on its first frame.
            _elevationEffect?.Cancel();
            var previous = _elevationTask;
            var enabled = IsEffectivelyEnabled;
            _elevationTask = motion.Launch(async token =>
            {
                if (previous is not null) { try { await previous; } catch (OperationCanceledException) { } }
                using var effect = CancellationTokenSource.CreateLinkedTokenSource(token);
                _elevationEffect = effect;
                try
                {
                    if (!enabled || (from == InteractionState.None && interaction == InteractionState.None))
                    {
                        // Surface reads this State during composition, before LaunchedEffect
                        // snaps it. Preserve that composed value until the next composition frame.
                        var surface = _surface!;
                        var composedElevation = surface.ShadowElevation;
                        _elevationBinding?.Dispose(); _elevationBinding = null;
                        surface.ShadowElevation = composedElevation;
                        await elevation.SnapToAsync(target, effect.Token);
                        _elevationCommit?.Cancel();
                        _ = motion.Launch(async commitToken =>
                        {
                            using var commit = CancellationTokenSource.CreateLinkedTokenSource(commitToken);
                            _elevationCommit = commit;
                            try
                            {
                                await motion.NextFrameAsync(commit.Token);
                                if (ReferenceEquals(_surface, surface) && ReferenceEquals(_elevation, elevation))
                                {
                                    _elevationBinding?.Dispose();
                                    _elevationBinding = motion.BindAnimatedValue(surface, MaterialSurface.ShadowElevationProperty, elevation);
                                }
                            }
                            finally { if (ReferenceEquals(_elevationCommit, commit)) _elevationCommit = null; }
                        }, "Card elevation composition");
                    }
                    else
                        await elevation.AnimateToAsync(motion, target, new TweenSpec<double>(TimeSpan.FromMilliseconds(
                            interaction != InteractionState.None || from == InteractionState.Hovered ? 120 : 150),
                            easing: interaction != InteractionState.None ? ComposeEasings.FastOutSlowIn : new CubicBezierEasing(.4, 0, .6, 1)),
                            cancellationToken: effect.Token);
                }
                finally { if (ReferenceEquals(_elevationEffect, effect)) _elevationEffect = null; }
            }, "Card elevation");
        }, "Card composition");
    }

    private void DisposeMotion()
    {
        _elevationEffect?.Cancel(); _elevationEffect = null; _elevationTask = null;
        _elevationBinding?.Dispose(); _elevationBinding = null;
        _elevationCommit?.Cancel(); _elevationCommit = null;
        _elevation = null; _pending = false;
    }

    protected override void OnClick() { if (IsClickable) base.OnClick(); }
    protected override bool IsEnabledCore => IsClickable ? base.IsEnabledCore : IsEnabled;
    protected override void OnKeyDown(KeyEventArgs e) { if (IsClickable) base.OnKeyDown(e); }
    protected override void OnKeyUp(KeyEventArgs e) { if (IsClickable) base.OnKeyUp(e); }
    protected override void OnPointerPressed(PointerPressedEventArgs e) { if (IsClickable) base.OnPointerPressed(e); }
    protected override void OnPointerReleased(PointerReleasedEventArgs e) { if (IsClickable) base.OnPointerReleased(e); }
    protected override AutomationPeer OnCreateAutomationPeer() => new CardAutomationPeer(this);

    private sealed class CardAutomationPeer(Card owner) : ButtonAutomationPeer(owner)
    {
        protected override object? GetProviderCore(Type providerType) => owner.IsClickable ? base.GetProviderCore(providerType) : null;
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.IsClickable ? AutomationControlType.Button : AutomationControlType.Pane;
        protected override string GetClassNameCore() => owner.GetType().Name;
    }
}

public sealed class ElevatedCard : Card;
public sealed class OutlinedCard : Card;

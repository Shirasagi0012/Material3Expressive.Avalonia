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

    internal static readonly StyledProperty<IControlTemplate?> DefaultTemplateProperty = AvaloniaProperty.Register<Card, IControlTemplate?>(nameof(DefaultTemplate));
    internal IControlTemplate? DefaultTemplate { get => GetValue(DefaultTemplateProperty); set => SetValue(DefaultTemplateProperty, value); }
    internal static readonly StyledProperty<Cursor?> DefaultCursorProperty = AvaloniaProperty.Register<Card, Cursor?>(nameof(DefaultCursor));
    internal Cursor? DefaultCursor { get => GetValue(DefaultCursorProperty); set => SetValue(DefaultCursorProperty, value); }

    private MaterialSurface? _surface;
    private MaterialInteractionController? _interaction;
    private AnimationScope? _motion;
    private Animatable<double>? _elevation;
    private CancellationTokenSource? _elevationEffect;
    private Task? _elevationTask;
    private double? _passiveElevation;
    private bool _pending;

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
        _passiveElevation = null;
        Refresh();
        InitializeMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _passiveElevation = null;
        DisposeMotion();
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
            else if (_interaction is null)
            {
                _interaction = MaterialInteraction.GetOrCreate(this);
                _interaction.Changed += QueueElevation;
            }
            Refresh();
        }
        else if (change.Property == ElevationProperty
                 || change.Property == IsEffectivelyEnabledProperty) Refresh();
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
        if (!IsClickable || _surface is null || !this.IsAttachedToVisualTree() || _motion is not null) return;
        _motion = AnimationScope.Get(this);
        _elevation = new Animatable<double>(new DoubleConverter(), Target(Elevation, _interaction?.LastInteraction ?? InteractionState.None, IsEffectivelyEnabled), VisibilityThresholds.Double);
        _motion.BindAnimatedValue(_surface, MaterialSurface.ShadowElevationProperty, _elevation);
    }

    private void QueueElevation()
    {
        if (!IsClickable || !this.IsAttachedToVisualTree() || _motion is null || _pending) return;
        var motion = _motion = AnimationScope.Get(this);
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
            var interaction = _interaction?.LastInteraction ?? InteractionState.None;
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
                        await elevation.SnapToAsync(target, effect.Token);
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
        _motion = null; _elevation = null; _pending = false;
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

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>Card.kt's filled container; opt in to the clickable overload with IsClickable.</summary>
public class Card : Button
{
    public static readonly StyledProperty<bool> IsClickableProperty = AvaloniaProperty.Register<Card, bool>(nameof(IsClickable));
    public bool IsClickable { get => GetValue(IsClickableProperty); set => SetValue(IsClickableProperty, value); }
    public static readonly StyledProperty<CardColors?> ColorsProperty = AvaloniaProperty.Register<Card, CardColors?>(nameof(Colors));
    public CardColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public static readonly StyledProperty<CardElevation?> ElevationProperty = AvaloniaProperty.Register<Card, CardElevation?>(nameof(Elevation));
    public CardElevation? Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }

    private MaterialSurface? _surface;
    private MaterialInteractionController? _interaction;
    private VisualAnimationScope? _motion;
    private Animatable<double>? _elevation;
    private CancellationTokenSource? _elevationEffect;
    private Task? _elevationTask;
    private double? _passiveElevation;
    private bool _pending, _refreshing;
    private Control? _ambient;

    public Card()
    {
        ResourcesChanged += (_, _) => Refresh();
        ActualThemeVariantChanged += (_, _) => Refresh();
    }

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
        _ambient = Parent as Control;
        if (_ambient is not null) _ambient.PropertyChanged += AmbientChanged;
        _passiveElevation = null;
        Refresh();
        InitializeMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_ambient is not null) _ambient.PropertyChanged -= AmbientChanged;
        _ambient = null;
        _passiveElevation = null;
        DisposeMotion();
        base.OnDetachedFromVisualTree(e);
    }

    private void AmbientChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextElement.ForegroundProperty) Refresh();
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
            SetValue(FocusableProperty, IsClickable, BindingPriority.Style);
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
        else if (change.Property == ColorsProperty || change.Property == ElevationProperty
                 || change.Property == IsEffectivelyEnabledProperty) Refresh();
    }

    private T Resolve<T>(MaterialTokenKey<T> key) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is T result
            ? result : throw new InvalidOperationException($"Card requires token {key}.");

    private void Refresh()
    {
        if (_surface is null || _refreshing
            || !TokenResolver.TryResolve(this, Comp.FilledCard.ContainerColor, ActualThemeVariant, out _)) return;
        _refreshing = true;
        try
        {
            var outlined = this is OutlinedCard;
            var elevated = this is ElevatedCard;
            var baseColor = Resolve(outlined ? Comp.OutlinedCard.ContainerColor
                : elevated ? Comp.ElevatedCard.ContainerColor : Comp.FilledCard.ContainerColor);
            var normal = Colors?.ContainerColor ?? baseColor;
            var ambient = _ambient is null ? null : TextElement.GetForeground(_ambient);
            var normalInk = Colors?.ContentColor is { } explicitInk ? new SolidColorBrush(explicitInk)
                : ContentColorForExtension.Resolve(this, normal, ambient);
            var enabled = !IsClickable || IsEffectivelyEnabled;
            Color Alpha(Color color, double alpha) => Color.FromArgb((byte)Math.Round(alpha * 255, MidpointRounding.AwayFromZero), color.R, color.G, color.B);
            var disabledBase = outlined ? baseColor : Resolve(elevated ? Comp.ElevatedCard.DisabledContainerColor : Comp.FilledCard.DisabledContainerColor);
            var disabled = outlined ? disabledBase : ColorComposite.Over(Alpha(disabledBase,
                Resolve(elevated ? Comp.ElevatedCard.DisabledContainerOpacity : Comp.FilledCard.DisabledContainerOpacity)), 1,
                elevated ? disabledBase : baseColor);
            SetValue(BackgroundProperty, new SolidColorBrush(enabled ? normal : Colors?.DisabledContainerColor ?? disabled), BindingPriority.Style);
            var ink = enabled ? normalInk : Colors?.DisabledContentColor is { } disabledInk ? new SolidColorBrush(disabledInk)
                : normalInk is ISolidColorBrush solid ? new SolidColorBrush(Alpha(solid.Color, .38)) : normalInk;
            SetValue(ForegroundProperty, ink, BindingPriority.Style);
            if (outlined)
            {
                var outline = enabled ? Resolve(Comp.OutlinedCard.OutlineColor)
                    : ColorComposite.Over(Alpha(Resolve(Comp.OutlinedCard.DisabledOutlineColor), Resolve(Comp.OutlinedCard.DisabledOutlineOpacity)),
                        1, Resolve(Comp.ElevatedCard.ContainerColor));
                SetValue(BorderBrushProperty, new SolidColorBrush(outline), BindingPriority.Style);
                SetValue(BorderThicknessProperty, new Thickness(Resolve(Comp.OutlinedCard.OutlineWidth)), BindingPriority.Style);
            }
            // With no InteractionSource, CardElevation.shadowElevation returns
            // remember { mutableStateOf(defaultElevation) }, without a value key.
            if (!IsClickable) _surface.ShadowElevation = _passiveElevation ??= Values().Default!.Value;
            else QueueElevation();
        }
        finally { _refreshing = false; }
    }

    private CardElevation Values()
    {
        var value = Elevation;
        var elevated = this is ElevatedCard;
        var outlined = this is OutlinedCard;
        var rest = value?.Default ?? Resolve(outlined ? Comp.OutlinedCard.ContainerElevation : elevated ? Comp.ElevatedCard.ContainerElevation : Comp.FilledCard.ContainerElevation);
        return new CardElevation
        {
            Default = rest,
            Pressed = value?.Pressed ?? (outlined ? rest : Resolve(elevated ? Comp.ElevatedCard.PressedContainerElevation : Comp.FilledCard.PressedContainerElevation)),
            Focused = value?.Focused ?? (outlined ? rest : Resolve(elevated ? Comp.ElevatedCard.FocusContainerElevation : Comp.FilledCard.FocusContainerElevation)),
            // CardDefaults.outlinedCardElevation deliberately uses defaultElevation, not HoverContainerElevation.
            Hovered = value?.Hovered ?? (outlined ? rest : Resolve(elevated ? Comp.ElevatedCard.HoverContainerElevation : Comp.FilledCard.HoverContainerElevation)),
            Dragged = value?.Dragged ?? Resolve(outlined ? Comp.OutlinedCard.DraggedContainerElevation : elevated ? Comp.ElevatedCard.DraggedContainerElevation : Comp.FilledCard.DraggedContainerElevation),
            Disabled = value?.Disabled ?? Resolve(outlined ? Comp.OutlinedCard.DisabledContainerElevation : elevated ? Comp.ElevatedCard.DisabledContainerElevation : Comp.FilledCard.DisabledContainerElevation),
        };
    }

    internal static double Target(CardElevation values, InteractionState interaction, bool enabled) =>
        (!enabled ? values.Disabled : interaction switch
        {
            InteractionState.Pressed => values.Pressed, InteractionState.Hovered => values.Hovered,
            InteractionState.Focused => values.Focused, InteractionState.Dragged => values.Dragged, _ => values.Default,
        })!.Value;

    private void InitializeMotion()
    {
        if (!IsClickable || _surface is null || !this.IsAttachedToVisualTree() || _motion is not null) return;
        _motion = VisualAnimationScope.Create(this);
        _elevation = _motion.CreateAnimatable(Target(Values(), _interaction?.LastInteraction ?? InteractionState.None, IsEffectivelyEnabled));
        _motion.BindAnimatedValue(_surface, MaterialSurface.ShadowElevationProperty, _elevation);
    }

    private void QueueElevation()
    {
        if (!IsClickable || _motion is not { IsEnding: false } motion || _pending) return;
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            // A recomposition commits effects after existing animation waiters sample this
            // frame. In particular, replacing CardElevation must retain that sampled value.
            await Task.Yield();
            ct.ThrowIfCancellationRequested();
            _pending = false;
            var values = Values();
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
                        await elevation.AnimateToAsync(target, new TweenSpec<double>(TimeSpan.FromMilliseconds(
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
        _motion?.Dispose(); _motion = null; _elevation = null; _pending = false;
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

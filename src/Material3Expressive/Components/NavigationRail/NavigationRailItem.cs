using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Tokens;
using Material3Expressive.Tokens;

namespace Material3Expressive.Components;

/// <summary>A regular navigation destination. Click/Command requests navigation; IsSelected is externally owned.</summary>
public class NavigationRailItem : Button
{
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<NavigationRailItem, object?>(nameof(Icon), null);
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty =
        AvaloniaProperty.Register<NavigationRailItem, IDataTemplate?>(nameof(IconTemplate), null);
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }

    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<NavigationRailItem, object?>(nameof(Label), null);
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty =
        AvaloniaProperty.Register<NavigationRailItem, IDataTemplate?>(nameof(LabelTemplate), null);
    public IDataTemplate? LabelTemplate { get => GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<NavigationRailItem, bool>(nameof(IsSelected), false);
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    public static readonly StyledProperty<bool> AlwaysShowLabelProperty =
        AvaloniaProperty.Register<NavigationRailItem, bool>(nameof(AlwaysShowLabel), true);
    public bool AlwaysShowLabel { get => GetValue(AlwaysShowLabelProperty); set => SetValue(AlwaysShowLabelProperty, value); }

    public static readonly StyledProperty<Color> SelectedIconColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(SelectedIconColor), default);
    public Color SelectedIconColor { get => GetValue(SelectedIconColorProperty); set => SetValue(SelectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> SelectedTextColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(SelectedTextColor), default);
    public Color SelectedTextColor { get => GetValue(SelectedTextColorProperty); set => SetValue(SelectedTextColorProperty, value); }

    public static readonly StyledProperty<Color> IndicatorColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(IndicatorColor), default);
    public Color IndicatorColor { get => GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedIconColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(UnselectedIconColor), default);
    public Color UnselectedIconColor { get => GetValue(UnselectedIconColorProperty); set => SetValue(UnselectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedTextColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(UnselectedTextColor), default);
    public Color UnselectedTextColor { get => GetValue(UnselectedTextColorProperty); set => SetValue(UnselectedTextColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledIconColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(DisabledIconColor), default);
    public Color DisabledIconColor { get => GetValue(DisabledIconColorProperty); set => SetValue(DisabledIconColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledTextColorProperty =
        AvaloniaProperty.Register<NavigationRailItem, Color>(nameof(DisabledTextColor), default);
    public Color DisabledTextColor { get => GetValue(DisabledTextColorProperty); set => SetValue(DisabledTextColorProperty, value); }

    private VisualAnimationScope? _motion;
    private Animatable<float>? _alpha;
    private Animatable<float>? _size;
    private Animatable<Color>? _iconColor;
    private Animatable<Color>? _textColor;
    private NavigationRailItemLayout? _layout;
    private ContentPresenter? _icon;
    private ContentPresenter? _label;
    private Border? _indicator;
    private NavigationRailItemAutomationPeer? _peer;
    private bool _targetsPending;
    private Key? _activationKey;
    private bool _suppressKeyboardClick;

    public NavigationRailItem()
    {
        // MappedInteractionSource uses the token widths, not actual item/ripple positions.
        // Its vertical delta is zero even when the pill has moved with the icon.
        MaterialInteraction.GetOrCreate(this).PressPositionMapper = origin =>
            new Point(origin.X - (Metric(MaterialTokens.Comp.NavigationRail.NarrowContainerWidth, 80)
                - Metric(MaterialTokens.Comp.NavigationRail.ActiveIndicatorWidth, 56)) / 2, origin.Y);
        LostFocus += (_, _) => _activationKey = null;
    }

    protected override Type StyleKeyOverride => typeof(NavigationRailItem);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _motion?.Dispose();
        _motion = null;
        base.OnApplyTemplate(e);
        _targetsPending = false;
        _layout = e.NameScope.Find<NavigationRailItemLayout>("PART_Layout");
        _icon = e.NameScope.Find<ContentPresenter>("PART_Icon");
        _label = e.NameScope.Find<ContentPresenter>("PART_Label");
        _indicator = e.NameScope.Find<Border>("PART_Indicator");
        if (_indicator is not null) _indicator.Background = new SolidColorBrush(IndicatorColor);
        InitializeMotion();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        InitializeMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _motion?.Dispose();
        _motion = null;
        _alpha = _size = null;
        _targetsPending = false;
        _activationKey = null;
        _iconColor = _textColor = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", IsSelected);
            if (_peer is { } peer)
                peer.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
        }
        if (change.Property == IsSelectedProperty || change.Property == IsEffectivelyEnabledProperty
            || change.Property == SelectedIconColorProperty || change.Property == SelectedTextColorProperty
            || change.Property == UnselectedIconColorProperty || change.Property == UnselectedTextColorProperty
            || change.Property == DisabledIconColorProperty || change.Property == DisabledTextColorProperty)
            UpdateTargets();
        if (change.Property == IsSelectedProperty || change.Property == AlwaysShowLabelProperty || change.Property == LabelProperty)
            UpdateIconSemantics();
        if (change.Property == IndicatorColorProperty && _indicator is not null)
            _indicator.Background = new SolidColorBrush(IndicatorColor);
    }

    private Color IconTarget => !IsEffectivelyEnabled ? DisabledIconColor : IsSelected ? SelectedIconColor : UnselectedIconColor;
    private Color TextTarget => !IsEffectivelyEnabled ? DisabledTextColor : IsSelected ? SelectedTextColor : UnselectedTextColor;

    private void InitializeMotion()
    {
        if (_layout is null || !this.IsAttachedToVisualTree() || _motion is not null)
            return;
        _motion = VisualAnimationScope.Create(this);
        _alpha = _motion.CreateAnimatable(IsSelected ? 1f : 0f);
        _size = _motion.CreateAnimatable(IsSelected ? 1f : 0f);
        _iconColor = _motion.CreateAnimatable(IconTarget);
        _textColor = _motion.CreateAnimatable(TextTarget);
        _motion.SubscribeValue(_alpha, value =>
        {
            _layout.AlphaProgress = value;
            if (_indicator is not null) _indicator.Opacity = Math.Clamp(value, 0, 1);
        });
        _motion.SubscribeValue(_size, value =>
        {
            var wasPlaced = _layout.SizeProgress > 0;
            _layout.SizeProgress = value;
            if (wasPlaced != (value > 0)) _peer?.RefreshChildren();
        });
        _motion.SubscribeValue(_iconColor, value => { if (_icon is not null) _icon.Foreground = new SolidColorBrush(value); });
        _motion.SubscribeValue(_textColor, value => { if (_label is not null) _label.Foreground = new SolidColorBrush(value); });
        UpdateIconSemantics();
    }

    private void UpdateIconSemantics()
    {
        // Suppress the entire icon subtree while the label supplies the destination name.
        _peer?.RefreshChildren();
    }

    private void UpdateTargets()
    {
        if (_motion is not { IsEnding: false } motion)
            return;
        if (_targetsPending) return;
        _targetsPending = true;
        // AnimateAsState.update is a composition SideEffect. The fixed Desktop
        // FrameRecomposer applies it on the next frame, before these animatables
        // receive their first animation frame. Coalesce targets in that same order.
        _ = motion.Launch(async ct =>
        {
            await motion.Clock.NextFrameAsync(ct);
            _targetsPending = false;
            CommitTargets();
        }, "Rail animation targets");
    }

    private void CommitTargets()
    {
        if (_motion is not { IsEnding: false } motion || _alpha is null || _size is null
            || _iconColor is null || _textColor is null)
            return;
        // Fixed AnimateAsState.restartAnimation cancels its previous job before
        // animateTo: cancellation resets velocity and the frame anchor. Calling
        // AnimateTo directly would instead carry the old velocity into the new target.
        // Spec changes apply at the next target change.
        var effects = Spec(MaterialTokens.Sys.Motion.Spring.EffectsDefault, MotionScheme.Expressive.DefaultEffects);
        var spatial = Spec(MaterialTokens.Sys.Motion.Spring.SpatialFast, MotionScheme.Expressive.FastSpatial);
        var selected = IsSelected ? 1f : 0f;
        Restart(_alpha, selected, new SpringSpec<float>(effects.DampingRatio, effects.Stiffness, 0.01f), "Rail indicator alpha");
        Restart(_size, selected, new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, 0.01f), "Rail indicator size");
        Restart(_iconColor, IconTarget, new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness), "Rail icon color");
        Restart(_textColor, TextTarget, new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness), "Rail label color");

        void Restart<T>(Animatable<T> track, T target, SpringSpec<T> spec, string label)
        {
            if (EqualityComparer<T>.Default.Equals(track.TargetValue, target)) return;
            _ = motion.Launch(async ct =>
            {
                await track.StopAsync();
                ct.ThrowIfCancellationRequested();
                await track.AnimateToAsync(target, spec, cancellationToken: ct);
            }, label);
        }
    }

    private Foundation.Motion.SpringSpec Spec(MaterialTokenKey<Foundation.Motion.SpringSpec> key, Foundation.Motion.SpringSpec fallback) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is Foundation.Motion.SpringSpec spec ? spec : fallback;
    private double Metric(MaterialTokenKey<double> key, double fallback) =>
        TokenResolver.TryResolve(this, key, ActualThemeVariant, out var value) && value is double number ? number : fallback;

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new NavigationRailItemAutomationPeer(this);

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Compose selectable invokes on key-up for both activation keys; Avalonia
        // Button invokes Enter on key-down. Keep the upstream press/release order.
        if (e.Key is Key.Space or Key.Enter && IsFocused && IsEffectivelyEnabled)
        {
            _activationKey ??= e.Key;
            // Reuse Button's Space path to maintain its read-only IsPressed state.
            // The shared controller already received the original physical key event.
            _suppressKeyboardClick = true;
            try { base.OnKeyDown(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            var invoke = _activationKey == e.Key && IsFocused && IsEffectivelyEnabled;
            _activationKey = null;
            _suppressKeyboardClick = true;
            try { base.OnKeyUp(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true;
            if (invoke) OnClick();
            return;
        }
        base.OnKeyUp(e);
    }

    protected override void OnClick()
    {
        if (!_suppressKeyboardClick) base.OnClick();
    }

    private sealed class NavigationRailItemAutomationPeer(NavigationRailItem owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
    {
        public void RefreshChildren() => InvalidateChildren();
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.FindAncestorOfType<NavigationRail>() is { } rail ? ControlAutomationPeer.CreatePeerForElement(rail) as ISelectionProvider : null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        // Upstream selectable exposes a click action, not an independent deselection action.
        public void RemoveFromSelection() { }
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
        {
            var clearIcon = owner.Label is not null && (owner.AlwaysShowLabel || owner.IsSelected);
            var labelPlaced = owner.Label is not null && (owner.AlwaysShowLabel || owner._layout?.SizeProgress > 0);
            // Deselection restores icon semantics immediately, while the label remains
            // placed (and semantic) until its outgoing size animation reaches zero.
            return new[] { clearIcon ? null : owner._icon, labelPlaced ? owner._label : null }
                .OfType<ContentPresenter>()
                .SelectMany(presenter => ControlAutomationPeer.CreatePeerForElement(presenter)?.GetChildren() ?? [])
                .ToArray();
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
        protected override string GetClassNameCore() => nameof(NavigationRailItem);
        protected override string? GetNameCore() =>
            !string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? base.GetNameCore()
            : owner.Label is not null && (owner.AlwaysShowLabel || owner.IsSelected)
                ? owner.Label as string ?? base.GetNameCore() : base.GetNameCore();
    }
}


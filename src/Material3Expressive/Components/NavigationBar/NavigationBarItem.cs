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

namespace Material3Expressive.Components;

/// <summary>A bottom navigation destination. Click/Command requests navigation; IsSelected is externally owned.</summary>
public class NavigationBarItem : Button
{
    public static readonly StyledProperty<object?> IconProperty =
        AvaloniaProperty.Register<NavigationBarItem, object?>(nameof(Icon), null);
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty =
        AvaloniaProperty.Register<NavigationBarItem, IDataTemplate?>(nameof(IconTemplate), null);
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }

    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<NavigationBarItem, object?>(nameof(Label), null);
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty =
        AvaloniaProperty.Register<NavigationBarItem, IDataTemplate?>(nameof(LabelTemplate), null);
    public IDataTemplate? LabelTemplate { get => GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<NavigationBarItem, bool>(nameof(IsSelected), false);
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    public static readonly StyledProperty<bool> AlwaysShowLabelProperty =
        AvaloniaProperty.Register<NavigationBarItem, bool>(nameof(AlwaysShowLabel), true);
    public bool AlwaysShowLabel { get => GetValue(AlwaysShowLabelProperty); set => SetValue(AlwaysShowLabelProperty, value); }

    public static readonly StyledProperty<Color> SelectedIconColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(SelectedIconColor), default);
    public Color SelectedIconColor { get => GetValue(SelectedIconColorProperty); set => SetValue(SelectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> SelectedTextColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(SelectedTextColor), default);
    public Color SelectedTextColor { get => GetValue(SelectedTextColorProperty); set => SetValue(SelectedTextColorProperty, value); }

    public static readonly StyledProperty<Color> IndicatorColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(IndicatorColor), default);
    public Color IndicatorColor { get => GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedIconColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(UnselectedIconColor), default);
    public Color UnselectedIconColor { get => GetValue(UnselectedIconColorProperty); set => SetValue(UnselectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedTextColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(UnselectedTextColor), default);
    public Color UnselectedTextColor { get => GetValue(UnselectedTextColorProperty); set => SetValue(UnselectedTextColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledIconColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(DisabledIconColor), default);
    public Color DisabledIconColor { get => GetValue(DisabledIconColorProperty); set => SetValue(DisabledIconColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledTextColorProperty =
        AvaloniaProperty.Register<NavigationBarItem, Color>(nameof(DisabledTextColor), default);
    public Color DisabledTextColor { get => GetValue(DisabledTextColorProperty); set => SetValue(DisabledTextColorProperty, value); }

    private AnimationScope? _motion;
    private Animatable<float>? _alpha;
    private Animatable<float>? _size;
    private Animatable<Color>? _iconColor;
    private Animatable<Color>? _textColor;
    private NavigationBarItemLayout? _layout;
    private ContentPresenter? _icon;
    private ContentPresenter? _label;
    private Border? _indicator;
    private NavigationBarItemAutomationPeer? _peer;
    private bool _targetsPending;
    private Key? _activationKey;
    private bool _suppressKeyboardClick;

    public NavigationBarItem()
    {
        // NavigationBar.kt maps using the measured item width and the token width.
        // The vertical offset stays 12 DIP even with a hidden label or a tall item.
        MaterialInteraction.GetOrCreate(this).PressPositionMapper = origin =>
        {
            var scale = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
            if (_layout is null) return origin;
            var indicatorWidth = Foundation.MathUtils.DipToPx((float)_layout.IndicatorWidth, (float)scale);
            var itemWidth = Foundation.MathUtils.DipToPx((float)Bounds.Width, (float)scale);
            return new Point(origin.X - (itemWidth - indicatorWidth) / (2 * scale), origin.Y - 12);
        };
        LostFocus += (_, _) => _activationKey = null;
    }

    protected override Type StyleKeyOverride => typeof(NavigationBarItem);

    private void MotionConfigurationChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == NavigationBarItemLayout.EffectsAnimationProperty || change.Property == NavigationBarItemLayout.IndicatorSizeAnimationProperty)
            UpdateTargets();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _motion = null;
        base.OnApplyTemplate(e);
        _targetsPending = false;
        if (_layout is not null) _layout.PropertyChanged -= MotionConfigurationChanged;
        _layout = e.NameScope.Find<NavigationBarItemLayout>("PART_Layout");
        if (_layout is not null) _layout.PropertyChanged += MotionConfigurationChanged;
        _icon = e.NameScope.Find<ContentPresenter>("PART_Icon");
        _label = e.NameScope.Find<ContentPresenter>("PART_Label");
        _indicator = e.NameScope.Find<Border>("PART_Indicator");
        if (_indicator is not null) _indicator.Background = new SolidColorBrush(IndicatorColor);
        InitializeMotion();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_layout is not null) { _layout.PropertyChanged -= MotionConfigurationChanged; _layout.PropertyChanged += MotionConfigurationChanged; }
        InitializeMotion();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _motion = null;
        _alpha = _size = null;
        _targetsPending = false;
        _activationKey = null;
        _iconColor = _textColor = null;
        if (_layout is not null) _layout.PropertyChanged -= MotionConfigurationChanged;
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
        _motion = AnimationScope.Get(this);
        _alpha = new Animatable<float>(new FloatConverter(), IsSelected ? 1f : 0f, VisibilityThresholds.Float);
        _size = new Animatable<float>(new FloatConverter(), IsSelected ? 1f : 0f, VisibilityThresholds.Float);
        _iconColor = new Animatable<Color>(new ColorConverter(), IconTarget, default);
        _textColor = new Animatable<Color>(new ColorConverter(), TextTarget, default);
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
        if (!this.IsAttachedToVisualTree() || _motion is null)
            return;
        var motion = _motion = AnimationScope.Get(this);
        if (_targetsPending) return;
        _targetsPending = true;
        // AnimateAsState.update is a composition SideEffect. The fixed Desktop
        // FrameRecomposer applies it on the next frame, before these animatables
        // receive their first animation frame. Coalesce targets in that same order.
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            _targetsPending = false;
            CommitTargets();
        }, "Navigation bar animation targets");
    }

    private void CommitTargets()
    {
        if (!this.IsAttachedToVisualTree() || _motion is null || _alpha is null || _size is null
            || _iconColor is null || _textColor is null)
            return;
        var motion = _motion = AnimationScope.Get(this);
        // Fixed AnimateAsState.restartAnimation cancels its previous job before
        // animateTo: cancellation resets velocity and the frame anchor. Calling
        // AnimateTo directly would instead carry the old velocity into the new target.
        // Spec changes apply at the next target change.
        var effects = _layout!.EffectsAnimation;
        var spatial = _layout!.IndicatorSizeAnimation;
        if (effects.Stiffness <= 0 || spatial.Stiffness <= 0) return;
        var selected = IsSelected ? 1f : 0f;
        Restart(_alpha, selected, new SpringSpec<float>(effects.DampingRatio, effects.Stiffness, 0.01f), "Navigation bar indicator alpha");
        Restart(_size, selected, new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, 0.01f), "Navigation bar indicator size");
        Restart(_iconColor, IconTarget, new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness), "Navigation bar icon color");
        Restart(_textColor, TextTarget, new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness), "Navigation bar label color");

        void Restart<T>(Animatable<T> track, T target, SpringSpec<T> spec, string label)
        {
            if (EqualityComparer<T>.Default.Equals(track.TargetValue, target)) return;
            _ = motion.Launch(async ct =>
            {
                await track.StopAsync();
                ct.ThrowIfCancellationRequested();
                await track.AnimateToAsync(motion, target, spec, cancellationToken: ct);
            }, label);
        }
    }


    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new NavigationBarItemAutomationPeer(this);

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

    private sealed class NavigationBarItemAutomationPeer(NavigationBarItem owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
    {
        public void RefreshChildren() => InvalidateChildren();
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.FindAncestorOfType<NavigationBar>() is { } rail ? ControlAutomationPeer.CreatePeerForElement(rail) as ISelectionProvider : null;
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
        protected override string GetClassNameCore() => nameof(NavigationBarItem);
        protected override string? GetNameCore() =>
            !string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? base.GetNameCore()
            : owner.Label is not null && (owner.AlwaysShowLabel || owner.IsSelected)
                ? owner.Label as string ?? base.GetNameCore() : base.GetNameCore();
    }
}


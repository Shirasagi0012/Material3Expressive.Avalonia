using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

public enum NavigationItemIconPosition { Top, Start }

/// <summary>A wide rail destination. Selection belongs to the application; expansion controls the animated icon position.</summary>
public class WideNavigationRailItem : Button
{
    internal static readonly StyledProperty<TextStyle?> TopIconLabelTextStyleProperty = AvaloniaProperty.Register<WideNavigationRailItem, TextStyle?>(nameof(TopIconLabelTextStyle));
    internal TextStyle? TopIconLabelTextStyle { get => GetValue(TopIconLabelTextStyleProperty); set => SetValue(TopIconLabelTextStyleProperty, value); }
    internal static readonly StyledProperty<TextStyle?> StartIconLabelTextStyleProperty = AvaloniaProperty.Register<WideNavigationRailItem, TextStyle?>(nameof(StartIconLabelTextStyle));
    internal TextStyle? StartIconLabelTextStyle { get => GetValue(StartIconLabelTextStyleProperty); set => SetValue(StartIconLabelTextStyleProperty, value); }
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<WideNavigationRailItem, object?>(nameof(Icon), null);
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<WideNavigationRailItem, IDataTemplate?>(nameof(IconTemplate), null);
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }

    public static readonly StyledProperty<object?> LabelProperty = AvaloniaProperty.Register<WideNavigationRailItem, object?>(nameof(Label), null);
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty = AvaloniaProperty.Register<WideNavigationRailItem, IDataTemplate?>(nameof(LabelTemplate), null);
    public IDataTemplate? LabelTemplate { get => GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<WideNavigationRailItem, bool>(nameof(IsSelected), false);
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    public static readonly StyledProperty<NavigationItemIconPosition?> IconPositionProperty = AvaloniaProperty.Register<WideNavigationRailItem, NavigationItemIconPosition?>(nameof(IconPosition), null);
    public NavigationItemIconPosition? IconPosition { get => GetValue(IconPositionProperty); set => SetValue(IconPositionProperty, value); }

    public static readonly StyledProperty<Thickness?> IndicatorPaddingProperty = AvaloniaProperty.Register<WideNavigationRailItem, Thickness?>(nameof(IndicatorPadding), null);
    public Thickness? IndicatorPadding { get => GetValue(IndicatorPaddingProperty); set => SetValue(IndicatorPaddingProperty, value); }

    public static readonly StyledProperty<Thickness> CollapsedIndicatorPaddingProperty = AvaloniaProperty.Register<WideNavigationRailItem, Thickness>(nameof(CollapsedIndicatorPadding));
    public Thickness CollapsedIndicatorPadding { get => GetValue(CollapsedIndicatorPaddingProperty); set => SetValue(CollapsedIndicatorPaddingProperty, value); }

    public static readonly StyledProperty<Thickness> ExpandedIndicatorPaddingProperty = AvaloniaProperty.Register<WideNavigationRailItem, Thickness>(nameof(ExpandedIndicatorPadding));
    public Thickness ExpandedIndicatorPadding { get => GetValue(ExpandedIndicatorPaddingProperty); set => SetValue(ExpandedIndicatorPaddingProperty, value); }

    public static readonly StyledProperty<Color> SelectedIconColorProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(SelectedIconColor), default);
    public Color SelectedIconColor { get => GetValue(SelectedIconColorProperty); set => SetValue(SelectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> SelectedTextColorTopIconPositionProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(SelectedTextColorTopIconPosition), default);
    public Color SelectedTextColorTopIconPosition { get => GetValue(SelectedTextColorTopIconPositionProperty); set => SetValue(SelectedTextColorTopIconPositionProperty, value); }

    public static readonly StyledProperty<Color> SelectedTextColorStartIconPositionProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(SelectedTextColorStartIconPosition), default);
    public Color SelectedTextColorStartIconPosition { get => GetValue(SelectedTextColorStartIconPositionProperty); set => SetValue(SelectedTextColorStartIconPositionProperty, value); }

    public static readonly StyledProperty<Color> IndicatorColorProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(IndicatorColor), default);
    public Color IndicatorColor { get => GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedIconColorProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(UnselectedIconColor), default);
    public Color UnselectedIconColor { get => GetValue(UnselectedIconColorProperty); set => SetValue(UnselectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedTextColorProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(UnselectedTextColor), default);
    public Color UnselectedTextColor { get => GetValue(UnselectedTextColorProperty); set => SetValue(UnselectedTextColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledIconColorProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(DisabledIconColor), default);
    public Color DisabledIconColor { get => GetValue(DisabledIconColorProperty); set => SetValue(DisabledIconColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledTextColorProperty = AvaloniaProperty.Register<WideNavigationRailItem, Color>(nameof(DisabledTextColor), default);
    public Color DisabledTextColor { get => GetValue(DisabledTextColorProperty); set => SetValue(DisabledTextColorProperty, value); }
    public static readonly StyledProperty<bool> RailExpandedProperty =
        AvaloniaProperty.Register<WideNavigationRailItem, bool>(nameof(RailExpanded), false, inherits: true);
    public bool RailExpanded { get => GetValue(RailExpandedProperty); set => SetValue(RailExpandedProperty, value); }
    private static readonly StyledProperty<bool> RailExpandedOwnerProperty = RailExpandedProperty.AddOwner<WideNavigationRail>();
    internal NavigationItemIconPosition EffectiveIconPosition => IconPosition ?? (RailExpanded ? NavigationItemIconPosition.Start : NavigationItemIconPosition.Top);

    private AnimatedState<float>? _selection, _position;
    private AnimatedState<Color>? _textColor;
    private WideNavigationRailItemLayout? _layout;
    private ContentPresenter? _icon, _label;
    private Border? _indicator;
    private MaterialSurface? _ripple;
    private bool _indicationTop;
    private WideNavigationRailItemAutomationPeer? _peer;
    private bool _suppressKeyboardClick;
    private Key? _activationKey;

    public WideNavigationRailItem()
    {
        // NavigationItem.kt's mapped source exists only for target Top, and uses the
        // actual item width plus the fixed 12dp vertical delta, unlike the regular rail.
        MaterialInteraction.GetOrCreate(this).PressPositionMapper = origin =>
        {
            if (EffectiveIconPosition != NavigationItemIconPosition.Top) return origin;
            var density = (float)LayoutHelper.GetLayoutScale(this);
            if (_layout is null) return origin;
            var indicatorWidth = MathUtils.DipToPx((float)_layout.ActiveIndicatorWidth, density) / density;
            return new Point(origin.X - (Bounds.Width - indicatorWidth) / 2, origin.Y - 12);
        };
        LostFocus += (_, _) => _activationKey = null;
    }
    protected override Type StyleKeyOverride => typeof(WideNavigationRailItem);
    private Color IconTarget => !IsEffectivelyEnabled ? DisabledIconColor : IsSelected ? SelectedIconColor : UnselectedIconColor;
    private Color TextTarget => !IsEffectivelyEnabled ? DisabledTextColor : !IsSelected ? UnselectedTextColor
        : EffectiveIconPosition == NavigationItemIconPosition.Top ? SelectedTextColorTopIconPosition : SelectedTextColorStartIconPosition;

    private void MotionConfigurationChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == WideNavigationRailItemLayout.SpatialAnimationProperty || change.Property == WideNavigationRailItemLayout.EffectsAnimationProperty)
            UpdateTargets();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        ReleaseMotion();
        base.OnApplyTemplate(e);
        if (_layout is not null) _layout.PropertyChanged -= MotionConfigurationChanged;
        _layout = e.NameScope.Find<WideNavigationRailItemLayout>("PART_Layout");
        if (_layout is not null) _layout.PropertyChanged += MotionConfigurationChanged;
        _icon = e.NameScope.Find<ContentPresenter>("PART_Icon");
        _label = e.NameScope.Find<ContentPresenter>("PART_Label");
        _indicator = e.NameScope.Find<Border>("PART_Indicator");
        _ripple = e.NameScope.Find<MaterialSurface>("PART_IndicatorRipple");
        _indicationTop = EffectiveIconPosition == NavigationItemIconPosition.Top;
        UpdateLayoutInputs();
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
        if (this.FindLogicalAncestorOfType<ModalWideNavigationRail>() is { IsMovingContent: true })
        {
            base.OnDetachedFromVisualTree(e);
            return;
        }
        ReleaseMotion();
        _activationKey = null;
        if (_layout is not null) _layout.PropertyChanged -= MotionConfigurationChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopIconLabelTextStyleProperty || change.Property == StartIconLabelTextStyleProperty) UpdateLabelStyle();
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", IsSelected);
            _peer?.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
        }
        if (change.Property == LabelProperty || change.Property == IconPositionProperty || change.Property == RailExpandedProperty
            || change.Property == IndicatorPaddingProperty || change.Property == CollapsedIndicatorPaddingProperty || change.Property == ExpandedIndicatorPaddingProperty)
            UpdateLayoutInputs();
        if (change.Property == IndicatorColorProperty && _indicator is not null) _indicator.Background = new SolidColorBrush(IndicatorColor);
        if (change.Property == IsSelectedProperty || change.Property == IsEffectivelyEnabledProperty
            || change.Property == RailExpandedProperty || change.Property == IconPositionProperty
            || change.Property == SelectedIconColorProperty || change.Property == SelectedTextColorTopIconPositionProperty
            || change.Property == SelectedTextColorStartIconPositionProperty || change.Property == UnselectedIconColorProperty
            || change.Property == UnselectedTextColorProperty || change.Property == DisabledIconColorProperty || change.Property == DisabledTextColorProperty)
            UpdateTargets();
    }
    private void UpdateLayoutInputs()
    {
        if (_layout is null) return;
        var indicationTop = EffectiveIconPosition == NavigationItemIconPosition.Top;
        if (_ripple is not null && indicationTop != _indicationTop)
        {
            // AnimatedNavigationItem switches between MappedInteractionSource and the
            // original source. Modifier.indication recreates its node, dropping its
            // collected feedback without changing the selectable's active input.
            // Reuse Surface's existing detach/attach lease instead of a second ripple.
            _layout.Children.Remove(_ripple);
            _layout.Children.Add(_ripple);
        }
        _indicationTop = indicationTop;
        _layout.HasLabel = Label is not null;
        _layout.IconPosition = EffectiveIconPosition;
        _layout.IndicatorPadding = IndicatorPadding;
        _layout.CollapsedIndicatorPadding = CollapsedIndicatorPadding;
        _layout.ExpandedIndicatorPadding = ExpandedIndicatorPadding;
        if (_indicator is not null) _indicator.Background = new SolidColorBrush(IndicatorColor);
        UpdateLabelStyle();
        _peer?.RefreshChildren();
    }
    private void InitializeMotion()
    {
        if (_layout is null || _selection is not null || !this.IsAttachedToVisualTree()) return;
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return; // A parent's detach can transfer the presenter before child detach notifications.
        var selected = IsSelected ? 1f : 0f;
        var position = EffectiveIconPosition == NavigationItemIconPosition.Top ? 0f : 1f;
        var text = TextTarget;
        // movableContentOf keeps one animation identity across the host transfer.
        // A window-root state survives that visual detach; a real detach still disposes it.
        // Hidden content is not moving, so it gets a new identity at the current targets.
        if (this.FindLogicalAncestorOfType<ModalWideNavigationRail>() is not null)
        {
            var root = AnimationScope.GetRoot(topLevel);
            _selection = AnimatedState.Create(root, selected, label: "Wide rail selection");
            _position = AnimatedState.Create(root, position, label: "Wide rail icon position");
            _textColor = AnimatedState.Create(root, text, label: "Wide rail text color");
        }
        else
        {
            _selection = AnimatedState.Create(this, selected, label: "Wide rail selection");
            _position = AnimatedState.Create(this, position, label: "Wide rail icon position");
            _textColor = AnimatedState.Create(this, text, label: "Wide rail text color");
        }
        _selection.ValueChanged += OnSelection;
        _position.ValueChanged += OnPosition;
        _textColor.ValueChanged += OnTextColor;
        if (_icon is not null) _icon.Foreground = new SolidColorBrush(IconTarget);
        OnSelection(selected);
        OnPosition(position);
        OnTextColor(text);
    }
    private void ReleaseMotion()
    {
        _selection?.Dispose();
        _position?.Dispose();
        _textColor?.Dispose();
        _selection = _position = null;
        _textColor = null;
    }
    private void OnSelection(float value)
    {
        if (_layout is not null) _layout.SelectionProgress = value;
        if (_indicator is not null) _indicator.Opacity = Math.Clamp(value, 0, 1);
    }
    private void OnPosition(float value)
    {
        if (_layout is not null) _layout.IconPositionProgress = value;
        UpdateLabelStyle();
    }
    private void OnTextColor(Color value)
    {
        if (_label is not null) _label.Foreground = new SolidColorBrush(value);
    }
    private void UpdateTargets()
    {
        if (_icon is not null) _icon.Foreground = new SolidColorBrush(IconTarget); // Upstream iconColor does not animate.
        if (_selection is null || _position is null || _textColor is null || _layout is null) return;
        var spatial = _layout.SpatialAnimation;
        var effects = _layout.EffectsAnimation;
        if (spatial.Stiffness <= 0 || effects.Stiffness <= 0) return;
        const float threshold = 0.01f;
        var spatialSpec = new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, threshold);
        _selection.UpdateTarget(IsSelected ? 1f : 0f, spatialSpec);
        _position.UpdateTarget(EffectiveIconPosition == NavigationItemIconPosition.Top ? 0f : 1f,
            new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, threshold));
        _textColor.UpdateTarget(TextTarget, new SpringSpec<Color>(effects.DampingRatio, effects.Stiffness));
        UpdateLabelStyle();
    }
    private void UpdateLabelStyle()
    {
        if (_label is null) return;
        var style = EffectiveIconPosition == NavigationItemIconPosition.Top && (_position?.Value ?? 0) < 0.5f
            ? TopIconLabelTextStyle : StartIconLabelTextStyle;
        _label.SetCurrentValue(MaterialText.StyleProperty, style);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Same approved selectable adaptation as regular NavigationRailItem: click on
        // key-up for both activation keys, preserving Button's pressed state.
        if (e.Key is Key.Space or Key.Enter && IsFocused && IsEffectivelyEnabled)
        {
            _activationKey ??= e.Key; _suppressKeyboardClick = true;
            try { base.OnKeyDown(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true; return;
        }
        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Space or Key.Enter)
        {
            var invoke = _activationKey == e.Key && IsFocused && IsEffectivelyEnabled;
            _activationKey = null; _suppressKeyboardClick = true;
            try { base.OnKeyUp(new KeyEventArgs { Key = Key.Space, KeyModifiers = e.KeyModifiers, Source = this }); }
            finally { _suppressKeyboardClick = false; }
            e.Handled = true;
            if (invoke) OnClick();
            return;
        }
        base.OnKeyUp(e);
    }
    protected override void OnClick() { if (!_suppressKeyboardClick) base.OnClick(); }
    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new WideNavigationRailItemAutomationPeer(this);

    private sealed class WideNavigationRailItemAutomationPeer(WideNavigationRailItem owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
    {
        public void RefreshChildren() => InvalidateChildren();
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.GetLogicalAncestors().OfType<WideNavigationRail>().FirstOrDefault() is { } rail
            ? ControlAutomationPeer.CreatePeerForElement(rail) as ISelectionProvider : null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        public void RemoveFromSelection() { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
        protected override string GetClassNameCore() => nameof(WideNavigationRailItem);
        protected override string? GetNameCore() => !string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? base.GetNameCore()
            : owner.Label as string ?? base.GetNameCore();
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() =>
            new[] { owner.Label is not null ? null : owner._icon, owner.Label is not null ? owner._label : null }
                .OfType<ContentPresenter>().SelectMany(presenter => ControlAutomationPeer.CreatePeerForElement(presenter)?.GetChildren() ?? []).ToArray();
    }
}


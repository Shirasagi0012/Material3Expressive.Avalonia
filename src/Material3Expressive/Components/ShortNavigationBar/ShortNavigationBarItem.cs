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

namespace Material3Expressive.Components;

/// <summary>A short bar destination. Labels remain visible; selection belongs to the application.</summary>
public class ShortNavigationBarItem : Button
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<ShortNavigationBarItem, object?>(nameof(Icon), null);
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<ShortNavigationBarItem, IDataTemplate?>(nameof(IconTemplate), null);
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }

    public static readonly StyledProperty<object?> LabelProperty = AvaloniaProperty.Register<ShortNavigationBarItem, object?>(nameof(Label), null);
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty = AvaloniaProperty.Register<ShortNavigationBarItem, IDataTemplate?>(nameof(LabelTemplate), null);
    public IDataTemplate? LabelTemplate { get => GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }

    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ShortNavigationBarItem, bool>(nameof(IsSelected), false);
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    public static readonly StyledProperty<NavigationItemIconPosition> IconPositionProperty = AvaloniaProperty.Register<ShortNavigationBarItem, NavigationItemIconPosition>(nameof(IconPosition), NavigationItemIconPosition.Top);
    public NavigationItemIconPosition IconPosition { get => GetValue(IconPositionProperty); set => SetValue(IconPositionProperty, value); }

    public static readonly StyledProperty<Color> SelectedIconColorProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(SelectedIconColor), default);
    public Color SelectedIconColor { get => GetValue(SelectedIconColorProperty); set => SetValue(SelectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> SelectedTextColorTopIconPositionProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(SelectedTextColorTopIconPosition), default);
    public Color SelectedTextColorTopIconPosition { get => GetValue(SelectedTextColorTopIconPositionProperty); set => SetValue(SelectedTextColorTopIconPositionProperty, value); }

    public static readonly StyledProperty<Color> SelectedTextColorStartIconPositionProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(SelectedTextColorStartIconPosition), default);
    public Color SelectedTextColorStartIconPosition { get => GetValue(SelectedTextColorStartIconPositionProperty); set => SetValue(SelectedTextColorStartIconPositionProperty, value); }

    public static readonly StyledProperty<Color> IndicatorColorProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(IndicatorColor), default);
    public Color IndicatorColor { get => GetValue(IndicatorColorProperty); set => SetValue(IndicatorColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedIconColorProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(UnselectedIconColor), default);
    public Color UnselectedIconColor { get => GetValue(UnselectedIconColorProperty); set => SetValue(UnselectedIconColorProperty, value); }

    public static readonly StyledProperty<Color> UnselectedTextColorProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(UnselectedTextColor), default);
    public Color UnselectedTextColor { get => GetValue(UnselectedTextColorProperty); set => SetValue(UnselectedTextColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledIconColorProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(DisabledIconColor), default);
    public Color DisabledIconColor { get => GetValue(DisabledIconColorProperty); set => SetValue(DisabledIconColorProperty, value); }

    public static readonly StyledProperty<Color> DisabledTextColorProperty = AvaloniaProperty.Register<ShortNavigationBarItem, Color>(nameof(DisabledTextColor), default);
    public Color DisabledTextColor { get => GetValue(DisabledTextColorProperty); set => SetValue(DisabledTextColorProperty, value); }
    private Animatable<float>? _selection;
    private ShortNavigationBarItemLayout? _layout;
    private ContentPresenter? _icon, _label;
    private Border? _indicator;
    private MaterialSurface? _ripple;
    private ShortNavigationBarItemAutomationPeer? _peer;
    private bool _pending, _suppressKeyboardClick;
    private NavigationItemIconPosition _indicationPosition;
    private Key? _activationKey;

    public ShortNavigationBarItem()
    {
        MaterialInteraction.GetOrCreate(this).PressPositionMapper = origin =>
        {
            if (IconPosition != NavigationItemIconPosition.Top) return origin;
            var scale = LayoutHelper.GetLayoutScale(this);
            if (_layout is null) return origin;
            var indicatorWidth = MathUtils.DipToPx((float)_layout.IndicatorWidth, (float)scale);
            var itemWidth = MathUtils.DipToPx((float)Bounds.Width, (float)scale);
            return new Point(origin.X - (itemWidth - indicatorWidth) / (2 * scale), origin.Y - 12);
        };
        LostFocus += (_, _) => _activationKey = null;
    }
    protected override Type StyleKeyOverride => typeof(ShortNavigationBarItem);
    private Color IconTarget => !IsEffectivelyEnabled ? DisabledIconColor : IsSelected ? SelectedIconColor : UnselectedIconColor;
    private Color TextTarget => !IsEffectivelyEnabled ? DisabledTextColor : !IsSelected ? UnselectedTextColor
        : IconPosition == NavigationItemIconPosition.Top ? SelectedTextColorTopIconPosition : SelectedTextColorStartIconPosition;

    private void MotionConfigurationChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property == ShortNavigationBarItemLayout.IndicatorAnimationProperty)
            UpdateTarget();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _selection = null; _pending = false;
        base.OnApplyTemplate(e);
        if (_layout is not null) _layout.PropertyChanged -= MotionConfigurationChanged;
        _layout = e.NameScope.Find<ShortNavigationBarItemLayout>("PART_Layout");
        if (_layout is not null) _layout.PropertyChanged += MotionConfigurationChanged;
        _icon = e.NameScope.Find<ContentPresenter>("PART_Icon");
        _label = e.NameScope.Find<ContentPresenter>("PART_Label");
        _indicator = e.NameScope.Find<Border>("PART_Indicator");
        _ripple = e.NameScope.Find<MaterialSurface>("PART_IndicatorRipple");
        _indicationPosition = IconPosition;
        UpdateContent(); InitializeMotion();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (_layout is not null) { _layout.PropertyChanged -= MotionConfigurationChanged; _layout.PropertyChanged += MotionConfigurationChanged; } InitializeMotion();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _selection = null; _pending = false; _activationKey = null;
        if (_layout is not null) _layout.PropertyChanged -= MotionConfigurationChanged;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty)
        {
            PseudoClasses.Set(":selected", IsSelected);
            _peer?.RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, change.OldValue, change.NewValue);
            UpdateTarget();
        }
        if (change.Property == LabelProperty || change.Property == IconPositionProperty)
        {
            // Switching MappedInteractionSource recreates Compose's indication node.
            if (_layout is not null && _ripple is not null && _indicationPosition != IconPosition)
            {
                _layout.Children.Remove(_ripple); _layout.Children.Add(_ripple);
            }
            _indicationPosition = IconPosition;
            _peer?.RefreshChildren();
        }
        if (change.Property == IconProperty || change.Property == IconTemplateProperty || change.Property == LabelProperty
            || change.Property == LabelTemplateProperty || change.Property == IconPositionProperty)
            this.GetVisualAncestors().OfType<ShortNavigationBarItemsPanel>().FirstOrDefault()?.InvalidateMeasure();
        if (change.Property == IsSelectedProperty || change.Property == IsEffectivelyEnabledProperty
            || change.Property == IconPositionProperty || change.Property == SelectedIconColorProperty
            || change.Property == SelectedTextColorTopIconPositionProperty || change.Property == SelectedTextColorStartIconPositionProperty
            || change.Property == UnselectedIconColorProperty || change.Property == UnselectedTextColorProperty
            || change.Property == DisabledIconColorProperty || change.Property == DisabledTextColorProperty
            || change.Property == IndicatorColorProperty) UpdateContent();
    }
    private void UpdateContent()
    {
        if (_icon is not null) _icon.Foreground = new SolidColorBrush(IconTarget);
        if (_label is not null) _label.Foreground = new SolidColorBrush(TextTarget);
        if (_indicator is not null) _indicator.Background = new SolidColorBrush(IndicatorColor);
    }
    private void InitializeMotion()
    {
        if (_layout is null || _selection is not null || !this.IsAttachedToVisualTree()) return;
        var scope = AnimationScope.Get(this);
        _selection = new Animatable<float>(new FloatConverter(), IsSelected ? 1f : 0f, VisibilityThresholds.Float);
        scope.SubscribeValue(_selection, value =>
        {
            _layout.SelectionProgress = value;
            if (_indicator is not null) _indicator.Opacity = Math.Clamp(value, 0, 1);
        });
    }
    private void UpdateTarget()
    {
        if (!this.IsAttachedToVisualTree() || _selection is null || _pending) return;
        var motion = AnimationScope.Get(this);
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct); _pending = false;
            if (_selection is not { } selection) return;
            var target = IsSelected ? 1f : 0f;
            if (selection.TargetValue == target) return;
            var spatial = _layout!.IndicatorAnimation;
            if (spatial.Stiffness <= 0) return;
            _ = motion.Launch(async lifetime =>
            {
                await selection.StopAsync(); lifetime.ThrowIfCancellationRequested();
                await selection.AnimateToAsync(motion, target, new SpringSpec<float>(spatial.DampingRatio, spatial.Stiffness, 0.01f), cancellationToken: lifetime);
            }, "Short navigation item indicator");
        }, "Short navigation item target");
    }

    // Compose's intrinsic queries have no Avalonia equivalent. Only this component's
    // content slots are measured here; final allocation still uses ordinary Measure.
    internal int IntrinsicHeight(int width, double scale)
    {
        ApplyTemplate();
        return _layout?.IntrinsicHeight(width, scale) ?? MathUtils.DipToPx(DesiredSize.Height, scale);
    }
    internal int IntrinsicWidth(int height, double scale)
    {
        ApplyTemplate();
        return _layout?.IntrinsicWidth(height, scale) ?? MathUtils.DipToPx(DesiredSize.Width, scale);
    }
    internal void Allocate(Size size)
    {
        ApplyTemplate();
        if (_layout is not null && _layout.MinimumSize != size)
        {
            _layout.MinimumSize = size; _layout.InvalidateMeasure(); InvalidateMeasure();
        }
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
    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new ShortNavigationBarItemAutomationPeer(this);

    private sealed class ShortNavigationBarItemAutomationPeer(ShortNavigationBarItem owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
    {
        public void RefreshChildren() => InvalidateChildren();
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.GetLogicalAncestors().OfType<ShortNavigationBar>().FirstOrDefault() is { } rail
            ? ControlAutomationPeer.CreatePeerForElement(rail) as ISelectionProvider : null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        public void RemoveFromSelection() { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;
        protected override string GetClassNameCore() => nameof(ShortNavigationBarItem);
        protected override string? GetNameCore() => !string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? base.GetNameCore()
            : owner.Label as string ?? base.GetNameCore();
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() =>
            new[] { owner._icon, owner.Label is not null ? owner._label : null }
                .OfType<ContentPresenter>().SelectMany(presenter => ControlAutomationPeer.CreatePeerForElement(presenter)?.GetChildren() ?? []).ToArray();
    }
}


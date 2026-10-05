using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>Single-choice selectable or multi-choice toggleable segment, with external selection.</summary>
public sealed class SegmentedButton : Button
{
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<SegmentedButton, bool>(nameof(IsSelected));
    public static readonly StyledProperty<bool> ShowSelectionIconProperty = AvaloniaProperty.Register<SegmentedButton, bool>(nameof(ShowSelectionIcon), true);
    public static readonly StyledProperty<object?> ActiveIconProperty = AvaloniaProperty.Register<SegmentedButton, object?>(nameof(ActiveIcon));
    public static readonly StyledProperty<object?> InactiveIconProperty = AvaloniaProperty.Register<SegmentedButton, object?>(nameof(InactiveIcon));
    public static readonly StyledProperty<IDataTemplate?> ActiveIconTemplateProperty = AvaloniaProperty.Register<SegmentedButton, IDataTemplate?>(nameof(ActiveIconTemplate));
    public static readonly StyledProperty<IDataTemplate?> InactiveIconTemplateProperty = AvaloniaProperty.Register<SegmentedButton, IDataTemplate?>(nameof(InactiveIconTemplate));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool ShowSelectionIcon { get => GetValue(ShowSelectionIconProperty); set => SetValue(ShowSelectionIconProperty, value); }
    /// <summary>Null uses the default vector checkmark.</summary>
    public object? ActiveIcon { get => GetValue(ActiveIconProperty); set => SetValue(ActiveIconProperty, value); }
    public object? InactiveIcon { get => GetValue(InactiveIconProperty); set => SetValue(InactiveIconProperty, value); }
    public IDataTemplate? ActiveIconTemplate { get => GetValue(ActiveIconTemplateProperty); set => SetValue(ActiveIconTemplateProperty, value); }
    public IDataTemplate? InactiveIconTemplate { get => GetValue(InactiveIconTemplateProperty); set => SetValue(InactiveIconTemplateProperty, value); }
    internal static readonly StyledProperty<CornerRadius> RowCornerRadiusProperty = AvaloniaProperty.Register<SegmentedButton, CornerRadius>(nameof(RowCornerRadius));
    internal CornerRadius RowCornerRadius { get => GetValue(RowCornerRadiusProperty); set => SetValue(RowCornerRadiusProperty, value); }
    private SegmentedButtonContentLayout? _layout;
    private SegmentedButtonIconSlot? _icon;
    private MaterialSurface? _surface;
    private Key? _activationKey;
    private bool _suppressKeyboardClick;
    internal SegmentedButtonRow? Row => this.GetVisualAncestors().OfType<SegmentedButtonRow>().FirstOrDefault();
    public SegmentedButton()
    {
        MaterialInteraction.GetOrCreate(this).Changed += RefreshZIndex;
        LostFocus += (_, _) => _activationKey = null;
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _layout?.Stop(); _icon?.Stop();
        if (_surface is not null) _surface.PropertyChanged -= SurfaceChanged;
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<SegmentedButtonContentLayout>("PART_Layout");
        _icon = e.NameScope.Find<SegmentedButtonIconSlot>("PART_Icon");
        _surface = e.NameScope.Find<MaterialSurface>("PART_Container");
        if (_surface is not null) _surface.PropertyChanged += SurfaceChanged;
        UpdateSurfaceCorners();
        Refresh();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); Refresh();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _activationKey = null; base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsSelectedProperty) PseudoClasses.Set(":selected", IsSelected);
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) _activationKey = null;
        if (change.Property == TemplateProperty)
        {
            _layout?.Stop(); _icon?.Stop(); _layout = null; _icon = null;
            if (_surface is not null) _surface.PropertyChanged -= SurfaceChanged;
            _surface = null;
        }
        if (change.Property == CornerRadiusProperty) UpdateSurfaceCorners();
        if (change.Property == IsSelectedProperty || change.Property == IsEffectivelyEnabledProperty
            || change.Property == ActiveIconProperty || change.Property == InactiveIconProperty || change.Property == ShowSelectionIconProperty
            || change.Property == ActiveIconTemplateProperty || change.Property == InactiveIconTemplateProperty || change.Property == FlowDirectionProperty) Refresh();
        if (change.Property == IsSelectedProperty || change.Property == ContentProperty || change.Property == ContentTemplateProperty
            || change.Property == ActiveIconProperty || change.Property == InactiveIconProperty || change.Property == ShowSelectionIconProperty
            || change.Property == ActiveIconTemplateProperty || change.Property == InactiveIconTemplateProperty || change.Property == PaddingProperty)
            Row?.InvalidateItemsLayout();
    }
    private void SurfaceChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty) UpdateSurfaceCorners();
    }
    private void UpdateSurfaceCorners()
    {
        if (_surface is null) return;
        // CornerBasedShape normalizes each vertical pair against min(width,height).
        // Avalonia's asymmetric Border corners instead normalize against the axes.
        var c = CornerRadius; var extent = (float)Math.Min(_surface.Bounds.Width, _surface.Bounds.Height);
        var left = (float)(c.TopLeft + c.BottomLeft); var right = (float)(c.TopRight + c.BottomRight);
        var leftScale = left > extent ? extent / left : 1f; var rightScale = right > extent ? extent / right : 1f;
        _surface.CornerRadius = new((float)c.TopLeft * leftScale, (float)c.TopRight * rightScale,
            (float)c.BottomRight * rightScale, (float)c.BottomLeft * leftScale);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        // Compose's desktop clickable invokes on release for both activation keys.
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
    private void Refresh()
    {
        RefreshZIndex();
        if (_icon is not null && this.IsAttachedToVisualTree()) _icon.Update(this);
    }
    private void RefreshZIndex()
    {
        var interaction = MaterialInteraction.GetOrCreate(this).Interaction;
        var z = (IsSelected ? 5 : 0) + (interaction.HasFlag(InteractionState.Pressed) ? 1 : 0) + (interaction.HasFlag(InteractionState.Focused) ? 1 : 0);
        // ItemsControl can wrap templated data in a ContentPresenter. The sibling is the drawing layer.
        if (this.GetVisualParent() is Avalonia.Controls.Presenters.ContentPresenter presenter) presenter.ZIndex = z;
        else ZIndex = z;
    }
    internal Size IntrinsicSize(double height)
    {
        ApplyTemplate();
        return _layout?.IntrinsicSize(height, Padding) ?? default;
    }
    protected override AutomationPeer OnCreateAutomationPeer() => Row is SingleChoiceSegmentedButtonRow
        ? new SingleChoicePeer(this) : new MultiChoicePeer(this);
    private sealed class SingleChoicePeer : ButtonAutomationPeer, ISelectionItemProvider
    {
        private readonly SegmentedButton owner;
        public SingleChoicePeer(SegmentedButton owner) : base(owner)
        {
            this.owner = owner;
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsSelectedProperty) RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty,
                    change.OldValue, change.NewValue);
            };
        }
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => owner.Row is { } row ? ControlAutomationPeer.CreatePeerForElement(row) as ISelectionProvider : null;
        public void Select() => Invoke();
        public void AddToSelection() => Invoke();
        public void RemoveFromSelection() { }
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.RadioButton;
    }
    private sealed class MultiChoicePeer : ButtonAutomationPeer, IToggleProvider
    {
        public MultiChoicePeer(SegmentedButton owner) : base(owner)
        {
            owner.PropertyChanged += (_, change) =>
            {
                if (change.Property == IsSelectedProperty) RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                    (bool)change.OldValue! ? ToggleState.On : ToggleState.Off, (bool)change.NewValue! ? ToggleState.On : ToggleState.Off);
            };
        }
        public ToggleState ToggleState => ((SegmentedButton)Owner).IsSelected ? ToggleState.On : ToggleState.Off;
        public void Toggle() => Invoke();
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.CheckBox;
    }
}

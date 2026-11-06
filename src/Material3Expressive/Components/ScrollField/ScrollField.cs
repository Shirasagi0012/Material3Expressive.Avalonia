using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Input;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>A cyclic, vertically paged selection field. Give the field a finite height.</summary>
public partial class ScrollField : TemplatedControl
{
    public static readonly StyledProperty<ScrollFieldState?> StateProperty = AvaloniaProperty.Register<ScrollField, ScrollFieldState?>(nameof(State));
    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty = AvaloniaProperty.Register<ScrollField, IDataTemplate?>(nameof(ItemTemplate));
    public static readonly StyledProperty<Func<int, string>?> OptionDescriptionProperty = AvaloniaProperty.Register<ScrollField, Func<int, string>?>(nameof(OptionDescription));
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = AvaloniaProperty.Register<ScrollField, MaterialInteractionSource?>(nameof(InteractionSource));
    public static readonly StyledProperty<Color> SelectedContentColorProperty = AvaloniaProperty.Register<ScrollField, Color>(nameof(SelectedContentColor));
    public static readonly StyledProperty<Color> UnselectedContentColorProperty = AvaloniaProperty.Register<ScrollField, Color>(nameof(UnselectedContentColor));
    internal static readonly StyledProperty<double> ItemHeightProperty = AvaloniaProperty.Register<ScrollField, double>(nameof(ItemHeight));
    internal double ItemHeight { get => GetValue(ItemHeightProperty); set => SetValue(ItemHeightProperty, value); }
    public ScrollFieldState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    /// <summary>Receives <see cref="ScrollFieldItem"/>. Null renders an animated, two-digit number.</summary>
    public IDataTemplate? ItemTemplate { get => GetValue(ItemTemplateProperty); set => SetValue(ItemTemplateProperty, value); }
    /// <summary>Accessible description of each option; defaults to the localized numeric index.</summary>
    public Func<int, string>? OptionDescription { get => GetValue(OptionDescriptionProperty); set => SetValue(OptionDescriptionProperty, value); }
    /// <summary>Focus interactions of the field. Page clicks and pager drags have independent sources.</summary>
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    public Color SelectedContentColor { get => GetValue(SelectedContentColorProperty); set => SetValue(SelectedContentColorProperty, value); }
    public Color UnselectedContentColor { get => GetValue(UnselectedContentColorProperty); set => SetValue(UnselectedContentColorProperty, value); }
    private ScrollFieldPanel? _panel;
    private readonly MaterialInteractionSource _localSource = new();
    private MaterialInteractionEvent.Focus? _focus;
    private MaterialInteractionSource? _focusSource;
    private readonly VelocityTracker _velocity = new();
    private IPointer? _pointer;
    private Point _down, _last;
    private bool _dragging;
    private bool _takingCapture;
    private ScrollFieldWheel? _wheel;

    public ScrollField()
    {
        AddHandler(PointerPressedEvent, PointerDown, RoutingStrategies.Tunnel);
        // Claim the scroll before the clicked page cancels its own press on leaving its bounds.
        AddHandler(PointerMovedEvent, PointerMove, RoutingStrategies.Tunnel, true);
        AddHandler(PointerReleasedEvent, PointerUp, RoutingStrategies.Tunnel);
        AddHandler(Thumb.DragStartedEvent, (_, _) => CancelPointer(), RoutingStrategies.Bubble, true);
        AddHandler(PointerCaptureLostEvent, (_, e) => { if (!_takingCapture && e.Source == this && _pointer == e.Pointer) CancelPointer(settle: true); });
        AddHandler(PointerWheelChangedEvent, (_, e) =>
        {
            if (e.Handled || (e.Route == RoutingStrategies.Tunnel) != (_wheel?.IsReceiving == true)) return;
            e.Handled = (_wheel ??= new(this)).Receive(e);
        }, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_panel is not null) _panel.Owner = null;
        base.OnApplyTemplate(e);
        _panel = e.NameScope.Find<ScrollFieldPanel>("PART_Pages");
        if (_panel is not null) _panel.Owner = this;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        AttachState(State);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelPointer(); EndFocus(); DetachState(State);
        base.OnDetachedFromVisualTree(e);
    }
    private void AttachState(ScrollFieldState? state)
    {
        if (state is null) return;
        state.Attach(this); state.LayoutChanged += InvalidatePages;
        InvalidatePages();
    }
    private void DetachState(ScrollFieldState? state)
    {
        if (state is null) return;
        state.LayoutChanged -= InvalidatePages; state.Detach(this);
    }
    internal event Action? SelectionChanged;
    private void InvalidatePages() { _panel?.InvalidateMeasure(); SelectionChanged?.Invoke(); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty && this.IsAttachedToVisualTree())
        {
            CancelPointer(); DetachState(change.GetOldValue<ScrollFieldState?>());
            _panel?.ClearPages(); AttachState(State);
        }
        if (change.Property == ItemTemplateProperty) _panel?.ClearPages();
        if (change.Property == ItemHeightProperty || change.Property == ItemTemplateProperty ||
            change.Property == SelectedContentColorProperty || change.Property == UnselectedContentColorProperty) InvalidatePages();
        if (change.Property == InteractionSourceProperty) { EndFocus(); if (IsFocused) BeginFocus(); }
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) { CancelPointer(); EndFocus(); }
    }
    protected override void OnGotFocus(FocusChangedEventArgs e) { base.OnGotFocus(e); if (IsFocused) BeginFocus(); }
    protected override void OnLostFocus(FocusChangedEventArgs e) { EndFocus(); base.OnLostFocus(e); }
    private void BeginFocus()
    {
        if (_focus is not null) return;
        _focusSource = InteractionSource ?? _localSource;
        _focusSource.Emit(_focus = new());
    }
    private void EndFocus()
    {
        var focus = _focus; var source = _focusSource; _focus = null; _focusSource = null;
        if (focus is not null) source?.Emit(new MaterialInteractionEvent.Unfocus(focus));
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && State is { } state && e.Key is Key.Up or Key.Down)
        {
            AnimatePage(state.TargetPage + (e.Key == Key.Down ? 1 : -1)); e.Handled = true;
        }
        base.OnKeyDown(e);
    }
    internal void AnimatePage(int page)
    {
        if (State is not { } state || !this.IsAttachedToVisualTree()) return;
        _ = AnimationScope.Get(this).Launch(async token =>
        {
            try { await state.AnimateToPageAsync(page, token); }
            catch (OperationCanceledException) { }
        }, "Scroll field selection");
    }
    private void PointerDown(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse || _pointer is not null || State is not { PageSize: > 0 } state || !IsEffectivelyEnabled) return;
        _pointer = e.Pointer; _down = _last = e.GetPosition(Avalonia.Controls.TopLevel.GetTopLevel(this));
        _velocity.Reset(); _velocity.Add(e.Timestamp, (float)_down.Y * state.Density);
        if (state.IsScrollInProgress) StartDrag(e);
    }
    private void StartDrag(PointerEventArgs e)
    {
        var state = State!;
        state.BeginDrag();
        // A state observer may replace the field or detach it during the notification.
        if (_pointer != e.Pointer || State != state || !this.IsAttachedToVisualTree() || !IsEffectivelyEnabled) return;
        _dragging = true;
        // Transferring a pressed page's capture also notifies its ancestors of capture loss.
        _takingCapture = true;
        try { e.Pointer.Capture(this); }
        finally { _takingCapture = false; }
        e.Handled = true;
    }
    private void PointerMove(object? sender, PointerEventArgs e)
    {
        if (_pointer != e.Pointer || State is not { } state) return;
        var point = e.GetPosition(Avalonia.Controls.TopLevel.GetTopLevel(this));
        var total = point.Y - _down.Y;
        if (!_dragging && Math.Abs(total) <= 18) return;
        if (!_dragging)
        {
            StartDrag(e);
            if (!_dragging || _pointer != e.Pointer) return;
            _last = new(_down.X, _down.Y + Math.Sign(total) * 18);
        }
        // requestScrollToPage cancels the current scroll mutation, not the pointer gesture.
        // The next delta re-enters UserInput scrolling from the requested position.
        if (!state.IsScrollInProgress) state.BeginDrag();
        if (_pointer != e.Pointer || State != state || !this.IsAttachedToVisualTree() || !IsEffectivelyEnabled) return;
        _velocity.Add(e.Timestamp, (float)point.Y * state.Density);
        state.Consume((float)(_last.Y - point.Y) * state.Density); _last = point; e.Handled = true;
    }
    private void PointerUp(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer != e.Pointer || State is not { } state) return;
        var dragging = _dragging; _pointer = null; _dragging = false;
        if (!dragging) return;
        var delta = (float)(e.GetPosition(Avalonia.Controls.TopLevel.GetTopLevel(this)).Y - _down.Y) * state.Density;
        var velocity = -_velocity.Calculate(e.Timestamp);
        if (e.Pointer.Captured == this) e.Pointer.Capture(null);
        e.Handled = true;
        Settle(velocity, delta);
    }
    internal void CancelPointer(bool settle = false)
    {
        var pointer = _pointer; var dragging = _dragging;
        _pointer = null; _dragging = false;
        if (pointer?.Captured == this) pointer.Capture(null);
        if (dragging)
        {
            State?.CancelScroll();
            if (settle && this.IsAttachedToVisualTree()) Settle(0, 0);
        }
    }
    private void Settle(float velocity, float delta)
    {
        if (State is not { } state) return;
        _ = AnimationScope.Get(this).Launch(async token =>
        {
            try { await state.FlingAsync(velocity, delta, token); }
            catch (OperationCanceledException) { }
        }, "Scroll field release");
    }
    internal void SettleWheel(float velocity) => Settle(velocity, 0);
}

/// <summary>Data supplied to a field's item template. Selection changes retain the same item instance.</summary>
public sealed class ScrollFieldItem : AvaloniaObject
{
    public static readonly DirectProperty<ScrollFieldItem, bool> IsSelectedProperty = AvaloniaProperty.RegisterDirect<ScrollFieldItem, bool>(nameof(IsSelected), i => i.IsSelected);
    private bool _selected;
    internal ScrollFieldItem(int index, bool selected) { Index = index; _selected = selected; }
    public int Index { get; }
    public bool IsSelected => _selected;
    internal void SetSelected(bool selected) => SetAndRaise(IsSelectedProperty, ref _selected, selected);
}

internal sealed class ScrollFieldPanel : Panel
{
    internal ScrollField? Owner { get; set; }
    private readonly Dictionary<int, (ClickableSurface View, ScrollFieldItem Item)> _pages = [];
    internal void ClearPages() { Children.Clear(); _pages.Clear(); InvalidateMeasure(); }
    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is not { State: { } state } owner || owner.ItemHeight <= 0) { ClearPages(); return default; }
        if (!double.IsFinite(availableSize.Height)) throw new InvalidOperationException("ScrollField requires a finite height.");
        var density = state.Density;
        state.Configure(MathUtils.RoundToInt((float)owner.ItemHeight * density), MathUtils.DipToPx(availableSize.Height, density));
        var first = state.FirstVisiblePage; var last = state.LastVisiblePage;
        foreach (var page in _pages.Keys.Where(p => p < first || p > last).ToArray()) { Children.Remove(_pages[page].View); _pages.Remove(page); }
        for (var page = first; page <= last; page++)
        {
            if (!_pages.TryGetValue(page, out var entry))
            {
                var item = new ScrollFieldItem(page % state.ItemCount, page == state.CurrentPage);
                var view = new ScrollFieldPage { Content = item, ContentTemplate = owner.ItemTemplate,
                    Focusable = false, Background = Brushes.Transparent, HorizontalContentAlignment = HorizontalAlignment.Center,
                    VerticalContentAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
                MaterialInteractiveSize.SetMinimum(view, 0);
                if (owner.ItemTemplate is null) view.Content = new ScrollFieldNumber
                {
                    Index = item.Index,
                    [!ScrollFieldNumber.IsSelectedProperty] = item[!ScrollFieldItem.IsSelectedProperty],
                    [!ScrollFieldNumber.SelectedContentColorProperty] = owner[!ScrollField.SelectedContentColorProperty],
                    [!ScrollFieldNumber.UnselectedContentColorProperty] = owner[!ScrollField.UnselectedContentColorProperty]
                };
                view.Click += (_, e) =>
                {
                    if (!ReferenceEquals(e.Source, view)) return;
                    // The upstream click maps the option into the CURRENT cycle, rather than
                    // navigating directly to the clicked virtual page at a wrapping boundary.
                    owner.AnimatePage(state.CurrentPage + item.Index - state.CurrentPage % state.ItemCount);
                };
                Children.Add(view); entry = (view, item); _pages.Add(page, entry);
            }
            entry.Item.SetSelected(page == state.CurrentPage);
            entry.View.Height = (double)state.PageSize / density;
            entry.View.Measure(new(availableSize.Width, (double)state.PageSize / density));
        }
        return new(Children.Select(c => c.DesiredSize.Width).DefaultIfEmpty(0).Max(), availableSize.Height);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Owner?.State is not { } state) return finalSize;
        var density = state.Density;
        // Foundation's VerticalScrollableClipShape permits 30 dp overflow on the cross axis.
        var inflate = MathUtils.RoundToInt(30 * density) / (double)density;
        Clip = new RectangleGeometry(new Rect(-inflate, 0, finalSize.Width + 2 * inflate, finalSize.Height));
        var offset = MathUtils.RoundToInt(state.OffsetFraction * state.PageSize);
        foreach (var (page, entry) in _pages)
        {
            var y = state.SnapPosition + (page - state.CurrentPage) * state.PageSize - offset;
            var width = Math.Min(finalSize.Width, entry.View.DesiredSize.Width);
            var x = MathUtils.RoundToInt((float)(finalSize.Width - width) * density / 2) / (double)density;
            entry.View.Arrange(new(x, (double)y / density, width, (double)state.PageSize / density));
        }
        return finalSize;
    }
}

// Clickable Box has bounded indication but does not clip its content like Surface does.
internal sealed class ScrollFieldPage : ClickableSurface;

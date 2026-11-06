using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Overlays;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

/// <summary>A focusable search popup anchored to the SearchBar sharing its State.</summary>
/// <remarks>The declaration takes no layout space. Escape and outside presses animate the shared state to collapsed.</remarks>
public class ExpandedDockedSearchBar : ContentControl
{
    public static readonly StyledProperty<SearchBarState?> StateProperty = SearchBar.StateProperty.AddOwner<ExpandedDockedSearchBar>();
    public static readonly StyledProperty<object?> InputFieldProperty = DockedSearchBar.InputFieldProperty.AddOwner<ExpandedDockedSearchBar>();
    public static readonly StyledProperty<IDataTemplate?> InputFieldTemplateProperty = DockedSearchBar.InputFieldTemplateProperty.AddOwner<ExpandedDockedSearchBar>();
    public static readonly StyledProperty<Color> DividerColorProperty = DockedSearchBar.DividerColorProperty.AddOwner<ExpandedDockedSearchBar>();
    public static readonly StyledProperty<double> TonalElevationProperty = MaterialSurface.TonalElevationProperty.AddOwner<ExpandedDockedSearchBar>();
    public static readonly StyledProperty<double> ShadowElevationProperty = MaterialSurface.ShadowElevationProperty.AddOwner<ExpandedDockedSearchBar>();
    public static readonly DirectProperty<ExpandedDockedSearchBar, bool> IsPresentedProperty = AvaloniaProperty.RegisterDirect<ExpandedDockedSearchBar, bool>(nameof(IsPresented), bar => bar.IsPresented);
    internal static readonly StyledProperty<MotionSpring?> ExpandAnimationProperty = SearchBar.ExpandAnimationProperty.AddOwner<ExpandedDockedSearchBar>();
    internal static readonly StyledProperty<MotionSpring?> CollapseAnimationProperty = SearchBar.CollapseAnimationProperty.AddOwner<ExpandedDockedSearchBar>();
    internal static readonly StyledProperty<MotionSpring?> ContainedAnimationProperty = SearchBar.ContainedAnimationProperty.AddOwner<ExpandedDockedSearchBar>();
    internal static readonly StyledProperty<MotionSpring?> GapExpandAnimationProperty = SearchBar.GapExpandAnimationProperty.AddOwner<ExpandedDockedSearchBar>();
    internal static readonly StyledProperty<MotionSpring?> GapCollapseAnimationProperty = SearchBar.GapCollapseAnimationProperty.AddOwner<ExpandedDockedSearchBar>();
    internal MotionSpring? GapExpandAnimation { get => GetValue(GapExpandAnimationProperty); set => SetValue(GapExpandAnimationProperty, value); }
    internal MotionSpring? GapCollapseAnimation { get => GetValue(GapCollapseAnimationProperty); set => SetValue(GapCollapseAnimationProperty, value); }
    internal MotionSpring? ContainedAnimation { get => GetValue(ContainedAnimationProperty); set => SetValue(ContainedAnimationProperty, value); }
    internal MotionSpring? ExpandAnimation { get => GetValue(ExpandAnimationProperty); set => SetValue(ExpandAnimationProperty, value); }
    internal MotionSpring? CollapseAnimation { get => GetValue(CollapseAnimationProperty); set => SetValue(CollapseAnimationProperty, value); }
    public SearchBarState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public object? InputField { get => GetValue(InputFieldProperty); set => SetValue(InputFieldProperty, value); }
    public IDataTemplate? InputFieldTemplate { get => GetValue(InputFieldTemplateProperty); set => SetValue(InputFieldTemplateProperty, value); }
    public Color DividerColor { get => GetValue(DividerColorProperty); set => SetValue(DividerColorProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    public bool IsPresented { get => _isPresented; private set => SetAndRaise(IsPresentedProperty, ref _isPresented, value); }
    private bool _isPresented;
    private CancellationTokenSource? _presentationLifetime;
    private readonly Popup _popup = new() { ShouldUseOverlayLayer = true, WindowManagerAddShadowHint = false };
    private Grid? _root;
    private SearchBarPopupPresenter? _presenter;
    private ExpandedSearchBarLayout? _layout;
    private ContentPresenter? _input;
    private ContentPresenter? _results;
    private MaterialSurface? _surface;
    private SearchBarState? _activeState;
    private TopLevel? _topLevel;
    private SearchBarInputField? _retainedInput;
    internal Control? Presentation => _surface;
    internal Control? PresentationLayout => _layout;

    static ExpandedDockedSearchBar()
    {
        TemplateProperty.OverrideDefaultValue<ExpandedDockedSearchBar>(null);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<ExpandedDockedSearchBar>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<ExpandedDockedSearchBar>(VerticalAlignment.Top);
    }
    public ExpandedDockedSearchBar() => LogicalChildren.Add(_popup);
    protected override Type StyleKeyOverride => typeof(ExpandedDockedSearchBar);
    protected override AutomationPeer OnCreateAutomationPeer() => new DeclarationPeer(this);
    private sealed class DeclarationPeer(Control owner) : ControlAutomationPeer(owner)
    {
        // The overlay host exposes the presented controls. The zero-size declaration
        // must not expose PopupAutomationPeer's second route to that same host.
        protected override IReadOnlyList<AutomationPeer>? GetChildrenCore() => [];
        protected override bool IsControlElementCore() => false;
        protected override bool IsContentElementCore() => false;
    }
    protected override Size MeasureCore(Size availableSize) { ApplyTemplate(); return default; }
    protected override bool RegisterContentPresenter(ContentPresenter presenter) => presenter.Name == "PART_InputField" || base.RegisterContentPresenter(presenter);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_presenter is not null)
        {
            _presenter.Close(true);
            _presenter.DismissRequested -= OnDismiss;
            _presenter.OutsidePressed -= OnOutsidePressed;
            _presenter.Closed -= OnClosed;
        }
        _root?.Children.Remove(_popup);
        base.OnApplyTemplate(e);
        _root = e.NameScope.Find<Grid>("PART_Root");
        _presenter = e.NameScope.Find<SearchBarPopupPresenter>("PART_Presentation");
        _presenter?.SetScrimButton(e.NameScope.Find<SearchBarScrimButton>("PART_Scrim"), e.NameScope.Find<SearchBarPopupPositioner>("PART_Positioner"));
        _layout = e.NameScope.Find<ExpandedSearchBarLayout>("PART_Layout");
        _input = e.NameScope.Find<ContentPresenter>("PART_InputField");
        _results = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _surface = e.NameScope.Find<MaterialSurface>("PART_Surface");
        if (_presenter is not null)
        {
            _root?.Children.Remove(_presenter);
            _presenter.DismissRequested += OnDismiss;
            _presenter.OutsidePressed += OnOutsidePressed;
            _presenter.Closed += OnClosed;
        }
        _root?.Children.Add(_popup);
        UpdatePresentation();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null) _topLevel.PropertyChanged += WindowChanged;
        AttachState();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DetachState(); _presenter?.Close(true);
        if (_topLevel is not null) _topLevel.PropertyChanged -= WindowChanged;
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InputFieldProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical next) LogicalChildren.Add(next);
        }
        if (change.Property == StateProperty && this.IsAttachedToVisualTree())
        {
            DetachState(); _presenter?.Close(true); AttachState();
        }
    }
    private void AttachState()
    {
        _activeState = State;
        if (_activeState is not null)
        {
            if (this is ExpandedFullScreenSearchBar) _activeState.MarkFullScreen();
            _activeState.Attach(this);
            _activeState.PropertyChanged += StateChanged;
            _activeState.LayoutChanged += LayoutChanged;
            _activeState.ContentProgressChanged += ContentProgressChanged;
        }
        UpdatePresentation();
    }
    private void DetachState()
    {
        if (_activeState is null) return;
        _activeState.PropertyChanged -= StateChanged; _activeState.LayoutChanged -= LayoutChanged;
        _activeState.ContentProgressChanged -= ContentProgressChanged;
        _activeState.Detach(this); _activeState = null;
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => UpdatePresentation();
    private void ContentProgressChanged(object? sender, EventArgs e)
    {
        if (_presenter is not null && _activeState is not null) UpdateVariant(_presenter, _activeState);
    }
    private void LayoutChanged(object? sender, EventArgs e) => UpdateGeometry();
    private void WindowChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty) UpdateGeometry();
    }
    private void OnDismiss(object? sender, DismissRequestedEventArgs e)
    {
        if (_activeState is { } state && _presentationLifetime is { } lifetime) _ = state.AnimateToCollapsedAsync(lifetime.Token);
    }
    private void OnOutsidePressed(object? sender, EventArgs e)
    {
        if (_activeState is { } state && _presentationLifetime is { } lifetime) _ = state.AnimateToCollapsedAsync(lifetime.Token);
    }
    private void OnClosed(object? sender, EventArgs e)
    {
        // ModalPresenter has restored native focus before this callback. Core's parent
        // scene never lost its FocusInteraction while the popup owned keyboard input.
        _retainedInput?.ClearValue(TextFieldThemeDefaults.RetainPopupFocusProperty);
        _retainedInput = null;
        var lifetime = _presentationLifetime; _presentationLifetime = null;
        IsPresented = false;
        _input?.UpdateChild(); _results?.UpdateChild();
        // Upstream dismissal is launched from the popup composition. Reaching the .02
        // threshold removes that composition and cancels its unfinished state animation.
        lifetime?.Cancel(); lifetime?.Dispose();
    }
    internal virtual void UpdateVariant(SearchBarPopupPresenter presenter, SearchBarState state) { }
    internal void UpdatePresentation()
    {
        if (_presenter is null || _topLevel is null) return;
        if (_activeState?.CurrentValue != SearchBarValue.Expanded) { _presenter.Close(true); return; }
        UpdateGeometry();
        UpdateVariant(_presenter, _activeState);
        if (!_presenter.IsPresented)
        {
            if (_topLevel.FocusManager?.GetFocusedElement() is SearchBarInputField focused
                && ReferenceEquals(focused.State, _activeState))
            {
                _retainedInput = focused;
                focused.SetValue(TextFieldThemeDefaults.RetainPopupFocusProperty, true);
            }
            _presentationLifetime = new CancellationTokenSource();
            IsPresented = true;
            _presenter.IsVisible = true;
            _presenter.Open(_popup, this, false);
            // Like FocusRequester, request input focus only on creation, never on later progress updates.
            Control? FindInput() => _input?.GetVisualDescendants().OfType<Control>()
                .FirstOrDefault(c => c.Focusable && c.IsEffectivelyEnabled && c.IsEffectivelyVisible);
            if (FindInput()?.Focus() != true)
            {
                var lifetime = _presentationLifetime;
                Dispatcher.UIThread.Post(() =>
                {
                    if (IsPresented && ReferenceEquals(lifetime, _presentationLifetime)) FindInput()?.Focus();
                });
            }
        }
    }
    private void UpdateGeometry()
    {
        if (_presenter is null || _layout is null || _topLevel is null) return;
        Rect? bounds = null;
        if (_activeState?.CollapsedBar is { } bar && bar.IsAttachedToVisualTree())
        {
            var mirrored = false;
            for (Visual? visual = bar; visual is not null && visual != _topLevel; visual = visual.GetVisualParent()) mirrored ^= visual.HasMirrorTransform;
            var origin = bar.TranslatePoint(mirrored ? new Point(bar.Bounds.Width, 0) : default, _topLevel);
            if (origin is { } point) bounds = new Rect(point, bar.Bounds.Size);
        }
        _presenter.SetAnchor(_layout is FullScreenSearchBarLayout ? default : bounds?.Position ?? default);
        _layout.SetInputs(bounds, _topLevel.ClientSize, _activeState?.RawProgress ?? 0);
    }
}

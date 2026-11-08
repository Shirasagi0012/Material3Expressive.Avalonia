using Material3Expressive.Foundation.Rendering;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Threading;
using ComposeAnimation;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>Search input with native editing and optional application-owned search state.</summary>
/// <remarks>Text, selection and editing remain native TextBox state. Applications handle requests.</remarks>
public class SearchBarInputField : TextBox
{
    public static readonly AttachedProperty<MaterialShape?> ShapeProperty = MaterialTextField.ShapeProperty;
    /// <summary>Container and keyboard focus-ring outline. Null uses CornerRadius.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> ExpandAnimationProperty = SearchBar.ExpandAnimationProperty.AddOwner<SearchBarInputField>();
    internal Foundation.Motion.SpringSpec? ExpandAnimation { get => GetValue(ExpandAnimationProperty); set => SetValue(ExpandAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> CollapseAnimationProperty = SearchBar.CollapseAnimationProperty.AddOwner<SearchBarInputField>();
    internal Foundation.Motion.SpringSpec? CollapseAnimation { get => GetValue(CollapseAnimationProperty); set => SetValue(CollapseAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> ContainedAnimationProperty = SearchBar.ContainedAnimationProperty.AddOwner<SearchBarInputField>();
    internal Foundation.Motion.SpringSpec? ContainedAnimation { get => GetValue(ContainedAnimationProperty); set => SetValue(ContainedAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> GapExpandAnimationProperty = SearchBar.GapExpandAnimationProperty.AddOwner<SearchBarInputField>();
    internal Foundation.Motion.SpringSpec? GapExpandAnimation { get => GetValue(GapExpandAnimationProperty); set => SetValue(GapExpandAnimationProperty, value); }
    internal static readonly StyledProperty<Foundation.Motion.SpringSpec?> GapCollapseAnimationProperty = SearchBar.GapCollapseAnimationProperty.AddOwner<SearchBarInputField>();
    internal Foundation.Motion.SpringSpec? GapCollapseAnimation { get => GetValue(GapCollapseAnimationProperty); set => SetValue(GapCollapseAnimationProperty, value); }
    public static readonly StyledProperty<SearchBarState?> StateProperty =
        AvaloniaProperty.Register<SearchBarInputField, SearchBarState?>(nameof(State));
    /// <summary>When set, input gestures animate this state. Null retains the Boolean request API.</summary>
    public SearchBarState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    internal bool Expanded => State?.CurrentValue == SearchBarValue.Expanded || State is null && IsExpanded;
    public static readonly AttachedProperty<MaterialInteractionSource?> InteractionSourceProperty = MaterialTextField.InteractionSourceProperty;
    /// <summary>Shared editing interactions. Null uses the text template's private source.</summary>
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<SearchBarInputField, bool>(nameof(IsExpanded));
    public static readonly RoutedEvent<RoutedEventArgs> ExpansionRequestedEvent =
        RoutedEvent.Register<SearchBarInputField, RoutedEventArgs>(nameof(ExpansionRequested), RoutingStrategies.Bubble);
    public static readonly RoutedEvent<RoutedEventArgs> SearchRequestedEvent =
        RoutedEvent.Register<SearchBarInputField, RoutedEventArgs>(nameof(SearchRequested), RoutingStrategies.Bubble);
    internal static readonly StyledProperty<double> PreferredMinimumWidthProperty =
        AvaloniaProperty.Register<SearchBarInputField, double>(nameof(PreferredMinimumWidth));
    internal static readonly StyledProperty<double> PreferredMaximumWidthProperty =
        AvaloniaProperty.Register<SearchBarInputField, double>(nameof(PreferredMaximumWidth));
    internal static readonly StyledProperty<TimeSpan> ClearFocusDelayProperty =
        AvaloniaProperty.Register<SearchBarInputField, TimeSpan>(nameof(ClearFocusDelay));
    internal double PreferredMinimumWidth { get => GetValue(PreferredMinimumWidthProperty); set => SetValue(PreferredMinimumWidthProperty, value); }
    internal double PreferredMaximumWidth { get => GetValue(PreferredMaximumWidthProperty); set => SetValue(PreferredMaximumWidthProperty, value); }
    internal TimeSpan ClearFocusDelay { get => GetValue(ClearFocusDelayProperty); set => SetValue(ClearFocusDelayProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public event EventHandler<RoutedEventArgs>? ExpansionRequested { add => AddHandler(ExpansionRequestedEvent, value); remove => RemoveHandler(ExpansionRequestedEvent, value); }
    public event EventHandler<RoutedEventArgs>? SearchRequested { add => AddHandler(SearchRequestedEvent, value); remove => RemoveHandler(SearchRequestedEvent, value); }
    private CancellationTokenSource? _clearFocus;
    private AnimationChildScope? _inputScope;
    private SearchBarState? _attachedState;
    private MaterialInteractionSource? _source;
    private IDisposable? _sourceCommit;
    private IDisposable? _ringCommit;
    private MaterialInputMode? _inputMode;
    private TextFieldDecoration? _decoration;
    private bool _observeTextGrowth;
    private bool _textPending;
    private int _textGeneration;
    private IDisposable? _textCollectorCommit;
    private bool _expanded;
    private int _previousLength;
    static SearchBarInputField() => AffectsMeasure<SearchBarInputField>(PreferredMinimumWidthProperty, PreferredMaximumWidthProperty);
    public SearchBarInputField()
    {
        AddHandler(KeyDownEvent, PreviewArrow, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, PreviewArrow, RoutingStrategies.Tunnel);
    }
    protected override Type StyleKeyOverride => typeof(SearchBarInputField);
    protected override AutomationPeer OnCreateAutomationPeer() => new SearchInputAutomationPeer(this);

    protected override Size MeasureOverride(Size availableSize)
    {
        var constrained = new Size(Math.Min(availableSize.Width, PreferredMaximumWidth), availableSize.Height);
        var measured = base.MeasureOverride(constrained);
        return new(Math.Min(availableSize.Width, Math.Max(PreferredMinimumWidth, measured.Width)), measured.Height);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        var mode = MaterialInputMode.For(this);
        if (ReferenceEquals(e.Source, this)) mode?.ObserveFocusNavigation(e.NavigationMethod);
        base.OnGotFocus(e);
        if (!IsFocused) return;
        if (State is null) RaiseEvent(new RoutedEventArgs(ExpansionRequestedEvent));
        else if (mode?.IsKeyboard == false && !GetValue(TextFieldThemeDefaults.RetainPopupFocusProperty)) Expand();
    }

    private void PreviewArrow(object? sender, KeyEventArgs e)
    {
        if (e.Handled || State is null || !IsEffectivelyEnabled || e.Key != Key.Down) return;
        if (!Expanded && MaterialInputMode.For(this)?.IsKeyboard != false)
        {
            Expand();
            e.Handled = true;
        }
        else if (Expanded)
        {
            TopLevel.GetTopLevel(this)?.FocusManager?.TryMoveFocus(NavigationDirection.Down);
            e.Handled = true;
        }
    }

    private void Expand()
    {
        if (_attachedState is not { } state || _inputScope is not { } scope) return;
        _ = scope.Launch(async token => await state.AnimateToExpandedAsync(token), "Search input expand");
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _inputMode = MaterialInputMode.For(this);
        if (_inputMode is not null) _inputMode.Changed += InputModeChanged;
        _inputScope = AnimationScope.Get(this).CreateChild("Search input");
        AttachState();
        ConnectSource();
        UpdateFocusRing();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _decoration?.SetSearchFocusRing(null, null);
        base.OnApplyTemplate(e);
        _decoration = e.NameScope.Find<TextFieldDecoration>("Decoration");
        UpdateFocusRing();
    }

    private void InputModeChanged(object? sender, EventArgs e)
    {
        // InputField adds/removes its focus-only indication during recomposition.
        _ringCommit?.Dispose();
        _ringCommit = _inputScope?.ScheduleFrameCommit(() => { _ringCommit = null; UpdateFocusRing(); });
    }

    private void UpdateFocusRing() => _decoration?.SetSearchFocusRing(
        _inputScope is not null && State is not null && _inputMode?.IsKeyboard != false
            && MaterialInteraction.GetFocusFeedback(this) == FocusFeedback.InsetRing ? this : null, _source);

    private void AttachState(bool deferTextCollector = false)
    {
        if (_attachedState is { } old)
        {
            old.PropertyChanged -= StateChanged;
            old.Detach(this);
        }
        _attachedState = null;
        if (_inputScope is not null && State is { } state)
        {
            state.Attach(this);
            state.PropertyChanged += StateChanged;
            _attachedState = state;
        }
        _textCollectorCommit?.Dispose(); _textCollectorCommit = null;
        _textGeneration++; _textPending = false; _observeTextGrowth = false;
        // A changed effect key installs its new collector after recomposition. Text
        // edited before that boundary becomes the initial snapshot, not a growth event.
        void StartCollector()
        {
            _textCollectorCommit = null;
            _observeTextGrowth = _attachedState is not null && !Expanded;
            _previousLength = Text?.Length ?? 0;
        }
        if (deferTextCollector && _inputScope is { } scope)
            _textCollectorCommit = scope.ScheduleFrameCommit(StartCollector);
        else StartCollector();
        UpdateExpanded();
    }

    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SearchBarState.CurrentValueProperty) UpdateExpanded();
    }

    private void ConnectSource()
    {
        var next = _inputScope is null ? null : GetValue(TextFieldThemeDefaults.EffectiveInteractionSourceProperty);
        _sourceCommit?.Dispose(); _sourceCommit = null;
        if (ReferenceEquals(next, _source)) return;
        if (State is not null && _source is not null && next is not null && _inputScope is { } scope)
        {
            _sourceCommit = scope.ScheduleFrameCommit(() =>
            {
                _sourceCommit = null;
                BindSource(next);
            });
        }
        else BindSource(next);
    }

    private void BindSource(MaterialInteractionSource? source)
    {
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = source;
        if (_source is not null) _source.Interaction += ReceiveInteraction;
        UpdateFocusRing();
    }

    private void ReceiveInteraction(MaterialInteractionEvent interaction)
    {
        // Fixed DetectClickFromInteractionSource observes Release, including externally
        // emitted releases. CancelPress and Press do not trigger this path.
        if (State is not null && !Expanded && interaction is MaterialInteractionEvent.Release) Expand();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == Key.Enter && IsEffectivelyEnabled)
        {
            RaiseEvent(new RoutedEventArgs(SearchRequestedEvent));
            e.Handled = true;
        }
        base.OnKeyDown(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty)
        {
            CancelClearFocus();
            if (_inputScope is not null)
            {
                _inputScope.Dispose();
                _inputScope = AnimationScope.Get(this).CreateChild("Search input");
            }
            AttachState(deferTextCollector: true);
            ConnectSource();
            UpdateFocusRing();
        }
        if (change.Property == MaterialInteraction.FocusFeedbackProperty || change.Property == CornerRadiusProperty) UpdateFocusRing();
        if (change.Property == TextFieldThemeDefaults.EffectiveInteractionSourceProperty) ConnectSource();
        if (change.Property == TextProperty && _observeTextGrowth && !_textPending)
        {
            _textPending = true;
            var generation = _textGeneration;
            // snapshotFlow observes the applied text snapshot. Coalesce synchronous
            // edits on the UI dispatcher before comparing lengths; it is not a frame delay.
            Dispatcher.UIThread.Post(() => {
                if (generation != _textGeneration) return;
                _textPending = false;
                var length = Text?.Length ?? 0;
                if (_observeTextGrowth && length > _previousLength && TextFieldThemeDefaults.GetIsFocused(this) && !Expanded) Expand();
                _previousLength = length;
            });
        }
        // LaunchedEffect(expanded): focus changes alone do not restart the effect.
        if (change.Property == IsExpandedProperty && State is null) UpdateExpanded();
    }

    private void UpdateExpanded()
    {
        var expanded = Expanded;
        if (_expanded == expanded) return;
        var old = _expanded;
        _expanded = expanded;
        if (ControlAutomationPeer.FromElement(this) is SearchInputAutomationPeer peer) peer.ExpandedChanged(old);
        CancelClearFocus();
        if (!expanded && (State is null || MaterialInputMode.For(this)?.IsKeyboard == false)
            && TextFieldThemeDefaults.GetIsFocused(this) && this.IsAttachedToVisualTree())
        {
            var cancellation = _clearFocus = new CancellationTokenSource();
            var scope = AnimationScope.Get(this);
            _ = scope.Launch(async _ => {
                try
                {
                    if (!await scope.DelayAsync(ClearFocusDelay, cancellation.Token)) return;
                    if (!Expanded && this.IsAttachedToVisualTree()) TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
                }
                catch (OperationCanceledException) when (cancellation.IsCancellationRequested || scope.Lifetime.IsCancellationRequested) { }
                finally
                {
                    if (ReferenceEquals(_clearFocus, cancellation)) _clearFocus = null;
                    cancellation.Dispose();
                }
            }, "Search input clear focus");
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelClearFocus();
        if (_inputMode is not null) _inputMode.Changed -= InputModeChanged;
        _inputMode = null;
        _ringCommit?.Dispose(); _ringCommit = null;
        _decoration?.SetSearchFocusRing(null, null);
        _inputScope?.Dispose(); _inputScope = null;
        AttachState();
        ConnectSource();
        base.OnDetachedFromVisualTree(e);
    }
    private void CancelClearFocus() { _clearFocus?.Cancel(); _clearFocus = null; }

    // Keep the native editable value/selection provider, adding the two upstream semantics.
    private sealed class SearchInputAutomationPeer(SearchBarInputField owner) : TextBoxAutomationPeer(owner)
    {
        protected override string? GetNameCore() => base.GetNameCore() ?? SearchBarStrings.Get("SearchBarSearch");
        protected override string? GetItemStatusCore() => base.GetItemStatusCore() ?? (owner.Expanded ? SearchBarStrings.Get("SuggestionsAvailable") : null);
        internal void ExpandedChanged(bool old)
        {
            if (AutomationProperties.GetItemStatus(owner) is null)
                RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty,
                    old ? SearchBarStrings.Get("SuggestionsAvailable") : null, GetItemStatus());
        }
    }
}

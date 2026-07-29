// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// ListItem.kt:262-847, 1216-1225, 1291-1388, 1450-1669, 1557-1602, 1937-1990;
// ListItemDefaults.kt:40-64, 246-399, 438-556, 1135-1159; and
// tokens/{ListTokens,ReorderListTokens}.kt (VERSION 29.0.0).
// Compose supplies separate non-interactive, click, radio-like, and checkbox-like overloads.
// Avalonia intentionally consolidates them through InteractionMode and keeps IsSelected as the
// single bindable selection state. The inherited ToggleButton.IsChecked remains a compatibility
// mirror for selection modes only; InteractionMode, rather than that implementation detail,
// defines public behaviour and accessibility. The alpha22 executable reference can verify static
// click/selection and segmented visuals, but cannot execute the alpha25 no-interaction overload or
// its text-line-aware 56/72/88dp layout. Those paths are parity unavailable and are covered by
// source review plus deterministic Avalonia behavior/layout tests.
// ListItemDefaults.ListItemColors resolves disabled before dragged before selected, while
// ListItemShapes.shapeForInteraction and InteractiveListItem's dragged elevation deliberately do
// not gate those states by enabled. InteractiveListItem also gives every positive animated shadow
// elevation zIndex = 1, keeps an externally supplied dragged state layer visible when disabled,
// and clips slot content (but not its shadow) to the current animated shape. Compose supplies the
// optional onLongClick/onLongClickLabel pair consistently on every interactive overload. Avalonia
// keeps the same consistency through InteractionMode: LongClick, LongClickCommand, and
// LongClickLabel are available only when the mode is not None. Holding is enabled for mouse as
// the deliberate desktop counterpart and suppresses the following primary Click. The companion
// MaterialSegmentedList owns group position; this row keeps that derived value internal.

using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialSkiaShadow;
using System.Windows.Input;

namespace Material3Expressive.Controls;

/// <summary>
/// Chooses the interaction and accessibility contract of a <see cref="MaterialListItem" />.
/// </summary>
/// <remarks>
/// Compose exposes four overloads for the same list-row anatomy. Avalonia deliberately represents
/// them with one control and this explicit property: the mode controls input, state transition,
/// selected visual state, focusability, and automation role together. <see cref="MaterialListItem.IsSelected" />
/// is meaningful only for the two selection modes; it is retained but visually and semantically
/// ignored by <see cref="ListItemInteractionMode.None" /> and
/// <see cref="ListItemInteractionMode.Click" />.
/// </remarks>
public enum ListItemInteractionMode
{
    /// <summary>Non-interactive content with ListItem semantics and no invoke/toggle pattern.</summary>
    None,

    /// <summary>An action row with button semantics and no persistent selected state.</summary>
    Click,

    /// <summary>A radio-like row whose activation selects it.</summary>
    SingleSelection,

    /// <summary>A checkbox-like row whose activation toggles its selected state.</summary>
    MultiSelection
}

/// <summary>
/// Internal position assigned by <see cref="MaterialSegmentedList" />. The container owns this
/// derived value so applications cannot create contradictory index/count combinations.
/// </summary>
internal enum MaterialListItemSegmentPosition
{
    None,
    Single,
    First,
    Middle,
    Last
}

/// <summary>A Material list row with leading, headline, overline, supporting and trailing slots.</summary>
public sealed class MaterialListItem : ToggleButton
{
    // Pinned Compose Text resolves Material's default fontFeatureSettings to an empty string.
    // Avalonia otherwise enables Roboto standard/contextual ligatures by default, changing the
    // UTF-16 text-layout contract for strings such as "Offline". A child TextBlock may still
    // provide an explicit feature collection for its own intentional typography override.
    private static readonly FontFeatureCollection s_defaultTextFontFeatures =
        FontFeatureCollection.Parse("liga=0, clig=0");

    /// <summary>Defines which Compose ListItem overload family this row adapts.</summary>
    public static readonly StyledProperty<ListItemInteractionMode> InteractionModeProperty =
        AvaloniaProperty.Register<MaterialListItem, ListItemInteractionMode>(nameof(InteractionMode));

    /// <summary>The canonical bindable state for single- and multi-selection rows.</summary>
    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<MaterialListItem, bool>(
            nameof(IsSelected),
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    /// <summary>
    /// Indicates that an interactive item is being reordered by an owning Avalonia drag/drop
    /// surface. This is the explicit desktop adaptation of Compose's internal DragInteraction
    /// collection; it enables the alpha25 reorder colour, shape, state layer, and elevation.
    /// </summary>
    public static readonly StyledProperty<bool> IsDraggedProperty =
        AvaloniaProperty.Register<MaterialListItem, bool>(nameof(IsDragged));

    internal static readonly StyledProperty<MaterialListItemSegmentPosition> SegmentPositionProperty =
        AvaloniaProperty.Register<MaterialListItem, MaterialListItemSegmentPosition>(
            nameof(SegmentPosition));

    /// <summary>Defines an optional command invoked by a successful long press on an interactive row.</summary>
    public static readonly StyledProperty<ICommand?> LongClickCommandProperty =
        AvaloniaProperty.Register<MaterialListItem, ICommand?>(nameof(LongClickCommand));

    /// <summary>Defines the parameter supplied to <see cref="LongClickCommand" />.</summary>
    public static readonly StyledProperty<object?> LongClickCommandParameterProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(LongClickCommandParameter));

    /// <summary>Defines the accessibility description of the optional long-press action.</summary>
    public static readonly StyledProperty<string?> LongClickLabelProperty =
        AvaloniaProperty.Register<MaterialListItem, string?>(nameof(LongClickLabel));

    /// <summary>Raised when a successful long press invokes the row's optional secondary action.</summary>
    public static readonly RoutedEvent<RoutedEventArgs> LongClickEvent =
        RoutedEvent.Register<MaterialListItem, RoutedEventArgs>(nameof(LongClick), RoutingStrategies.Bubble);

    /// <summary>Defines the optional logical-start slot.</summary>
    public static readonly StyledProperty<object?> LeadingContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(LeadingContent));

    /// <summary>Defines the optional logical-end slot.</summary>
    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(TrailingContent));

    /// <summary>Defines the optional label above the headline.</summary>
    public static readonly StyledProperty<object?> OverlineContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(OverlineContent));

    /// <summary>Defines the optional label below the headline.</summary>
    public static readonly StyledProperty<object?> SupportingContentProperty =
        AvaloniaProperty.Register<MaterialListItem, object?>(nameof(SupportingContent));

    /// <summary>The resolved brush for supporting-slot content.</summary>
    public static readonly DirectProperty<MaterialListItem, IBrush> SupportingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(SupportingBrush), item => item.SupportingBrush);

    /// <summary>The resolved brush for leading-slot content.</summary>
    public static readonly DirectProperty<MaterialListItem, IBrush> LeadingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(LeadingBrush), item => item.LeadingBrush);

    /// <summary>The resolved brush for trailing-slot content.</summary>
    public static readonly DirectProperty<MaterialListItem, IBrush> TrailingBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(TrailingBrush), item => item.TrailingBrush);

    /// <summary>The resolved brush for overline-slot content.</summary>
    public static readonly DirectProperty<MaterialListItem, IBrush> OverlineBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialListItem, IBrush>(nameof(OverlineBrush), item => item.OverlineBrush);

    private readonly MaterialThemeWatcher _theme;
    private readonly MaterialInteractionHost _interaction;
    private readonly Dictionary<TextBlock, SlotTypographyAssignment> _slotTypographyAssignments = new();

    private ListItemTokens _tokens = ListItemTokens.Default;
    private ListItemColors _colors = ListItemTokens.Default.Resolve(null, false);
    private AnimatedShape? _shape;
    private AnimatedBrush? _containerBrushAnimation;
    private AnimatedBrush? _contentBrushAnimation;
    private AnimatedBrush? _leadingBrushAnimation;
    private AnimatedBrush? _trailingBrushAnimation;
    private AnimatedBrush? _overlineBrushAnimation;
    private AnimatedBrush? _supportingBrushAnimation;
    private SpringAnimation? _elevation;
    private bool _syncingSelection;
    private IBrush _supportingBrush = Brushes.Gray;
    private IBrush _leadingBrush = Brushes.Gray;
    private IBrush _trailingBrush = Brushes.Gray;
    private IBrush _overlineBrush = Brushes.Gray;
    private Border? _contentClip;
    private IDisposable? _elevatedZIndexLease;
    private int _longClickHandlerCount;
    private bool _suppressNextClickAfterLongPress;

    private sealed record SlotTypographyAssignment(
        TypographyRole SlotRole,
        TextBlock TextBlock,
        IDisposable? TextStyleLease,
        IReadOnlyList<Action> RestoreMetricOverrides,
        EventHandler<AvaloniaPropertyChangedEventArgs>? ExplicitTextStyleChangedHandler);

    public MaterialListItem()
    {
        _theme = new MaterialThemeWatcher(this);
        _interaction = new MaterialInteractionHost(this);
        _theme.Changed += (_, _) => ApplyTokens();
        _interaction.InteractionChanged += (_, _) => UpdateShape();
        _interaction.Ticked += (_, _) =>
        {
            UpdateContentClip();
            UpdateElevationZIndex();
        };
        ApplyTokens();
    }

    /// <summary>Gets or sets the row's unified interaction contract.</summary>
    public ListItemInteractionMode InteractionMode
    {
        get => GetValue(InteractionModeProperty);
        set => SetValue(InteractionModeProperty, value);
    }

    /// <summary>Gets or sets whether a selection-mode row is selected.</summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>Gets or sets whether an owning reorder interaction currently drags this row.</summary>
    public bool IsDragged
    {
        get => GetValue(IsDraggedProperty);
        set => SetValue(IsDraggedProperty, value);
    }

    internal MaterialListItemSegmentPosition SegmentPosition
    {
        get => GetValue(SegmentPositionProperty);
        set => SetValue(SegmentPositionProperty, value);
    }

    /// <summary>Gets or sets the optional command invoked by a long press on an interactive row.</summary>
    public ICommand? LongClickCommand
    {
        get => GetValue(LongClickCommandProperty);
        set => SetValue(LongClickCommandProperty, value);
    }

    /// <summary>Gets or sets the parameter supplied to <see cref="LongClickCommand" />.</summary>
    public object? LongClickCommandParameter
    {
        get => GetValue(LongClickCommandParameterProperty);
        set => SetValue(LongClickCommandParameterProperty, value);
    }

    /// <summary>Gets or sets the accessibility description of the optional long-press action.</summary>
    public string? LongClickLabel
    {
        get => GetValue(LongClickLabelProperty);
        set => SetValue(LongClickLabelProperty, value);
    }

    /// <summary>Occurs when a successful long press invokes the row's optional secondary action.</summary>
    public event EventHandler<RoutedEventArgs>? LongClick
    {
        add
        {
            AddHandler(LongClickEvent, value);
            _longClickHandlerCount++;
            UpdateLongClickGesture();
        }
        remove
        {
            RemoveHandler(LongClickEvent, value);
            _longClickHandlerCount = Math.Max(0, _longClickHandlerCount - 1);
            UpdateLongClickGesture();
        }
    }

    /// <summary>Gets or sets the optional content at the logical start of the row.</summary>
    public object? LeadingContent
    {
        get => GetValue(LeadingContentProperty);
        set => SetValue(LeadingContentProperty, value);
    }

    /// <summary>Gets or sets the optional content at the logical end of the row.</summary>
    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    /// <summary>Gets or sets the optional overline content.</summary>
    public object? OverlineContent
    {
        get => GetValue(OverlineContentProperty);
        set => SetValue(OverlineContentProperty, value);
    }

    /// <summary>Gets or sets the optional supporting content.</summary>
    public object? SupportingContent
    {
        get => GetValue(SupportingContentProperty);
        set => SetValue(SupportingContentProperty, value);
    }

    /// <summary>Gets the resolved supporting-slot brush.</summary>
    public IBrush SupportingBrush => _supportingBrush;

    /// <summary>Gets the resolved leading-slot brush.</summary>
    public IBrush LeadingBrush => _leadingBrush;

    /// <summary>Gets the resolved trailing-slot brush.</summary>
    public IBrush TrailingBrush => _trailingBrush;

    /// <summary>Gets the resolved overline-slot brush.</summary>
    public IBrush OverlineBrush => _overlineBrush;

    private bool IsInteractive => InteractionMode != ListItemInteractionMode.None;

    private bool HasSelection => InteractionMode is ListItemInteractionMode.SingleSelection or ListItemInteractionMode.MultiSelection;

    private bool EffectiveSelected => HasSelection && IsSelected;

    private bool EffectiveDragged => IsInteractive && IsDragged;

    private bool IsSegmented => SegmentPosition != MaterialListItemSegmentPosition.None;

    internal bool HasLongClickAction => _longClickHandlerCount > 0 || LongClickCommand is not null;

    private bool CanInvokeLongClick =>
        _longClickHandlerCount > 0
        || LongClickCommand?.CanExecute(LongClickCommandParameter) == true;

    private AnimatedShape Shape =>
        _shape ??= _interaction.CreateAnimatedShape(RestingShape(), _theme.Spring(_tokens.ShapeMotion));

    private CornerRadius ContainerCornerRadius =>
        Shape.ToCornerRadius(Bounds.Size, FlowDirection == FlowDirection.RightToLeft);

    // The visual-parity capture project consumes these internal values through a test-only
    // adapter. Keeping the shadow inputs beside Render prevents its structural contract from
    // drifting from the MaterialContainer call that produces the actual pixels.
    internal ComposeShadowShape ParityShadowShape =>
        ComposeShadowShape.RoundedRectangle(new Rect(Bounds.Size), ContainerCornerRadius);

    internal ComposeShadowParameters ParityShadowParameters =>
        ComposeShadowParameters.Material(CurrentElevation(), ShadowColor);

    internal bool ParityShadowOccluderDrawn => true;

    private Color ShadowColor =>
        _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark) ?? Colors.Black;

    /// <inheritdoc />
    /// <remarks>
    /// Button normally folds <c>Command.CanExecute</c> into this result. The AndroidX
    /// non-interactive overload has no click action or command path at all, so a caller-owned
    /// command must not make an otherwise enabled static row appear disabled. Parent and local
    /// <see cref="InputElement.IsEnabled" /> values still participate through the normal
    /// InputElement effective-enabled propagation.
    /// </remarks>
    protected override bool IsEnabledCore =>
        InteractionMode == ListItemInteractionMode.None ? IsEnabled : base.IsEnabledCore;

    static MaterialListItem()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialListItem>(false);
        AffectsRender<MaterialListItem>(
            InteractionModeProperty,
            IsSelectedProperty,
            IsDraggedProperty,
            SegmentPositionProperty,
            IsEnabledProperty);
        AffectsMeasure<MaterialListItem>(
            LeadingContentProperty,
            TrailingContentProperty,
            OverlineContentProperty,
            SupportingContentProperty);
    }

    /// <inheritdoc />
    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        var corners = ContainerCornerRadius;
        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            ContainerBrush(),
            outline: null,
            outlineWidth: 0,
            elevation: CurrentElevation(),
            shadowColor: ShadowColor);

        // A disabled row does not originate pointer/keyboard feedback, but an owning reorder
        // surface can still supply DragInteraction. Compose keeps that dragged state layer active
        // through the same interaction source even though combinedClickable itself is disabled.
        if (IsInteractive && (IsEffectivelyEnabled || EffectiveDragged))
            _interaction.DrawLayers(context, ContentBrush(), corners);

        base.Render(context);
    }

    /// <summary>
    /// Suppresses ToggleButton's inherited state machine. <see cref="InteractionMode" /> is the
    /// only source of state transitions, while <see cref="ToggleButton.IsChecked" /> is retained solely as a
    /// compatibility mirror for selection modes.
    /// </summary>
    protected override void Toggle()
    {
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // Do not call Button here for static rows: its class handler sets IsPressed and consumes
        // the routed event even when OnClick subsequently does nothing. Compose's onClick = null
        // path has no combinedClickable and therefore leaves the input route to surrounding UI.
        if (!IsInteractive)
            return;

        base.OnPointerPressed(e);
    }

    /// <inheritdoc />
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (!IsInteractive)
        {
            // A mode can change while Button is already holding its private pressed state. Let
            // the inherited route retire that state on the matching release; OnClick remains
            // guarded above, so this cannot invoke a row that became static mid-gesture.
            if (IsPressed)
                base.OnPointerReleased(e);

            _suppressNextClickAfterLongPress = false;
            return;
        }

        base.OnPointerReleased(e);
        _suppressNextClickAfterLongPress = false;
    }

    /// <inheritdoc />
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        _suppressNextClickAfterLongPress = false;
        base.OnPointerCaptureLost(e);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!IsInteractive)
            return;

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (!IsInteractive)
        {
            // See OnPointerReleased: this is only the balancing key-up for a press that started
            // before a live mode change. A static row that never entered Button's pressed state
            // still leaves the key event unhandled.
            if (IsPressed)
                base.OnKeyUp(e);

            return;
        }

        base.OnKeyUp(e);
    }

    /// <inheritdoc />
    protected override void OnAccessKey(RoutedEventArgs e)
    {
        if (!IsInteractive)
            return;

        base.OnAccessKey(e);
    }

    /// <inheritdoc />
    protected override void OnHolding(HoldingRoutedEventArgs e)
    {
        base.OnHolding(e);

        if (e.HoldingState != HoldingState.Started || !IsInteractive || !IsEffectivelyEnabled || !CanInvokeLongClick)
            return;

        // Mark the gesture before raising the routed event. An application handler can re-enter
        // input processing, but the eventual left-button release must still not turn this
        // successful long press into the normal primary Click.
        _suppressNextClickAfterLongPress = true;
        e.Handled = true;
        InvokeLongClick();
    }

    /// <inheritdoc />
    protected override void OnClick()
    {
        if (!IsEffectivelyEnabled || !IsInteractive)
            return;

        if (_suppressNextClickAfterLongPress)
            return;

        // Update the canonical state before base.OnClick(). ToggleButton then calls our no-op
        // Toggle override and Button raises Click/executes Command, so observers always receive
        // the final radio/checkbox state rather than a transient ToggleButton state.
        switch (InteractionMode)
        {
            case ListItemInteractionMode.SingleSelection:
                IsSelected = true;
                break;
            case ListItemInteractionMode.MultiSelection:
                IsSelected = !IsSelected;
                break;
        }

        base.OnClick();
    }

    /// <summary>Lets the mode-specific automation provider follow the normal input route.</summary>
    internal void InvokeFromAutomation() => OnClick();

    internal void InvokeLongClick()
    {
        if (!IsInteractive || !IsEffectivelyEnabled || !CanInvokeLongClick)
            return;

        var args = new RoutedEventArgs(LongClickEvent);
        RaiseEvent(args);

        var command = LongClickCommand;
        if (!args.Handled && command?.CanExecute(LongClickCommandParameter) == true)
            command.Execute(LongClickCommandParameter);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new MaterialListItemAutomationPeer(this);

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RefreshSlotTypography(refreshMetrics: true);

        // Parent attachment precedes the supplied TextBlock's final ControlTheme/style pass.
        // Reapply on the next dispatcher turn so its previously inherited BodyLarge trigger has
        // settled before the current-value overrides are installed.
        Dispatcher.UIThread.Post(
            () =>
            {
                if (this.IsAttachedToVisualTree())
                    RefreshSlotTypography(refreshMetrics: true);
            },
            DispatcherPriority.Loaded);
    }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        if (LongClickCommand is { } command)
            command.CanExecuteChanged += LongClickCommandCanExecuteChanged;

        UpdateLongClickGesture();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        if (LongClickCommand is { } command)
            command.CanExecuteChanged -= LongClickCommandCanExecuteChanged;

        base.OnDetachedFromLogicalTree(e);
    }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _contentClip = e.NameScope.Find<Border>("PART_ContentClip");
        UpdateContentClip();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // A supplied TextBlock remains caller-owned. The non-local template value used while it
        // occupies a ListItem slot must leave with the slot rather than turning into a persistent
        // typography override when the caller reparents that same TextBlock elsewhere.
        ClearSlotTypography();
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty)
        {
            if (!_syncingSelection && HasSelection)
                SyncSelectedToChecked();

            ApplyVisualState();
        }
        else if (change.Property == IsCheckedProperty)
        {
            if (!_syncingSelection)
            {
                if (HasSelection)
                    SyncCheckedToSelected();
                else
                    ClearCompatibilityCheckedState();
            }

            ApplyVisualState();
        }
        else if (change.Property == IsThreeStateProperty && IsThreeState)
        {
            // Neither the Compose radio-like nor checkbox-like overload admits an indeterminate
            // state. Keep the inherited ToggleButton surface from creating a fifth, undocumented
            // InteractionMode through a local IsThreeState assignment.
            SetCurrentValue(IsThreeStateProperty, false);
        }
        else if (change.Property == InteractionModeProperty)
        {
            if (HasSelection)
                SyncSelectedToChecked();
            else
                ClearCompatibilityCheckedState();

            // IsEnabledCore deliberately stops considering Command.CanExecute for a static row.
            // Button cannot know that this derived mode changed, so it needs an explicit refresh.
            UpdateIsEffectivelyEnabled();

            if (!IsInteractive)
                _interaction.Set(InteractionState.Dragged, false);
            else
                _interaction.Set(InteractionState.Dragged, EffectiveDragged);

            UpdateLongClickGesture();
            ApplyTokens();
        }
        else if (change.Property == LongClickCommandProperty)
        {
            var (oldCommand, newCommand) = change.GetOldAndNewValue<ICommand?>();
            if (((ILogical)this).IsAttachedToLogicalTree)
            {
                if (oldCommand is not null)
                    oldCommand.CanExecuteChanged -= LongClickCommandCanExecuteChanged;
                if (newCommand is not null)
                    newCommand.CanExecuteChanged += LongClickCommandCanExecuteChanged;
            }

            UpdateLongClickGesture();
        }
        else if (change.Property == LongClickCommandParameterProperty)
        {
            UpdateLongClickGesture();
        }
        else if (change.Property == IsDraggedProperty)
        {
            _interaction.Set(InteractionState.Dragged, EffectiveDragged);
            ApplyVisualState();
        }
        else if (change.Property == SegmentPositionProperty)
        {
            // ListItemDefaults.segmentedShapes(index, count) returns a new shape set when the
            // group structure changes. Compose keys its remembered animated shape by that set,
            // so the new resting shape begins at its resolved target instead of morphing from a
            // now-invalid former position.
            ApplyVisualState(snapShape: true);
        }
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            _interaction.Set(InteractionState.Dragged, EffectiveDragged);
            ApplyTokens();
        }
        else if (change.Property == BoundsProperty)
        {
            UpdateContentClip();
        }
        else if (change.Property == ContentProperty
                 || change.Property == LeadingContentProperty
                 || change.Property == TrailingContentProperty
                 || change.Property == OverlineContentProperty
                 || change.Property == SupportingContentProperty)
        {
            RefreshSlotTypography();
        }
    }

    private void ApplyTokens()
    {
        _tokens = _theme.Theme.ListItemTokens;
        _colors = _theme.Theme.GetListItemColors(_theme.IsDark);

        var typography = _theme.Theme.Typography;
        var scale = typography.GetTypeScale(_tokens.ContentTextStyle);
        FontSize = scale.FontSize;
        FontFamily = typography.GetFontFamily(_tokens.ContentTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.ContentTextStyle);
        TypographyScheme.SetTextStyle(this, _tokens.ContentTextStyle);
        if (TextElement.GetFontFeatures(this) is null)
            TextElement.SetFontFeatures(this, s_defaultTextFontFeatures);
        RefreshSlotTypography(refreshMetrics: true);

        // DefaultMinSize is a constraint, not a forced value in Compose. The template panel owns
        // the 56/72/88dp minimum and preserves an application-provided MinHeight.
        Focusable = IsInteractive;
        UpdateLongClickGesture();
        _interaction.IsInputEnabled = IsInteractive && IsEffectivelyEnabled;
        ApplyVisualState(snapShape: true);
        InvalidateMeasure();
    }

    private void UpdateLongClickGesture()
    {
        var enabled = IsInteractive && CanInvokeLongClick;
        InputElement.SetIsHoldingEnabled(this, enabled);
        // Avalonia leaves mouse holding disabled by default. A ListItem long-click action is a
        // public desktop command, so opt in explicitly instead of requiring applications to set
        // an unrelated attached property on every row.
        InputElement.SetIsHoldWithMouseEnabled(this, enabled);
    }

    private void LongClickCommandCanExecuteChanged(object? sender, EventArgs e) =>
        UpdateLongClickGesture();

    private void RefreshSlotTypography(bool refreshMetrics = false)
    {
        // The supplied controls are caller-owned. Do not recreate a lease after this ListItem has
        // detached (for example, from a queued binding update); attachment performs the first
        // application after the TextBlock's own theme has joined the visual tree.
        if (!this.IsAttachedToVisualTree())
        {
            ClearSlotTypography();
            return;
        }

        // ListItem.kt decorates each slot with ProvideTextStyleFromToken. Avalonia's
        // ContentPresenter does not make a supplied TextBlock its logical child, so the inherited
        // slot role cannot reach that TextBlock. A template-priority value recreates the decorator
        // without replacing a caller's local value or local binding. It is leased and disposed on
        // slot replacement/detach so caller-owned TextBlocks do not retain a ListItem role.
        var requested = new Dictionary<TextBlock, TypographyRole>();
        AddRequestedSlotTypography(requested, LeadingContent, TypographyRole.TitleMedium);
        AddRequestedSlotTypography(requested, TrailingContent, TypographyRole.LabelSmall);
        AddRequestedSlotTypography(requested, OverlineContent, TypographyRole.LabelSmall);
        AddRequestedSlotTypography(requested, Content, TypographyRole.BodyLarge);
        AddRequestedSlotTypography(requested, SupportingContent, TypographyRole.BodyMedium);

        var stale = new List<TextBlock>();
        foreach (var pair in _slotTypographyAssignments)
        {
            if (refreshMetrics || !requested.TryGetValue(pair.Key, out var role) || role != pair.Value.SlotRole)
            {
                DisposeSlotTypography(pair.Value);
                stale.Add(pair.Key);
            }
        }

        foreach (var textBlock in stale)
            _slotTypographyAssignments.Remove(textBlock);

        foreach (var pair in requested)
        {
            if (_slotTypographyAssignments.ContainsKey(pair.Key))
                continue;

            var assignment = CreateSlotTypographyAssignment(pair.Key, pair.Value);
            if (assignment is not null)
                _slotTypographyAssignments.Add(pair.Key, assignment);
        }
    }

    private void ClearSlotTypography()
    {
        foreach (var assignment in _slotTypographyAssignments.Values)
            DisposeSlotTypography(assignment);

        _slotTypographyAssignments.Clear();
    }

    private SlotTypographyAssignment? CreateSlotTypographyAssignment(TextBlock textBlock, TypographyRole role)
    {
        // A direct TextStyle is the caller's equivalent of Compose's explicit Text style. It owns
        // all of that TextBlock's typography, including later binding updates, so the ListItem
        // must not inject a competing role. We still repair the framework's stale conditional
        // TextBlock metrics from that explicit role, without touching an explicit Font*/spacing
        // local value or binding.
        var hasExplicitTextStyle = textBlock.GetBaseValue(TypographyScheme.TextStyleProperty).HasValue;
        var resolvedRole = hasExplicitTextStyle ? TypographyScheme.GetTextStyle(textBlock) : role;
        var textStyleLease = hasExplicitTextStyle
            ? null
            : textBlock.SetValue(TypographyScheme.TextStyleProperty, role, BindingPriority.Template);

        // TextBlock's ControlTheme may already have resolved BodyLarge from the ListItem before a
        // supplied control enters its ContentPresenter. Its conditional style remains a higher
        // StyleTrigger value, so update only that current style result rather than creating a
        // local value. Clearing the recorded current overrides restores the original style when
        // this caller-owned TextBlock leaves the slot.
        var restoreMetricOverrides = new List<Action>();
        var typography = _theme.Theme.Typography;
        var scale = typography.GetTypeScale(resolvedRole);
        OverrideSlotMetric(
            textBlock,
            TextElement.FontFamilyProperty,
            typography.GetFontFamily(resolvedRole),
            restoreMetricOverrides);
        OverrideSlotMetric(
            textBlock,
            TextElement.FontWeightProperty,
            typography.GetFontWeight(resolvedRole),
            restoreMetricOverrides);
        OverrideSlotMetric(
            textBlock,
            TextElement.FontSizeProperty,
            scale.FontSize,
            restoreMetricOverrides);
        OverrideSlotMetric(textBlock, TextBlock.LineHeightProperty, scale.LineHeight, restoreMetricOverrides);
        OverrideSlotMetric(textBlock, TextBlock.LetterSpacingProperty, scale.LetterSpacing, restoreMetricOverrides);

        // Observe every direct slot, not just slots that were already explicit when they joined
        // the ListItem. A caller can install a local TextStyle or binding after attachment; its
        // higher-priority value then replaces our leased role, but the current metric repairs for
        // the former slot role would otherwise remain. The lease is set before this subscription,
        // so ListItem's own initial template-priority role cannot schedule a redundant refresh.
        EventHandler<AvaloniaPropertyChangedEventArgs> textStyleChangedHandler = (_, change) =>
        {
            if (change.Property != TypographyScheme.TextStyleProperty)
                return;

            // A binding may change role after template attachment. The control theme's role
            // selector does not replace its earlier inherited line-height/tracking result,
            // so refresh the non-local current metrics after the binding/style turn settles.
            Dispatcher.UIThread.Post(
                () => RefreshSlotTypography(refreshMetrics: true),
                DispatcherPriority.Loaded);
        };
        textBlock.PropertyChanged += textStyleChangedHandler;

        return new SlotTypographyAssignment(
            role,
            textBlock,
            textStyleLease,
            restoreMetricOverrides,
            textStyleChangedHandler);
    }

    private static void OverrideSlotMetric<T>(
        TextBlock textBlock,
        StyledProperty<T> property,
        T value,
        List<Action> restoreMetricOverrides)
    {
        var diagnostic = textBlock.GetDiagnostic(property);
        if (diagnostic.Priority <= BindingPriority.LocalValue)
            return;

        textBlock.SetCurrentValue(property, value);
        restoreMetricOverrides.Add(() => RestoreSlotMetric(textBlock, property, value));
    }

    private static void RestoreSlotMetric<T>(TextBlock textBlock, StyledProperty<T> property, T value)
    {
        var diagnostic = textBlock.GetDiagnostic(property);
        if (diagnostic.IsOverriddenCurrentValue
            && diagnostic.Priority > BindingPriority.LocalValue
            && Equals(diagnostic.Value, value))
        {
            textBlock.ClearValue(property);
        }
    }

    private static void DisposeSlotTypography(SlotTypographyAssignment assignment)
    {
        if (assignment.ExplicitTextStyleChangedHandler is not null)
            assignment.TextBlock.PropertyChanged -= assignment.ExplicitTextStyleChangedHandler;

        foreach (var restore in assignment.RestoreMetricOverrides)
            restore();

        assignment.TextStyleLease?.Dispose();
    }

    private static void AddRequestedSlotTypography(
        Dictionary<TextBlock, TypographyRole> requested,
        object? content,
        TypographyRole role)
    {
        // Arbitrary slot controls own their own text subtree. This adaptation applies the Compose
        // ambient slot typography only when the caller explicitly supplies the direct TextBlock.
        if (content is TextBlock textBlock)
            requested[textBlock] = role;
    }

    private void ApplyVisualState(bool snapShape = false)
    {
        // Compose creates updateTransition/animateDpAsState at the final initial state. A state
        // configured before the row joins a visual tree is therefore not an animation from the
        // default row; only live state changes own a transition.
        var snapEffects = !this.IsAttachedToVisualTree();
        UpdateAnimatedColors(snapEffects);
        UpdateElevation(snapEffects);
        UpdateElevationZIndex();
        Foreground = ContentBrush();
        SetBrushes(
            SupportingContentBrush(),
            LeadingContentBrush(),
            TrailingContentBrush(),
            OverlineContentBrush());
        UpdateShape(snapShape || snapEffects);
        InvalidateVisual();
    }

    private void SetBrushes(IBrush supporting, IBrush leading, IBrush trailing, IBrush overline)
    {
        RaisePropertyChanged(SupportingBrushProperty, _supportingBrush, _supportingBrush = supporting);
        RaisePropertyChanged(LeadingBrushProperty, _leadingBrush, _leadingBrush = leading);
        RaisePropertyChanged(TrailingBrushProperty, _trailingBrush, _trailingBrush = trailing);
        RaisePropertyChanged(OverlineBrushProperty, _overlineBrush, _overlineBrush = overline);
    }

    private void SyncSelectedToChecked()
    {
        _syncingSelection = true;
        try
        {
            SetCurrentValue(IsCheckedProperty, (bool?)IsSelected);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void SyncCheckedToSelected()
    {
        _syncingSelection = true;
        try
        {
            if (IsChecked is null)
                SetCurrentValue(IsCheckedProperty, false);

            SetCurrentValue(IsSelectedProperty, IsChecked == true);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void ClearCompatibilityCheckedState()
    {
        // ToggleButton's :checked pseudo-class is meaningful only in the two selection modes.
        // Keep the public IsSelected value intact while clearing the inherited implementation
        // detail so a stale checkbox/radio visual cannot leak into Click or None.
        if (IsChecked is false)
            return;

        _syncingSelection = true;
        try
        {
            SetCurrentValue(IsCheckedProperty, false);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void UpdateShape(bool snap = false)
    {
        var target = ShapeForCurrentState();

        if (snap && !Shape.IsRunning)
        {
            Shape.SnapTo(target);
            UpdateContentClip();
            InvalidateVisual();
            return;
        }

        Shape.AnimateTo(target, _theme.Spring(_tokens.ShapeMotion));
        UpdateContentClip();
        _interaction.Motion.Start();
        InvalidateVisual();
    }

    private void UpdateContentClip()
    {
        if (_contentClip is not { } contentClip)
            return;

        var corners = ContainerCornerRadius;
        if (!contentClip.CornerRadius.Equals(corners))
            contentClip.CornerRadius = corners;
    }

    private void UpdateElevationZIndex()
    {
        // InteractiveListItem.kt raises a row only while its *animated* shadow elevation is
        // positive. A template-priority lease recreates that behaviour for ordinary Avalonia
        // sibling ordering without taking ownership away from an application-local ZIndex or
        // binding, which remains an explicit caller override.
        if (CurrentElevation() > 0)
        {
            _elevatedZIndexLease ??= SetValue(ZIndexProperty, 1, BindingPriority.Template);
            return;
        }

        _elevatedZIndexLease?.Dispose();
        _elevatedZIndexLease = null;
    }

    private RoundedCornerShape RestingShape()
    {
        var shape = _theme.Theme.Shapes.Get(_tokens.RestingShape);
        if (SegmentPosition is MaterialListItemSegmentPosition.None or MaterialListItemSegmentPosition.Middle)
            return shape;

        var outer = _theme.Theme.Shapes.Get(_tokens.SegmentedOuterShape);
        return SegmentPosition switch
        {
            MaterialListItemSegmentPosition.Single => shape with
            {
                TopStart = outer.TopStart,
                TopEnd = outer.TopEnd,
                BottomEnd = outer.BottomEnd,
                BottomStart = outer.BottomStart
            },
            MaterialListItemSegmentPosition.First => shape with
            {
                TopStart = outer.TopStart,
                TopEnd = outer.TopEnd
            },
            MaterialListItemSegmentPosition.Last => shape with
            {
                BottomEnd = outer.BottomEnd,
                BottomStart = outer.BottomStart
            },
            _ => shape
        };
    }

    private RoundedCornerShape ShapeForCurrentState()
    {
        if (!IsInteractive)
            return RestingShape();

        var interaction = _interaction.Interaction;
        ShapeRole? role = interaction.HasFlag(InteractionState.Pressed) ? _tokens.PressedShape
            : EffectiveDragged ? _tokens.DraggedShape
            : EffectiveSelected ? _tokens.SelectedShape
            : interaction.HasFlag(InteractionState.Focused) ? _tokens.FocusedShape
            : interaction.HasFlag(InteractionState.Hovered) ? _tokens.HoveredShape
            : null;
        return role is { } activeRole
            ? _theme.Theme.Shapes.Get(activeRole)
            : RestingShape();
    }

    private void UpdateAnimatedColors(bool snap)
    {
        var spring = _theme.Spring(_tokens.ColorMotion);
        var started = false;

        RetargetBrush(ref _containerBrushAnimation, TargetContainerBrush(), spring, snap, ref started);
        RetargetBrush(ref _contentBrushAnimation, TargetContentBrush(), spring, snap, ref started);
        RetargetBrush(ref _leadingBrushAnimation, TargetLeadingContentBrush(), spring, snap, ref started);
        RetargetBrush(ref _trailingBrushAnimation, TargetTrailingContentBrush(), spring, snap, ref started);
        RetargetBrush(ref _overlineBrushAnimation, TargetOverlineContentBrush(), spring, snap, ref started);
        RetargetBrush(ref _supportingBrushAnimation, TargetSupportingContentBrush(), spring, snap, ref started);

        if (started)
            _interaction.Motion.Start();
    }

    private void UpdateElevation(bool snap)
    {
        var target = TargetElevation();
        var spring = _theme.Spring(_tokens.ElevationMotion);

        if (_elevation is null)
        {
            _elevation = _interaction.Motion.AddSpring(target, spring);
            return;
        }

        if (snap)
        {
            _elevation.SnapTo(target);
            return;
        }

        _elevation.AnimateTo(target, spring);
        if (_elevation.IsRunning)
            _interaction.Motion.Start();
    }

    private void RetargetBrush(
        ref AnimatedBrush? animation,
        IBrush target,
        SpringSpec spring,
        bool snap,
        ref bool started)
    {
        if (animation is null)
        {
            animation = _interaction.Motion.Add(new AnimatedBrush(target));
            animation.SnapTo(target);
            return;
        }

        if (snap)
        {
            animation.SnapTo(target);
            return;
        }

        animation.AnimateTo(target, spring);
        started |= animation.IsRunning;
    }

    private double CurrentElevation() => _elevation?.Value ?? TargetElevation();

    private double TargetElevation() =>
        EffectiveDragged ? _tokens.DraggedContainerElevation : _tokens.ContainerElevation;

    private IBrush ContainerBrush() => _containerBrushAnimation?.Brush ?? TargetContainerBrush();

    private IBrush ContentBrush() => _contentBrushAnimation?.Brush ?? TargetContentBrush();

    private IBrush LeadingContentBrush() => _leadingBrushAnimation?.Brush ?? TargetLeadingContentBrush();

    private IBrush TrailingContentBrush() => _trailingBrushAnimation?.Brush ?? TargetTrailingContentBrush();

    private IBrush OverlineContentBrush() => _overlineBrushAnimation?.Brush ?? TargetOverlineContentBrush();

    private IBrush SupportingContentBrush() => _supportingBrushAnimation?.Brush ?? TargetSupportingContentBrush();

    private IBrush TargetContainerBrush()
    {
        if (!IsEffectivelyEnabled)
            return IsSegmented ? _colors.SegmentedContainerColor : _colors.DisabledContainerColor;
        if (EffectiveDragged)
            return _colors.DraggedContainerColor;
        if (EffectiveSelected)
            return _colors.SelectedContainerColor;
        return IsSegmented ? _colors.SegmentedContainerColor : _colors.ContainerColor;
    }

    internal void ClearSegmentPosition() => ClearValue(SegmentPositionProperty);

    private IBrush TargetContentBrush()
    {
        if (!IsEffectivelyEnabled)
            return _colors.DisabledContentColor;
        if (EffectiveDragged)
            return _colors.DraggedContentColor;
        return EffectiveSelected ? _colors.SelectedContentColor : _colors.ContentColor;
    }

    private IBrush TargetLeadingContentBrush()
    {
        if (!IsEffectivelyEnabled)
            return _colors.DisabledLeadingContentColor;
        if (EffectiveDragged)
            return _colors.DraggedLeadingContentColor;
        return EffectiveSelected ? _colors.SelectedLeadingContentColor : _colors.LeadingContentColor;
    }

    private IBrush TargetTrailingContentBrush()
    {
        if (!IsEffectivelyEnabled)
            return _colors.DisabledTrailingContentColor;
        if (EffectiveDragged)
            return _colors.DraggedTrailingContentColor;
        return EffectiveSelected ? _colors.SelectedTrailingContentColor : _colors.TrailingContentColor;
    }

    private IBrush TargetOverlineContentBrush()
    {
        if (!IsEffectivelyEnabled)
            return _colors.DisabledOverlineContentColor;
        if (EffectiveDragged)
            return _colors.DraggedOverlineContentColor;
        return EffectiveSelected ? _colors.SelectedOverlineContentColor : _colors.OverlineContentColor;
    }

    private IBrush TargetSupportingContentBrush()
    {
        if (!IsEffectivelyEnabled)
            return _colors.DisabledSupportingContentColor;
        if (EffectiveDragged)
            return _colors.DraggedSupportingContentColor;
        return EffectiveSelected ? _colors.SelectedSupportingContentColor : _colors.SupportingContentColor;
    }
}

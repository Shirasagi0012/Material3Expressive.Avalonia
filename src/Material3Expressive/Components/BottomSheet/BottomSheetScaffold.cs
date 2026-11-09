// BottomSheetScaffold.kt, fixed AndroidX baseline. Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;

using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

public sealed class BottomSheetScaffoldState
{
    public BottomSheetScaffoldState() : this(new SheetState(SheetValue.PartiallyExpanded, [SheetValue.PartiallyExpanded, SheetValue.Expanded]), new SnackbarHostState()) { }
    public BottomSheetScaffoldState(SheetState bottomSheetState, SnackbarHostState snackbarHostState)
    { BottomSheetState = bottomSheetState ?? throw new ArgumentNullException(nameof(bottomSheetState)); SnackbarHostState = snackbarHostState ?? throw new ArgumentNullException(nameof(snackbarHostState)); }
    public SheetState BottomSheetState { get; }
    public SnackbarHostState SnackbarHostState { get; }
}

/// <summary>A persistent bottom sheet, body, top bar and snackbar host in one finite-height layout.</summary>
public class BottomSheetScaffold : ContentControl
{
    public static readonly StyledProperty<MaterialShape?> SheetShapeProperty = AvaloniaProperty.Register<BottomSheetScaffold, MaterialShape?>(nameof(SheetShape));
    /// <summary>Explicit sheet outline; null uses SheetCornerRadius.</summary>
    public MaterialShape? SheetShape { get => GetValue(SheetShapeProperty); set => SetValue(SheetShapeProperty, value); }
    public static readonly StyledProperty<BottomSheetScaffoldState> StateProperty = AvaloniaProperty.Register<BottomSheetScaffold, BottomSheetScaffoldState>(nameof(State));
    public static readonly StyledProperty<object?> SheetContentProperty = AvaloniaProperty.Register<BottomSheetScaffold, object?>(nameof(SheetContent));
    public static readonly StyledProperty<IDataTemplate?> SheetContentTemplateProperty = AvaloniaProperty.Register<BottomSheetScaffold, IDataTemplate?>(nameof(SheetContentTemplate));
    public static readonly StyledProperty<object?> TopBarProperty = AvaloniaProperty.Register<BottomSheetScaffold, object?>(nameof(TopBar));
    public static readonly StyledProperty<IDataTemplate?> TopBarTemplateProperty = AvaloniaProperty.Register<BottomSheetScaffold, IDataTemplate?>(nameof(TopBarTemplate));
    public static readonly StyledProperty<object?> SnackbarHostProperty = AvaloniaProperty.Register<BottomSheetScaffold, object?>(nameof(SnackbarHost));
    public static readonly StyledProperty<IDataTemplate?> SnackbarHostTemplateProperty = AvaloniaProperty.Register<BottomSheetScaffold, IDataTemplate?>(nameof(SnackbarHostTemplate));
    public static readonly StyledProperty<double> SheetPeekHeightProperty = AvaloniaProperty.Register<BottomSheetScaffold, double>(nameof(SheetPeekHeight));
    public static readonly StyledProperty<double> SheetMaxWidthProperty = AvaloniaProperty.Register<BottomSheetScaffold, double>(nameof(SheetMaxWidth));
    public static readonly StyledProperty<bool> SheetSwipeEnabledProperty = AvaloniaProperty.Register<BottomSheetScaffold, bool>(nameof(SheetSwipeEnabled), true);
    public static readonly StyledProperty<IBrush?> SheetBackgroundProperty = AvaloniaProperty.Register<BottomSheetScaffold, IBrush?>(nameof(SheetBackground));
    public static readonly StyledProperty<IBrush?> SheetForegroundProperty = AvaloniaProperty.Register<BottomSheetScaffold, IBrush?>(nameof(SheetForeground));
    public static readonly StyledProperty<CornerRadius> SheetCornerRadiusProperty = AvaloniaProperty.Register<BottomSheetScaffold, CornerRadius>(nameof(SheetCornerRadius));
    public static readonly StyledProperty<double> SheetTonalElevationProperty = AvaloniaProperty.Register<BottomSheetScaffold, double>(nameof(SheetTonalElevation));
    public static readonly StyledProperty<double> SheetShadowElevationProperty = AvaloniaProperty.Register<BottomSheetScaffold, double>(nameof(SheetShadowElevation));
    public static readonly StyledProperty<object?> SheetDragHandleProperty = AvaloniaProperty.Register<BottomSheetScaffold, object?>(nameof(SheetDragHandle), string.Empty);
    public static readonly StyledProperty<IDataTemplate?> SheetDragHandleTemplateProperty = AvaloniaProperty.Register<BottomSheetScaffold, IDataTemplate?>(nameof(SheetDragHandleTemplate));
    public static readonly DirectProperty<BottomSheetScaffold, Thickness> ContentPaddingProperty = AvaloniaProperty.RegisterDirect<BottomSheetScaffold, Thickness>(nameof(ContentPadding), s => s.ContentPadding);
    public BottomSheetScaffoldState State { get => GetValue(StateProperty); set => SetValue(StateProperty, value ?? throw new ArgumentNullException(nameof(value))); }
    public object? SheetContent { get => GetValue(SheetContentProperty); set => SetValue(SheetContentProperty, value); }
    public IDataTemplate? SheetContentTemplate { get => GetValue(SheetContentTemplateProperty); set => SetValue(SheetContentTemplateProperty, value); }
    public object? TopBar { get => GetValue(TopBarProperty); set => SetValue(TopBarProperty, value); }
    public IDataTemplate? TopBarTemplate { get => GetValue(TopBarTemplateProperty); set => SetValue(TopBarTemplateProperty, value); }
    public object? SnackbarHost { get => GetValue(SnackbarHostProperty); set => SetValue(SnackbarHostProperty, value); }
    public IDataTemplate? SnackbarHostTemplate { get => GetValue(SnackbarHostTemplateProperty); set => SetValue(SnackbarHostTemplateProperty, value); }
    public double SheetPeekHeight { get => GetValue(SheetPeekHeightProperty); set => SetValue(SheetPeekHeightProperty, value); }
    public double SheetMaxWidth { get => GetValue(SheetMaxWidthProperty); set => SetValue(SheetMaxWidthProperty, value); }
    public bool SheetSwipeEnabled { get => GetValue(SheetSwipeEnabledProperty); set => SetValue(SheetSwipeEnabledProperty, value); }
    public IBrush? SheetBackground { get => GetValue(SheetBackgroundProperty); set => SetValue(SheetBackgroundProperty, value); }
    public IBrush? SheetForeground { get => GetValue(SheetForegroundProperty); set => SetValue(SheetForegroundProperty, value); }
    public CornerRadius SheetCornerRadius { get => GetValue(SheetCornerRadiusProperty); set => SetValue(SheetCornerRadiusProperty, value); }
    public double SheetTonalElevation { get => GetValue(SheetTonalElevationProperty); set => SetValue(SheetTonalElevationProperty, value); }
    public double SheetShadowElevation { get => GetValue(SheetShadowElevationProperty); set => SetValue(SheetShadowElevationProperty, value); }
    public object? SheetDragHandle { get => GetValue(SheetDragHandleProperty); set => SetValue(SheetDragHandleProperty, value); }
    public IDataTemplate? SheetDragHandleTemplate { get => GetValue(SheetDragHandleTemplateProperty); set => SetValue(SheetDragHandleTemplateProperty, value); }
    public Thickness ContentPadding => _contentPadding;
    private Thickness _contentPadding;
    private BottomSheetScaffoldLayout? _layout;
    private BottomSheet? _sheet;
    private SheetState? _observedState;
    private readonly SnackbarHost _defaultSnackbar = new();

    public BottomSheetScaffold()
    {
        SetCurrentValue(StateProperty, new BottomSheetScaffoldState());
        _defaultSnackbar.State = State.SnackbarHostState;
        SetCurrentValue(SnackbarHostProperty, _defaultSnackbar);
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_layout is not null) _layout.Owner = null;
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<BottomSheetScaffoldLayout>("PART_Layout");
        _sheet = e.NameScope.Find<BottomSheet>("PART_Sheet");
        if (_layout is not null) _layout.Owner = this;
        ObserveState();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); ObserveState(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observedState is { } old) old.PropertyChanged -= StateChanged;
        _observedState = null; base.OnDetachedFromVisualTree(e);
    }
    private void ObserveState()
    {
        if (!this.IsAttachedToVisualTree() || _observedState == State.BottomSheetState) return;
        if (_observedState is { } old) old.PropertyChanged -= StateChanged;
        _observedState = State.BottomSheetState; _observedState.PropertyChanged += StateChanged;
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == SheetState.CurrentOffsetProperty || e.Property == SheetState.CurrentValueProperty) _layout?.InvalidateArrange();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SheetContentProperty || change.Property == TopBarProperty || change.Property == SnackbarHostProperty || change.Property == SheetDragHandleProperty)
        {
            if (change.OldValue is ILogical oldChild) LogicalChildren.Remove(oldChild);
            if (change.NewValue is ILogical newChild) LogicalChildren.Add(newChild);
        }
        if (change.Property == StateProperty)
        { if (_defaultSnackbar is not null) _defaultSnackbar.State = State.SnackbarHostState; ObserveState(); }
        if (change.Property == SheetPeekHeightProperty) _layout?.InvalidateMeasure();
    }
    internal void UpdateContentPadding(Thickness padding) => SetAndRaise(ContentPaddingProperty, ref _contentPadding, padding);
    public double OnPreScroll(double availableY, bool isUserInput = true) => _sheet?.OnPreScroll(availableY, isUserInput) ?? 0;
    public double OnPostScroll(double availableY, bool isUserInput = true) => _sheet?.OnPostScroll(availableY, isUserInput) ?? 0;
    public Task<double> OnPreFlingAsync(double availableY, CancellationToken cancellationToken = default) => _sheet?.OnPreFlingAsync(availableY, cancellationToken) ?? Task.FromResult(0d);
    public Task<double> OnPostFlingAsync(double availableY, CancellationToken cancellationToken = default) => _sheet?.OnPostFlingAsync(availableY, cancellationToken) ?? Task.FromResult(0d);
}

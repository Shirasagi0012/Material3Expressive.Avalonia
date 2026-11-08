using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

// Owns the inner hoverable / pressable / indication area, inside minimumInteractiveComponentSize.
internal sealed class DragHandlePresenter : TemplatedControl
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<DragHandleSizes?> SizesProperty = VerticalDragHandle.SizesProperty.AddOwner<DragHandlePresenter>();
    public static readonly StyledProperty<DragHandleColors?> ColorsProperty = VerticalDragHandle.ColorsProperty.AddOwner<DragHandlePresenter>();
    public static readonly StyledProperty<DragHandleShapes?> ShapesProperty = VerticalDragHandle.ShapesProperty.AddOwner<DragHandlePresenter>();
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = VerticalDragHandle.InteractionSourceProperty.AddOwner<DragHandlePresenter>();
    public DragHandleSizes? Sizes { get => GetValue(SizesProperty); set => SetValue(SizesProperty, value); }
    public DragHandleColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public DragHandleShapes? Shapes { get => GetValue(ShapesProperty); set => SetValue(ShapesProperty, value); }
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = VerticalDragHandle.ShapeProperty.AddOwner<DragHandlePresenter>();
    public static readonly StyledProperty<MaterialShape?> PressedShapeProperty = VerticalDragHandle.PressedShapeProperty.AddOwner<DragHandlePresenter>();
    public static readonly StyledProperty<MaterialShape?> DraggedShapeProperty = VerticalDragHandle.DraggedShapeProperty.AddOwner<DragHandlePresenter>();
    public static readonly StyledProperty<MaterialShape?> CurrentShapeProperty = AvaloniaProperty.Register<DragHandlePresenter, MaterialShape?>(nameof(CurrentShape));
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public MaterialShape? PressedShape { get => GetValue(PressedShapeProperty); set => SetValue(PressedShapeProperty, value); }
    public MaterialShape? DraggedShape { get => GetValue(DraggedShapeProperty); set => SetValue(DraggedShapeProperty, value); }
    public MaterialShape? CurrentShape { get => GetValue(CurrentShapeProperty); private set => SetValue(CurrentShapeProperty, value); }
    private readonly MaterialInteractionController _interaction;
    private VerticalDragHandle? _owner;
    private MaterialInteractionSource? _connectedSource;
    private TopLevel? _topLevel;
    private IPointer? _pointer;
    internal bool IsPressed { get; private set; }
    internal bool IsDragged => _owner?.IsDragged ?? false;

    public DragHandlePresenter()
    {
        _interaction = MaterialInteraction.GetOrCreate(this);
        _interaction.ObservePointerPresses = false;
        AddHandler(PointerPressedEvent, Pressed, RoutingStrategies.Tunnel);
        PointerCaptureLost += (_, e) => { if (ReferenceEquals(_pointer, e.Pointer)) Release(); };
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = TemplatedParent as VerticalDragHandle;
        if (_owner is not null) _owner.DraggedChanged += Refresh;
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(PointerReleasedEvent, Released, RoutingStrategies.Tunnel, true);
        _topLevel?.AddHandler(PointerMovedEvent, Moved, RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);
        ConnectSource();
        Refresh();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Release();
        _topLevel?.RemoveHandler(PointerReleasedEvent, Released);
        _topLevel?.RemoveHandler(PointerMovedEvent, Moved);
        _topLevel = null;
        if (_owner is not null) _owner.DraggedChanged -= Refresh;
        _owner = null;
        _interaction.ConnectSource(null);
        _connectedSource = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty && _topLevel is not null) ConnectSource();
        if (change.Property == SizesProperty || change.Property == ColorsProperty || change.Property == ShapesProperty
            || change.Property == ShapeProperty || change.Property == PressedShapeProperty || change.Property == DraggedShapeProperty) Refresh();
        if (change.Property == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) Release();
    }

    private void ConnectSource()
    {
        var source = _owner?.ActualInteractionSource;
        var replaced = _connectedSource is not null && !ReferenceEquals(_connectedSource, source);
        _interaction.ConnectSource(source);
        // indication(source, ripple()) replaces its node on source changes; its
        // animations restart independently of collectIsDraggedAsState above it.
        if (replaced) _interaction.ResetSourceIndication();
        _connectedSource = source;
    }

    private void Pressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer is not null || !IsEffectivelyEnabled || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _pointer = e.Pointer; IsPressed = true; Refresh();
        // pressable observes Initial without consuming or capturing: a host may own the drag.
    }
    private void Released(object? sender, PointerReleasedEventArgs e)
    {
        if (ReferenceEquals(_pointer, e.Pointer)) Release();
    }
    private void Moved(object? sender, PointerEventArgs e)
    {
        if (ReferenceEquals(_pointer, e.Pointer) && (e.Handled || !new Rect(Bounds.Size).Contains(e.GetPosition(this)))) Release();
    }
    private void Release() { _pointer = null; IsPressed = false; Refresh(); }

    private void Refresh()
    {
        if (Sizes is { } sizes)
        {
            var size = IsDragged ? sizes.DraggedSize : IsPressed ? sizes.PressedSize : sizes.Size;
            Width = size.Width; Height = size.Height;
        }
        if (Colors is { } colors) Background = new SolidColorBrush(IsDragged ? colors.DraggedColor : IsPressed ? colors.PressedColor : colors.Color);
        if (Shapes is { } shapes) CornerRadius = IsDragged ? shapes.DraggedShape : IsPressed ? shapes.PressedShape : shapes.Shape;
        CurrentShape = IsDragged ? DraggedShape : IsPressed ? PressedShape : Shape;
    }
}

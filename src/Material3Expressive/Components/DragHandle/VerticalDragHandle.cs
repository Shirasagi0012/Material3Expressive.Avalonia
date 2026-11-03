// DragHandle.kt, AndroidX f3ed195f0c9e (identical in the pinned Core); Apache-2.0.
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

public sealed record DragHandleSizes(Size Size, Size PressedSize, Size DraggedSize)
{
    public DragHandleSizes() : this(default, default, default) { }
}
public sealed record DragHandleColors(Color Color, Color PressedColor, Color DraggedColor)
{
    public DragHandleColors() : this(default, default, default) { }
}
public sealed record DragHandleShapes(CornerRadius Shape, CornerRadius PressedShape, CornerRadius DraggedShape)
{
    public DragHandleShapes() : this(default, default, default) { }
}

/// <summary>A vertical handle indicating horizontal dragging. The host owns the drag gesture and position.</summary>
public class VerticalDragHandle : TemplatedControl
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<DragHandleSizes?> SizesProperty = AvaloniaProperty.Register<VerticalDragHandle, DragHandleSizes?>(nameof(Sizes));
    public static readonly StyledProperty<DragHandleColors?> ColorsProperty = AvaloniaProperty.Register<VerticalDragHandle, DragHandleColors?>(nameof(Colors));
    public static readonly StyledProperty<DragHandleShapes?> ShapesProperty = AvaloniaProperty.Register<VerticalDragHandle, DragHandleShapes?>(nameof(Shapes));
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = AvaloniaProperty.Register<VerticalDragHandle, MaterialInteractionSource?>(nameof(InteractionSource));

    public DragHandleSizes? Sizes { get => GetValue(SizesProperty); set => SetValue(SizesProperty, value); }
    public DragHandleColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public DragHandleShapes? Shapes { get => GetValue(ShapesProperty); set => SetValue(ShapesProperty, value); }
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }

    private readonly MaterialInteractionSource _localSource = new();
    private readonly List<MaterialInteractionEvent.DragStart> _drags = [];
    private MaterialInteractionSource? _source;
    internal MaterialInteractionSource ActualInteractionSource => InteractionSource ?? _localSource;
    internal bool IsDragged { get; private set; }
    internal event Action? DraggedChanged;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ConnectSource();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_source is not null) _source.Interaction -= Receive;
        _source = null; _drags.Clear(); IsDragged = false;
        DraggedChanged?.Invoke();
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty && _source is not null) ConnectSource();
    }
    private void ConnectSource()
    {
        if (_source is not null) _source.Interaction -= Receive;
        _source = ActualInteractionSource; _drags.Clear();
        _source.Interaction += Receive;
        // collectIsDraggedAsState remembers the old value until the new collector's
        // first event. Keep this collector on the owner across template replacement.
    }
    private void Receive(MaterialInteractionEvent interaction)
    {
        switch (interaction)
        {
            case MaterialInteractionEvent.DragStart start: _drags.Add(start); break;
            case MaterialInteractionEvent.DragStop stop: _drags.Remove(stop.Start); break;
            case MaterialInteractionEvent.DragCancel cancel: _drags.Remove(cancel.Start); break;
        }
        var dragged = _drags.Count > 0;
        if (IsDragged == dragged) return;
        IsDragged = dragged; DraggedChanged?.Invoke();
    }
}

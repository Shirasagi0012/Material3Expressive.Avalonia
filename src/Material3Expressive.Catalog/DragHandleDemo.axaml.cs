using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using ComposeAnimation;
using Material3Expressive.Components;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Catalog;

public partial class DragHandleDemo : Window
{
    private readonly MaterialInteractionSource _source = new();
    private MaterialInteractionEvent.DragStart? _drag;
    private IPointer? _pointer;
    private double _startX, _startWidth;

    public DragHandleDemo()
    {
        AnimationScope.Initialize(this);
        InitializeComponent();
        Handle.InteractionSource = _source;
        CutHandle.InteractionSource = _source;
        LinkedCard.InteractionSource = _source;
        LinkedChip.InteractionSource = _source;
        LinkedCard.Bind(IsEnabledProperty, new Avalonia.Data.Binding("IsChecked") { Source = FeedbackEnabled });
        LinkedChip.Bind(IsEnabledProperty, new Avalonia.Data.Binding("IsChecked") { Source = FeedbackEnabled });
        CustomHandle.Sizes = new(new Size(8, 40), new Size(14, 56), new Size(20, 64));
        CustomHandle.Shapes = new(new CornerRadius(3), new CornerRadius(4), new CornerRadius(6));
        CustomHandle.Colors = new(Colors.SlateBlue, Colors.DarkSlateBlue, Colors.MediumPurple);
        Closed += (_, _) => EndDrag(cancelled: true);
    }

    private bool _customElevations;
    private void OnChangeElevations(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _customElevations = !_customElevations;
        if (_customElevations)
        {
            LinkedCard.Elevation = new() { Default = 2, Pressed = 3, Focused = 4, Hovered = 5, Dragged = 12.5, Disabled = 1 };
            LinkedChip.Elevation = new(2, 3, 4, 5, 12.5, 1);
        }
        else
        {
            LinkedCard.ClearValue(Card.ElevationProperty);
            LinkedChip.ClearValue(Chip.ElevationProperty);
        }
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_pointer is not null || !e.GetCurrentPoint(DragRegion).Properties.IsLeftButtonPressed) return;
        _pointer = e.Pointer;
        _startX = e.GetPosition(Panels).X;
        _startWidth = Panels.ColumnDefinitions[0].ActualWidth;
        e.Pointer.Capture(DragRegion);
        _drag = new MaterialInteractionEvent.DragStart();
        _source.Emit(_drag);
        e.Handled = true;
    }

    private void OnMoved(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(_pointer, e.Pointer)) return;
        var width = Math.Clamp(_startWidth + e.GetPosition(Panels).X - _startX, 120, Math.Max(120, Panels.Bounds.Width - 168));
        Panels.ColumnDefinitions[0].Width = new GridLength(width);
        LinkedCard.Width = width;
        Status.Text = $"First panel: {width:F0} dip";
        e.Handled = true;
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!ReferenceEquals(_pointer, e.Pointer)) return;
        EndDrag(cancelled: false);
        e.Handled = true;
    }

    private void OnCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (ReferenceEquals(_pointer, e.Pointer)) EndDrag(cancelled: true);
    }

    private void EndDrag(bool cancelled)
    {
        var pointer = _pointer;
        _pointer = null;
        if (_drag is { } drag)
        {
            _drag = null;
            _source.Emit(cancelled ? new MaterialInteractionEvent.DragCancel(drag) : new MaterialInteractionEvent.DragStop(drag));
        }
        pointer?.Capture(null);
    }
}

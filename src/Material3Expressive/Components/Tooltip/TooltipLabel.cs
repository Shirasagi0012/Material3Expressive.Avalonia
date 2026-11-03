/*
 * Portions translated from AndroidX Label.kt, Copyright 2023 The Android Open Source Project.
 * Licensed under the Apache License, Version 2.0 (http://www.apache.org/licenses/LICENSE-2.0).
 * Fixed baseline: docs/upstream-baseline.json; Label / HandleInteractions / LabelStateImpl.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>Appends a label above its content while the shared interaction source is active.</summary>
/// <remarks>
/// Corresponds to Material3 Label, not Avalonia's text Label. Only press, drag and hover events
/// control visibility. The content must use the same interaction source; this wrapper does not
/// synthesize input. Persistent labels are always visible. Labels do not animate or arbitrate with
/// other tooltips, and do not take keyboard focus.
/// </remarks>
public class TooltipLabel : ContentControl
{
    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<TooltipLabel, object?>(nameof(Label));
    public static readonly StyledProperty<IDataTemplate?> LabelTemplateProperty =
        AvaloniaProperty.Register<TooltipLabel, IDataTemplate?>(nameof(LabelTemplate));
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty =
        AvaloniaProperty.Register<TooltipLabel, MaterialInteractionSource?>(nameof(InteractionSource));
    public static readonly StyledProperty<bool> IsPersistentProperty =
        AvaloniaProperty.Register<TooltipLabel, bool>(nameof(IsPersistent));

    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public IDataTemplate? LabelTemplate { get => GetValue(LabelTemplateProperty); set => SetValue(LabelTemplateProperty, value); }
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    public bool IsPersistent { get => GetValue(IsPersistentProperty); set => SetValue(IsPersistentProperty, value); }

    private readonly TooltipPresenter _presenter = new();
    private readonly Popup _popup;
    private readonly List<MaterialInteractionEvent> _activeInteractions = [];
    private MaterialInteractionSource? _source;
    private Grid? _root;
    private TopLevel? _topLevel;
    private bool _wasVisible, _visible;
    private Size _lastWindowSize;
    private readonly TooltipAnchor _anchor;

    internal bool IsPresented => _popup.IsOpen;

    public TooltipLabel()
    {
        _anchor = new TooltipAnchor(this, () => UpdatePosition());
        _popup = new Popup
        {
            Child = _presenter, ShouldUseOverlayLayer = true, IsLightDismissEnabled = false,
            TakesFocusFromNativeControl = false, Placement = PlacementMode.Custom,
            CustomPopupPlacementCallback = PlacePopup
        };
        _popup.Styles.Add(new Style(selector => selector.OfType<OverlayPopupHost>())
            { Setters = { new Setter(ClipToBoundsProperty, false) } });
        _popup.Styles.Add(new Style(selector => selector.OfType<OverlayPopupHost>().Template().OfType<LayoutTransformControl>())
            { Setters = { new Setter(ClipToBoundsProperty, false) } });
        LogicalChildren.Add(_popup);
        _presenter.Bind(ContentPresenter.ContentProperty, this.GetObservable(LabelProperty));
        _presenter.Bind(ContentPresenter.ContentTemplateProperty, this.GetObservable(LabelTemplateProperty));
        LayoutUpdated += (_, _) => UpdatePosition();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _popup.Close();
        _root?.Children.Remove(_popup);
        base.OnApplyTemplate(e);
        _root = e.NameScope.Find<Grid>("PART_Root");
        _root?.Children.Add(_popup);
        UpdatePresentation();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _anchor.Attach();
        _topLevel?.AddHandler(PointerPressedEvent, OnOutsidePressed, RoutingStrategies.Tunnel, true);
        AttachSource();
        // A Window being mounted establishes its overlay layer in the first layout pass.
        Dispatcher.UIThread.Post(() =>
        {
            if (this.IsAttachedToVisualTree()) { ApplyTemplate(); UpdatePresentation(); }
        });
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _anchor.Detach();
        _topLevel?.RemoveHandler(PointerPressedEvent, OnOutsidePressed);
        DetachSource();
        _topLevel = null;
        _popup.Close();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LabelProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical child) LogicalChildren.Add(child);
        }
        if ((change.Property == InteractionSourceProperty || change.Property == IsPersistentProperty)
            && this.IsAttachedToVisualTree())
        {
            DetachSource();
            AttachSource();
        }
        if (change.Property == FlowDirectionProperty)
            UpdatePosition(force: true);
    }

    private void AttachSource()
    {
        _visible = IsPersistent;
        if (!IsPersistent && InteractionSource is { } source)
        {
            _source = source;
            source.Interaction += OnInteraction;
        }
        UpdatePresentation();
    }

    private void DetachSource()
    {
        if (_source is not null) _source.Interaction -= OnInteraction;
        _source = null;
        _activeInteractions.Clear();
        _wasVisible = _visible = false;
    }

    private void OnInteraction(MaterialInteractionEvent interaction)
    {
        // Label.kt HandleInteractions: ending events remove their exact start object. React only
        // when the aggregate changes, so hover exit during an ongoing drag cannot hide the label.
        switch (interaction)
        {
            case MaterialInteractionEvent.Press or MaterialInteractionEvent.DragStart or MaterialInteractionEvent.HoverEnter:
                _activeInteractions.Add(interaction); break;
            case MaterialInteractionEvent.Release release: _activeInteractions.Remove(release.Start); break;
            case MaterialInteractionEvent.CancelPress cancel: _activeInteractions.Remove(cancel.Start); break;
            case MaterialInteractionEvent.DragStop stop: _activeInteractions.Remove(stop.Start); break;
            case MaterialInteractionEvent.DragCancel cancel: _activeInteractions.Remove(cancel.Start); break;
            case MaterialInteractionEvent.HoverExit exit: _activeInteractions.Remove(exit.Start); break;
        }
        var visible = _activeInteractions.Count != 0;
        if (visible == _wasVisible) return;
        _wasVisible = _visible = visible;
        UpdatePresentation();
    }

    private void UpdatePresentation()
    {
        if (_root is null || _topLevel is null) return;
        if (_visible)
        {
            _popup.PlacementTarget = this;
            if (!_popup.IsOpen) _popup.Open();
            UpdatePosition();
        }
        else _popup.Close();
    }

    private void UpdatePosition(bool force = false)
    {
        if (!_popup.IsOpen || _topLevel is null) return;
        if (_lastWindowSize != _topLevel.ClientSize) force = true;
        _lastWindowSize = _topLevel.ClientSize;
        if (force) _popup.PlacementRect = null;
        _popup.PlacementRect = TooltipAnchor.BoundsInWindow(this, _topLevel);
    }

    private void PlacePopup(CustomPopupPlacement placement)
    {
        var topLevel = _topLevel!;
        var density = topLevel.RenderScaling;
        int Pixel(double dip) => (int)MathF.Floor((float)(dip * density) + .5f);
        var bounds = TooltipAnchor.BoundsInWindow(this, topLevel);
        var point = TooltipDefaults.CreatePositionProvider(renderScaling: density).CalculatePosition(
            new PixelRect(Pixel(bounds.Left), Pixel(bounds.Top), Pixel(bounds.Width), Pixel(bounds.Height)),
            new PixelSize(Pixel(topLevel.ClientSize.Width), Pixel(topLevel.ClientSize.Height)),
            FlowDirection == FlowDirection.RightToLeft,
            new PixelSize(Pixel(placement.PopupSize.Width), Pixel(placement.PopupSize.Height)));
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        placement.AnchorRectangle = new Rect(point.X / density, point.Y / density, 0, 0);
        placement.Anchor = rtl ? PopupAnchor.TopRight : PopupAnchor.TopLeft;
        placement.Gravity = rtl ? PopupGravity.BottomLeft : PopupGravity.BottomRight;
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
    }

    private void OnOutsidePressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_popup.IsOpen || IsPersistent || _presenter.Bounds.Contains(e.GetPosition(_presenter))) return;
        // BasicTooltipState.dismiss does not change the interaction collector's wasVisible flag.
        // Another start while the aggregate remains active must not reopen the dismissed popup.
        _visible = false;
        UpdatePresentation();
    }
}

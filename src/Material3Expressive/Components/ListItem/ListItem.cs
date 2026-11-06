// New ListItem / SegmentedListItem overloads at AndroidX f3ed195f0c9e.
// Copyright 2022-2025 The Android Open Source Project; Apache-2.0.
using System.Windows.Input;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Motion;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

public enum ListItemInteractionMode { None, Click, SingleSelection, MultipleSelection }
public enum ListItemVerticalAlignment { Auto, Top, Center, Bottom }
public sealed class ListItemCheckedChangeEventArgs(bool value) : EventArgs { public bool Value { get; } = value; }
public sealed record ListItemShapes(CornerRadius Shape, CornerRadius SelectedShape, CornerRadius PressedShape,
    CornerRadius FocusedShape, CornerRadius HoveredShape, CornerRadius DraggedShape)
{
    public ListItemShapes() : this(new(4), new(16), new(16), new(16), new(12), new(16)) { }
}

public class ListItem : Button
{
    internal static readonly StyledProperty<double> VerticalAlignmentBreakpointProperty = AvaloniaProperty.Register<ListItem, double>(nameof(VerticalAlignmentBreakpoint));
    internal double VerticalAlignmentBreakpoint { get => GetValue(VerticalAlignmentBreakpointProperty); set => SetValue(VerticalAlignmentBreakpointProperty, value); }
    internal static readonly StyledProperty<double> MinimumLayoutSizeProperty = AvaloniaProperty.Register<ListItem, double>(nameof(MinimumLayoutSize));
    internal double MinimumLayoutSize { get => GetValue(MinimumLayoutSizeProperty); private set => SetValue(MinimumLayoutSizeProperty, value); }
    public static readonly StyledProperty<ListItemInteractionMode> InteractionModeProperty = AvaloniaProperty.Register<ListItem, ListItemInteractionMode>(nameof(InteractionMode));
    public ListItemInteractionMode InteractionMode { get => GetValue(InteractionModeProperty); set => SetValue(InteractionModeProperty, value); }
    public static readonly StyledProperty<bool> IsInteractionEnabledProperty = AvaloniaProperty.Register<ListItem, bool>(nameof(IsInteractionEnabled), true);
    public bool IsInteractionEnabled { get => GetValue(IsInteractionEnabledProperty); set => SetValue(IsInteractionEnabledProperty, value); }
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<ListItem, bool>(nameof(IsSelected));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public static readonly StyledProperty<bool> IsCheckedProperty = AvaloniaProperty.Register<ListItem, bool>(nameof(IsChecked));
    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = AvaloniaProperty.Register<ListItem, MaterialInteractionSource?>(nameof(InteractionSource));
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    public static readonly StyledProperty<ListItemShapes?> ShapesProperty = AvaloniaProperty.Register<ListItem, ListItemShapes?>(nameof(Shapes));
    public ListItemShapes? Shapes { get => GetValue(ShapesProperty); set => SetValue(ShapesProperty, value); }
    public static readonly StyledProperty<ListItemVerticalAlignment> ItemVerticalAlignmentProperty = AvaloniaProperty.Register<ListItem, ListItemVerticalAlignment>(nameof(ItemVerticalAlignment));
    public ListItemVerticalAlignment ItemVerticalAlignment { get => GetValue(ItemVerticalAlignmentProperty); set => SetValue(ItemVerticalAlignmentProperty, value); }
    public static readonly StyledProperty<ICommand?> LongClickCommandProperty = AvaloniaProperty.Register<ListItem, ICommand?>(nameof(LongClickCommand));
    public ICommand? LongClickCommand { get => GetValue(LongClickCommandProperty); set => SetValue(LongClickCommandProperty, value); }
    public static readonly StyledProperty<object?> LongClickCommandParameterProperty = AvaloniaProperty.Register<ListItem, object?>(nameof(LongClickCommandParameter));
    public object? LongClickCommandParameter { get => GetValue(LongClickCommandParameterProperty); set => SetValue(LongClickCommandParameterProperty, value); }
    public static readonly StyledProperty<string?> LongClickLabelProperty = AvaloniaProperty.Register<ListItem, string?>(nameof(LongClickLabel));
    public string? LongClickLabel { get => GetValue(LongClickLabelProperty); set => SetValue(LongClickLabelProperty, value); }
    public static readonly StyledProperty<object?> LeadingContentProperty = AvaloniaProperty.Register<ListItem, object?>(nameof(LeadingContent));
    public object? LeadingContent { get => GetValue(LeadingContentProperty); set => SetValue(LeadingContentProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> LeadingContentTemplateProperty = AvaloniaProperty.Register<ListItem, IDataTemplate?>(nameof(LeadingContentTemplate));
    public IDataTemplate? LeadingContentTemplate { get => GetValue(LeadingContentTemplateProperty); set => SetValue(LeadingContentTemplateProperty, value); }
    public static readonly StyledProperty<object?> TrailingContentProperty = AvaloniaProperty.Register<ListItem, object?>(nameof(TrailingContent));
    public object? TrailingContent { get => GetValue(TrailingContentProperty); set => SetValue(TrailingContentProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> TrailingContentTemplateProperty = AvaloniaProperty.Register<ListItem, IDataTemplate?>(nameof(TrailingContentTemplate));
    public IDataTemplate? TrailingContentTemplate { get => GetValue(TrailingContentTemplateProperty); set => SetValue(TrailingContentTemplateProperty, value); }
    public static readonly StyledProperty<object?> OverlineContentProperty = AvaloniaProperty.Register<ListItem, object?>(nameof(OverlineContent));
    public object? OverlineContent { get => GetValue(OverlineContentProperty); set => SetValue(OverlineContentProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> OverlineContentTemplateProperty = AvaloniaProperty.Register<ListItem, IDataTemplate?>(nameof(OverlineContentTemplate));
    public IDataTemplate? OverlineContentTemplate { get => GetValue(OverlineContentTemplateProperty); set => SetValue(OverlineContentTemplateProperty, value); }
    public static readonly StyledProperty<object?> SupportingContentProperty = AvaloniaProperty.Register<ListItem, object?>(nameof(SupportingContent));
    public object? SupportingContent { get => GetValue(SupportingContentProperty); set => SetValue(SupportingContentProperty, value); }
    public static readonly StyledProperty<IDataTemplate?> SupportingContentTemplateProperty = AvaloniaProperty.Register<ListItem, IDataTemplate?>(nameof(SupportingContentTemplate));
    public IDataTemplate? SupportingContentTemplate { get => GetValue(SupportingContentTemplateProperty); set => SetValue(SupportingContentTemplateProperty, value); }
    public static readonly StyledProperty<Color> ContainerColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(ContainerColor));
    public Color ContainerColor { get => GetValue(ContainerColorProperty); set => SetValue(ContainerColorProperty, value); }
    public static readonly StyledProperty<Color> ContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(ContentColor));
    public Color ContentColor { get => GetValue(ContentColorProperty); set => SetValue(ContentColorProperty, value); }
    public static readonly StyledProperty<Color> LeadingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(LeadingContentColor));
    public Color LeadingContentColor { get => GetValue(LeadingContentColorProperty); set => SetValue(LeadingContentColorProperty, value); }
    public static readonly StyledProperty<Color> TrailingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(TrailingContentColor));
    public Color TrailingContentColor { get => GetValue(TrailingContentColorProperty); set => SetValue(TrailingContentColorProperty, value); }
    public static readonly StyledProperty<Color> OverlineContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(OverlineContentColor));
    public Color OverlineContentColor { get => GetValue(OverlineContentColorProperty); set => SetValue(OverlineContentColorProperty, value); }
    public static readonly StyledProperty<Color> SupportingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SupportingContentColor));
    public Color SupportingContentColor { get => GetValue(SupportingContentColorProperty); set => SetValue(SupportingContentColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledContainerColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DisabledContainerColor));
    public Color DisabledContainerColor { get => GetValue(DisabledContainerColorProperty); set => SetValue(DisabledContainerColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DisabledContentColor));
    public Color DisabledContentColor { get => GetValue(DisabledContentColorProperty); set => SetValue(DisabledContentColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledLeadingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DisabledLeadingContentColor));
    public Color DisabledLeadingContentColor { get => GetValue(DisabledLeadingContentColorProperty); set => SetValue(DisabledLeadingContentColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledTrailingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DisabledTrailingContentColor));
    public Color DisabledTrailingContentColor { get => GetValue(DisabledTrailingContentColorProperty); set => SetValue(DisabledTrailingContentColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledOverlineContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DisabledOverlineContentColor));
    public Color DisabledOverlineContentColor { get => GetValue(DisabledOverlineContentColorProperty); set => SetValue(DisabledOverlineContentColorProperty, value); }
    public static readonly StyledProperty<Color> DisabledSupportingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DisabledSupportingContentColor));
    public Color DisabledSupportingContentColor { get => GetValue(DisabledSupportingContentColorProperty); set => SetValue(DisabledSupportingContentColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedContainerColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SelectedContainerColor));
    public Color SelectedContainerColor { get => GetValue(SelectedContainerColorProperty); set => SetValue(SelectedContainerColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SelectedContentColor));
    public Color SelectedContentColor { get => GetValue(SelectedContentColorProperty); set => SetValue(SelectedContentColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedLeadingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SelectedLeadingContentColor));
    public Color SelectedLeadingContentColor { get => GetValue(SelectedLeadingContentColorProperty); set => SetValue(SelectedLeadingContentColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedTrailingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SelectedTrailingContentColor));
    public Color SelectedTrailingContentColor { get => GetValue(SelectedTrailingContentColorProperty); set => SetValue(SelectedTrailingContentColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedOverlineContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SelectedOverlineContentColor));
    public Color SelectedOverlineContentColor { get => GetValue(SelectedOverlineContentColorProperty); set => SetValue(SelectedOverlineContentColorProperty, value); }
    public static readonly StyledProperty<Color> SelectedSupportingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(SelectedSupportingContentColor));
    public Color SelectedSupportingContentColor { get => GetValue(SelectedSupportingContentColorProperty); set => SetValue(SelectedSupportingContentColorProperty, value); }
    public static readonly StyledProperty<Color> DraggedContainerColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DraggedContainerColor));
    public Color DraggedContainerColor { get => GetValue(DraggedContainerColorProperty); set => SetValue(DraggedContainerColorProperty, value); }
    public static readonly StyledProperty<Color> DraggedContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DraggedContentColor));
    public Color DraggedContentColor { get => GetValue(DraggedContentColorProperty); set => SetValue(DraggedContentColorProperty, value); }
    public static readonly StyledProperty<Color> DraggedLeadingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DraggedLeadingContentColor));
    public Color DraggedLeadingContentColor { get => GetValue(DraggedLeadingContentColorProperty); set => SetValue(DraggedLeadingContentColorProperty, value); }
    public static readonly StyledProperty<Color> DraggedTrailingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DraggedTrailingContentColor));
    public Color DraggedTrailingContentColor { get => GetValue(DraggedTrailingContentColorProperty); set => SetValue(DraggedTrailingContentColorProperty, value); }
    public static readonly StyledProperty<Color> DraggedOverlineContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DraggedOverlineContentColor));
    public Color DraggedOverlineContentColor { get => GetValue(DraggedOverlineContentColorProperty); set => SetValue(DraggedOverlineContentColorProperty, value); }
    public static readonly StyledProperty<Color> DraggedSupportingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(DraggedSupportingContentColor));
    public Color DraggedSupportingContentColor { get => GetValue(DraggedSupportingContentColorProperty); set => SetValue(DraggedSupportingContentColorProperty, value); }
    public static readonly StyledProperty<double> ElevationProperty = AvaloniaProperty.Register<ListItem, double>(nameof(Elevation));
    public double Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public static readonly StyledProperty<double> DraggedElevationProperty = AvaloniaProperty.Register<ListItem, double>(nameof(DraggedElevation));
    public double DraggedElevation { get => GetValue(DraggedElevationProperty); set => SetValue(DraggedElevationProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultShape));
    internal CornerRadius DefaultShape { get => GetValue(DefaultShapeProperty); set => SetValue(DefaultShapeProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultSelectedShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultSelectedShape));
    internal CornerRadius DefaultSelectedShape { get => GetValue(DefaultSelectedShapeProperty); set => SetValue(DefaultSelectedShapeProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultPressedShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultPressedShape));
    internal CornerRadius DefaultPressedShape { get => GetValue(DefaultPressedShapeProperty); set => SetValue(DefaultPressedShapeProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultFocusedShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultFocusedShape));
    internal CornerRadius DefaultFocusedShape { get => GetValue(DefaultFocusedShapeProperty); set => SetValue(DefaultFocusedShapeProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultHoveredShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultHoveredShape));
    internal CornerRadius DefaultHoveredShape { get => GetValue(DefaultHoveredShapeProperty); set => SetValue(DefaultHoveredShapeProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultDraggedShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultDraggedShape));
    internal CornerRadius DefaultDraggedShape { get => GetValue(DefaultDraggedShapeProperty); set => SetValue(DefaultDraggedShapeProperty, value); }
    internal static readonly StyledProperty<CornerRadius> DefaultGroupShapeProperty = AvaloniaProperty.Register<ListItem, CornerRadius>(nameof(DefaultGroupShape));
    internal CornerRadius DefaultGroupShape { get => GetValue(DefaultGroupShapeProperty); set => SetValue(DefaultGroupShapeProperty, value); }
    internal static readonly StyledProperty<double> OneLineHeightProperty = AvaloniaProperty.Register<ListItem, double>(nameof(OneLineHeight));
    internal double OneLineHeight { get => GetValue(OneLineHeightProperty); set => SetValue(OneLineHeightProperty, value); }
    internal static readonly StyledProperty<double> TwoLineHeightProperty = AvaloniaProperty.Register<ListItem, double>(nameof(TwoLineHeight));
    internal double TwoLineHeight { get => GetValue(TwoLineHeightProperty); set => SetValue(TwoLineHeightProperty, value); }
    internal static readonly StyledProperty<double> ThreeLineHeightProperty = AvaloniaProperty.Register<ListItem, double>(nameof(ThreeLineHeight));
    internal double ThreeLineHeight { get => GetValue(ThreeLineHeightProperty); set => SetValue(ThreeLineHeightProperty, value); }
    internal static readonly StyledProperty<double> InternalSpacingProperty = AvaloniaProperty.Register<ListItem, double>(nameof(InternalSpacing));
    internal double InternalSpacing { get => GetValue(InternalSpacingProperty); set => SetValue(InternalSpacingProperty, value); }
    internal static readonly StyledProperty<SpringSpec> ColorAnimationProperty = AvaloniaProperty.Register<ListItem, SpringSpec>(nameof(ColorAnimation));
    internal SpringSpec ColorAnimation { get => GetValue(ColorAnimationProperty); set => SetValue(ColorAnimationProperty, value); }
    internal static readonly StyledProperty<SpringSpec> ShapeAnimationProperty = AvaloniaProperty.Register<ListItem, SpringSpec>(nameof(ShapeAnimation));
    internal SpringSpec ShapeAnimation { get => GetValue(ShapeAnimationProperty); set => SetValue(ShapeAnimationProperty, value); }
    internal static readonly StyledProperty<SpringSpec> ElevationAnimationProperty = AvaloniaProperty.Register<ListItem, SpringSpec>(nameof(ElevationAnimation));
    internal SpringSpec ElevationAnimation { get => GetValue(ElevationAnimationProperty); set => SetValue(ElevationAnimationProperty, value); }
    internal static readonly StyledProperty<TextStyle?> ContentTextStyleProperty = AvaloniaProperty.Register<ListItem, TextStyle?>(nameof(ContentTextStyle));
    internal TextStyle? ContentTextStyle { get => GetValue(ContentTextStyleProperty); set => SetValue(ContentTextStyleProperty, value); }
    internal static readonly StyledProperty<TextStyle?> LeadingTextStyleProperty = AvaloniaProperty.Register<ListItem, TextStyle?>(nameof(LeadingTextStyle));
    internal TextStyle? LeadingTextStyle { get => GetValue(LeadingTextStyleProperty); set => SetValue(LeadingTextStyleProperty, value); }
    internal static readonly StyledProperty<TextStyle?> TrailingTextStyleProperty = AvaloniaProperty.Register<ListItem, TextStyle?>(nameof(TrailingTextStyle));
    internal TextStyle? TrailingTextStyle { get => GetValue(TrailingTextStyleProperty); set => SetValue(TrailingTextStyleProperty, value); }
    internal static readonly StyledProperty<TextStyle?> OverlineTextStyleProperty = AvaloniaProperty.Register<ListItem, TextStyle?>(nameof(OverlineTextStyle));
    internal TextStyle? OverlineTextStyle { get => GetValue(OverlineTextStyleProperty); set => SetValue(OverlineTextStyleProperty, value); }
    internal static readonly StyledProperty<TextStyle?> SupportingTextStyleProperty = AvaloniaProperty.Register<ListItem, TextStyle?>(nameof(SupportingTextStyle));
    internal TextStyle? SupportingTextStyle { get => GetValue(SupportingTextStyleProperty); set => SetValue(SupportingTextStyleProperty, value); }
    internal static readonly StyledProperty<Color> AnimatedContainerColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(AnimatedContainerColor));
    internal Color AnimatedContainerColor { get => GetValue(AnimatedContainerColorProperty); set => SetValue(AnimatedContainerColorProperty, value); }
    internal static readonly StyledProperty<Color> AnimatedContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(AnimatedContentColor));
    internal Color AnimatedContentColor { get => GetValue(AnimatedContentColorProperty); set => SetValue(AnimatedContentColorProperty, value); }
    internal static readonly StyledProperty<Color> AnimatedLeadingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(AnimatedLeadingContentColor));
    internal Color AnimatedLeadingContentColor { get => GetValue(AnimatedLeadingContentColorProperty); set => SetValue(AnimatedLeadingContentColorProperty, value); }
    internal static readonly StyledProperty<Color> AnimatedTrailingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(AnimatedTrailingContentColor));
    internal Color AnimatedTrailingContentColor { get => GetValue(AnimatedTrailingContentColorProperty); set => SetValue(AnimatedTrailingContentColorProperty, value); }
    internal static readonly StyledProperty<Color> AnimatedOverlineContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(AnimatedOverlineContentColor));
    internal Color AnimatedOverlineContentColor { get => GetValue(AnimatedOverlineContentColorProperty); set => SetValue(AnimatedOverlineContentColorProperty, value); }
    internal static readonly StyledProperty<Color> AnimatedSupportingContentColorProperty = AvaloniaProperty.Register<ListItem, Color>(nameof(AnimatedSupportingContentColor));
    internal Color AnimatedSupportingContentColor { get => GetValue(AnimatedSupportingContentColorProperty); set => SetValue(AnimatedSupportingContentColorProperty, value); }
    internal static readonly StyledProperty<float> AnimatedElevationProperty = AvaloniaProperty.Register<ListItem, float>(nameof(AnimatedElevation));
    internal float AnimatedElevation { get => GetValue(AnimatedElevationProperty); set => SetValue(AnimatedElevationProperty, value); }

    public event EventHandler<ListItemCheckedChangeEventArgs>? CheckedChangeRequested;
    public event EventHandler? LongClick;
    private readonly MaterialInteractionSource _localSource = new();
    private MaterialInteractionController? _interaction;
    private IDisposable? _feedbackLease;
    private MaterialSurface? _surface;
    private ListItemLayout? _layout;
    private ListItemMotion? _motion;
    private CancellationTokenSource? _longPress;
    private sealed class KeyLongPress { internal CancellationTokenSource? Timer; internal bool Triggered; }
    private readonly Dictionary<Key, KeyLongPress> _keyLongPresses = [];
    private bool _longTriggered, _enterPressed, _refreshing;
    internal bool Selected => InteractionMode == ListItemInteractionMode.SingleSelection ? IsSelected
        : InteractionMode == ListItemInteractionMode.MultipleSelection && IsChecked;
    internal bool RowEnabled => IsEffectivelyEnabled && IsInteractionEnabled;
    internal bool Interactive => InteractionMode != ListItemInteractionMode.None;
    internal MaterialSurface? Surface => _surface;
    internal InteractionState Interactions => _interaction?.Interaction ?? InteractionState.None;

    internal ListItemShapes ResolveShapes()
    {
        if (Shapes is { } shapes) return shapes;
        var shape = DefaultShape;
        if (this is SegmentedListItem grouped)
        {
            var top = grouped.GroupCount == 1 || grouped.GroupIndex == 0;
            var bottom = grouped.GroupCount == 1 || grouped.GroupIndex == grouped.GroupCount - 1;
            shape = new CornerRadius(top ? DefaultGroupShape.TopLeft : shape.TopLeft,
                top ? DefaultGroupShape.TopRight : shape.TopRight,
                bottom ? DefaultGroupShape.BottomRight : shape.BottomRight,
                bottom ? DefaultGroupShape.BottomLeft : shape.BottomLeft);
        }
        return new(shape, DefaultSelectedShape, DefaultPressedShape, DefaultFocusedShape, DefaultHoveredShape, DefaultDraggedShape);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _feedbackLease?.Dispose(); _feedbackLease = null;
        _layout?.SetOwner(null);
        _surface?.SetAnimatedCorners(null);
        base.OnApplyTemplate(e);
        _surface = e.NameScope.Find<MaterialSurface>("PART_Container");
        _layout = e.NameScope.Find<ListItemLayout>("PART_Layout");
        _layout?.SetOwner(this);
        if (_surface is not null) _surface.ApplyTonalElevation = false;
        Refresh();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _interaction = MaterialInteraction.GetOrCreate(this);
        _interaction.Changed += InteractionChanged;
        Refresh();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        CancelAllLongPresses();
        if (_interaction is not null) { _interaction.Changed -= InteractionChanged; _interaction.ConnectSource(null); }
        _feedbackLease?.Dispose(); _feedbackLease = null;
        _motion?.Dispose(); _motion = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LeadingContentProperty || change.Property == TrailingContentProperty
            || change.Property == OverlineContentProperty || change.Property == SupportingContentProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical child) LogicalChildren.Add(child);
        }
        if (change.Property == IsPressedProperty && !IsPressed) CancelLongPress();
        if (change.Property.Name.StartsWith("Animated", StringComparison.Ordinal)) return;
        if (change.Property.OwnerType == typeof(ListItem) || change.Property.OwnerType == typeof(SegmentedListItem)
            || change.Property == IsEffectivelyEnabledProperty || change.Property == PaddingProperty
            || change.Property == FlowDirectionProperty || change.Property == MinHeightProperty
            || change.Property == HeightProperty || change.Property == MaterialInteractiveSize.MinimumProperty)
            Refresh();
    }
    private void Refresh()
    {
        if (_refreshing) return;
        _refreshing = true;
        try
        {
            if (MaterialInteraction.GetIsInteractive(this) && !(Interactive && RowEnabled))
                _motion?.DeferShapeForInputCancellation();
            MaterialInteraction.SetIsInteractive(this, Interactive && RowEnabled);
            MinimumLayoutSize = Interactive ? MaterialInteractiveSize.Resolve(this) : 0;
            PseudoClasses.Set(":interactive", Interactive && RowEnabled);
            PseudoClasses.Set(":selected", Selected);
            PseudoClasses.Set(":row-disabled", !RowEnabled);
            if (!Interactive || !RowEnabled)
            {
                CancelAllLongPresses(); _enterPressed = false;
                if (IsPressed) base.OnLostFocus(new FocusChangedEventArgs(LostFocusEvent));
            }
            if (this.IsAttachedToVisualTree() && _interaction is not null)
            {
                _interaction.ConnectSource(Interactive ? InteractionSource ?? _localSource : null);
                if (_surface is not null && _feedbackLease is null && Interactive)
                    _feedbackLease = _interaction.Attach(_surface);
                if (!Interactive) { _feedbackLease?.Dispose(); _feedbackLease = null; }
                if (_surface is not null && ColorAnimation.Stiffness > 0 && ShapeAnimation.Stiffness > 0 && ElevationAnimation.Stiffness > 0)
                {
                    _motion ??= new ListItemMotion(this);
                    _motion.Refresh();
                }
            }
            _layout?.Refresh();
        }
        finally { _refreshing = false; }
    }
    private void InteractionChanged() => _motion?.Refresh();
    internal void LayoutChanged() => _motion?.PaintShape();
    internal void ResetIndication() => _interaction?.ResetSourceIndication();

    protected override bool IsEnabledCore => IsEnabled;
    protected override void OnClick()
    {
        if (!Interactive || !RowEnabled || _longTriggered || Command?.CanExecute(CommandParameter) == false) return;
        if (InteractionMode == ListItemInteractionMode.MultipleSelection)
        {
            CheckedChangeRequested?.Invoke(this, new(!IsChecked));
            Command?.Execute(CommandParameter);
            return;
        }
        base.OnClick();
    }
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (!Interactive || !RowEnabled) return;
        base.OnPointerPressed(e);
        if (IsPressed) BeginLongPress();
    }
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        CancelLongPress();
        if (Interactive && RowEnabled) base.OnPointerReleased(e);
        _longTriggered = false;
    }
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        CancelLongPress();
        base.OnPointerCaptureLost(e);
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!Interactive || !RowEnabled || !ReferenceEquals(e.Source, this)) return;
        if (e.Key == Key.Enter)
        {
            if (!_enterPressed) { _enterPressed = true; BeginLongPress(Key.Enter); }
            e.Handled = true;
        }
        else
        {
            var wasPressed = IsPressed;
            base.OnKeyDown(e);
            if (e.Key == Key.Space && !wasPressed && IsPressed) BeginLongPress(Key.Space);
        }
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (!ReferenceEquals(e.Source, this)) return;
        if (e.Key is not (Key.Enter or Key.Space)) { base.OnKeyUp(e); return; }
        _longTriggered = false;
        if (_keyLongPresses.Remove(e.Key, out var gesture))
        {
            gesture.Timer?.Cancel();
            _longTriggered = gesture.Triggered;
        }
        if (e.Key == Key.Enter)
        {
            var activate = _enterPressed; _enterPressed = false; e.Handled = Interactive;
            if (activate) OnClick();
        }
        else if (Interactive && RowEnabled) base.OnKeyUp(e);
        _longTriggered = false;
    }
    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        _enterPressed = false; CancelAllLongPresses(); base.OnLostFocus(e);
    }
    protected override void OnGettingFocus(FocusChangingEventArgs e)
    {
        // Fixed Core CombinedClickableNode does not request pointer focus (ClickableNode does).
        // Keep keyboard/programmatic focus and focusable descendants available.
        if (ReferenceEquals(e.NewFocusedElement, this) && e.NavigationMethod == NavigationMethod.Pointer)
            e.TryCancel();
        base.OnGettingFocus(e);
    }
    private void BeginLongPress(Key? key = null)
    {
        if (key is null) { CancelLongPress(); _longTriggered = false; }
        else if (_keyLongPresses.ContainsKey(key.Value)) return;
        if (LongClick is null && LongClickCommand is null) return;
        var gesture = key is null ? null : new KeyLongPress();
        if (gesture is not null) _keyLongPresses.Add(key!.Value, gesture);
        var scope = AnimationScope.Get(this);
        var effect = CancellationTokenSource.CreateLinkedTokenSource(scope.Lifetime);
        if (gesture is not null) gesture.Timer = effect;
        else _longPress = effect;
        _ = scope.Launch(async ct =>
        {
            try
            {
                // Fixed Core Desktop ViewConfiguration uses 500 ms; ordinary delay, not animation time.
                if (!await scope.DelayAsync(TimeSpan.FromMilliseconds(500), effect.Token)) return;
                if (!RowEnabled || !Interactive) return;
                if (gesture is not null) gesture.Triggered = true;
                else _longTriggered = true;
                LongClick?.Invoke(this, EventArgs.Empty);
                if (LongClickCommand?.CanExecute(LongClickCommandParameter) == true)
                    LongClickCommand.Execute(LongClickCommandParameter);
            }
            finally
            {
                if (ReferenceEquals(_longPress, effect)) _longPress = null;
                if (gesture is not null) gesture.Timer = null;
                effect.Dispose();
            }
        }, "List item long press");
    }
    private void CancelLongPress() { _longPress?.Cancel(); _longPress = null; }
    private void CancelAllLongPresses()
    {
        CancelLongPress();
        foreach (var gesture in _keyLongPresses.Values) gesture.Timer?.Cancel();
        _keyLongPresses.Clear();
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new ListItemPeer(this);
    private sealed class ListItemPeer : ButtonAutomationPeer, IToggleProvider, ISelectionItemProvider
    {
        private readonly ListItem owner;
        public ListItemPeer(ListItem owner) : base(owner)
        {
            this.owner = owner;
            owner.PropertyChanged += (_, e) =>
            {
                if (e.Property == IsCheckedProperty)
                    RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty,
                        e.OldValue is true ? ToggleState.On : ToggleState.Off, e.NewValue is true ? ToggleState.On : ToggleState.Off);
                else if (e.Property == IsSelectedProperty)
                    RaisePropertyChangedEvent(SelectionItemPatternIdentifiers.IsSelectedProperty, e.OldValue, e.NewValue);
                else if (e.Property == LongClickLabelProperty)
                    RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, e.OldValue, e.NewValue);
            };
        }
        protected override string? GetHelpTextCore() => owner.LongClickLabel ?? base.GetHelpTextCore();
        protected override bool IsEnabledCore() => owner.RowEnabled;
        protected override object? GetProviderCore(Type type)
        {
            if (!owner.Interactive) return null;
            if (type == typeof(IToggleProvider)) return owner.InteractionMode == ListItemInteractionMode.MultipleSelection ? this : null;
            if (type == typeof(ISelectionItemProvider)) return owner.InteractionMode == ListItemInteractionMode.SingleSelection ? this : null;
            return base.GetProviderCore(type);
        }
        protected override AutomationControlType GetAutomationControlTypeCore() => owner.InteractionMode switch
        {
            ListItemInteractionMode.SingleSelection => AutomationControlType.RadioButton,
            ListItemInteractionMode.MultipleSelection => AutomationControlType.CheckBox,
            ListItemInteractionMode.Click => AutomationControlType.Button,
            _ => AutomationControlType.ListItem,
        };
        public ToggleState ToggleState => owner.IsChecked ? ToggleState.On : ToggleState.Off;
        public void Toggle() => owner.OnClick();
        public bool IsSelected => owner.IsSelected;
        public ISelectionProvider? SelectionContainer => null;
        public void Select() => owner.OnClick();
        public void AddToSelection() => Select();
        public void RemoveFromSelection() { }
    }
}

public sealed class SegmentedListItem : ListItem
{
    public static readonly StyledProperty<int> GroupIndexProperty = AvaloniaProperty.Register<SegmentedListItem, int>(nameof(GroupIndex));
    public static readonly StyledProperty<int> GroupCountProperty = AvaloniaProperty.Register<SegmentedListItem, int>(nameof(GroupCount), 1);
    public int GroupIndex { get => GetValue(GroupIndexProperty); set => SetValue(GroupIndexProperty, value); }
    public int GroupCount { get => GetValue(GroupCountProperty); set => SetValue(GroupCountProperty, value); }
}


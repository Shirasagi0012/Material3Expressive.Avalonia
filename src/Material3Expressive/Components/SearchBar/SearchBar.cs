using Material3Expressive.Foundation.Rendering;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.VisualTree;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

/// <summary>The collapsed search surface and anchor for an expanded search presentation.</summary>
public class SearchBar : ContentControl
{
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<SearchBar>();
    /// <summary>Explicit search outline; null uses CornerRadius. Full-screen variants use this as the collapsed shape.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    public static readonly StyledProperty<SearchBarState?> StateProperty = AvaloniaProperty.Register<SearchBar, SearchBarState?>(nameof(State));
    public static readonly StyledProperty<double> TonalElevationProperty = MaterialSurface.TonalElevationProperty.AddOwner<SearchBar>();
    public static readonly StyledProperty<double> ShadowElevationProperty = MaterialSurface.ShadowElevationProperty.AddOwner<SearchBar>();
    internal static readonly StyledProperty<MotionSpring?> ExpandAnimationProperty = AvaloniaProperty.Register<SearchBar, MotionSpring?>(nameof(ExpandAnimation));
    internal static readonly StyledProperty<MotionSpring?> CollapseAnimationProperty = AvaloniaProperty.Register<SearchBar, MotionSpring?>(nameof(CollapseAnimation));
    internal static readonly StyledProperty<MotionSpring?> ContainedAnimationProperty = AvaloniaProperty.Register<SearchBar, MotionSpring?>(nameof(ContainedAnimation));
    internal static readonly StyledProperty<MotionSpring?> GapExpandAnimationProperty = AvaloniaProperty.Register<SearchBar, MotionSpring?>(nameof(GapExpandAnimation));
    internal static readonly StyledProperty<MotionSpring?> GapCollapseAnimationProperty = AvaloniaProperty.Register<SearchBar, MotionSpring?>(nameof(GapCollapseAnimation));
    internal MotionSpring? GapExpandAnimation { get => GetValue(GapExpandAnimationProperty); set => SetValue(GapExpandAnimationProperty, value); }
    internal MotionSpring? GapCollapseAnimation { get => GetValue(GapCollapseAnimationProperty); set => SetValue(GapCollapseAnimationProperty, value); }
    internal MotionSpring? ContainedAnimation { get => GetValue(ContainedAnimationProperty); set => SetValue(ContainedAnimationProperty, value); }
    internal MotionSpring? ExpandAnimation { get => GetValue(ExpandAnimationProperty); set => SetValue(ExpandAnimationProperty, value); }
    internal MotionSpring? CollapseAnimation { get => GetValue(CollapseAnimationProperty); set => SetValue(CollapseAnimationProperty, value); }
    public SearchBarState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    private SearchBarState? _activeState;
    static SearchBar()
    {
        TemplateProperty.OverrideDefaultValue<SearchBar>(null);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<SearchBar>(HorizontalAlignment.Stretch);
        VerticalContentAlignmentProperty.OverrideDefaultValue<SearchBar>(VerticalAlignment.Stretch);
    }
    public SearchBar() => LayoutUpdated += (_, _) =>
    {
        // onGloballyPositioned stores the LayoutCoordinates object. Moving the same
        // object does not invalidate the upstream popup's composition/measurement.
        if (!IsArrangeValid || _activeState is null || ReferenceEquals(_activeState.CollapsedBar, this)) return;
        _activeState.CollapsedBar = this; _activeState.NotifyLayoutChanged();
    };
    protected override Type StyleKeyOverride => typeof(SearchBar);
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e) { base.OnAttachedToVisualTree(e); AttachState(); }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e) { DetachState(); base.OnDetachedFromVisualTree(e); }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == StateProperty && this.IsAttachedToVisualTree()) { DetachState(); AttachState(); }
    }
    private void AttachState()
    {
        _activeState = State;
        if (_activeState is null) return;
        _activeState.Attach(this);
    }
    private void DetachState()
    {
        if (_activeState is null) return;
        if (ReferenceEquals(_activeState.CollapsedBar, this)) _activeState.CollapsedBar = null;
        _activeState.NotifyLayoutChanged(); _activeState.Detach(this); _activeState = null;
    }
}

using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>Surface and content environment corresponding to DrawerSheet in NavigationDrawer.kt.</summary>
public abstract class DrawerSheet : ContentControl
{
    internal static readonly StyledProperty<double> ContainerWidthProperty = AvaloniaProperty.Register<DrawerSheet, double>(nameof(ContainerWidth));
    internal double ContainerWidth { get => GetValue(ContainerWidthProperty); set => SetValue(ContainerWidthProperty, value); }
    public static readonly StyledProperty<Thickness> WindowInsetsProperty = AvaloniaProperty.Register<DrawerSheet, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<double> TonalElevationProperty = MaterialSurface.TonalElevationProperty.AddOwner<DrawerSheet>();
    public Thickness WindowInsets { get => GetValue(WindowInsetsProperty); set => SetValue(WindowInsetsProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    internal DrawerState? AnimationState => (this as DismissibleDrawerSheet)?.State;
    private DrawerState? _observedState;
    private MaterialSurface? _surface;
    private ContentPresenter? _content;
    private TopLevel? _topLevel;
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _surface = e.NameScope.Find<MaterialSurface>("PART_Surface");
        _content = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        ObserveState(); UpdateScale();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e); ObserveState();
        _topLevel = TopLevel.GetTopLevel(this);
        if (_topLevel is not null) _topLevel.BackRequested += BackRequested;
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_observedState is not null) _observedState.PropertyChanged -= StateChanged;
        _observedState = null;
        if (_topLevel is not null) _topLevel.BackRequested -= BackRequested;
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DismissibleDrawerSheet.StateProperty) ObserveState();
        if (change.Property == FlowDirectionProperty || change.Property == ContainerWidthProperty) UpdateScale();
    }
    private void ObserveState()
    {
        if (_observedState == AnimationState || !this.IsAttachedToVisualTree()) return;
        if (_observedState is not null) _observedState.PropertyChanged -= StateChanged;
        _observedState = AnimationState;
        if (_observedState is not null) _observedState.PropertyChanged += StateChanged;
        UpdateScale();
    }
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => UpdateScale();
    private void UpdateScale()
    {
        if (_surface is null || _content is null) return;
        var offset = AnimationState?.CurrentOffset ?? 0;
        var width = ContainerWidth;
        var scale = offset > 0 ? 1f + (float)offset / (float)width : 1f;
        _surface.RenderTransformOrigin = new RelativePoint(1, .5, RelativeUnit.Relative);
        _surface.RenderTransform = new ScaleTransform(scale, 1);
        _content.RenderTransformOrigin = new RelativePoint(1, 0, RelativeUnit.Relative);
        _content.RenderTransform = new ScaleTransform(1f / scale, 1);
    }
    private void BackRequested(object? sender, RoutedEventArgs e)
    {
        if (!e.Handled && AnimationState is { TargetValue: DrawerValue.Open } state)
        { _ = state.CloseAsync(); e.Handled = true; }
    }
    protected override AutomationPeer OnCreateAutomationPeer() => new SheetPeer(this);
    private sealed class SheetPeer(DrawerSheet owner) : ControlAutomationPeer(owner)
    {
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
        protected override string? GetNameCore() => string.IsNullOrEmpty(AutomationProperties.GetName(owner)) ? "Navigation menu" : base.GetNameCore();
    }
}

public class PermanentDrawerSheet : DrawerSheet { }
public class DismissibleDrawerSheet : DrawerSheet
{
    public static readonly StyledProperty<DrawerState?> StateProperty = AvaloniaProperty.Register<DismissibleDrawerSheet, DrawerState?>(nameof(State));
    /// <summary>Optional state: enables open overshoot compensation and platform back handling.</summary>
    public DrawerState? State { get => GetValue(StateProperty); set => SetValue(StateProperty, value); }
}
public class ModalDrawerSheet : DismissibleDrawerSheet { }

internal sealed class DrawerSheetLayout : Decorator
{
    public static readonly StyledProperty<double> MinimumWidthProperty = AvaloniaProperty.Register<DrawerSheetLayout, double>(nameof(MinimumWidth));
    public double MinimumWidth { get => GetValue(MinimumWidthProperty); set => SetValue(MinimumWidthProperty, value); }
    public static readonly StyledProperty<double> MaximumWidthProperty = AvaloniaProperty.Register<DrawerSheetLayout, double>(nameof(MaximumWidth));
    public double MaximumWidth { get => GetValue(MaximumWidthProperty); set => SetValue(MaximumWidthProperty, value); }
    static DrawerSheetLayout() => AffectsMeasure<DrawerSheetLayout>(MaximumWidthProperty, MinimumWidthProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        // A preceding Modifier.width supplies fixed incoming constraints, which sizeIn respects.
        var owner = TemplatedParent as DrawerSheet;
        var width = owner is not null && (!double.IsNaN(owner.Width) || owner.MinWidth > MaximumWidth)
            ? availableSize.Width : Math.Min(MaximumWidth, availableSize.Width);
        Child?.Measure(new Size(width, availableSize.Height));
        var desired = Child?.DesiredSize ?? default;
        return new Size(Math.Clamp(desired.Width, Math.Min(MinimumWidth, width), width), double.IsFinite(availableSize.Height) ? availableSize.Height : desired.Height);
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Material3Expressive.Foundation.Motion;

namespace Material3Expressive.Components;

/// <summary>A horizontal group whose pressed items borrow width from their neighbours.</summary>
public class ButtonGroup : ItemsControl
{
    public static readonly StyledProperty<double> ExpandedRatioProperty =
        AvaloniaProperty.Register<ButtonGroup, double>(nameof(ExpandedRatio));

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ButtonGroup, double>(nameof(Spacing));

    public static readonly StyledProperty<ButtonGroupArrangement> HorizontalArrangementProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonGroupArrangement>(nameof(HorizontalArrangement));

    public static readonly StyledProperty<VerticalAlignment> ItemVerticalAlignmentProperty =
        AvaloniaProperty.Register<ButtonGroup, VerticalAlignment>(nameof(ItemVerticalAlignment));

    public static readonly StyledProperty<HorizontalAlignment> SpacingAlignmentProperty =
        AvaloniaProperty.Register<ButtonGroup, HorizontalAlignment>(nameof(SpacingAlignment));

    public static readonly StyledProperty<IDataTemplate?> OverflowIndicatorTemplateProperty =
        AvaloniaProperty.Register<ButtonGroup, IDataTemplate?>(nameof(OverflowIndicatorTemplate));

    public static readonly StyledProperty<SpringSpec> WidthAnimationProperty =
        AvaloniaProperty.Register<ButtonGroup, SpringSpec>(nameof(WidthAnimation));

    public double ExpandedRatio
    {
        get => GetValue(ExpandedRatioProperty);
        set => SetValue(ExpandedRatioProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public ButtonGroupArrangement HorizontalArrangement
    {
        get => GetValue(HorizontalArrangementProperty);
        set => SetValue(HorizontalArrangementProperty, value);
    }

    public VerticalAlignment ItemVerticalAlignment
    {
        get => GetValue(ItemVerticalAlignmentProperty);
        set => SetValue(ItemVerticalAlignmentProperty, value);
    }

    /// <summary>Logical group alignment within SpacedBy's remaining width.</summary>
    public HorizontalAlignment SpacingAlignment
    {
        get => GetValue(SpacingAlignmentProperty);
        set => SetValue(SpacingAlignmentProperty, value);
    }

    public IDataTemplate? OverflowIndicatorTemplate
    {
        get => GetValue(OverflowIndicatorTemplateProperty);
        set => SetValue(OverflowIndicatorTemplateProperty, value);
    }

    public SpringSpec WidthAnimation
    {
        get => GetValue(WidthAnimationProperty);
        set => SetValue(WidthAnimationProperty, value);
    }

    public ButtonGroupMenuState MenuState { get; } = new();
    internal ButtonGroupLayout? Layout { get; private set; }

    static ButtonGroup() => TemplateProperty.OverrideDefaultValue<ButtonGroup>(null);

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) =>
        new ButtonGroupSlot();

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        recycleKey = null;
        return true;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Layout?.Disconnect();
        base.OnApplyTemplate(e);
        Layout = e.NameScope.Find<ButtonGroupLayout>("PART_Layout");
        Layout?.Connect(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ExpandedRatioProperty || change.Property == SpacingProperty ||
            change.Property == HorizontalArrangementProperty || change.Property == ItemVerticalAlignmentProperty ||
            change.Property == SpacingAlignmentProperty || change.Property == FlowDirectionProperty)
            Layout?.InvalidateItemsMeasure();
    }
}

/// <summary>Compose horizontal arrangements. SpacedBy uses the group's Spacing property.</summary>
public enum ButtonGroupArrangement
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
    SpacedBy
}

public sealed class ButtonGroupMenuState : AvaloniaObject
{
    public static readonly DirectProperty<ButtonGroupMenuState, bool> IsShowingProperty =
        AvaloniaProperty.RegisterDirect<ButtonGroupMenuState, bool>(nameof(IsShowing), state => state.IsShowing);

    private bool _isShowing;

    public ButtonGroupMenuState(bool initialIsShowing = false) => _isShowing = initialIsShowing;

    public bool IsShowing => _isShowing;

    public void Show() => SetAndRaise(IsShowingProperty, ref _isShowing, true);

    public void Dismiss() => SetAndRaise(IsShowingProperty, ref _isShowing, false);
}

internal sealed class ButtonGroupOverflowButton : IconButton
{
    protected override Type StyleKeyOverride => typeof(IconButton);
    protected override void OnClick()
    {
        if (DataContext is ButtonGroupMenuState state)
        {
            if (state.IsShowing)
                state.Dismiss();
            else
                state.Show();
        }

        base.OnClick();
    }
}

// An unplaced Compose child remains available for intrinsic queries. Hide only this wrapper.
internal sealed class ButtonGroupSlot : ContentPresenter
{
    internal Size MeasureContent(Size available)
    {
        ApplyTemplate();
        Child?.Measure(available);
        return Child?.DesiredSize ?? default;
    }
}


using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

public enum AppBarArrangement
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
    SpacedBy
}

/// <summary>Bottom actions, optional FAB, or a replacement custom content region.</summary>
public class BottomAppBar : ItemsControl
{
    static BottomAppBar() => TemplateProperty.OverrideDefaultValue<BottomAppBar>(null);
    public static readonly StyledProperty<object?> FloatingActionButtonProperty =
        AvaloniaProperty.Register<BottomAppBar, object?>(nameof(FloatingActionButton));
    public static readonly StyledProperty<IDataTemplate?> FloatingActionButtonTemplateProperty =
        AvaloniaProperty.Register<BottomAppBar, IDataTemplate?>(nameof(FloatingActionButtonTemplate));
    public static readonly StyledProperty<object?> CustomContentProperty =
        AvaloniaProperty.Register<BottomAppBar, object?>(nameof(CustomContent));
    public static readonly StyledProperty<IDataTemplate?> CustomContentTemplateProperty =
        AvaloniaProperty.Register<BottomAppBar, IDataTemplate?>(nameof(CustomContentTemplate));
    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<BottomAppBar, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<Thickness> WindowInsetsProperty =
        AvaloniaProperty.Register<BottomAppBar, Thickness>(nameof(WindowInsets));
    public static readonly StyledProperty<double> TonalElevationProperty =
        AvaloniaProperty.Register<BottomAppBar, double>(nameof(TonalElevation));
    public static readonly StyledProperty<double> ExpandedHeightProperty =
        AvaloniaProperty.Register<BottomAppBar, double>(nameof(ExpandedHeight));
    public static readonly StyledProperty<AppBarArrangement> HorizontalArrangementProperty =
        AvaloniaProperty.Register<BottomAppBar, AppBarArrangement>(nameof(HorizontalArrangement));
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<BottomAppBar, double>(nameof(Spacing));
    public static readonly StyledProperty<bool> TouchExplorationEnabledProperty =
        AvaloniaProperty.Register<BottomAppBar, bool>(nameof(TouchExplorationEnabled));
    public static readonly StyledProperty<BottomAppBarScrollBehavior?> ScrollBehaviorProperty =
        AvaloniaProperty.Register<BottomAppBar, BottomAppBarScrollBehavior?>(nameof(ScrollBehavior));
    public object? FloatingActionButton
    {
        get => GetValue(FloatingActionButtonProperty);
        set => SetValue(FloatingActionButtonProperty, value);
    }

    public IDataTemplate? FloatingActionButtonTemplate
    {
        get => GetValue(FloatingActionButtonTemplateProperty);
        set => SetValue(FloatingActionButtonTemplateProperty, value);
    }

    public object? CustomContent
    {
        get => GetValue(CustomContentProperty);
        set => SetValue(CustomContentProperty, value);
    }

    public IDataTemplate? CustomContentTemplate
    {
        get => GetValue(CustomContentTemplateProperty);
        set => SetValue(CustomContentTemplateProperty, value);
    }

    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    public Thickness WindowInsets
    {
        get => GetValue(WindowInsetsProperty);
        set => SetValue(WindowInsetsProperty, value);
    }

    public double TonalElevation
    {
        get => GetValue(TonalElevationProperty);
        set => SetValue(TonalElevationProperty, value);
    }

    public double ExpandedHeight
    {
        get => GetValue(ExpandedHeightProperty);
        set => SetValue(ExpandedHeightProperty, value);
    }

    public AppBarArrangement HorizontalArrangement
    {
        get => GetValue(HorizontalArrangementProperty);
        set => SetValue(HorizontalArrangementProperty, value);
    }

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public bool TouchExplorationEnabled
    {
        get => GetValue(TouchExplorationEnabledProperty);
        set => SetValue(TouchExplorationEnabledProperty, value);
    }

    public BottomAppBarScrollBehavior? ScrollBehavior
    {
        get => GetValue(ScrollBehaviorProperty);
        set => SetValue(ScrollBehaviorProperty, value);
    }

    internal bool HasCustomContent => CustomContent is { } || CustomContentTemplate is { };

    internal static readonly StyledProperty<double> DefaultExpandedHeightProperty =
        AvaloniaProperty.Register<BottomAppBar, double>(nameof(DefaultExpandedHeight));
    internal double DefaultExpandedHeight
    {
        get => GetValue(DefaultExpandedHeightProperty);
        set => SetValue(DefaultExpandedHeightProperty, value);
    }

    internal double EffectiveExpandedHeight =>
        this is FlexibleBottomAppBar && (!double.IsFinite(ExpandedHeight) || ExpandedHeight <= 0)
            ? DefaultExpandedHeight
            : ExpandedHeight;

    protected override Type StyleKeyOverride => typeof(BottomAppBar);

    private AppBarBottomChrome? _chrome;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _chrome = e.NameScope.Find<AppBarBottomChrome>("PART_Chrome");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FloatingActionButtonProperty || change.Property == CustomContentProperty)
        {
            if (change.OldValue is Control old)
                LogicalChildren.Remove(old);
            if (change.NewValue is Control current)
                LogicalChildren.Add(current);
        }

        if (change.Property.OwnerType == typeof(BottomAppBar) || change.Property == BackgroundProperty ||
            change.Property == ForegroundProperty || change.Property == FlowDirectionProperty ||
            change.Property == MaterialWindowInsets.ConsumedPaddingProperty)
            _chrome?.OwnerChanged(change);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new AppBarAutomationPeer(this);
}

public class FlexibleBottomAppBar : BottomAppBar
{
}

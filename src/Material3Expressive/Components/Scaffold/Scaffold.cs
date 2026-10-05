using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>The four placements supported by Compose Material3 Scaffold.</summary>
public enum FabPosition { Start, Center, End, EndOverlay }

/// <summary>Layers screen content, bars, a snackbar host and a floating action button.</summary>
/// <remarks>
/// Content is measured against the entire scaffold. Bind <see cref="ContentPadding"/> to the
/// content root's padding and MaterialWindowInsets.ConsumePadding when it should avoid the bars.
/// Bars handle their own window insets. Snackbar/FAB content uses the remaining horizontal and
/// bottom insets. This container does not implement a snackbar queue or a FAB button itself.
/// </remarks>
public class Scaffold : ContentControl
{
    public static readonly StyledProperty<object?> TopBarProperty = AvaloniaProperty.Register<Scaffold, object?>(nameof(TopBar));
    public static readonly StyledProperty<IDataTemplate?> TopBarTemplateProperty = AvaloniaProperty.Register<Scaffold, IDataTemplate?>(nameof(TopBarTemplate));
    public static readonly StyledProperty<object?> BottomBarProperty = AvaloniaProperty.Register<Scaffold, object?>(nameof(BottomBar));
    public static readonly StyledProperty<IDataTemplate?> BottomBarTemplateProperty = AvaloniaProperty.Register<Scaffold, IDataTemplate?>(nameof(BottomBarTemplate));
    public static readonly StyledProperty<object?> SnackbarHostProperty = AvaloniaProperty.Register<Scaffold, object?>(nameof(SnackbarHost));
    public static readonly StyledProperty<IDataTemplate?> SnackbarHostTemplateProperty = AvaloniaProperty.Register<Scaffold, IDataTemplate?>(nameof(SnackbarHostTemplate));
    public static readonly StyledProperty<object?> FloatingActionButtonProperty = AvaloniaProperty.Register<Scaffold, object?>(nameof(FloatingActionButton));
    public static readonly StyledProperty<IDataTemplate?> FloatingActionButtonTemplateProperty = AvaloniaProperty.Register<Scaffold, IDataTemplate?>(nameof(FloatingActionButtonTemplate));
    public static readonly StyledProperty<FabPosition> FloatingActionButtonPositionProperty =
        AvaloniaProperty.Register<Scaffold, FabPosition>(nameof(FloatingActionButtonPosition), FabPosition.End);
    public static readonly StyledProperty<Thickness> ContentWindowInsetsProperty =
        AvaloniaProperty.Register<Scaffold, Thickness>(nameof(ContentWindowInsets));
    public static readonly DirectProperty<Scaffold, Thickness> ContentPaddingProperty =
        AvaloniaProperty.RegisterDirect<Scaffold, Thickness>(nameof(ContentPadding), owner => owner.ContentPadding);

    public object? TopBar { get => GetValue(TopBarProperty); set => SetValue(TopBarProperty, value); }
    public IDataTemplate? TopBarTemplate { get => GetValue(TopBarTemplateProperty); set => SetValue(TopBarTemplateProperty, value); }
    public object? BottomBar { get => GetValue(BottomBarProperty); set => SetValue(BottomBarProperty, value); }
    public IDataTemplate? BottomBarTemplate { get => GetValue(BottomBarTemplateProperty); set => SetValue(BottomBarTemplateProperty, value); }
    public object? SnackbarHost { get => GetValue(SnackbarHostProperty); set => SetValue(SnackbarHostProperty, value); }
    public IDataTemplate? SnackbarHostTemplate { get => GetValue(SnackbarHostTemplateProperty); set => SetValue(SnackbarHostTemplateProperty, value); }
    public object? FloatingActionButton { get => GetValue(FloatingActionButtonProperty); set => SetValue(FloatingActionButtonProperty, value); }
    public IDataTemplate? FloatingActionButtonTemplate { get => GetValue(FloatingActionButtonTemplateProperty); set => SetValue(FloatingActionButtonTemplateProperty, value); }
    public FabPosition FloatingActionButtonPosition { get => GetValue(FloatingActionButtonPositionProperty); set => SetValue(FloatingActionButtonPositionProperty, value); }
    /// <summary>Physical left/top/right/bottom host insets; the desktop default is zero.</summary>
    public Thickness ContentWindowInsets { get => GetValue(ContentWindowInsetsProperty); set => SetValue(ContentWindowInsetsProperty, value); }
    /// <summary>Logical start/top/end/bottom padding, published before measuring the body.</summary>
    public Thickness ContentPadding => _contentPadding;

    private Thickness _contentPadding;
    private ScaffoldLayout? _layout;
    private AppBarScrollScope? _scrollScope;
    private ContentPresenter? _topPresenter, _bottomPresenter;

    internal TopAppBar? PresentedTopBar => _topPresenter?.Child as TopAppBar;
    internal BottomAppBar? PresentedBottomBar => _bottomPresenter?.Child as BottomAppBar;

    // Clear ContentControl's single-presenter fallback; Scaffold requires its five-slot template.
    static Scaffold() => TemplateProperty.OverrideDefaultValue<Scaffold>(null);

    protected override Type StyleKeyOverride => typeof(Scaffold);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _scrollScope?.SetHost(null);
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<ScaffoldLayout>("PART_Layout");
        _topPresenter = e.NameScope.Find<ContentPresenter>("PART_TopBar");
        _bottomPresenter = e.NameScope.Find<ContentPresenter>("PART_BottomBar");
        _scrollScope = e.NameScope.Find<AppBarScrollScope>("PART_ScrollScope");
        _scrollScope?.SetHost(this);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TopBarProperty || change.Property == BottomBarProperty
            || change.Property == SnackbarHostProperty || change.Property == FloatingActionButtonProperty)
        {
            if (change.OldValue is Control old) LogicalChildren.Remove(old);
            if (change.NewValue is Control current) LogicalChildren.Add(current);
        }
        if (change.Property == FloatingActionButtonPositionProperty || change.Property == ContentWindowInsetsProperty
            || change.Property == MaterialWindowInsets.ConsumedPaddingProperty || change.Property == FlowDirectionProperty
            || change.Property == TopBarProperty || change.Property == TopBarTemplateProperty
            || change.Property == BottomBarProperty || change.Property == BottomBarTemplateProperty
            || change.Property == SnackbarHostProperty || change.Property == SnackbarHostTemplateProperty
            || change.Property == FloatingActionButtonProperty || change.Property == FloatingActionButtonTemplateProperty)
            _layout?.InvalidateMeasure();
    }

    internal void UpdateContentPadding(Thickness padding) => SetAndRaise(ContentPaddingProperty, ref _contentPadding, padding);

    protected override AutomationPeer OnCreateAutomationPeer() => new ScaffoldAutomationPeer(this);

    private sealed class ScaffoldAutomationPeer(Scaffold owner) : ControlAutomationPeer(owner)
    {
        protected override string GetClassNameCore() => nameof(Scaffold);
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;
    }
}

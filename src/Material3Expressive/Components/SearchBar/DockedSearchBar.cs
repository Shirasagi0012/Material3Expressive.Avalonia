using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

/// <summary>A docked search surface with an input slot and animated result content.</summary>
/// <remarks>The application owns the expanded state, query and search results.</remarks>
public class DockedSearchBar : ContentControl
{
    public static readonly StyledProperty<object?> InputFieldProperty =
        AvaloniaProperty.Register<DockedSearchBar, object?>(nameof(InputField));
    public static readonly StyledProperty<IDataTemplate?> InputFieldTemplateProperty =
        AvaloniaProperty.Register<DockedSearchBar, IDataTemplate?>(nameof(InputFieldTemplate));
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<DockedSearchBar, bool>(nameof(IsExpanded));
    public static readonly StyledProperty<Color> DividerColorProperty =
        AvaloniaProperty.Register<DockedSearchBar, Color>(nameof(DividerColor));
    public static readonly StyledProperty<double> TonalElevationProperty =
        MaterialSurface.TonalElevationProperty.AddOwner<DockedSearchBar>();
    public static readonly StyledProperty<double> ShadowElevationProperty =
        MaterialSurface.ShadowElevationProperty.AddOwner<DockedSearchBar>();
    public static readonly RoutedEvent<RoutedEventArgs> CollapseRequestedEvent =
        RoutedEvent.Register<DockedSearchBar, RoutedEventArgs>(nameof(CollapseRequested), RoutingStrategies.Bubble);

    public object? InputField { get => GetValue(InputFieldProperty); set => SetValue(InputFieldProperty, value); }
    public IDataTemplate? InputFieldTemplate { get => GetValue(InputFieldTemplateProperty); set => SetValue(InputFieldTemplateProperty, value); }
    public bool IsExpanded { get => GetValue(IsExpandedProperty); set => SetValue(IsExpandedProperty, value); }
    public Color DividerColor { get => GetValue(DividerColorProperty); set => SetValue(DividerColorProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }
    /// <summary>Requests IsExpanded=false after an unhandled desktop Escape press.</summary>
    public event EventHandler<RoutedEventArgs>? CollapseRequested
    {
        add => AddHandler(CollapseRequestedEvent, value);
        remove => RemoveHandler(CollapseRequestedEvent, value);
    }

    private TopLevel? _topLevel;
    static DockedSearchBar()
    {
        TemplateProperty.OverrideDefaultValue<DockedSearchBar>(null);
        HorizontalContentAlignmentProperty.OverrideDefaultValue<DockedSearchBar>(HorizontalAlignment.Stretch);
    }
    protected override Type StyleKeyOverride => typeof(DockedSearchBar);
    protected override bool RegisterContentPresenter(ContentPresenter presenter) =>
        presenter.Name == "PART_InputField" || base.RegisterContentPresenter(presenter);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InputFieldProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical current) LogicalChildren.Add(current);
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Bubble);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _topLevel?.RemoveHandler(KeyDownEvent, OnWindowKeyDown);
        _topLevel = null;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || e.Key != Key.Escape || !IsExpanded || !IsEffectivelyVisible || _topLevel is null) return;
        // Core's BackHandler is window-wide and the last composed enabled handler wins.
        // Keep this component-local; child controls/popups consume Escape before this bubble handler.
        var last = _topLevel.GetVisualDescendants().OfType<DockedSearchBar>()
            .LastOrDefault(bar => bar.IsExpanded && bar.IsEffectivelyVisible);
        if (!ReferenceEquals(last, this)) return;
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(CollapseRequestedEvent));
    }
}

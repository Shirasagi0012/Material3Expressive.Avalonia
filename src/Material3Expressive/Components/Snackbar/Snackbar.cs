using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>Snackbar visuals. Content is the message; Action and DismissAction are caller-owned
/// slots. Data supplies the standard message and buttons when those slots are not overridden.</summary>
public class Snackbar : ContentControl
{
    public static readonly StyledProperty<ISnackbarData?> DataProperty = AvaloniaProperty.Register<Snackbar, ISnackbarData?>(nameof(Data));
    public static readonly StyledProperty<object?> ActionProperty = AvaloniaProperty.Register<Snackbar, object?>(nameof(Action));
    public static readonly StyledProperty<IDataTemplate?> ActionTemplateProperty = AvaloniaProperty.Register<Snackbar, IDataTemplate?>(nameof(ActionTemplate));
    public static readonly StyledProperty<object?> DismissActionProperty = AvaloniaProperty.Register<Snackbar, object?>(nameof(DismissAction));
    public static readonly StyledProperty<IDataTemplate?> DismissActionTemplateProperty = AvaloniaProperty.Register<Snackbar, IDataTemplate?>(nameof(DismissActionTemplate));
    public static readonly StyledProperty<bool> ActionOnNewLineProperty = AvaloniaProperty.Register<Snackbar, bool>(nameof(ActionOnNewLine));
    public static readonly StyledProperty<IBrush?> ActionForegroundProperty = AvaloniaProperty.Register<Snackbar, IBrush?>(nameof(ActionForeground));
    public static readonly StyledProperty<IBrush?> DismissActionForegroundProperty = AvaloniaProperty.Register<Snackbar, IBrush?>(nameof(DismissActionForeground));
    public static readonly StyledProperty<IBrush?> DataActionForegroundProperty = AvaloniaProperty.Register<Snackbar, IBrush?>(nameof(DataActionForeground));
    public static readonly StyledProperty<string> DismissActionTextProperty = AvaloniaProperty.Register<Snackbar, string>(nameof(DismissActionText), "Dismiss");

    public ISnackbarData? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }
    public object? Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }
    public IDataTemplate? ActionTemplate { get => GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    public object? DismissAction { get => GetValue(DismissActionProperty); set => SetValue(DismissActionProperty, value); }
    public IDataTemplate? DismissActionTemplate { get => GetValue(DismissActionTemplateProperty); set => SetValue(DismissActionTemplateProperty, value); }
    public bool ActionOnNewLine { get => GetValue(ActionOnNewLineProperty); set => SetValue(ActionOnNewLineProperty, value); }
    public IBrush? ActionForeground { get => GetValue(ActionForegroundProperty); set => SetValue(ActionForegroundProperty, value); }
    public IBrush? DismissActionForeground { get => GetValue(DismissActionForegroundProperty); set => SetValue(DismissActionForegroundProperty, value); }
    /// <summary>Foreground of the standard data action button, corresponding to Compose actionColor.</summary>
    public IBrush? DataActionForeground { get => GetValue(DataActionForegroundProperty); set => SetValue(DataActionForegroundProperty, value); }
    /// <summary>Accessible name and tooltip for the standard close action. Applications can localize it.</summary>
    public string DismissActionText { get => GetValue(DismissActionTextProperty); set => SetValue(DismissActionTextProperty, value); }

    internal bool HasAction => Action is not null || ActionTemplate is not null || Data?.Visuals.ActionLabel is not null;
    internal bool HasDismiss => DismissAction is not null || DismissActionTemplate is not null || Data?.Visuals.WithDismissAction == true;

    private Button? _actionButton;
    private Button? _dismissButton;
    private SnackbarLayout? _layout;

    public Snackbar() => UpdateSlots();
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_actionButton is not null) _actionButton.Click -= OnAction;
        if (_dismissButton is not null) _dismissButton.Click -= OnDismiss;
        base.OnApplyTemplate(e);
        _actionButton = e.NameScope.Find<Button>("PART_DataAction");
        _dismissButton = e.NameScope.Find<Button>("PART_DataDismiss");
        _layout = e.NameScope.Find<SnackbarLayout>("PART_Layout");
        if (_actionButton is not null) _actionButton.Click += OnAction;
        if (_dismissButton is not null) _dismissButton.Click += OnDismiss;
        // Tooltip content lives in a separate popup tree, so bind its source explicitly.
        if (e.NameScope.Find<TooltipBox>("PART_DismissTooltip")?.Tooltip is PlainTooltip tooltip)
            tooltip.Bind(ContentProperty, new Binding(nameof(DismissActionText)) { Source = this });
    }
    private void OnAction(object? sender, RoutedEventArgs e) => Data?.PerformAction();
    private void OnDismiss(object? sender, RoutedEventArgs e) => Data?.Dismiss();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Let each presenter own its slot's typography/content-color environment.
        if (change.Property == ContentProperty && change.NewValue is ILogical content) LogicalChildren.Remove(content);
        if (change.Property == DataProperty || change.Property == ActionProperty || change.Property == ActionTemplateProperty
            || change.Property == DismissActionProperty || change.Property == DismissActionTemplateProperty || change.Property == ActionOnNewLineProperty
            || change.Property == ContentProperty || change.Property == ContentTemplateProperty)
            UpdateSlots();
        if (change.Property == WidthProperty || change.Property == HeightProperty || change.Property == MinWidthProperty
            || change.Property == MinHeightProperty || change.Property == PaddingProperty) _layout?.InvalidateMeasure();
    }

    private void UpdateSlots()
    {
        PseudoClasses.Set(":has-data", Data is not null);
        PseudoClasses.Set(":data-content", Data is not null && Content is null && ContentTemplate is null);
        PseudoClasses.Set(":has-action", HasAction);
        PseudoClasses.Set(":has-dismiss", HasDismiss);
        PseudoClasses.Set(":custom-action", Action is not null || ActionTemplate is not null);
        PseudoClasses.Set(":custom-dismiss", DismissAction is not null || DismissActionTemplate is not null);
        PseudoClasses.Set(":data-action", Data?.Visuals.ActionLabel is not null && Action is null && ActionTemplate is null);
        PseudoClasses.Set(":data-dismiss", Data?.Visuals.WithDismissAction == true && DismissAction is null && DismissActionTemplate is null);
        InvalidateMeasure();
        _layout?.InvalidateMeasure();
    }
}

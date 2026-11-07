using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;

namespace Material3Expressive.Components;

/// <summary>A Material rich tooltip surface with optional title and action slots.</summary>
/// <remarks>Content is the supporting text. Actions do not implicitly dismiss the containing TooltipBox.</remarks>
public class RichTooltip : ContentControl
{
    public static readonly StyledProperty<TooltipCaretShape?> CaretShapeProperty = PlainTooltip.CaretShapeProperty.AddOwner<RichTooltip>();
    public TooltipCaretShape? CaretShape { get => GetValue(CaretShapeProperty); set => SetValue(CaretShapeProperty, value); }
    public static readonly StyledProperty<Size?> CaretSizeProperty = PlainTooltip.CaretSizeProperty.AddOwner<RichTooltip>();
    public Size? CaretSize { get => GetValue(CaretSizeProperty); set => SetValue(CaretSizeProperty, value); }
    // The slot layout resolves logical start explicitly, as do other Material layouts.
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<RichTooltip, object?>(nameof(Title));
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<RichTooltip, IDataTemplate?>(nameof(TitleTemplate));
    public static readonly StyledProperty<object?> ActionProperty = AvaloniaProperty.Register<RichTooltip, object?>(nameof(Action));
    public static readonly StyledProperty<IDataTemplate?> ActionTemplateProperty = AvaloniaProperty.Register<RichTooltip, IDataTemplate?>(nameof(ActionTemplate));
    public static readonly StyledProperty<IBrush?> TitleForegroundProperty = AvaloniaProperty.Register<RichTooltip, IBrush?>(nameof(TitleForeground));
    public static readonly StyledProperty<IBrush?> ActionForegroundProperty = AvaloniaProperty.Register<RichTooltip, IBrush?>(nameof(ActionForeground));
    public static readonly StyledProperty<double> TooltipMaxWidthProperty = PlainTooltip.TooltipMaxWidthProperty.AddOwner<RichTooltip>();
    public static readonly StyledProperty<double> TonalElevationProperty = MaterialSurface.TonalElevationProperty.AddOwner<RichTooltip>();
    public static readonly StyledProperty<double> ShadowElevationProperty = MaterialSurface.ShadowElevationProperty.AddOwner<RichTooltip>();

    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public IDataTemplate? TitleTemplate { get => GetValue(TitleTemplateProperty); set => SetValue(TitleTemplateProperty, value); }
    public object? Action { get => GetValue(ActionProperty); set => SetValue(ActionProperty, value); }
    public IDataTemplate? ActionTemplate { get => GetValue(ActionTemplateProperty); set => SetValue(ActionTemplateProperty, value); }
    public IBrush? TitleForeground { get => GetValue(TitleForegroundProperty); set => SetValue(TitleForegroundProperty, value); }
    public IBrush? ActionForeground { get => GetValue(ActionForegroundProperty); set => SetValue(ActionForegroundProperty, value); }
    public double TooltipMaxWidth { get => GetValue(TooltipMaxWidthProperty); set => SetValue(TooltipMaxWidthProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }
    public double ShadowElevation { get => GetValue(ShadowElevationProperty); set => SetValue(ShadowElevationProperty, value); }

    public RichTooltip() => UpdateSlots();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TitleProperty || change.Property == TitleTemplateProperty
            || change.Property == ActionProperty || change.Property == ActionTemplateProperty)
            UpdateSlots();
    }

    private void UpdateSlots()
    {
        PseudoClasses.Set(":has-title", Title is not null || TitleTemplate is not null);
        PseudoClasses.Set(":has-action", Action is not null || ActionTemplate is not null);
    }
}

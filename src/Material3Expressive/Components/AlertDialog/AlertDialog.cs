using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.LogicalTree;

namespace Material3Expressive.Components;

/// <summary>Material alert dialog. Content is the supporting-text slot. Actions and dismissal
/// requests remain application-owned and never implicitly change IsOpen.</summary>
public class AlertDialog : BasicAlertDialog
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<AlertDialog, object?>(nameof(Icon));
    public static readonly StyledProperty<object?> TitleProperty = AvaloniaProperty.Register<AlertDialog, object?>(nameof(Title));
    public static readonly StyledProperty<object?> ConfirmButtonProperty = AvaloniaProperty.Register<AlertDialog, object?>(nameof(ConfirmButton));
    public static readonly StyledProperty<object?> DismissButtonProperty = AvaloniaProperty.Register<AlertDialog, object?>(nameof(DismissButton));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<AlertDialog, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<IDataTemplate?> TitleTemplateProperty = AvaloniaProperty.Register<AlertDialog, IDataTemplate?>(nameof(TitleTemplate));
    public static readonly StyledProperty<IDataTemplate?> ConfirmButtonTemplateProperty = AvaloniaProperty.Register<AlertDialog, IDataTemplate?>(nameof(ConfirmButtonTemplate));
    public static readonly StyledProperty<IDataTemplate?> DismissButtonTemplateProperty = AvaloniaProperty.Register<AlertDialog, IDataTemplate?>(nameof(DismissButtonTemplate));
    public static readonly StyledProperty<IBrush?> IconForegroundProperty = AvaloniaProperty.Register<AlertDialog, IBrush?>(nameof(IconForeground));
    public static readonly StyledProperty<IBrush?> TitleForegroundProperty = AvaloniaProperty.Register<AlertDialog, IBrush?>(nameof(TitleForeground));
    public static readonly StyledProperty<IBrush?> TextForegroundProperty = AvaloniaProperty.Register<AlertDialog, IBrush?>(nameof(TextForeground));
    public static readonly StyledProperty<double> TonalElevationProperty = AvaloniaProperty.Register<AlertDialog, double>(nameof(TonalElevation));

    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public object? ConfirmButton { get => GetValue(ConfirmButtonProperty); set => SetValue(ConfirmButtonProperty, value); }
    public object? DismissButton { get => GetValue(DismissButtonProperty); set => SetValue(DismissButtonProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public IDataTemplate? TitleTemplate { get => GetValue(TitleTemplateProperty); set => SetValue(TitleTemplateProperty, value); }
    public IDataTemplate? ConfirmButtonTemplate { get => GetValue(ConfirmButtonTemplateProperty); set => SetValue(ConfirmButtonTemplateProperty, value); }
    public IDataTemplate? DismissButtonTemplate { get => GetValue(DismissButtonTemplateProperty); set => SetValue(DismissButtonTemplateProperty, value); }
    public IBrush? IconForeground { get => GetValue(IconForegroundProperty); set => SetValue(IconForegroundProperty, value); }
    public IBrush? TitleForeground { get => GetValue(TitleForegroundProperty); set => SetValue(TitleForegroundProperty, value); }
    public IBrush? TextForeground { get => GetValue(TextForegroundProperty); set => SetValue(TextForegroundProperty, value); }
    public double TonalElevation { get => GetValue(TonalElevationProperty); set => SetValue(TonalElevationProperty, value); }

    public AlertDialog() => UpdateSlots();
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // This slot has its own text-style provider. Let its presenter own the logical
        // content so MaterialText observes that provider instead of the dialog declaration.
        if (change.Property == ContentProperty && change.NewValue is ILogical content)
            LogicalChildren.Remove(content);
        if (change.Property == IconProperty || change.Property == IconTemplateProperty
            || change.Property == TitleProperty || change.Property == TitleTemplateProperty
            || change.Property == ContentProperty || change.Property == ContentTemplateProperty
            || change.Property == DismissButtonProperty || change.Property == DismissButtonTemplateProperty)
            UpdateSlots();
    }

    private void UpdateSlots()
    {
        PseudoClasses.Set(":has-icon", Icon is not null || IconTemplate is not null);
        PseudoClasses.Set(":has-title", Title is not null || TitleTemplate is not null);
        PseudoClasses.Set(":has-text", Content is not null || ContentTemplate is not null);
        PseudoClasses.Set(":has-dismiss", DismissButton is not null || DismissButtonTemplate is not null);
    }
}

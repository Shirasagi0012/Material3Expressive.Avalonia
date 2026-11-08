using Avalonia;
using Material3Expressive.Foundation.Rendering;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Material3Expressive.Foundation.Overlays;

namespace Material3Expressive.Components;

/// <summary>A time-picker dialog container. The caller supplies editor and actions; this variant has no title slot.</summary>
public class VibrantTimePickerDialog : Dialog
{
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<VibrantTimePickerDialog>();
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    public static readonly StyledProperty<object?> ConfirmButtonProperty = AvaloniaProperty.Register<VibrantTimePickerDialog, object?>(nameof(ConfirmButton));
    public static readonly StyledProperty<object?> DismissButtonProperty = AvaloniaProperty.Register<VibrantTimePickerDialog, object?>(nameof(DismissButton));
    public static readonly StyledProperty<object?> ModeToggleButtonProperty = AvaloniaProperty.Register<VibrantTimePickerDialog, object?>(nameof(ModeToggleButton));
    public static readonly StyledProperty<IDataTemplate?> ConfirmButtonTemplateProperty = AvaloniaProperty.Register<VibrantTimePickerDialog, IDataTemplate?>(nameof(ConfirmButtonTemplate));
    public static readonly StyledProperty<IDataTemplate?> DismissButtonTemplateProperty = AvaloniaProperty.Register<VibrantTimePickerDialog, IDataTemplate?>(nameof(DismissButtonTemplate));
    public static readonly StyledProperty<IDataTemplate?> ModeToggleButtonTemplateProperty = AvaloniaProperty.Register<VibrantTimePickerDialog, IDataTemplate?>(nameof(ModeToggleButtonTemplate));

    public object? ConfirmButton { get => GetValue(ConfirmButtonProperty); set => SetValue(ConfirmButtonProperty, value); }
    public object? DismissButton { get => GetValue(DismissButtonProperty); set => SetValue(DismissButtonProperty, value); }
    public object? ModeToggleButton { get => GetValue(ModeToggleButtonProperty); set => SetValue(ModeToggleButtonProperty, value); }
    public IDataTemplate? ConfirmButtonTemplate { get => GetValue(ConfirmButtonTemplateProperty); set => SetValue(ConfirmButtonTemplateProperty, value); }
    public IDataTemplate? DismissButtonTemplate { get => GetValue(DismissButtonTemplateProperty); set => SetValue(DismissButtonTemplateProperty, value); }
    public IDataTemplate? ModeToggleButtonTemplate { get => GetValue(ModeToggleButtonTemplateProperty); set => SetValue(ModeToggleButtonTemplateProperty, value); }

    static VibrantTimePickerDialog() => UsePlatformDefaultWidthProperty.OverrideDefaultValue<VibrantTimePickerDialog>(false);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty && change.NewValue is ILogical content)
            LogicalChildren.Remove(content);
        if (change.Property == DismissButtonProperty || change.Property == DismissButtonTemplateProperty)
            PseudoClasses.Set(":has-dismiss", DismissButton is not null || DismissButtonTemplate is not null);
        if (change.Property == ModeToggleButtonProperty || change.Property == ModeToggleButtonTemplateProperty)
            PseudoClasses.Set(":has-mode-toggle", ModeToggleButton is not null || ModeToggleButtonTemplate is not null);
    }
}

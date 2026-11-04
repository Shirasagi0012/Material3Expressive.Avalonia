using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

public class ButtonGroupCustomItem : ButtonGroupItem
{
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty =
        AvaloniaProperty.Register<ButtonGroupCustomItem, MaterialInteractionSource?>(nameof(InteractionSource));

    /// <summary>The stream observed for width animation. When null, PressSource is used.</summary>
    /// <remarks>Assign the same source to the content's input control to share its native interactions.
    /// This property only observes events; it does not emit input or activate content.</remarks>
    public MaterialInteractionSource? InteractionSource
    {
        get => GetValue(InteractionSourceProperty);
        set => SetValue(InteractionSourceProperty, value);
    }

    internal override MaterialInteractionSource? WidthInteractionSource => InteractionSource;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty)
            ReconnectPress();
    }

    public static readonly StyledProperty<object?> MenuContentProperty =
        AvaloniaProperty.Register<ButtonGroupCustomItem, object?>(nameof(MenuContent));
    public static readonly StyledProperty<IDataTemplate?> MenuContentTemplateProperty =
        AvaloniaProperty.Register<ButtonGroupCustomItem, IDataTemplate?>(nameof(MenuContentTemplate));
    public object? MenuContent
    {
        get => GetValue(MenuContentProperty);
        set => SetValue(MenuContentProperty, value);
    }

    public IDataTemplate? MenuContentTemplate
    {
        get => GetValue(MenuContentTemplateProperty);
        set => SetValue(MenuContentTemplateProperty, value);
    }

    internal override Control CreateMenuContent(ButtonGroupMenuState state)
    {
        var result = new ContentControl();
        result.Bind(ContentProperty, new Binding(nameof(MenuContent)) { Source = this });
        result.Bind(ContentTemplateProperty, new Binding(nameof(MenuContentTemplate)) { Source = this });
        result.Bind(DataContextProperty, new Binding(nameof(DataContext)) { Source = this });
        result.Bind(IsEnabledProperty, new Binding(nameof(IsEffectivelyEnabled)) { Source = this });
        return result;
    }
}

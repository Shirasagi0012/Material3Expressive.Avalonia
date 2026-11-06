using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Material3Expressive.Foundation.Interaction;

namespace Material3Expressive.Components;

/// <summary>A switch whose selection is owned by its containing row.</summary>
/// <remarks>Corresponds to Switch with onCheckedChange=null. No input, focus, minimum
/// interactive size or toggle automation is added. The shared source still drives indication.</remarks>
public class SwitchDisplay : TemplatedControl
{
    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<SwitchDisplay, bool>(nameof(IsChecked));
    public static readonly StyledProperty<SwitchColors?> ColorsProperty = SwitchAppearance.ColorsProperty.AddOwner<SwitchDisplay>();
    public static readonly StyledProperty<object?> ThumbIconProperty = SwitchAppearance.ThumbIconProperty.AddOwner<SwitchDisplay>();
    public static readonly StyledProperty<IDataTemplate?> ThumbIconTemplateProperty = SwitchAppearance.ThumbIconTemplateProperty.AddOwner<SwitchDisplay>();
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty = SwitchAppearance.InteractionSourceProperty.AddOwner<SwitchDisplay>();

    public bool IsChecked { get => GetValue(IsCheckedProperty); set => SetValue(IsCheckedProperty, value); }
    public SwitchColors? Colors { get => GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }
    public object? ThumbIcon { get => GetValue(ThumbIconProperty); set => SetValue(ThumbIconProperty, value); }
    public IDataTemplate? ThumbIconTemplate { get => GetValue(ThumbIconTemplateProperty); set => SetValue(ThumbIconTemplateProperty, value); }
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
}

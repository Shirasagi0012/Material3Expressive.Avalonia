// AppBarDsl.kt / AppBarOverflowIndicator, fixed AndroidX baseline; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>An overflow button and tooltip that toggle an explicitly supplied app-bar menu state.</summary>
public class AppBarOverflowIndicator : TemplatedControl
{
    // Compose's enabled belongs to IconButton; TooltipBox keeps its own input. The wrapper
    // has no interaction or automation of its own. Ancestor disabling still applies normally.
    protected override bool IsEnabledCore => true;

    public static readonly StyledProperty<AppBarMenuState?> MenuStateProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, AppBarMenuState?>(nameof(MenuState));
    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, string>(nameof(Description), "More options");
    public static readonly StyledProperty<MaterialShape?> ShapeProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, MaterialShape?>(nameof(Shape));
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty =
        IconButton.InteractionSourceProperty.AddOwner<AppBarOverflowIndicator>();
    public static readonly StyledProperty<Color?> ContainerColorProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, Color?>(nameof(ContainerColor));
    public static readonly StyledProperty<Color?> ContentColorProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, Color?>(nameof(ContentColor));
    public static readonly StyledProperty<Color?> DisabledContainerColorProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, Color?>(nameof(DisabledContainerColor));
    public static readonly StyledProperty<Color?> DisabledContentColorProperty =
        AvaloniaProperty.Register<AppBarOverflowIndicator, Color?>(nameof(DisabledContentColor));

    public AppBarMenuState? MenuState { get => GetValue(MenuStateProperty); set => SetValue(MenuStateProperty, value); }
    /// <summary>The tooltip and accessible description. Set this to localize the default English label.</summary>
    public string Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
    /// <summary>An explicit container path; null retains the standard icon-button shape.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public MaterialInteractionSource? InteractionSource { get => GetValue(InteractionSourceProperty); set => SetValue(InteractionSourceProperty, value); }
    /// <summary>The enabled container color; null is transparent.</summary>
    public Color? ContainerColor { get => GetValue(ContainerColorProperty); set => SetValue(ContainerColorProperty, value); }
    /// <summary>The enabled icon color; null follows the ambient Foreground.</summary>
    public Color? ContentColor { get => GetValue(ContentColorProperty); set => SetValue(ContentColorProperty, value); }
    /// <summary>The disabled container color; null is transparent.</summary>
    public Color? DisabledContainerColor { get => GetValue(DisabledContainerColorProperty); set => SetValue(DisabledContainerColorProperty, value); }
    /// <summary>The disabled icon color; null replaces the enabled color's alpha with the disabled-content opacity.</summary>
    public Color? DisabledContentColor { get => GetValue(DisabledContentColorProperty); set => SetValue(DisabledContentColorProperty, value); }

    private IconButton? _button;
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        if (_button is not null) _button.Click -= ToggleMenu;
        base.OnApplyTemplate(e);
        _button = e.NameScope.Find<IconButton>("PART_Button");
        if (_button is not null) _button.Click += ToggleMenu;
    }

    private void ToggleMenu(object? sender, RoutedEventArgs e)
    {
        if (_button is not { IsEffectivelyEnabled: true } || MenuState is not { } state) return;
        if (state.IsShowing) state.Dismiss(); else state.Show();
    }
}

internal static class AppBarOverflowIndicatorTheme
{
    public static readonly Geometry MoreVert = Geometry.Parse(
        "M12 8 c1.1 0 2 -.9 2 -2 s-.9 -2 -2 -2 s-2 .9 -2 2 s.9 2 2 2 Z " +
        "M12 10 c-1.1 0 -2 .9 -2 2 s.9 2 2 2 s2 -.9 2 -2 s-.9 -2 -2 -2 Z " +
        "M12 16 c-1.1 0 -2 .9 -2 2 s.9 2 2 2 s2 -.9 2 -2 s-.9 -2 -2 -2 Z");

    public static readonly FuncMultiValueConverter<object, object> ContainerBrushConverter = new(values =>
        values.Count < 3 || values[0] is not bool enabled ? AvaloniaProperty.UnsetValue
            : new SolidColorBrush(values[enabled ? 1 : 2] is Color color ? color : Colors.Transparent));
    public static readonly FuncMultiValueConverter<object, object> ContentBrushConverter = new(values =>
    {
        if (values.Count < 5 || values[0] is not bool enabled || values[4] is not double disabledOpacity ||
            values[1] is not Color && values[3] is not ISolidColorBrush)
            return AvaloniaProperty.UnsetValue;
        var content = values[1] is Color color ? color : ((ISolidColorBrush)values[3]!).Color;
        return new SolidColorBrush(enabled ? content : values[2] is Color disabled ? disabled
            : Color.FromArgb((byte)Math.Round(Math.Clamp(disabledOpacity, 0, 1) * 255), content.R, content.G, content.B));
    });

}

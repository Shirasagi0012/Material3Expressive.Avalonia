using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum FloatingActionButtonSize { Default, Small, Medium, Large }

/// <summary>A Material FAB. Content is centered without imposing an icon size.</summary>
public class FloatingActionButton : Button
{
    public static readonly StyledProperty<FloatingActionButtonSize> SizeProperty = AvaloniaProperty.Register<FloatingActionButton, FloatingActionButtonSize>(nameof(Size));
    public static readonly StyledProperty<FloatingActionButtonElevation?> ElevationProperty = AvaloniaProperty.Register<FloatingActionButton, FloatingActionButtonElevation?>(nameof(Elevation));
    public static readonly StyledProperty<bool> AnimatedVisibleProperty = AvaloniaProperty.Register<FloatingActionButton, bool>(nameof(AnimatedVisible), true);
    public static readonly StyledProperty<float> HiddenScaleProperty = AvaloniaProperty.Register<FloatingActionButton, float>(nameof(HiddenScale), 0.2f);
    public static readonly StyledProperty<RelativePoint> VisibilityAlignmentProperty = AvaloniaProperty.Register<FloatingActionButton, RelativePoint>(nameof(VisibilityAlignment), RelativePoint.BottomRight);
    public static readonly StyledProperty<IFiniteAnimationSpec<float>?> VisibilityScaleSpecProperty = AvaloniaProperty.Register<FloatingActionButton, IFiniteAnimationSpec<float>?>(nameof(VisibilityScaleSpec));
    public static readonly StyledProperty<IFiniteAnimationSpec<float>?> VisibilityAlphaSpecProperty = AvaloniaProperty.Register<FloatingActionButton, IFiniteAnimationSpec<float>?>(nameof(VisibilityAlphaSpec));

    public FloatingActionButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public FloatingActionButtonElevation? Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public bool AnimatedVisible { get => GetValue(AnimatedVisibleProperty); set => SetValue(AnimatedVisibleProperty, value); }
    public float HiddenScale { get => GetValue(HiddenScaleProperty); set => SetValue(HiddenScaleProperty, value); }
    /// <summary>Logical alignment of the scale pivot; the horizontal coordinate follows RTL.</summary>
    public RelativePoint VisibilityAlignment { get => GetValue(VisibilityAlignmentProperty); set => SetValue(VisibilityAlignmentProperty, value); }
    public IFiniteAnimationSpec<float>? VisibilityScaleSpec { get => GetValue(VisibilityScaleSpecProperty); set => SetValue(VisibilityScaleSpecProperty, value); }
    public IFiniteAnimationSpec<float>? VisibilityAlphaSpec { get => GetValue(VisibilityAlphaSpecProperty); set => SetValue(VisibilityAlphaSpecProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeProperty)
        {
            PseudoClasses.Set(":small", Size == FloatingActionButtonSize.Small);
            PseudoClasses.Set(":medium", Size == FloatingActionButtonSize.Medium);
            PseudoClasses.Set(":large", Size == FloatingActionButtonSize.Large);
        }
    }
}

/// <summary>Content-only or icon-and-label Extended FAB, preserving the distinct upstream transitions.</summary>
public class ExtendedFloatingActionButton : FloatingActionButton
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<ExtendedFloatingActionButton, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<ExtendedFloatingActionButton, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<ExtendedFloatingActionButton, bool>(nameof(Expanded), true);
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
}

public static class FloatingActionButtonDefaults
{
    public const double MediumIconSize = 28;
    // Explicit correction in FloatingActionButton.kt, rather than FabLargeTokens.IconSize (32).
    public const double LargeIconSize = 36;
    public static FloatingActionButtonElevation Elevation(double @default = 6, double pressed = 6, double focused = 6, double hovered = 8) => new(@default, pressed, focused, hovered);
    public static FloatingActionButtonElevation LoweredElevation(double @default = 1, double pressed = 1, double focused = 1, double hovered = 3) => new(@default, pressed, focused, hovered);
    public static FloatingActionButtonElevation BottomAppBarFabElevation(double @default = 0, double pressed = 0, double focused = 0, double hovered = 0) => new(@default, pressed, focused, hovered);
}

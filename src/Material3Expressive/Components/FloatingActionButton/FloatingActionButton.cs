using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using ComposeAnimation.Runtime;

namespace Material3Expressive.Components;

public enum FloatingActionButtonSize { Default, Small, Medium, Large }

/// <summary>A Material FAB. Content is centered without imposing an icon size.</summary>
public class FloatingActionButton : Button
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<FloatingActionButton, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<FloatingActionButton, IDataTemplate?>(nameof(IconTemplate));
    public static readonly StyledProperty<FloatingActionButtonSize> SizeProperty = AvaloniaProperty.Register<FloatingActionButton, FloatingActionButtonSize>(nameof(Size));
    public static readonly StyledProperty<FloatingActionButtonElevation?> ElevationProperty = AvaloniaProperty.Register<FloatingActionButton, FloatingActionButtonElevation?>(nameof(Elevation));
    public static readonly StyledProperty<bool> AnimatedVisibleProperty = AvaloniaProperty.Register<FloatingActionButton, bool>(nameof(AnimatedVisible), true);
    public static readonly StyledProperty<float> HiddenScaleProperty = AvaloniaProperty.Register<FloatingActionButton, float>(nameof(HiddenScale));
    public static readonly StyledProperty<RelativePoint> VisibilityAlignmentProperty = AvaloniaProperty.Register<FloatingActionButton, RelativePoint>(nameof(VisibilityAlignment));
    public static readonly StyledProperty<IFiniteAnimationSpec<float>> VisibilityScaleSpecProperty = AvaloniaProperty.Register<FloatingActionButton, IFiniteAnimationSpec<float>>(nameof(VisibilityScaleSpec), null!);
    public static readonly StyledProperty<IFiniteAnimationSpec<float>> VisibilityAlphaSpecProperty = AvaloniaProperty.Register<FloatingActionButton, IFiniteAnimationSpec<float>>(nameof(VisibilityAlphaSpec), null!);

    private FabIconContentLayout? _iconContentLayout;

    /// <summary>The icon content shown instead of Content when set.</summary>
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    /// <summary>The data template used to create Icon content.</summary>
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    public FloatingActionButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }
    public FloatingActionButtonElevation? Elevation { get => GetValue(ElevationProperty); set => SetValue(ElevationProperty, value); }
    public bool AnimatedVisible { get => GetValue(AnimatedVisibleProperty); set => SetValue(AnimatedVisibleProperty, value); }
    public float HiddenScale { get => GetValue(HiddenScaleProperty); set => SetValue(HiddenScaleProperty, value); }
    /// <summary>Logical alignment of the scale pivot; the horizontal coordinate follows RTL.</summary>
    public RelativePoint VisibilityAlignment { get => GetValue(VisibilityAlignmentProperty); set => SetValue(VisibilityAlignmentProperty, value); }
    public IFiniteAnimationSpec<float> VisibilityScaleSpec { get => GetValue(VisibilityScaleSpecProperty); set => SetValue(VisibilityScaleSpecProperty, value); }
    public IFiniteAnimationSpec<float> VisibilityAlphaSpec { get => GetValue(VisibilityAlphaSpecProperty); set => SetValue(VisibilityAlphaSpecProperty, value); }
    internal bool HasIcon => Icon is not null || IconTemplate is not null;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _iconContentLayout = e.NameScope.Find<FabIconContentLayout>("PART_IconContentLayout");
        _iconContentLayout?.InvalidateMeasure();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IconProperty || change.Property == IconTemplateProperty)
            _iconContentLayout?.InvalidateMeasure();
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
    public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<ExtendedFloatingActionButton, bool>(nameof(Expanded), true);
    public bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
}

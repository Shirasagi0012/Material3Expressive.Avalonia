using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Whether a badge is a dot or carries a count.</summary>
public enum BadgeVariant { Small, Large }

/// <summary>
/// A badge: a dot, or a dot with a number in it.
/// </summary>
/// <remarks>
/// The small badge carries no count on purpose — it says only that something happened, which is
/// often all a navigation item has room to say. The large one says how much, and grows wider than
/// it is tall to fit.
/// </remarks>
public class MaterialBadge : TemplatedControl
{
    // ShapeRole is deliberately retained in the profile and converted to a CornerRadius only at
    // render time, when Bounds is available.  The snapshot controller is used solely for this
    // late shape-scheme lookup; all badge colours, metrics and typography arrive through the
    // MaterialValue endpoints below.
    private readonly MaterialThemeController _theme;

    public static readonly StyledProperty<BadgeVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialBadge, BadgeVariant>(nameof(Variant), BadgeVariant.Large);

    /// <inheritdoc cref="Count" />
    public static readonly StyledProperty<object?> CountProperty =
        AvaloniaProperty.Register<MaterialBadge, object?>(nameof(Count));

    private static readonly BadgeGeometryProfile DefaultGeometryProfile = new(
        16,
        ShapeRole.CornerFull,
        new Thickness(4, 0),
        TypographyRole.LabelSmall);

    private static readonly BadgeVisualColors DefaultVisualColors = new(
        TransparentBrush(),
        TransparentBrush());

    /// <summary>Gets the variant-selected layout and late-shape profile.</summary>
    internal static readonly StyledProperty<BadgeGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialBadge, BadgeGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    /// <summary>Gets the variant-selected container and label brushes.</summary>
    internal static readonly StyledProperty<BadgeVisualColors> VisualColorsProperty =
        AvaloniaProperty.Register<MaterialBadge, BadgeVisualColors>(
            nameof(VisualColors),
            DefaultVisualColors);

    public MaterialBadge()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
    }

    static MaterialBadge()
    {
        AffectsMeasure<MaterialBadge>(GeometryProfileProperty);
        AffectsRender<MaterialBadge>(
            VariantProperty,
            CountProperty,
            GeometryProfileProperty,
            VisualColorsProperty);
    }

    /// <summary>Small (a dot) or large (a dot with a count).</summary>
    public BadgeVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    /// <summary>What to show inside a large badge. Ignored by the small one.</summary>
    public object? Count
    {
        get => GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    /// <summary>Whether this size shows its count.</summary>
    public bool HasLabel => Variant == BadgeVariant.Large && Count is not null;

    public static readonly DirectProperty<MaterialBadge, bool> HasLabelProperty =
        AvaloniaProperty.RegisterDirect<MaterialBadge, bool>(nameof(HasLabel), o => o.HasLabel);

    public override void Render(DrawingContext context)
    {
        var geometry = GeometryProfile;
        var corners = _theme.Current.Theme.Shapes.GetCornerRadius(
            geometry.ContainerShape,
            Bounds.Size);
        MaterialContainer.Draw(context, Bounds.Size, corners, VisualColors.ContainerColor);

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);

        return new Size(
            Math.Max(content.Width, GeometryProfile.Size),
            GeometryProfile.Size);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        var previousHasLabel = HasLabel;
        if (change.Property == VariantProperty)
        {
            previousHasLabel =
                change.GetOldValue<BadgeVariant>() == BadgeVariant.Large && Count is not null;
        }
        else if (change.Property == CountProperty)
        {
            previousHasLabel =
                Variant == BadgeVariant.Large && change.GetOldValue<object?>() is not null;
        }

        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty || change.Property == CountProperty)
            RaisePropertyChanged(HasLabelProperty, previousHasLabel, HasLabel);
    }

    /// <summary>Gets or sets the hidden variant-selected layout profile.</summary>
    internal BadgeGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    /// <summary>Gets or sets the hidden variant-selected brush profile.</summary>
    internal BadgeVisualColors VisualColors
    {
        get => GetValue(VisualColorsProperty);
        set => SetValue(VisualColorsProperty, value);
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}

using System;
using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// A badge: a dot, or a dot with a number in it.
/// </summary>
/// <remarks>
/// The small badge carries no count on purpose — it says only that something happened, which is
/// often all a navigation item has room to say. The large one says how much, and grows wider than
/// it is tall to fit.
/// </remarks>
public class MaterialBadge : MaterialControl
{
    public static readonly StyledProperty<BadgeVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialBadge, BadgeVariant>(nameof(Variant), BadgeVariant.Large);

    /// <inheritdoc cref="Count" />
    public static readonly StyledProperty<object?> CountProperty =
        AvaloniaProperty.Register<MaterialBadge, object?>(nameof(Count));

    private BadgeDefaultsTokens _tokens = BadgeDefaultsTokens.Large;
    private BadgeColors _colors = BadgeDefaultsTokens.Large.Resolve(null, false);

    public MaterialBadge()
    {
        ApplyTokens();
    }

    static MaterialBadge()
    {
        AffectsRender<MaterialBadge>(VariantProperty, CountProperty);
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
    public bool HasLabel => _tokens.HasLabel && Count is not null;

    public static readonly DirectProperty<MaterialBadge, bool> HasLabelProperty =
        AvaloniaProperty.RegisterDirect<MaterialBadge, bool>(nameof(HasLabel), o => o.HasLabel);

    public override void Render(DrawingContext context)
    {
        var corners = ResolvedTheme.Shapes.GetCornerRadius(_tokens.ContainerShape, Bounds.Size);
        MaterialContainer.Draw(context, Bounds.Size, corners, _colors.ContainerColor);

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var content = base.MeasureOverride(availableSize);

        return new Size(
            Math.Max(content.Width, _tokens.Size),
            _tokens.Size);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty || change.Property == CountProperty)
            ApplyTokens();
    }

    protected override void OnThemeChanged(ResolvedTheme theme, bool isDark)
    {
        ApplyTokens();
    }

    private void ApplyTokens()
    {
        var previousHasLabel = HasLabel;

        _tokens = ResolvedTheme.GetBadgeDefaultsTokens(Variant);
        _colors = ResolvedTheme.GetBadgeColors(Variant, IsDarkVariant);
        RaisePropertyChanged(HasLabelProperty, previousHasLabel, HasLabel);

        var typography = ResolvedTheme.Typography;

        Foreground = _colors.LabelTextColor;
        Padding = new Thickness(_tokens.HorizontalSpace, 0, _tokens.HorizontalSpace, 0);
        MinWidth = _tokens.Size;
        MinHeight = _tokens.Size;
        FontSize = typography.GetTypeScale(_tokens.LabelTextStyle).FontSize;
        FontFamily = typography.GetFontFamily(_tokens.LabelTextStyle);
        FontWeight = typography.GetFontWeight(_tokens.LabelTextStyle);

        InvalidateMeasure();
        InvalidateVisual();
    }
}

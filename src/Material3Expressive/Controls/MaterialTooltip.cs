using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>The compact inverted tooltip or the richer elevated tooltip treatment.</summary>
public enum TooltipVariant
{
    Plain,
    Rich
}

/// <summary>
/// A Material 3 tooltip, plain or rich.
/// </summary>
/// <remarks>
/// Attached to a control rather than wrapping one, so adding a tooltip does not change the layout
/// around it. The plain form names a control and inverts; the rich form explains one and sits on a
/// normal surface with room for a title and an action.
/// <para>
/// The popup is Avalonia's, which is what makes the tooltip escape its parent's clip and follow
/// the window — and, because a popup root's logical parent is the popup itself, it inherits the
/// theme in scope at the control it is attached to for free.
/// </para>
/// </remarks>
public class MaterialTooltip : ContentControl
{
    /// <summary>The tooltip content to show for a control.</summary>
    public static readonly AttachedProperty<object?> TipProperty =
        AvaloniaProperty.RegisterAttached<MaterialTooltip, Control, object?>("Tip");

    /// <summary>Which tooltip style to use.</summary>
    public static readonly AttachedProperty<TooltipVariant> VariantProperty =
        AvaloniaProperty.RegisterAttached<MaterialTooltip, Control, TooltipVariant>("Variant");

    /// <inheritdoc cref="Title" />
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<MaterialTooltip, object?>(nameof(Title));

    /// <inheritdoc cref="Style" />
    public static readonly StyledProperty<TooltipVariant> StyleProperty =
        AvaloniaProperty.Register<MaterialTooltip, TooltipVariant>(nameof(Style));

    internal static readonly StyledProperty<TooltipGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialTooltip, TooltipGeometryProfile>(
            nameof(GeometryProfile),
            new TooltipGeometryProfile(
                ShapeTokens.CornerExtraSmall,
                0,
                24,
                200,
                new Thickness(8, 4),
                TypographyRole.BodySmall,
                TypographyRole.TitleSmall));

    internal static readonly StyledProperty<TooltipVisualProfile> VisualProfileProperty =
        AvaloniaProperty.Register<MaterialTooltip, TooltipVisualProfile>(
            nameof(VisualProfile),
            new TooltipVisualProfile(
                TransparentBrush(),
                TransparentBrush(),
                TransparentBrush(),
                TransparentBrush(),
                Colors.Black));

    public MaterialTooltip() { }

    static MaterialTooltip()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialTooltip>(false);
        AffectsMeasure<MaterialTooltip>(GeometryProfileProperty);
        AffectsRender<MaterialTooltip>(GeometryProfileProperty, VisualProfileProperty);
        TipProperty.Changed.AddClassHandler<Control>(OnTipChanged);
    }

    /// <summary>A rich tooltip's title.</summary>
    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Plain or rich.</summary>
    public TooltipVariant Style
    {
        get => GetValue(StyleProperty);
        set => SetValue(StyleProperty, value);
    }

    /// <summary>The title brush, exposed for the template.</summary>
    public IImmutableBrush TitleBrush => VisualProfile.TitleColor;

    public static readonly DirectProperty<MaterialTooltip, IImmutableBrush> TitleBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialTooltip, IImmutableBrush>(nameof(TitleBrush), o => o.TitleBrush);

    public static object? GetTip(Control control)
    {
        return control.GetValue(TipProperty);
    }

    public static void SetTip(Control control, object? value)
    {
        control.SetValue(TipProperty, value);
    }

    public static TooltipVariant GetVariant(Control control)
    {
        return control.GetValue(VariantProperty);
    }

    public static void SetVariant(Control control, TooltipVariant value)
    {
        control.SetValue(VariantProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var corners = GeometryProfile.ContainerShape.ToCornerRadius(
            Bounds.Size,
            FlowDirection == FlowDirection.RightToLeft);

        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            corners,
            VisualProfile.ContainerColor,
            null,
            0,
            GeometryProfile.ContainerElevation,
            VisualProfile.ShadowColor);

        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(
            new Size(Math.Min(availableSize.Width, GeometryProfile.MaxWidth), availableSize.Height));
        return new Size(size.Width, Math.Max(size.Height, GeometryProfile.MinHeight));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VisualProfileProperty)
        {
            var oldProfile = change.GetOldValue<TooltipVisualProfile>();
            RaisePropertyChanged(TitleBrushProperty, oldProfile.TitleColor, TitleBrush);
        }
    }

    /// <summary>
    /// Hands the tip to Avalonia's tooltip service, wrapped so it gets Material's chrome.
    /// </summary>
    /// <remarks>
    /// Reusing the stock service rather than reimplementing hover timing means the show delay,
    /// the follow-the-pointer behaviour and the keyboard handling are all the platform's.
    /// </remarks>
    private static void OnTipChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is null)
        {
            ToolTip.SetTip(control, null);
            return;
        }

        ToolTip.SetTip(control, new MaterialTooltip
        {
            Content = e.NewValue,
            Style = GetVariant(control)
        });
    }

    internal TooltipGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    internal TooltipVisualProfile VisualProfile
    {
        get => GetValue(VisualProfileProperty);
        set => SetValue(VisualProfileProperty, value);
    }

    private static IImmutableBrush TransparentBrush() =>
        new ImmutableSolidColorBrush(Colors.Transparent);
}

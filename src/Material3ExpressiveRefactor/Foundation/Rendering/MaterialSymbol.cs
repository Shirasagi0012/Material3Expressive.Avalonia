using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Rendering.Symbols;
using MaterialSymbols;
using MaterialSymbols.Avalonia;

namespace Material3ExpressiveRefactor.Foundation.Rendering;

/// <summary>
/// Draws a Material Symbol from the Rounded variable font, with the font's four axes exposed as
/// animatable properties.
/// </summary>
/// <remarks>
/// <para>
/// The symbol fills the largest square that fits the control and is painted with
/// <see cref="Foreground"/>, which is <see cref="TextElement.ForegroundProperty"/>, so a symbol
/// inherits the surrounding text colour without being told about it. Unconstrained, the control
/// measures 24×24 — Material's baseline symbol size.
/// </para>
/// <para>
/// <see cref="Fill"/> is a <see cref="double"/> rather than a flag on purpose: the font interpolates
/// continuously between the outlined and solid forms, so a selection state can be animated (or
/// spring-driven) instead of snapping.
/// </para>
/// <para>
/// <see cref="Grade"/> and <see cref="OpticalSize"/> default to <see cref="double.NaN"/>, meaning
/// "track the context": optical size follows the rendered size and grade follows the theme variant.
/// Assigning either one takes over from that.
/// </para>
/// <para>
/// Unlike the MaterialSymbols.Avalonia module's control, this one is hardwired to the Rounded
/// family (Material 3 Expressive's first-party icon style) and carries no style axis or font
/// registry — the variable-font render stack is ported in-house under
/// <c>Foundation/Rendering/Symbols</c>, borrowing only the font asset and the generated
/// <see cref="MaterialSymbolCodepoint"/> enum from the module.
/// </para>
/// </remarks>
public class MaterialSymbol : Control
{
    /// <summary>The size a symbol occupies when nothing constrains it.</summary>
    public const double DefaultSize = 24d;

    /// <summary>Creates a symbol showing <see cref="MaterialSymbolCodepoint.None"/> until one is set.</summary>
    public MaterialSymbol()
    {
        // ThemeVariant.ActualThemeVariantProperty is internal to Avalonia, so auto grade has to
        // hang off the event rather than off OnPropertyChanged. Subscribing to our own event keeps
        // the reference self-contained, so there is nothing to unsubscribe.
        ActualThemeVariantChanged += (_, _) =>
        {
            if (double.IsNaN(Grade))
                InvalidateVisual();
        };
    }

    /// <summary>Defines the <see cref="Symbol"/> property.</summary>
    public static readonly StyledProperty<MaterialSymbolCodepoint> SymbolProperty =
        AvaloniaProperty.Register<MaterialSymbol, MaterialSymbolCodepoint>(
            nameof(Symbol),
            MaterialSymbolCodepoint.None);

    /// <summary>
    /// The symbol to draw. <see cref="MaterialSymbolCodepoint.None"/> — the default — draws nothing.
    /// </summary>
    public MaterialSymbolCodepoint Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }

    /// <summary>
    /// The <c>wght</c> axis, clamped to <see cref="SymbolVariation.MinWeight"/>–<see cref="SymbolVariation.MaxWeight"/>.
    /// </summary>
    public static readonly StyledProperty<double> WeightProperty =
        AvaloniaProperty.Register<MaterialSymbol, double>(nameof(Weight), SymbolVariation.DefaultWeight);

    public double Weight
    {
        get => GetValue(WeightProperty);
        set => SetValue(WeightProperty, value);
    }

    /// <summary>
    /// The <c>FILL</c> axis, 0 (outlined) to 1 (solid). Intermediate values are real intermediate
    /// shapes, so this is the property to animate for a selection state.
    /// </summary>
    public static readonly StyledProperty<double> FillProperty =
        AvaloniaProperty.Register<MaterialSymbol, double>(nameof(Fill), SymbolVariation.MinFill);

    public double Fill
    {
        get => GetValue(FillProperty);
        set => SetValue(FillProperty, value);
    }

    /// <summary>
    /// The <c>GRAD</c> axis. <see cref="double.NaN"/> (the default) follows the theme variant, using
    /// <see cref="SymbolVariation.DarkGrade"/> on a dark one and <see cref="SymbolVariation.LightGrade"/>
    /// otherwise.
    /// </summary>
    public static readonly StyledProperty<double> GradeProperty =
        AvaloniaProperty.Register<MaterialSymbol, double>(nameof(Grade), double.NaN);

    public double Grade
    {
        get => GetValue(GradeProperty);
        set => SetValue(GradeProperty, value);
    }

    /// <summary>
    /// The <c>opsz</c> axis. <see cref="double.NaN"/> (the default) follows the size the symbol is
    /// actually being drawn at, clamped to the axis range.
    /// </summary>
    public static readonly StyledProperty<double> OpticalSizeProperty =
        AvaloniaProperty.Register<MaterialSymbol, double>(nameof(OpticalSize), double.NaN);

    public double OpticalSize
    {
        get => GetValue(OpticalSizeProperty);
        set => SetValue(OpticalSizeProperty, value);
    }

    /// <summary>
    /// The brush the symbol is painted with. Inherited from the surrounding text context; only the
    /// colour of a solid-colour brush is used (see the type remarks).
    /// </summary>
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<MaterialSymbol>();

    /// <inheritdoc cref="ForegroundProperty"/>
    public IBrush? Foreground
    {
        get => GetValue(ForegroundProperty);
        set => SetValue(ForegroundProperty, value);
    }

    /// <summary>
    /// The axis values actually in effect, with <see cref="double.NaN"/> resolved against the
    /// current size and theme and everything clamped to the font's ranges.
    /// </summary>
    public SymbolVariation ResolvedVariation => ResolveVariation(RenderedSize);

    private double RenderedSize
    {
        get
        {
            var size = Math.Min(Bounds.Width, Bounds.Height);
            return size > 0 ? size : DefaultSize;
        }
    }

    private SymbolVariation ResolveVariation(double renderedSize)
    {
        var grade = double.IsNaN(Grade)
            ? IsDark(ActualThemeVariant) ? SymbolVariation.DarkGrade : SymbolVariation.LightGrade
            : Grade;

        var opticalSize = double.IsNaN(OpticalSize) ? renderedSize : OpticalSize;

        return new SymbolVariation(Weight, Fill, grade, opticalSize).Clamp();
    }

    /// <summary>
    /// Whether a variant is dark, following <see cref="ThemeVariant.InheritVariant"/> so that a
    /// custom variant built on top of <see cref="ThemeVariant.Dark"/> still counts as dark.
    /// </summary>
    private static bool IsDark(ThemeVariant? variant)
    {
        for (var current = variant; current is not null; current = current.InheritVariant)
        {
            if (current == ThemeVariant.Dark)
                return true;
        }

        return false;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // Square, and never bigger than Material's baseline size unless something explicitly sizes
        // the control — availableSize cannot distinguish "Width=48" from "a 48-wide parent", so the
        // cap here plus Layoutable's explicit-size handling is what makes Width/Height mean what a
        // caller expects. Math.Min against an infinite available dimension yields the other one.
        var side = Math.Min(Math.Min(availableSize.Width, availableSize.Height), DefaultSize);
        return new Size(side, side);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = new Rect(Bounds.Size);
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        if (Foreground is not ISolidColorBrush { Color: var color })
            return;

        var variation = ResolveVariation(Math.Min(bounds.Width, bounds.Height));

        context.Custom(new SymbolDrawOperation(
            bounds, SymbolTypeface.Rounded, (int)Symbol, VariationKey.From(variation), color));
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Bounds is in the list because it feeds auto optical size — the symbol has to be redrawn
        // when it is resized, not just relaid out.
        if (change.Property == WeightProperty
            || change.Property == FillProperty
            || change.Property == GradeProperty
            || change.Property == OpticalSizeProperty
            || change.Property == SymbolProperty
            || change.Property == ForegroundProperty
            || change.Property == BoundsProperty)
        {
            InvalidateVisual();
        }
    }
}

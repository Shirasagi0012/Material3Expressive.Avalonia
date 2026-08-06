using Avalonia;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls.ButtonRuntime;

/// <summary>
/// The component-owned runtime adapter for an ordinary Material button.
/// </summary>
/// <remarks>
/// <para>
/// The generated records are still the immutable token values and therefore remain the source of
/// defaults. This adapter is the boundary where a control asks for a theme-resolved value or
/// combines several raw values into an Avalonia value.
/// </para>
/// <para>
/// Interaction selection and Avalonia-specific value shaping belong here rather than in a token
/// table. In particular, the JSON table must not become a behaviour language for pointer state,
/// bindings, or control templates.
/// </para>
/// </remarks>
internal static class ButtonDefaults
{
    /// <summary>Returns the generated colour values resolved for the current theme variant.</summary>
    public static ButtonColors Colors(ResolvedTheme theme, ButtonVariant variant, bool isDark) =>
        theme.GetButtonColors(variant, isDark);

    /// <summary>Returns the generated, unresolved colour tokens for a component policy.</summary>
    public static ButtonColorTokens ColorTokens(ResolvedTheme theme, ButtonVariant variant) =>
        theme.GetButtonColorTokens(variant);

    /// <summary>Returns the generated metrics for one expressive button size.</summary>
    public static ButtonSizeTokens Metrics(ResolvedTheme theme, ButtonSize size) =>
        theme.GetButtonSizeTokens(size);

    /// <summary>Returns the three geometry roles used by an ordinary button.</summary>
    public static ButtonShapes Shapes(ResolvedTheme theme, ButtonSize size)
    {
        var metrics = Metrics(theme, size);
        return new ButtonShapes(
            metrics.ContainerShape,
            metrics.PressedContainerShape,
            metrics.SquareContainerShape);
    }

    /// <summary>Folds the size tokens into the Avalonia padding value consumed by a template.</summary>
    public static Thickness ContentPadding(ResolvedTheme theme, ButtonSize size)
    {
        return ContentPadding(Metrics(theme, size));
    }

    /// <summary>Folds an already selected size set into the Avalonia padding value.</summary>
    public static Thickness ContentPadding(ButtonSizeTokens metrics) =>
        new(
            metrics.LeadingSpace,
            metrics.VerticalSpace,
            metrics.TrailingSpace,
            metrics.VerticalSpace);

    /// <summary>
    /// Selects the component elevation for the current enabled and interaction state.
    /// </summary>
    public static double Elevation(
        ResolvedTheme theme,
        ButtonVariant variant,
        bool enabled,
        InteractionState interaction)
    {
        return Elevation(ColorTokens(theme, variant), enabled, interaction);
    }

    /// <summary>Selects elevation from an already selected colour token set.</summary>
    public static double Elevation(
        ButtonColorTokens tokens,
        bool enabled,
        InteractionState interaction)
    {
        if (!enabled)
            return tokens.DisabledContainerElevation;

        if (interaction.HasFlag(InteractionState.Pressed))
            return tokens.PressedContainerElevation;

        if (interaction.HasFlag(InteractionState.Hovered))
            return tokens.HoveredContainerElevation;

        if (interaction.HasFlag(InteractionState.Focused))
            return tokens.FocusedContainerElevation;

        return tokens.ContainerElevation;
    }
}

/// <summary>Geometry roles consumed by an ordinary Button presenter.</summary>
internal sealed record ButtonShapes(
    ShapeRole ContainerShape,
    ShapeRole PressedContainerShape,
    ShapeRole SquareContainerShape);

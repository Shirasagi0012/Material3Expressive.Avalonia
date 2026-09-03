using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Material3ExpressiveRefactor.Foundation.Tokens;
using MaterialSymbols;
using static Material3ExpressiveRefactor.Tokens.MaterialTokens;

namespace Material3ExpressiveRefactor.Controls;

/// <summary>
/// An icon-only button (upstream IconButton): a <see cref="Button"/> whose content is a Material
/// Symbol, drawn by a <c>MaterialSymbol</c> template part at the size table's icon size. Themes
/// (Styles/IconButton.axaml) provide the four styles (standard — the implicit default, filled,
/// tonal, outlined) and the five size classes; all values come from the comp slots below.
/// </summary>
public class IconButton : Button
{
    /// <summary>Defines the <see cref="Symbol"/> property.</summary>
    public static readonly StyledProperty<MaterialSymbolCodepoint> SymbolProperty =
        AvaloniaProperty.Register<IconButton, MaterialSymbolCodepoint>(
            nameof(Symbol),
            MaterialSymbolCodepoint.None);

    /// <summary>The symbol to draw. <see cref="MaterialSymbolCodepoint.None"/> — the default — draws nothing.</summary>
    public MaterialSymbolCodepoint Symbol
    {
        get => GetValue(SymbolProperty);
        set => SetValue(SymbolProperty, value);
    }
}

/// <summary>
/// Baseline defaults for the five IconButton size tables (upstream XSmall/Small/Medium/Large/
/// XLargeIconButtonTokens 14_1_0, except the Large table which still ships as v0_11_0), shared by
/// every icon button style. Shapes hop into the sys corner scale. The three width options are
/// recorded as space pairs (container width = space + icon + space) but only the default option is
/// consumed for now — it is square at every size, so themes bind MinWidth to ContainerHeight and
/// the narrow/wide XAML expression is deferred with the width-option slice. The older Large
/// revision names the default pair Uniform*; the 14_1_0 Default* naming is kept across all five
/// tables. Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class IconButtonSizesTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.IconButtonXSmall.ContainerHeight] = 32d,
        [Comp.IconButtonXSmall.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.IconButtonXSmall.ContainerShapeSquare] = new TokenRef(Sys.Shape.Medium),
        [Comp.IconButtonXSmall.PressedContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.IconButtonXSmall.IconSize] = 20d,
        [Comp.IconButtonXSmall.DefaultLeadingSpace] = 6d,
        [Comp.IconButtonXSmall.DefaultTrailingSpace] = 6d,
        [Comp.IconButtonXSmall.NarrowLeadingSpace] = 4d,
        [Comp.IconButtonXSmall.NarrowTrailingSpace] = 4d,
        [Comp.IconButtonXSmall.WideLeadingSpace] = 10d,
        [Comp.IconButtonXSmall.WideTrailingSpace] = 10d,
        [Comp.IconButtonXSmall.OutlinedOutlineWidth] = new Thickness(1),

        [Comp.IconButtonSmall.ContainerHeight] = 40d,
        [Comp.IconButtonSmall.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.IconButtonSmall.ContainerShapeSquare] = new TokenRef(Sys.Shape.Medium),
        [Comp.IconButtonSmall.PressedContainerShape] = new TokenRef(Sys.Shape.Small),
        [Comp.IconButtonSmall.IconSize] = 24d,
        [Comp.IconButtonSmall.DefaultLeadingSpace] = 8d,
        [Comp.IconButtonSmall.DefaultTrailingSpace] = 8d,
        [Comp.IconButtonSmall.NarrowLeadingSpace] = 4d,
        [Comp.IconButtonSmall.NarrowTrailingSpace] = 4d,
        [Comp.IconButtonSmall.WideLeadingSpace] = 14d,
        [Comp.IconButtonSmall.WideTrailingSpace] = 14d,
        [Comp.IconButtonSmall.OutlinedOutlineWidth] = new Thickness(1),

        [Comp.IconButtonMedium.ContainerHeight] = 56d,
        [Comp.IconButtonMedium.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.IconButtonMedium.ContainerShapeSquare] = new TokenRef(Sys.Shape.Large),
        [Comp.IconButtonMedium.PressedContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.IconButtonMedium.IconSize] = 24d,
        [Comp.IconButtonMedium.DefaultLeadingSpace] = 16d,
        [Comp.IconButtonMedium.DefaultTrailingSpace] = 16d,
        [Comp.IconButtonMedium.NarrowLeadingSpace] = 12d,
        [Comp.IconButtonMedium.NarrowTrailingSpace] = 12d,
        [Comp.IconButtonMedium.WideLeadingSpace] = 24d,
        [Comp.IconButtonMedium.WideTrailingSpace] = 24d,
        [Comp.IconButtonMedium.OutlinedOutlineWidth] = new Thickness(1),

        [Comp.IconButtonLarge.ContainerHeight] = 96d,
        [Comp.IconButtonLarge.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.IconButtonLarge.ContainerShapeSquare] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.IconButtonLarge.PressedContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.IconButtonLarge.IconSize] = 32d,
        [Comp.IconButtonLarge.DefaultLeadingSpace] = 32d,
        [Comp.IconButtonLarge.DefaultTrailingSpace] = 32d,
        [Comp.IconButtonLarge.NarrowLeadingSpace] = 16d,
        [Comp.IconButtonLarge.NarrowTrailingSpace] = 16d,
        [Comp.IconButtonLarge.WideLeadingSpace] = 48d,
        [Comp.IconButtonLarge.WideTrailingSpace] = 48d,
        [Comp.IconButtonLarge.OutlinedOutlineWidth] = new Thickness(2),

        [Comp.IconButtonXLarge.ContainerHeight] = 136d,
        [Comp.IconButtonXLarge.ContainerShapeRound] = new TokenRef(Sys.Shape.Full),
        [Comp.IconButtonXLarge.ContainerShapeSquare] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.IconButtonXLarge.PressedContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.IconButtonXLarge.IconSize] = 40d,
        [Comp.IconButtonXLarge.DefaultLeadingSpace] = 48d,
        [Comp.IconButtonXLarge.DefaultTrailingSpace] = 48d,
        [Comp.IconButtonXLarge.NarrowLeadingSpace] = 32d,
        [Comp.IconButtonXLarge.NarrowTrailingSpace] = 32d,
        [Comp.IconButtonXLarge.WideLeadingSpace] = 72d,
        [Comp.IconButtonXLarge.WideTrailingSpace] = 72d,
        [Comp.IconButtonXLarge.OutlinedOutlineWidth] = new Thickness(3),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the standard IconButton slots (upstream StandardIconButtonTokens 14_1_0).
/// The container is transparent at rest and when disabled (upstream composable, not a token).
/// Recorded deviation: the plain upstream composable defaults the content color to the ambient
/// LocalContentColor and only the vibrant variant uses the token table (OnSurfaceVariant); the
/// table value is taken here because the state-layer and disabled derivations need a concrete
/// color to hang off. Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class StandardIconButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.StandardIconButton.ContainerColor] = Colors.Transparent,
        [Comp.StandardIconButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.StandardIconButton.StateLayerColor] = new TokenRef(Comp.StandardIconButton.ContentColor),
        [Comp.StandardIconButton.HoverStateLayerColor] = new TokenRef(Comp.StandardIconButton.StateLayerColor),
        [Comp.StandardIconButton.PressedStateLayerColor] = new TokenRef(Comp.StandardIconButton.StateLayerColor),
        [Comp.StandardIconButton.FocusedStateLayerColor] = new TokenRef(Comp.StandardIconButton.StateLayerColor),
        [Comp.StandardIconButton.DisabledContainerColor] = Colors.Transparent,
        [Comp.StandardIconButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the filled IconButton slots (upstream FilledIconButtonTokens 14_1_0). The
/// disabled container is OnSurface at 0.10 — the icon button table's own opacity, not the
/// <c>md.sys.state.disabled-container-opacity</c> 0.12 the Button family uses. Immutable
/// baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class FilledIconButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.FilledIconButton.ContainerColor] = new TokenRef(Sys.Color.Primary),
        [Comp.FilledIconButton.ContentColor] = new TokenRef(Sys.Color.OnPrimary),
        [Comp.FilledIconButton.StateLayerColor] = new TokenRef(Comp.FilledIconButton.ContentColor),
        [Comp.FilledIconButton.HoverStateLayerColor] = new TokenRef(Comp.FilledIconButton.StateLayerColor),
        [Comp.FilledIconButton.PressedStateLayerColor] = new TokenRef(Comp.FilledIconButton.StateLayerColor),
        [Comp.FilledIconButton.FocusedStateLayerColor] = new TokenRef(Comp.FilledIconButton.StateLayerColor),
        [Comp.FilledIconButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.1d),
        [Comp.FilledIconButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the filled tonal IconButton slots (upstream FilledTonalIconButtonTokens
/// 14_1_0). Immutable baseline: <see cref="Changed"/> is a documented no-op.
/// </summary>
public class TonalIconButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.TonalIconButton.ContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.TonalIconButton.ContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.TonalIconButton.StateLayerColor] = new TokenRef(Comp.TonalIconButton.ContentColor),
        [Comp.TonalIconButton.HoverStateLayerColor] = new TokenRef(Comp.TonalIconButton.StateLayerColor),
        [Comp.TonalIconButton.PressedStateLayerColor] = new TokenRef(Comp.TonalIconButton.StateLayerColor),
        [Comp.TonalIconButton.FocusedStateLayerColor] = new TokenRef(Comp.TonalIconButton.StateLayerColor),
        [Comp.TonalIconButton.DisabledContainerColor] = new WithAlpha(Sys.Color.OnSurface, 0.1d),
        [Comp.TonalIconButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

/// <summary>
/// Baseline defaults for the outlined IconButton slots (upstream OutlinedIconButtonTokens 14_1_0).
/// Same LocalContentColor deviation as the standard style. The disabled outline is the outline
/// color at the disabled-content opacity (0.38): the raw token table lists plain OutlineVariant,
/// but both upstream border paths (plain and vibrant) derive it that way — the composable is the
/// source of truth, as with the Button family. Immutable baseline: <see cref="Changed"/> is a
/// documented no-op.
/// </summary>
public class OutlinedIconButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.OutlinedIconButton.ContainerColor] = Colors.Transparent,
        [Comp.OutlinedIconButton.ContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.OutlinedIconButton.StateLayerColor] = new TokenRef(Comp.OutlinedIconButton.ContentColor),
        [Comp.OutlinedIconButton.HoverStateLayerColor] = new TokenRef(Comp.OutlinedIconButton.StateLayerColor),
        [Comp.OutlinedIconButton.PressedStateLayerColor] = new TokenRef(Comp.OutlinedIconButton.StateLayerColor),
        [Comp.OutlinedIconButton.FocusedStateLayerColor] = new TokenRef(Comp.OutlinedIconButton.StateLayerColor),
        [Comp.OutlinedIconButton.DisabledContainerColor] = Colors.Transparent,
        [Comp.OutlinedIconButton.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, Sys.State.DisabledContentOpacity),

        [Comp.OutlinedIconButton.OutlineColor] = new TokenRef(Sys.Color.OutlineVariant),
        [Comp.OutlinedIconButton.DisabledOutlineColor] = new WithAlpha(Comp.OutlinedIconButton.OutlineColor, Sys.State.DisabledContentOpacity),
    }.ToFrozenDictionary();

    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) =>
        Values.TryGetValue(key, out value!);

    // Immutable baseline: the table never changes, so the event is a documented no-op.
    public event EventHandler? Changed { add { } remove { } }
}

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Interaction;
using Material3Expressive.Foundation.Tokens;
using MaterialSymbols;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

/// <summary>
/// An icon-only button (upstream IconButton): a <see cref="Button"/> whose content is a Material
/// Symbol, drawn by a <c>MaterialSymbol</c> template part at the size table's icon size. Themes
/// (Styles/IconButton.axaml) provide the four styles (standard — the implicit default, filled,
/// tonal, outlined) and the five size classes; all values come from the comp slots below.
/// </summary>
public class IconButton : Button
{
    /// <summary>An optional shared stream for observing input and emitting feedback interactions.</summary>
    public static readonly StyledProperty<MaterialInteractionSource?> InteractionSourceProperty =
        AvaloniaProperty.Register<IconButton, MaterialInteractionSource?>(nameof(InteractionSource));

    public MaterialInteractionSource? InteractionSource
    {
        get => GetValue(InteractionSourceProperty);
        set => SetValue(InteractionSourceProperty, value);
    }

    // collectIsPressedAsState is independent of the indication node. In particular its remembered
    // bool survives source replacement, while the new collector starts with an empty press list.
    internal static readonly StyledProperty<bool> SourceIsPressedProperty =
        AvaloniaProperty.Register<IconButton, bool>(nameof(SourceIsPressed));
    internal bool SourceIsPressed { get => GetValue(SourceIsPressedProperty); private set => SetValue(SourceIsPressedProperty, value); }
    private readonly MaterialInteractionSource _localSource = new();
    private readonly List<MaterialInteractionEvent.Press> _presses = [];
    private MaterialInteractionSource? _source;
    private bool _usesAnimatedShape;

    private void UpdateShapeCollection()
    {
        // The static shapeForInteraction branch has no remembered press collector. Keep the
        // source connection for indication, but recreate the shape collector at this boundary.
        var animated = MaterialButton.GetPressedCornerRadius(this) is not null
            || this is IconToggleButton { CheckedCornerRadius: not null };
        if (_usesAnimatedShape == animated) return;
        _usesAnimatedShape = animated;
        _presses.Clear();
        SourceIsPressed = false;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ConnectSource();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        MaterialInteraction.GetOrCreate(this).ConnectSource(null);
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = null;
        _presses.Clear();
        SourceIsPressed = false;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        // Reset before template bindings observe a newly enabled target and initialize its shape.
        if (change.Property == MaterialButton.PressedCornerRadiusProperty
            || change.Property == IconToggleButton.CheckedCornerRadiusProperty)
            UpdateShapeCollection();
        base.OnPropertyChanged(change);
        if (change.Property == InteractionSourceProperty) ConnectSource();
    }

    private void ConnectSource()
    {
        if (!this.IsAttachedToVisualTree()) return;
        var source = InteractionSource ?? _localSource;
        if (ReferenceEquals(source, _source)) return;
        var replacing = _source is not null;
        var cancelsNativePress = replacing && IsPressed;
        var controller = MaterialInteraction.GetOrCreate(this);
        controller.ConnectSource(null);
        if (_source is not null) _source.Interaction -= ReceiveInteraction;
        _source = source;
        _presses.Clear();
        source.Interaction += ReceiveInteraction;
        controller.ConnectSource(source);
        if (replacing) controller.ResetSourceIndication(preserveSurfaceMotion: true);
        // Clickable disposes its native Press while applying the new source in composition.
        // As with disabling, the shape observer commits that cancellation in the following frame.
        if (cancelsNativePress) controller.NotifyInteractionChanged(deferShape: true);
    }

    private void ReceiveInteraction(MaterialInteractionEvent interaction)
    {
        switch (interaction)
        {
            case MaterialInteractionEvent.Press press: _presses.Add(press); break;
            case MaterialInteractionEvent.Release release: _presses.Remove(release.Start); break;
            case MaterialInteractionEvent.CancelPress cancel: _presses.Remove(cancel.Start); break;
        }
        SourceIsPressed = _presses.Count != 0;
    }

    /// <summary>Defines the <see cref="WidthOption"/> property.</summary>
    public static readonly StyledProperty<IconButtonWidthOption> WidthOptionProperty =
        AvaloniaProperty.Register<IconButton, IconButtonWidthOption>(nameof(WidthOption), IconButtonWidthOption.Uniform);

    /// <summary>Selects the size table's narrow, uniform, or wide container. Explicit Width still takes precedence.</summary>
    public IconButtonWidthOption WidthOption
    {
        get => GetValue(WidthOptionProperty);
        set => SetValue(WidthOptionProperty, value);
    }

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

/// <summary>The container width choices in IconButtonDefaults.</summary>
public enum IconButtonWidthOption { Narrow, Uniform, Wide }

/// <summary>
/// Baseline defaults for the five IconButton size tables (upstream XSmall/Small/Medium/Large/
/// XLargeIconButtonTokens 14_1_0, except the Large table which still ships as v0_11_0), shared by
/// every icon button style. Shapes hop into the sys corner scale. The three width options are
/// consumed as space pairs (container width = space + icon + space). The older Large
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
        [Comp.IconButtonXSmall.SelectedContainerShapeRound] = new TokenRef(Sys.Shape.Medium),
        [Comp.IconButtonXSmall.SelectedContainerShapeSquare] = new TokenRef(Sys.Shape.Full),
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
        [Comp.IconButtonSmall.SelectedContainerShapeRound] = new TokenRef(Sys.Shape.Medium),
        [Comp.IconButtonSmall.SelectedContainerShapeSquare] = new TokenRef(Sys.Shape.Full),
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
        [Comp.IconButtonMedium.SelectedContainerShapeRound] = new TokenRef(Sys.Shape.Large),
        [Comp.IconButtonMedium.SelectedContainerShapeSquare] = new TokenRef(Sys.Shape.Full),
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
        [Comp.IconButtonLarge.SelectedContainerShapeRound] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.IconButtonLarge.SelectedContainerShapeSquare] = new TokenRef(Sys.Shape.Full),
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
        [Comp.IconButtonXLarge.SelectedContainerShapeRound] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.IconButtonXLarge.SelectedContainerShapeSquare] = new TokenRef(Sys.Shape.Full),
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
/// MaterialVibrantIconButtonTheme consumes these color slots; the ordinary standard theme uses
/// the ambient content color. Immutable baseline: <see cref="Changed"/> is a documented no-op.
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
/// MaterialOutlinedVibrantIconButtonTheme consumes these slots. The disabled outline is the outline
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

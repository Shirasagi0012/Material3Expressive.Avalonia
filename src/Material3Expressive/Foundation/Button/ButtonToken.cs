namespace Material3Expressive.Foundation;

/// <summary>
/// The button tokens reachable from XAML. Names follow androidx's token names rather than
/// Avalonia's property names, so that a token table and its generated code read the same.
/// </summary>
/// <remarks>
/// A few entries here are shapes of Avalonia values rather than tokens in their own right —
/// <see cref="ContainerCornerRadius" /> resolves a shape role against the size's container height,
/// and <see cref="ContentPadding" /> folds the leading, trailing and vertical spaces into one
/// <c>Thickness</c> — because a XAML setter wants the value the property actually takes.
/// </remarks>
public enum ButtonToken
{
    ContainerColor,
    LabelTextColor,
    IconColor,
    OutlineColor,
    StateLayerColor,
    DisabledContainerColor,
    DisabledLabelTextColor,
    DisabledIconColor,
    DisabledOutlineColor,

    ContainerCornerRadius,
    PressedContainerCornerRadius,
    ContentPadding,
    ContainerHeight,
    IconSize,
    IconLabelSpace,
    OutlineWidth,
    ContainerOutlineThickness,
    LabelTextStyle,

    HoverStateLayerOpacity,
    FocusStateLayerOpacity,
    PressedStateLayerOpacity
}

using System.Collections.Frozen;
using Avalonia;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

// Fixed FloatingActionButton.kt and Fab*/ExtendedFab*/FabMenuBaselineTokens.
// Medium shape follows the composable fallback. Raw icon spacing/size tokens stay raw;
// the composable's explicit 12/16dp and 36dp corrections are applied by its layout/defaults.
public sealed class FloatingActionButtonTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.FabBaseline.ContainerWidth] = 56d,
        [Comp.FabBaseline.ContainerHeight] = 56d,
        [Comp.FabBaseline.ContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.FabBaseline.IconSize] = 24d,
        [Comp.FabSmall.ContainerWidth] = 40d,
        [Comp.FabSmall.ContainerHeight] = 40d,
        [Comp.FabSmall.ContainerShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.FabSmall.IconSize] = 24d,
        [Comp.FabMedium.ContainerWidth] = 80d,
        [Comp.FabMedium.ContainerHeight] = 80d,
        [Comp.FabMedium.ContainerShape] = new TokenRef(Sys.Shape.LargeIncreased),
        [Comp.FabMedium.IconSize] = 28d,
        [Comp.FabLarge.ContainerWidth] = 96d,
        [Comp.FabLarge.ContainerHeight] = 96d,
        [Comp.FabLarge.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.FabLarge.IconSize] = 32d,
        [Comp.FabPrimaryContainer.ContainerColor] = new TokenRef(Sys.Color.PrimaryContainer),
        [Comp.FabPrimaryContainer.ContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.FabPrimaryContainer.PressedContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.FabPrimaryContainer.FocusedContainerElevation] = new TokenRef(Sys.Elevation.Level3),
        [Comp.FabPrimaryContainer.HoveredContainerElevation] = new TokenRef(Sys.Elevation.Level4),
        [Comp.ExtendedFabPrimary.ContainerHeight] = 56d,
        [Comp.ExtendedFabPrimary.ContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ExtendedFabPrimary.LeadingSpace] = 20d,
        [Comp.ExtendedFabPrimary.TrailingSpace] = 20d,
        [Comp.ExtendedFabPrimary.IconLabelSpace] = 12d,
        [Comp.ExtendedFabPrimary.LabelTextStyle] = new TokenRef(Sys.TypeScale.LabelLarge.Style),
        [Comp.ExtendedFabSmall.ContainerHeight] = 56d,
        [Comp.ExtendedFabSmall.ContainerShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ExtendedFabSmall.LeadingSpace] = 16d,
        [Comp.ExtendedFabSmall.TrailingSpace] = 16d,
        [Comp.ExtendedFabSmall.IconLabelSpace] = 8d,
        [Comp.ExtendedFabSmall.LabelTextStyle] = new TokenRef(Sys.TypeScale.TitleMedium.Style),
        [Comp.ExtendedFabMedium.ContainerHeight] = 80d,
        [Comp.ExtendedFabMedium.ContainerShape] = new TokenRef(Sys.Shape.LargeIncreased),
        [Comp.ExtendedFabMedium.LeadingSpace] = 26d,
        [Comp.ExtendedFabMedium.TrailingSpace] = 26d,
        [Comp.ExtendedFabMedium.IconLabelSpace] = 16d,
        [Comp.ExtendedFabMedium.LabelTextStyle] = new TokenRef(Sys.TypeScale.TitleLarge.Style),
        [Comp.ExtendedFabLarge.ContainerHeight] = 96d,
        [Comp.ExtendedFabLarge.ContainerShape] = new TokenRef(Sys.Shape.ExtraLarge),
        [Comp.ExtendedFabLarge.LeadingSpace] = 28d,
        [Comp.ExtendedFabLarge.TrailingSpace] = 28d,
        [Comp.ExtendedFabLarge.IconLabelSpace] = 20d,
        [Comp.ExtendedFabLarge.LabelTextStyle] = new TokenRef(Sys.TypeScale.HeadlineSmall.Style),
        [Comp.FabMenu.ItemHeight] = 56d,
        [Comp.FabMenu.ItemShape] = new TokenRef(Sys.Shape.Full),
        [Comp.FabMenu.ItemSpacing] = 4d,
        [Comp.FabMenu.ContentPadding] = new Thickness(24, 0),
        [Comp.FabMenu.IconLabelSpace] = 8d,
        [Comp.FabMenu.ButtonBottomPadding] = 16d,
        [Comp.FabMenu.ButtonBetweenSpace] = 8d,
        [Comp.FabMenu.HorizontalPadding] = 16d,
        [Comp.FabMenu.CloseButtonSize] = 56d,
        [Comp.FabMenu.CloseIconSize] = 20d,
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}

// ListItemDefaults.kt, ListTokens and ReorderListTokens at AndroidX f3ed195f0c9e.
// Copyright 2022-2025 The Android Open Source Project; Apache-2.0.
using System.Collections.Frozen;
using Avalonia.Styling;
using Material3Expressive.Foundation.Tokens;
using static Material3Expressive.Tokens.MaterialTokens;

namespace Material3Expressive.Components;

public sealed class ListItemTokenSource : ITokenSource
{
    private static readonly FrozenDictionary<MaterialTokenKey, object> Values = new Dictionary<MaterialTokenKey, object>
    {
        [Comp.ListItem.ContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.ListItem.ContentColor] = new TokenRef(Sys.Color.OnSurface),
        [Comp.ListItem.LeadingContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.ListItem.TrailingContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.ListItem.OverlineContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.ListItem.SupportingContentColor] = new TokenRef(Sys.Color.OnSurfaceVariant),
        [Comp.ListItem.DisabledContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.ListItem.DisabledContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.ListItem.DisabledLeadingContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.ListItem.DisabledTrailingContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.ListItem.DisabledOverlineContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.ListItem.DisabledSupportingContentColor] = new WithAlpha(Sys.Color.OnSurface, 0.38),
        [Comp.ListItem.SelectedContainerColor] = new TokenRef(Sys.Color.SecondaryContainer),
        [Comp.ListItem.SelectedContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ListItem.SelectedLeadingContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ListItem.SelectedTrailingContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ListItem.SelectedOverlineContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ListItem.SelectedSupportingContentColor] = new TokenRef(Sys.Color.OnSecondaryContainer),
        [Comp.ListItem.DraggedContainerColor] = new TokenRef(Sys.Color.TertiaryContainer),
        [Comp.ListItem.DraggedContentColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.ListItem.DraggedLeadingContentColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.ListItem.DraggedTrailingContentColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.ListItem.DraggedOverlineContentColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.ListItem.DraggedSupportingContentColor] = new TokenRef(Sys.Color.OnTertiaryContainer),
        [Comp.ListItem.SegmentedContainerColor] = new TokenRef(Sys.Color.Surface),
        [Comp.ListItem.Shape] = new TokenRef(Sys.Shape.ExtraSmall),
        [Comp.ListItem.SelectedShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ListItem.PressedShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ListItem.FocusedShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ListItem.HoveredShape] = new TokenRef(Sys.Shape.Medium),
        [Comp.ListItem.DraggedShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ListItem.GroupShape] = new TokenRef(Sys.Shape.Large),
        [Comp.ListItem.StartPadding] = 16d,
        [Comp.ListItem.EndPadding] = 16d,
        [Comp.ListItem.TopPadding] = 10d,
        [Comp.ListItem.BottomPadding] = 10d,
        [Comp.ListItem.InternalSpacing] = 12d,
        [Comp.ListItem.OneLineHeight] = 56d,
        [Comp.ListItem.TwoLineHeight] = 72d,
        [Comp.ListItem.ThreeLineHeight] = 88d,
        [Comp.ListItem.SegmentedGap] = 2d,
        [Comp.ListItem.Elevation] = new TokenRef(Sys.Elevation.Level0),
        [Comp.ListItem.DraggedElevation] = new TokenRef(Sys.Elevation.Level4),
        [Comp.ListItem.ContentTextStyle] = new TokenRef(Sys.TypeScale.BodyLarge.Style),
        [Comp.ListItem.LeadingTextStyle] = new TokenRef(Sys.TypeScale.TitleMedium.Style),
        [Comp.ListItem.TrailingTextStyle] = new TokenRef(Sys.TypeScale.LabelSmall.Style),
        [Comp.ListItem.OverlineTextStyle] = new TokenRef(Sys.TypeScale.LabelSmall.Style),
        [Comp.ListItem.SupportingTextStyle] = new TokenRef(Sys.TypeScale.BodyMedium.Style),
        [Comp.ListItem.ColorAnimation] = new TokenRef(Sys.Motion.Spring.EffectsDefault),
        [Comp.ListItem.ShapeAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
        [Comp.ListItem.ElevationAnimation] = new TokenRef(Sys.Motion.Spring.SpatialFast),
    }.ToFrozenDictionary();
    public bool TryGetToken(MaterialTokenKey key, ThemeVariant? theme, out object value) => Values.TryGetValue(key, out value!);
    public event EventHandler? Changed { add { } remove { } }
}


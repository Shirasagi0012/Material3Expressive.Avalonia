using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;
using MaterialColorUtilities.Avalonia.Tokens;
using MaterialDesignTokens.Avalonia;

namespace Material3Expressive.Controls;

/// <summary>Stable Material Value endpoints used by bottom-sheet containers.</summary>
public static class BottomSheetValueKeys
{
    public static MaterialValueKey<BottomSheetGeometryProfile> ModalGeometry { get; } =
        Geometry(
            "Value.BottomSheet.Modal.Geometry",
            SheetBottomTokensKeys.DockedModalContainerElevation,
            isModal: true);

    public static MaterialValueKey<BottomSheetGeometryProfile> StandardGeometry { get; } =
        Geometry(
            "Value.BottomSheet.Standard.Geometry",
            SheetBottomTokensKeys.DockedStandardContainerElevation,
            isModal: false);

    public static MaterialValueKey<BottomSheetVisualProfile> Visuals { get; } =
        new(
            "Value.BottomSheet.Visuals",
            new RawRecipePlan<BottomSheetVisualProfile>(new BottomSheetVisualRecipe()));

    private static MaterialValueKey<BottomSheetGeometryProfile> Geometry(
        string path,
        MaterialToken<double> elevation,
        bool isModal) =>
        new(
            path,
            new RawRecipePlan<BottomSheetGeometryProfile>(
                new BottomSheetGeometryRecipe(elevation, isModal)));
}

public sealed record BottomSheetGeometryProfile(
    double ContainerElevation,
    ShapeRole ContainerShape,
    double DragHandleWidth,
    double DragHandleHeight,
    double DragHandleSpace,
    Thickness ContentPadding,
    double MaxWidth,
    double ScrimOpacity,
    bool IsModal);

public sealed record BottomSheetVisualProfile(
    IImmutableBrush ContainerColor,
    IImmutableBrush ScrimColor,
    IImmutableBrush DragHandleColor,
    Color ShadowColor);

internal sealed class BottomSheetGeometryRecipe : IRawValueRecipe<BottomSheetGeometryProfile>
{
    private readonly MaterialToken<double> _elevation;
    private readonly bool _isModal;
    private readonly IReadOnlyList<MaterialToken> _dependencies;

    internal BottomSheetGeometryRecipe(MaterialToken<double> elevation, bool isModal)
    {
        _elevation = elevation;
        _isModal = isModal;
        var dependencies = new List<MaterialToken>
        {
            SheetBottomTokensKeys.DockedContainerShape,
            SheetBottomTokensKeys.DockedDragHandleWidth,
            SheetBottomTokensKeys.DockedDragHandleHeight,
            elevation,
        };
        if (isModal)
            dependencies.Add(ScrimTokensKeys.ContainerOpacity);
        _dependencies = Array.AsReadOnly(dependencies.ToArray());
    }

    public IReadOnlyList<MaterialToken> Dependencies => _dependencies;

    public BottomSheetGeometryProfile Evaluate(MaterialValueContext context) => new(
        context.Get(_elevation),
        context.Get(SheetBottomTokensKeys.DockedContainerShape),
        context.Get(SheetBottomTokensKeys.DockedDragHandleWidth),
        context.Get(SheetBottomTokensKeys.DockedDragHandleHeight),
        22,
        new Thickness(0, 22, 0, 0),
        640,
        _isModal ? context.Get(ScrimTokensKeys.ContainerOpacity) : 0,
        _isModal);
}

internal sealed class BottomSheetVisualRecipe : IRawValueRecipe<BottomSheetVisualProfile>
{
    private static readonly IReadOnlyList<MaterialToken> Tokens =
        Array.AsReadOnly<MaterialToken>(
        [
            SheetBottomTokensKeys.DockedContainerColor,
            ScrimTokensKeys.ContainerColor,
            SheetBottomTokensKeys.DockedDragHandleColor,
            SystemColorTokens.Shadow,
        ]);

    public IReadOnlyList<MaterialToken> Dependencies => Tokens;

    public BottomSheetVisualProfile Evaluate(MaterialValueContext context) => new(
        Brush(context.Get(SheetBottomTokensKeys.DockedContainerColor)),
        Brush(context.Get(ScrimTokensKeys.ContainerColor)),
        Brush(context.Get(SheetBottomTokensKeys.DockedDragHandleColor)),
        context.Get(SystemColorTokens.Shadow));

    private static IImmutableBrush Brush(Color color) => new ImmutableSolidColorBrush(color);
}

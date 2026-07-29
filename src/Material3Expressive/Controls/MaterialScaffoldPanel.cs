// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/Scaffold.kt:134-305.
// This template panel mirrors ScaffoldLayout: it measures overlays against loose constraints,
// updates caller-applied content padding before measuring the body, and uses integer physical-pixel
// arithmetic for FAB/snackbar placement. Alpha22 has matching geometry and draw order but lacks the
// alpha25 implementation's explicit z-index constants; the template records those z-indices.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;

namespace Material3Expressive.Controls;

internal sealed class MaterialScaffoldPanel : Panel
{
    private const double FabSpacing = 16;

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Owner();
        var slots = Slots();
        var insets = owner.ResolvedContentWindowInsets;
        var loose = Loose(availableSize);
        var overlayConstraint = new Size(
            Subtract(loose.Width, insets.Left + insets.Right),
            Subtract(loose.Height, insets.Bottom));

        slots.TopBar.Measure(loose);
        slots.Snackbar.Measure(overlayConstraint);
        slots.Fab.Measure(overlayConstraint);
        slots.BottomBar.Measure(loose);

        owner.SetContentPadding(
            new Thickness(
                owner.FlowDirection == FlowDirection.RightToLeft ? insets.Right : insets.Left,
                owner.TopBar is null ? insets.Top : slots.TopBar.DesiredSize.Height,
                owner.FlowDirection == FlowDirection.RightToLeft ? insets.Left : insets.Right,
                owner.BottomBar is null ? insets.Bottom : slots.BottomBar.DesiredSize.Height));

        slots.Content.Measure(loose);

        var desiredWidth = IsFinite(availableSize.Width)
            ? Math.Max(0, availableSize.Width)
            : Max(
                slots.Content.DesiredSize.Width,
                slots.TopBar.DesiredSize.Width,
                slots.BottomBar.DesiredSize.Width,
                insets.Left + slots.Snackbar.DesiredSize.Width + insets.Right,
                insets.Left + slots.Fab.DesiredSize.Width + insets.Right);
        var desiredHeight = IsFinite(availableSize.Height)
            ? Math.Max(0, availableSize.Height)
            : Max(
                slots.Content.DesiredSize.Height,
                slots.TopBar.DesiredSize.Height,
                slots.BottomBar.DesiredSize.Height,
                slots.Snackbar.DesiredSize.Height + slots.Fab.DesiredSize.Height + FabSpacing + insets.Bottom,
                slots.Fab.DesiredSize.Height + FabSpacing + insets.Bottom);

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = Owner();
        var slots = Slots();
        var insets = owner.ResolvedContentWindowInsets;
        var scale = LayoutHelper.GetLayoutScale(this);
        if (!(scale > 0) || !double.IsFinite(scale))
            scale = 1;

        var layoutWidth = ToPixels(finalSize.Width, scale);
        var layoutHeight = ToPixels(finalSize.Height, scale);
        var leftInset = ToPixels(insets.Left, scale);
        var rightInset = ToPixels(insets.Right, scale);
        var bottomInset = ToPixels(insets.Bottom, scale);
        var topHeight = owner.TopBar is null ? 0 : ToPixels(slots.TopBar.DesiredSize.Height, scale);
        var bottomHeight = owner.BottomBar is null ? 0 : ToPixels(slots.BottomBar.DesiredSize.Height, scale);
        var snackbarWidth = ToPixels(slots.Snackbar.DesiredSize.Width, scale);
        var snackbarHeight = ToPixels(slots.Snackbar.DesiredSize.Height, scale);
        var fabWidth = ToPixels(slots.Fab.DesiredSize.Width, scale);
        var fabHeight = ToPixels(slots.Fab.DesiredSize.Height, scale);
        var hasFab = owner.FloatingActionButton is not null && fabWidth != 0 && fabHeight != 0;
        var fabSpacing = ToPixels(FabSpacing, scale);

        var fabPhysicalLeft = owner.FloatingActionButtonPosition switch
        {
            ScaffoldFabPosition.Start when owner.FlowDirection == FlowDirection.LeftToRight =>
                fabSpacing + leftInset,
            ScaffoldFabPosition.Start =>
                layoutWidth - fabSpacing - fabWidth - rightInset,
            ScaffoldFabPosition.Center =>
                (layoutWidth - fabWidth + leftInset - rightInset) / 2,
            ScaffoldFabPosition.End or ScaffoldFabPosition.EndOverlay
                when owner.FlowDirection == FlowDirection.RightToLeft =>
                fabSpacing + leftInset,
            _ => layoutWidth - fabSpacing - fabWidth - rightInset
        };
        var fabLeft = LogicalLeft(
            fabPhysicalLeft,
            fabWidth,
            layoutWidth,
            owner.FlowDirection);

        var fabOffsetFromBottom = !hasFab
            ? 0
            : owner.BottomBar is null || owner.FloatingActionButtonPosition == ScaffoldFabPosition.EndOverlay
                ? fabHeight + fabSpacing + bottomInset
                : bottomHeight + fabHeight + fabSpacing;
        var snackbarOffsetFromBottom = snackbarHeight == 0
            ? 0
            : snackbarHeight + (hasFab
                ? fabOffsetFromBottom
                : owner.BottomBar is not null
                    ? bottomHeight
                    : bottomInset);

        Arrange(slots.Content, 0, 0, layoutWidth, layoutHeight, scale);
        Arrange(slots.TopBar, 0, 0, layoutWidth, topHeight, scale);
        var snackbarPhysicalLeft =
            (layoutWidth - snackbarWidth + leftInset - rightInset) / 2;
        Arrange(
            slots.Snackbar,
            LogicalLeft(
                snackbarPhysicalLeft,
                snackbarWidth,
                layoutWidth,
                owner.FlowDirection),
            layoutHeight - snackbarOffsetFromBottom,
            snackbarWidth,
            snackbarHeight,
            scale);
        Arrange(
            slots.Fab,
            fabLeft,
            layoutHeight - fabOffsetFromBottom,
            fabWidth,
            fabHeight,
            scale);
        Arrange(slots.BottomBar, 0, layoutHeight - bottomHeight, layoutWidth, bottomHeight, scale);

        return finalSize;
    }

    private MaterialScaffold Owner() =>
        TemplatedParent as MaterialScaffold
        ?? throw new InvalidOperationException(
            $"{nameof(MaterialScaffoldPanel)} must be used by a {nameof(MaterialScaffold)} template.");

    private ScaffoldSlots Slots()
    {
        ContentPresenter? top = null;
        ContentPresenter? content = null;
        ContentPresenter? snackbar = null;
        ContentPresenter? fab = null;
        ContentPresenter? bottom = null;

        foreach (var presenter in Children.OfType<ContentPresenter>())
        {
            switch (presenter.Name)
            {
                case "PART_TopBar":
                    Assign(ref top, presenter);
                    break;
                case "PART_Content":
                    Assign(ref content, presenter);
                    break;
                case "PART_SnackbarHost":
                    Assign(ref snackbar, presenter);
                    break;
                case "PART_FloatingActionButton":
                    Assign(ref fab, presenter);
                    break;
                case "PART_BottomBar":
                    Assign(ref bottom, presenter);
                    break;
            }
        }

        return new ScaffoldSlots(
            top ?? Missing("PART_TopBar"),
            content ?? Missing("PART_Content"),
            snackbar ?? Missing("PART_SnackbarHost"),
            fab ?? Missing("PART_FloatingActionButton"),
            bottom ?? Missing("PART_BottomBar"));
    }

    private static void Assign(ref ContentPresenter? target, ContentPresenter presenter)
    {
        if (target is not null)
            throw new InvalidOperationException($"Duplicate scaffold presenter '{presenter.Name}'.");
        target = presenter;
    }

    private static ContentPresenter Missing(string name) =>
        throw new InvalidOperationException($"MaterialScaffold template is missing '{name}'.");

    private static void Arrange(
        Control control,
        int x,
        int y,
        int width,
        int height,
        double scale) =>
        control.Arrange(
            new Rect(
                x / scale,
                y / scale,
                Math.Max(0, width) / scale,
                Math.Max(0, height) / scale));

    private static int ToPixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(Math.Max(0, value) * scale + 0.5));

    private static int LogicalLeft(
        int physicalLeft,
        int width,
        int layoutWidth,
        FlowDirection flowDirection) =>
        flowDirection == FlowDirection.RightToLeft
            ? layoutWidth - physicalLeft - width
            : physicalLeft;

    private static Size Loose(Size available) =>
        new(
            IsFinite(available.Width) ? Math.Max(0, available.Width) : double.PositiveInfinity,
            IsFinite(available.Height) ? Math.Max(0, available.Height) : double.PositiveInfinity);

    private static double Subtract(double value, double amount) =>
        IsFinite(value) ? Math.Max(0, value - amount) : value;

    private static bool IsFinite(double value) => double.IsFinite(value);

    private static double Max(params double[] values) => values.Max();

    private readonly record struct ScaffoldSlots(
        ContentPresenter TopBar,
        ContentPresenter Content,
        ContentPresenter Snackbar,
        ContentPresenter Fab,
        ContentPresenter BottomBar);
}

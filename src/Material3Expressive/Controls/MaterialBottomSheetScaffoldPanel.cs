// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// BottomSheetScaffold.kt:453-620. The panel keeps body content full-size, applies the caller-visible
// bottom peek padding, and places the standard sheet above the body with physical-pixel rounding.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;

namespace Material3Expressive.Controls;

internal sealed class MaterialBottomSheetScaffoldPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Owner();
        var slots = Slots();
        var loose = new Size(
            double.IsFinite(availableSize.Width) ? Math.Max(0, availableSize.Width) : double.PositiveInfinity,
            double.IsFinite(availableSize.Height) ? Math.Max(0, availableSize.Height) : double.PositiveInfinity);

        slots.Top.Measure(loose);
        slots.Sheet.Measure(new Size(
            Math.Min(loose.Width, owner.SheetMaxWidth),
            loose.Height));
        slots.Snackbar.Measure(loose);
        owner.SetContentPadding(new Thickness(0, 0, 0, owner.SheetPeekHeight));
        slots.Content.Measure(loose);

        var width = double.IsFinite(availableSize.Width)
            ? availableSize.Width
            : Math.Max(slots.Content.DesiredSize.Width, slots.Top.DesiredSize.Width);
        var height = double.IsFinite(availableSize.Height)
            ? availableSize.Height
            : Math.Max(slots.Content.DesiredSize.Height, slots.Sheet.DesiredSize.Height);
        return new Size(Math.Max(0, width), Math.Max(0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = Owner();
        var slots = Slots();
        var scale = LayoutHelper.GetLayoutScale(this);
        if (!(scale > 0) || !double.IsFinite(scale))
            scale = 1;

        var width = Pixels(finalSize.Width, scale);
        var height = Pixels(finalSize.Height, scale);
        var sheetWidth = Math.Min(width, Pixels(owner.SheetMaxWidth, scale));
        var sheetHeight = Pixels(slots.Sheet.DesiredSize.Height, scale);
        var peek = Math.Min(sheetHeight, Pixels(owner.SheetPeekHeight, scale));
        var fraction = slots.Sheet is MaterialBottomSheetScaffoldSheet sheetControl
            ? sheetControl.SheetFraction
            : owner.EffectiveState.CurrentValue == BottomSheetValue.Expanded ? 0 : 1;
        var sheetTop = fraction >= 2
            ? height
            : height - (int)Math.Round(
                sheetHeight - ((sheetHeight - peek) * fraction),
                MidpointRounding.AwayFromZero);
        var sheetLeft = (width - sheetWidth) / 2;

        slots.Content.Arrange(new Rect(0, 0, finalSize.Width, finalSize.Height));
        slots.Top.Arrange(new Rect(0, 0, finalSize.Width, slots.Top.DesiredSize.Height));
        slots.Snackbar.Arrange(new Rect(
            (finalSize.Width - slots.Snackbar.DesiredSize.Width) / 2,
            Math.Max(0, sheetTop / scale - slots.Snackbar.DesiredSize.Height - 16),
            slots.Snackbar.DesiredSize.Width,
            slots.Snackbar.DesiredSize.Height));
        slots.Sheet.Arrange(new Rect(
            sheetLeft / scale,
            sheetTop / scale,
            sheetWidth / scale,
            sheetHeight / scale));
        return finalSize;
    }

    private MaterialBottomSheetScaffold Owner() =>
        TemplatedParent as MaterialBottomSheetScaffold
        ?? throw new InvalidOperationException(
            $"{nameof(MaterialBottomSheetScaffoldPanel)} must be used by a " +
            $"{nameof(MaterialBottomSheetScaffold)} template.");

    private SheetSlots Slots()
    {
        ContentPresenter? top = null;
        ContentPresenter? content = null;
        ContentPresenter? snackbar = null;
        Control? sheet = null;
        foreach (var presenter in Children.OfType<ContentPresenter>())
        {
            switch (presenter.Name)
            {
                case "PART_TopBar": top = Assign(top, presenter); break;
                case "PART_Content": content = Assign(content, presenter); break;
                case "PART_SnackbarHost": snackbar = Assign(snackbar, presenter); break;
            }
        }
        sheet = Children.OfType<MaterialBottomSheetScaffoldSheet>().SingleOrDefault()
            ?? throw new InvalidOperationException("MaterialBottomSheetScaffold template is missing 'PART_Sheet'.");

        return new SheetSlots(
            top ?? Missing("PART_TopBar"),
            content ?? Missing("PART_Content"),
            snackbar ?? Missing("PART_SnackbarHost"),
            sheet ?? Missing("PART_Sheet"));
    }

    private static ContentPresenter Assign(ContentPresenter? existing, ContentPresenter value)
    {
        if (existing is not null)
            throw new InvalidOperationException($"Duplicate scaffold presenter '{value.Name}'.");
        return value;
    }

    private static ContentPresenter Missing(string name) =>
        throw new InvalidOperationException($"MaterialBottomSheetScaffold template is missing '{name}'.");

    private static int Pixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(Math.Max(0, value) * scale + 0.5));

    private readonly record struct SheetSlots(
        ContentPresenter Top,
        ContentPresenter Content,
        ContentPresenter Snackbar,
        Control Sheet);
}

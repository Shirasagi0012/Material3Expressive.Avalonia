// Upstream baseline: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt,
// ListItemDefaults.kt, and tokens/ListTokens.kt (VERSION 29.0.0). This panel follows the alpha25 interactive-list
// layout tokens: 16dp start/end, 10dp top/bottom, and a conditional 12dp gap beside a leading
// or trailing slot. Its 56/72/88dp line-height selection also follows the alpha25 expressive
// height fix. The executable Compose reference is Material3 1.5.0-alpha22, which cannot execute
// that alpha25 behavior; deterministic Avalonia layout tests are the parity evidence for it.

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace Material3Expressive.Controls;

/// <summary>Identifies one of the five direct child slots in a <see cref="MaterialListItemPanel" />.</summary>
public enum MaterialListItemSlot
{
    /// <summary>The optional content at the logical start of the list item.</summary>
    Leading,

    /// <summary>The optional content at the logical end of the list item.</summary>
    Trailing,

    /// <summary>The optional label placed above the headline.</summary>
    Overline,

    /// <summary>The primary list-item label.</summary>
    Headline,

    /// <summary>The optional label placed below the headline.</summary>
    Supporting
}

/// <summary>
/// Lays out the five Material list-item slots using the alpha25 expressive list-item metrics.
/// </summary>
/// <remarks>
/// <para>
/// Add at most one direct child for each <see cref="MaterialListItemSlot" /> and set its
/// <see cref="SlotProperty" />. An unassigned child is a <see cref="MaterialListItemSlot.Headline" />
/// so a simple one-line item remains convenient to declare. <see cref="ContentPadding" /> defaults
/// to the alpha25 16dp horizontal and 10dp vertical insets and is intended for a template binding
/// from the owning <c>MaterialListItem.Padding</c> property.
/// </para>
/// <para>
/// Leading and trailing slots receive the 12dp <c>ItemBetweenSpace</c> only when a visible main
/// text slot exists. Overline, headline, and supporting slots are vertically contiguous, as in
/// Compose. A visible supporting slot with more than one measured <see cref="TextBlock.TextLayout" />
/// line, or an item with both overline and supporting content, is a three-line item. Arbitrary
/// custom supporting content has no public Avalonia baseline contract, so it falls back to the
/// same 30dp intrinsic heuristic used by upstream. The source default alignment centres layouts below
/// its 60dp inner-height breakpoint; every default three-line item exceeds that breakpoint and
/// begins at the 10dp top inset.
/// </para>
/// </remarks>
public sealed class MaterialListItemPanel : Panel
{
    /// <summary>Identifies the role played by a direct child of the panel.</summary>
    public static readonly AttachedProperty<MaterialListItemSlot> SlotProperty =
        AvaloniaProperty.RegisterAttached<MaterialListItemPanel, Control, MaterialListItemSlot>(
            "Slot", MaterialListItemSlot.Headline);

    /// <summary>
    /// Defines the outer content insets. The default is the alpha25
    /// <c>ListItemDefaults.ContentPadding</c> value of 16dp horizontally and 10dp vertically.
    /// </summary>
    public static readonly StyledProperty<Thickness> ContentPaddingProperty =
        AvaloniaProperty.Register<MaterialListItemPanel, Thickness>(nameof(ContentPadding), new Thickness(16, 10));

    private const double SlotSpacing = 12;
    private const double SupportingMultilineThreshold = 30;
    private const double OneLineMinimumHeight = 56;
    private const double TwoLineMinimumHeight = 72;
    private const double ThreeLineMinimumHeight = 88;
    private const double TopAlignmentInnerHeightBreakpoint = 60;

    static MaterialListItemPanel()
    {
        // Slot assignment changes which child consumes a constrained lane and therefore affects
        // both the parent panel's desired size and its child rectangles.
        AffectsParentMeasure<MaterialListItemPanel>(SlotProperty);
        AffectsParentArrange<MaterialListItemPanel>(SlotProperty);
        AffectsMeasure<MaterialListItemPanel>(ContentPaddingProperty);
    }

    /// <inheritdoc cref="ContentPaddingProperty" />
    public Thickness ContentPadding
    {
        get => GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    /// <summary>Gets the list-item slot assigned to <paramref name="control" />.</summary>
    public static MaterialListItemSlot GetSlot(Control control)
    {
        ArgumentNullException.ThrowIfNull(control);
        return control.GetValue(SlotProperty);
    }

    /// <summary>Assigns <paramref name="control" /> to a list-item slot.</summary>
    public static void SetSlot(Control control, MaterialListItemSlot value)
    {
        ArgumentNullException.ThrowIfNull(control);
        control.SetValue(SlotProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var slots = GetVisibleSlots();
        var mainSlotPresent = slots.HasMainContent;
        var padding = GetLogicalPadding();
        var availableContentWidth = Subtract(availableSize.Width, padding.Start + padding.End);
        var availableContentHeight = Subtract(availableSize.Height, padding.Top + padding.Bottom);

        var leadingSize = MeasureSlot(slots.Leading, availableContentWidth, availableContentHeight);
        var leadingGap = slots.Leading is not null && mainSlotPresent ? SlotSpacing : 0;
        var widthAfterLeading = Subtract(availableContentWidth, leadingSize.Width + leadingGap);

        // Compose measures leading before trailing. Retaining that ordering matters when a caller
        // supplies oversized custom slot content: the main lane gets only the remaining width.
        var trailingSize = MeasureSlot(slots.Trailing, widthAfterLeading, availableContentHeight);
        var trailingGap = slots.Trailing is not null && mainSlotPresent ? SlotSpacing : 0;
        var mainAvailableWidth = Subtract(widthAfterLeading, trailingSize.Width + trailingGap);

        var overlineSize = MeasureSlot(slots.Overline, mainAvailableWidth, availableContentHeight);
        var headlineSize = MeasureSlot(
            slots.Headline,
            mainAvailableWidth,
            Subtract(availableContentHeight, overlineSize.Height));
        var supportingSize = MeasureSlot(
            slots.Supporting,
            mainAvailableWidth,
            Subtract(availableContentHeight, overlineSize.Height + headlineSize.Height));

        var lineCount = GetLineCount(slots, supportingSize.Height);
        var mainWidth = Math.Max(overlineSize.Width, Math.Max(headlineSize.Width, supportingSize.Width));
        var mainHeight = overlineSize.Height + headlineSize.Height + supportingSize.Height;
        var desiredWidth = IsFinite(availableSize.Width)
            ? Math.Max(0, availableSize.Width)
            : padding.Start + padding.End + leadingSize.Width + leadingGap + mainWidth + trailingGap + trailingSize.Width;
        var desiredHeight = Math.Max(
            GetMinimumHeight(lineCount),
            padding.Top + padding.Bottom + Math.Max(mainHeight, Math.Max(leadingSize.Height, trailingSize.Height)));

        return new Size(desiredWidth, ConstrainToAvailable(desiredHeight, availableSize.Height));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        var slots = GetVisibleSlots();
        var mainSlotPresent = slots.HasMainContent;
        var padding = GetLogicalPadding();
        var availableContentWidth = Math.Max(0, finalSize.Width - padding.Start - padding.End);
        var availableContentHeight = Math.Max(0, finalSize.Height - padding.Top - padding.Bottom);

        var leadingSize = Constrain(slots.Leading?.DesiredSize ?? default, availableContentWidth, availableContentHeight);
        var leadingGap = slots.Leading is not null && mainSlotPresent ? SlotSpacing : 0;
        var widthAfterLeading = Math.Max(0, availableContentWidth - leadingSize.Width - leadingGap);
        var trailingSize = Constrain(slots.Trailing?.DesiredSize ?? default, widthAfterLeading, availableContentHeight);
        var trailingGap = slots.Trailing is not null && mainSlotPresent ? SlotSpacing : 0;
        var mainWidth = Math.Max(0, widthAfterLeading - trailingSize.Width - trailingGap);

        var overlineSize = slots.Overline?.DesiredSize ?? default;
        var headlineSize = slots.Headline?.DesiredSize ?? default;
        var supportingSize = slots.Supporting?.DesiredSize ?? default;
        var mainHeight = overlineSize.Height + headlineSize.Height + supportingSize.Height;
        // ListItemDefaults.verticalAlignment() centres short layouts and switches to top
        // alignment at InteractiveListVerticalAlignmentBreakpoint. The Panel owns the outer
        // padding, so compare the remaining inner height here.
        var isTopAligned = availableContentHeight >= TopAlignmentInnerHeightBreakpoint;
        var mainTop = padding.Top + (isTopAligned ? 0 : Centre(mainHeight, availableContentHeight));
        var leadingTop = padding.Top + (isTopAligned ? 0 : Centre(leadingSize.Height, availableContentHeight));
        var trailingTop = padding.Top + (isTopAligned ? 0 : Centre(trailingSize.Height, availableContentHeight));

        if (FlowDirection == Avalonia.Media.FlowDirection.RightToLeft)
        {
            var leadingX = finalSize.Width - padding.Start - leadingSize.Width;
            var trailingX = padding.End;
            var mainX = padding.End + trailingSize.Width + trailingGap;

            ArrangeSlot(slots.Leading, leadingX, leadingTop, leadingSize);
            ArrangeSlot(slots.Trailing, trailingX, trailingTop, trailingSize);
            ArrangeMainSlots(slots, mainX, mainTop, mainWidth, overlineSize, headlineSize, supportingSize);
        }
        else
        {
            var leadingX = padding.Start;
            var trailingX = finalSize.Width - padding.End - trailingSize.Width;
            var mainX = padding.Start + leadingSize.Width + leadingGap;

            ArrangeSlot(slots.Leading, leadingX, leadingTop, leadingSize);
            ArrangeSlot(slots.Trailing, trailingX, trailingTop, trailingSize);
            ArrangeMainSlots(slots, mainX, mainTop, mainWidth, overlineSize, headlineSize, supportingSize);
        }

        return finalSize;
    }

    private Slots GetVisibleSlots()
    {
        Control? leading = null;
        Control? trailing = null;
        Control? overline = null;
        Control? headline = null;
        Control? supporting = null;

        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            switch (GetSlot(child))
            {
                case MaterialListItemSlot.Leading:
                    AssignSlot(ref leading, child, MaterialListItemSlot.Leading);
                    break;
                case MaterialListItemSlot.Trailing:
                    AssignSlot(ref trailing, child, MaterialListItemSlot.Trailing);
                    break;
                case MaterialListItemSlot.Overline:
                    AssignSlot(ref overline, child, MaterialListItemSlot.Overline);
                    break;
                case MaterialListItemSlot.Headline:
                    AssignSlot(ref headline, child, MaterialListItemSlot.Headline);
                    break;
                case MaterialListItemSlot.Supporting:
                    AssignSlot(ref supporting, child, MaterialListItemSlot.Supporting);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(MaterialListItemSlot)} value.");
            }
        }

        return new Slots(leading, trailing, overline, headline, supporting);
    }

    private static void AssignSlot(ref Control? slot, Control child, MaterialListItemSlot slotName)
    {
        if (slot is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(MaterialListItemPanel)} accepts at most one visible child in the {slotName} slot.");
        }

        slot = child;
    }

    private static Size MeasureSlot(Control? control, double width, double height)
    {
        if (control is null)
            return default;

        control.Measure(new Size(width, height));
        return control.DesiredSize;
    }

    private static int GetLineCount(Slots slots, double supportingHeight)
    {
        if ((slots.Overline is not null && slots.Supporting is not null) ||
            IsSupportingMultiline(slots.Supporting, supportingHeight))
        {
            return 3;
        }

        return slots.Overline is not null || slots.Supporting is not null ? 2 : 1;
    }

    private static bool IsSupportingMultiline(Control? supporting, double supportingHeight)
    {
        if (supporting is null)
            return false;

        // InteractiveListItemMeasurePolicy queries FirstBaseline and LastBaseline after measure.
        // A templated ContentPresenter carrying ordinary text contains exactly one TextBlock, whose
        // measured TextLayout is the direct Avalonia equivalent. For arbitrary custom content there
        // is no public common baseline API; retain AndroidX's intrinsic 30sp-height fallback rather
        // than guessing from unrelated descendant controls.
        var textBlocks = supporting
            .GetVisualDescendants()
            .Prepend(supporting)
            .OfType<TextBlock>()
            .Take(2)
            .ToArray();

        if (textBlocks.Length == 1)
            return textBlocks[0].TextLayout.TextLines.Count > 1;

        return supportingHeight > SupportingMultilineThreshold;
    }

    private static double GetMinimumHeight(int lineCount) => lineCount switch
    {
        1 => OneLineMinimumHeight,
        2 => TwoLineMinimumHeight,
        _ => ThreeLineMinimumHeight
    };

    private void ArrangeMainSlots(
        Slots slots,
        double x,
        double y,
        double laneWidth,
        Size overlineSize,
        Size headlineSize,
        Size supportingSize)
    {
        ArrangeMainSlot(slots.Overline, x, y, laneWidth, overlineSize);
        y += overlineSize.Height;
        ArrangeMainSlot(slots.Headline, x, y, laneWidth, headlineSize);
        y += headlineSize.Height;
        ArrangeMainSlot(slots.Supporting, x, y, laneWidth, supportingSize);
    }

    private void ArrangeMainSlot(Control? control, double laneX, double y, double laneWidth, Size desired)
    {
        if (control is null)
            return;

        // Compose measures each main slot against the available lane but places its resulting
        // Placeable at natural width with placeRelative. Stretching it to the full lane changes
        // both TextBlock geometry and RTL start alignment. Retain the measured width and mirror
        // that start-edge placement explicitly for Avalonia's physical arrange coordinates.
        var width = Math.Min(Math.Max(0, laneWidth), Math.Max(0, desired.Width));
        var x = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
            ? laneX + Math.Max(0, laneWidth - width)
            : laneX;
        ArrangeSlot(control, x, y, new Size(width, Math.Max(0, desired.Height)));
    }

    private static void ArrangeSlot(Control? control, double x, double y, Size size)
    {
        control?.Arrange(new Rect(x, y, Math.Max(0, size.Width), Math.Max(0, size.Height)));
    }

    private static Size Constrain(Size desired, double availableWidth, double availableHeight) => new(
        ConstrainToAvailable(desired.Width, availableWidth),
        ConstrainToAvailable(desired.Height, availableHeight));

    private LogicalPadding GetLogicalPadding()
    {
        var padding = ContentPadding;
        var isRightToLeft = FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
        return new LogicalPadding(
            Start: Math.Max(0, isRightToLeft ? padding.Right : padding.Left),
            End: Math.Max(0, isRightToLeft ? padding.Left : padding.Right),
            Top: Math.Max(0, padding.Top),
            Bottom: Math.Max(0, padding.Bottom));
    }

    private static double Centre(double content, double container) => Math.Max(0, (container - content) / 2);

    private static double Subtract(double available, double amount)
    {
        if (!IsFinite(available))
            return available;

        return Math.Max(0, available - amount);
    }

    private static double ConstrainToAvailable(double desired, double available)
    {
        if (!IsFinite(available))
            return Math.Max(0, desired);

        return Math.Min(Math.Max(0, desired), Math.Max(0, available));
    }

    private static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

    private readonly record struct Slots(
        Control? Leading,
        Control? Trailing,
        Control? Overline,
        Control? Headline,
        Control? Supporting)
    {
        public bool HasMainContent => Overline is not null || Headline is not null || Supporting is not null;
    }

    private readonly record struct LogicalPadding(double Start, double End, double Top, double Bottom);
}

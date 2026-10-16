/* ButtonGroup.kt: ButtonGroupMeasurePolicy. Fixed AndroidX/Core baseline, Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class ButtonGroupLayout : Panel
{
    private ButtonGroupItem[] _overflowItems = [];
    private Size _indicatorSize;
    private Size _rowSize;
    private ItemsPresenter Items => (ItemsPresenter)Children[0];
    private ButtonGroupSlot Indicator => (ButtonGroupSlot)Children[1];
    internal DropdownMenu Menu => (DropdownMenu)Children[2];
    internal Control Row => Items;
    internal ButtonGroup Owner { get; private set; } = null!;
    internal double IndicatorIntrinsicWidth { get; private set; }
    internal double IndicatorConstraint { get; set; }
    internal double ShownItemWidth { get; set; }
    internal int VisibleCount { get; set; }

    internal void Connect(ButtonGroup owner)
    {
        Owner = owner;
        Indicator.Content = owner.MenuState;
        Menu.PlacementTarget = Indicator;
        Menu.DismissRequested += OnDismissRequested;
        owner.MenuState.PropertyChanged += OnMenuStateChanged;
        Menu.IsOpen = owner.MenuState.IsShowing;
    }

    internal void Disconnect()
    {
        Menu.IsOpen = false;
        Menu.DismissRequested -= OnDismissRequested;
        Owner.MenuState.PropertyChanged -= OnMenuStateChanged;
        ReleaseMenuContents();
    }

    private void OnDismissRequested(object? sender, Foundation.Overlays.DismissRequestedEventArgs e) =>
        Owner.MenuState.Dismiss();

    private void OnMenuStateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) =>
        Menu.IsOpen = Owner.MenuState.IsShowing;

    internal void UpdateOverflow(ButtonGroupSlot[] slots)
    {
        var overflow = slots.Skip(VisibleCount).Select(slot => (ButtonGroupItem)slot.Child!).ToArray();
        if (_overflowItems.SequenceEqual(overflow))
            return;

        ReleaseMenuContents();
        _overflowItems = overflow;
        foreach (var item in overflow)
            Menu.Items.Add(item.CreateMenuContent(Owner.MenuState));
    }

    private void ReleaseMenuContents()
    {
        foreach (var content in Menu.Items.OfType<ContentControl>())
        {
            if (content is DropdownMenuItem menuItem)
            {
                menuItem.ClearValue(DropdownMenuItem.LeadingIconProperty);
                menuItem.ClearValue(DropdownMenuItem.LeadingIconTemplateProperty);
            }

            content.ClearValue(ContentControl.ContentTemplateProperty);
            content.ClearValue(ContentControl.ContentProperty);
        }

        Menu.Items.Clear();
        _overflowItems = [];
    }

    internal void InvalidateItemsMeasure()
    {
        Items.Panel?.InvalidateMeasure();
        Items.InvalidateMeasure();
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is null)
            return default;

        var indicatorWidth = Indicator.MeasureContent(new Size(double.PositiveInfinity, availableSize.Height)).Width;
        if (IndicatorIntrinsicWidth != indicatorWidth)
        {
            IndicatorIntrinsicWidth = indicatorWidth;
            Items.Panel?.InvalidateMeasure();
            Items.InvalidateMeasure();
        }

        Items.Measure(availableSize);
        _rowSize = (Items.Panel as ButtonGroupItemsPanel)?.MeasuredSize ?? default;
        Indicator.IsVisible = _overflowItems.Length > 0;
        _indicatorSize = default;
        if (Indicator.IsVisible)
        {
            Indicator.Measure(new Size(IndicatorConstraint, availableSize.Height));
            _indicatorSize = Indicator.DesiredSize;
        }

        Menu.Measure(availableSize);
        // The fixed upstream derives row height only from placed content, excluding the indicator.
        return _rowSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Compose keeps the measure policy's real size even when Constraints coerce the
        // apparent size reported to its parent. Avalonia clamps DesiredSize; retain the real
        // row explicitly and apply Placeable.apparentToRealOffset in physical pixels.
        var scale = LayoutHelper.GetLayoutScale(this);
        var offset = new Point(
            Math.Truncate((finalSize.Width - _rowSize.Width) * scale / 2) / scale,
            Math.Truncate((finalSize.Height - _rowSize.Height) * scale / 2) / scale);
        Items.Arrange(new Rect(offset, _rowSize));
        if (Indicator.IsVisible)
        {
            var y = AlignVertically(Owner.ItemVerticalAlignment, _indicatorSize.Height, _rowSize.Height);
            Indicator.Arrange(new Rect(offset + new Vector(ShownItemWidth, y), _indicatorSize));
        }

        Menu.Arrange(default);
        return finalSize;
    }

    internal static double AlignVertically(VerticalAlignment alignment, double childHeight, double height) =>
        alignment switch
        {
            VerticalAlignment.Center => (height - childHeight) / 2,
            VerticalAlignment.Bottom => height - childHeight,
            _ => 0
        };
}

internal sealed class ButtonGroupItemsPanel : Panel
{
    private Size[] _sizes = [];
    private double[] _positions = [];
    internal Size MeasuredSize { get; private set; }

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = this.FindAncestorOfType<ButtonGroupLayout>()!;
        var group = layout.Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        int Px(double value) => double.IsPositiveInfinity(value) ? int.MaxValue : MathUtils.DipToPx(value, scale);
        double Dip(int value) => value / scale;
        var maxWidth = Px(availableSize.Width);
        var minWidth = Math.Min(maxWidth, Px(double.IsNaN(group.Width) ? group.MinWidth : Math.Max(group.Width, group.MinWidth)));
        var minHeight = Math.Min(Px(availableSize.Height), Px(double.IsNaN(group.Height) ? group.MinHeight : Math.Max(group.Height, group.MinHeight)));
        var spacing = group.HorizontalArrangement == ButtonGroupArrangement.SpacedBy ? Px(group.Spacing) : 0;
        var slots = Children.Cast<ButtonGroupSlot>().ToArray();
        var items = slots.Select(slot =>
        {
            slot.ApplyTemplate();
            return slot.Child as ButtonGroupItem ?? throw new InvalidOperationException("ButtonGroup.ItemTemplate must create a ButtonGroupItem.");
        }).ToArray();
        var widths = new int[items.Length];
        var weights = items.Select(item => double.IsNaN(item.Weight) ? 0f : (float)Math.Min(item.Weight, float.MaxValue)).ToArray();
        var totalWeight = 0f;
        var weightedCount = 0;
        var fixedSpace = 0;
        var lastUnweightedSpacing = 0;

        for (var index = 0; index < items.Length; index++)
        {
            if (weights[index] > 0)
            {
                totalWeight += weights[index];
                weightedCount++;
                continue;
            }

            widths[index] = Px(slots[index].MeasureContent(new Size(double.PositiveInfinity, availableSize.Height)).Width);
            lastUnweightedSpacing = Math.Min(spacing, Math.Max(0, maxWidth - fixedSpace - widths[index]));
            fixedSpace += widths[index] + lastUnweightedSpacing;
        }

        if (weightedCount == 0)
        {
            fixedSpace -= lastUnweightedSpacing;
        }
        else
        {
            var targetWidth = maxWidth == int.MaxValue ? minWidth : maxWidth;
            var weightedSpacing = (long)spacing * (weightedCount - 1);
            var remainingWidth = Math.Max(0, targetWidth - fixedSpace - weightedSpacing);
            var weightUnitWidth = remainingWidth / totalWeight;
            var remainder = remainingWidth - weights.Sum(weight => (long)MathUtils.RoundToInt(weightUnitWidth * weight));

            for (var index = 0; index < items.Length; index++)
            {
                if (weights[index] <= 0)
                    continue;

                var remainderUnit = Math.Sign(remainder);
                remainder -= remainderUnit;
                widths[index] = Math.Max(0, MathUtils.RoundToInt(weightUnitWidth * weights[index]) + remainderUnit);
            }
        }

        var desiredWidth = widths.Sum() + spacing * (items.Length - 1);
        var visibleCount = items.Length;
        var rowWidth = desiredWidth;
        var shownWidth = 0;
        var remainingSpace = maxWidth;
        if (desiredWidth > maxWidth)
        {
            var overflowWidth = Px(layout.IndicatorIntrinsicWidth);
            remainingSpace -= overflowWidth;
            rowWidth = overflowWidth;
            visibleCount = 0;
            while (visibleCount < widths.Length && widths[visibleCount] <= remainingSpace)
            {
                rowWidth += widths[visibleCount];
                shownWidth += widths[visibleCount];
                remainingSpace -= widths[visibleCount++] + spacing;
            }

            rowWidth += spacing * visibleCount;
            shownWidth += spacing * visibleCount;
            layout.IndicatorConstraint = Dip(Math.Max(0, remainingSpace + overflowWidth));
        }

        layout.VisibleCount = visibleCount;
        layout.ShownItemWidth = Dip(shownWidth);
        layout.UpdateOverflow(slots);

        // Keep the upstream iteration order: adjacent simultaneous presses consume the
        // widths already adjusted by earlier items. A single visible item can use its hidden neighbour.
        if (items.Length > 1)
        {
            for (var index = 0; index < visibleCount; index++)
            {
                var progress = items[index].PressProgress;
                if (progress == 0)
                    continue;

                var ratio = (float)group.ExpandedRatio;
                float Compression(int neighbour) => (float)(items[neighbour].ResolvedCompressionLimit * scale);
                if (index > 0 && index < visibleCount - 1)
                {
                    var growth = MathUtils.RoundToInt(progress * Math.Min(ratio * widths[index] / 2f,
                        Math.Min(Compression(index - 1), Compression(index + 1))));
                    var leftGrowth = Math.Min(growth, widths[index - 1]);
                    var rightGrowth = Math.Min(growth, widths[index + 1]);
                    widths[index - 1] -= leftGrowth;
                    widths[index + 1] -= rightGrowth;
                    widths[index] += leftGrowth + rightGrowth;
                }
                else
                {
                    var neighbour = index == 0 ? 1 : index - 1;
                    var growth = Math.Min(widths[neighbour], MathUtils.RoundToInt(progress * Math.Min(ratio * widths[index], Compression(neighbour))));
                    widths[neighbour] -= growth;
                    widths[index] += growth;
                }
            }
        }

        _sizes = new Size[visibleCount];
        var height = minHeight;
        for (var index = 0; index < slots.Length; index++)
        {
            slots[index].IsVisible = index < visibleCount;
            if (index >= visibleCount)
                continue;

            slots[index].Measure(new Size(Dip(widths[index]), availableSize.Height));
            _sizes[index] = new Size(Dip(widths[index]), Dip(Math.Max(minHeight, Px(slots[index].DesiredSize.Height))));
            height = Math.Max(height, Px(_sizes[index].Height));
        }

        var layoutWidth = Math.Max(Math.Max(0, rowWidth), minWidth);
        _positions = ArrangeHorizontally(layoutWidth, widths[..visibleCount], spacing, group.HorizontalArrangement,
                group.SpacingAlignment, group.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft)
            .Select(position => position / scale).ToArray();
        MeasuredSize = new Size(Dip(layoutWidth), Dip(height));
        return MeasuredSize;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var group = this.FindAncestorOfType<ButtonGroupLayout>()!.Owner;
        var scale = LayoutHelper.GetLayoutScale(this);
        for (var index = 0; index < _sizes.Length; index++)
        {
            var slot = (ButtonGroupSlot)Children[index];
            var item = (ButtonGroupItem)slot.Child!;
            var y = ButtonGroupLayout.AlignVertically(item.Alignment ?? group.ItemVerticalAlignment, _sizes[index].Height, finalSize.Height);
            y = MathUtils.DipToPx(y, scale) / scale;
            slot.Arrange(new Rect(new Point(_positions[index], y), _sizes[index]));
        }

        return finalSize;
    }

    private static int[] ArrangeHorizontally(int totalWidth, int[] widths, int spacing,
        ButtonGroupArrangement arrangement, HorizontalAlignment spacingAlignment, bool rtl)
    {
        var positions = new int[widths.Length];
        if (widths.Length == 0)
            return positions;

        if (arrangement == ButtonGroupArrangement.SpacedBy)
        {
            var occupiedWidth = 0;
            var lastSpacing = 0;
            var remainingWidth = totalWidth;
            for (var index = 0; index < widths.Length; index++)
            {
                if (rtl)
                {
                    positions[index] = Math.Max(0, remainingWidth - widths[index]);
                    lastSpacing = Math.Min(spacing, positions[index]);
                    remainingWidth = positions[index] - lastSpacing;
                }
                else
                {
                    positions[index] = Math.Min(occupiedWidth, totalWidth - widths[index]);
                    lastSpacing = Math.Min(spacing, totalWidth - positions[index] - widths[index]);
                    occupiedWidth = positions[index] + widths[index] + lastSpacing;
                }
            }

            occupiedWidth -= lastSpacing;
            var freeWidth = rtl ? remainingWidth + lastSpacing : totalWidth - occupiedWidth;
            if (freeWidth > 0)
            {
                var groupPosition = spacingAlignment switch
                {
                    HorizontalAlignment.Center => MathUtils.RoundToInt(freeWidth / 2f),
                    HorizontalAlignment.Right => rtl ? 0 : freeWidth,
                    _ => rtl ? freeWidth : 0
                };
                var offset = rtl ? groupPosition - freeWidth : groupPosition;
                for (var index = 0; index < positions.Length; index++)
                    positions[index] += offset;
            }

            if (rtl)
                for (var index = 0; index < positions.Length; index++)
                    positions[index] = totalWidth - positions[index] - widths[index];
            return positions;
        }

        var freeSpace = totalWidth - widths.Sum();
        var gap = arrangement switch
        {
            ButtonGroupArrangement.SpaceBetween => freeSpace / (float)Math.Max(widths.Length - 1, 1),
            ButtonGroupArrangement.SpaceAround => freeSpace / (float)widths.Length,
            ButtonGroupArrangement.SpaceEvenly => freeSpace / (float)(widths.Length + 1),
            _ => 0f
        };
        var current = arrangement switch
        {
            ButtonGroupArrangement.Center => freeSpace / 2f,
            ButtonGroupArrangement.Start => rtl ? (float)freeSpace : 0,
            ButtonGroupArrangement.End => rtl ? 0 : (float)freeSpace,
            ButtonGroupArrangement.SpaceBetween when rtl && widths.Length == 1 => gap,
            ButtonGroupArrangement.SpaceAround => gap / 2,
            ButtonGroupArrangement.SpaceEvenly => gap,
            _ => 0f
        };
        for (var offset = 0; offset < widths.Length; offset++)
        {
            var index = rtl ? widths.Length - 1 - offset : offset;
            var physicalPosition = MathUtils.RoundToInt(current);
            positions[index] = rtl ? totalWidth - physicalPosition - widths[index] : physicalPosition;
            current += widths[index] + gap;
        }

        return positions;
    }
}

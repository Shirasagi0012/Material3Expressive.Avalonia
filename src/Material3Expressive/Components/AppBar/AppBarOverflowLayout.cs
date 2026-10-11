/* AppBarDsl.kt OverflowMeasurePolicy, fixed AndroidX/Core baseline. Apache-2.0. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

internal sealed class AppBarOverflowLayout : Panel
{
    internal AppBarRow Owner { get; private set; } = null!;
    internal double ReservedSpace { get; private set; }
    internal double RemainingSpace { get; set; }
    internal int VisibleCount { get; set; }
    private ItemsPresenter Items => (ItemsPresenter)Children[0];
    private AppBarSlot Indicator => (AppBarSlot)Children[1];
    internal DropdownMenu Menu => (DropdownMenu)Children[2];
    private AppBarItem[] _overflow = [];
    private Size _itemsSize, _indicatorSize;

    internal void Connect(AppBarRow owner)
    {
        Owner = owner;
        Indicator.Content = owner.MenuState;
        Menu.PlacementTarget = Indicator;
        Menu.DismissRequested += Dismiss;
        owner.MenuState.PropertyChanged += StateChanged;
        Menu.IsOpen = owner.MenuState.IsShowing;
    }

    internal void Disconnect()
    {
        Menu.IsOpen = false;
        Menu.DismissRequested -= Dismiss;
        Owner.MenuState.PropertyChanged -= StateChanged;
        ReleaseMenuContents();
    }

    private void Dismiss(object? sender, Foundation.Overlays.DismissRequestedEventArgs e) => Owner.MenuState.Dismiss();
    private void StateChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Menu.IsOpen = Owner.MenuState.IsShowing;

    internal void UpdateOverflow(IEnumerable<AppBarSlot> slots)
    {
        var overflow = slots.Skip(VisibleCount).Select(slot => slot.Child).Cast<AppBarItem>().ToArray();
        if (_overflow.SequenceEqual(overflow)) return;
        ReleaseMenuContents();
        _overflow = overflow;
        foreach (var item in overflow) Menu.Items.Add(item.CreateMenuContent(Owner.MenuState));
        InvalidateMeasure();
    }

    private void ReleaseMenuContents()
    {
        // Release direct Control content while its presenter is still attached. Avalonia
        // otherwise retains that visual parent in the old, detached template tree.
        foreach (var content in Menu.Items.OfType<ContentControl>())
        {
            content.ClearValue(ContentControl.ContentTemplateProperty);
            content.ClearValue(ContentControl.ContentProperty);
        }
        Menu.Items.Clear();
        _overflow = [];
    }

    internal void InvalidateItemsMeasure()
    {
        Items.Panel?.InvalidateMeasure();
        Items.InvalidateMeasure();
        InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Owner is null) return default;
        var vertical = Owner.IsVertical;
        var intrinsicConstraint = vertical ? new Size(availableSize.Width, double.PositiveInfinity)
            : new Size(double.PositiveInfinity, availableSize.Height);
        var intrinsicSize = Indicator.MeasureContent(intrinsicConstraint);
        var reserved = vertical ? intrinsicSize.Height : intrinsicSize.Width;
        if (ReservedSpace != reserved)
        {
            ReservedSpace = reserved;
            Items.Panel?.InvalidateMeasure();
            Items.InvalidateMeasure();
        }
        Items.Measure(availableSize);
        _itemsSize = Items.DesiredSize;
        Indicator.IsVisible = _overflow.Length != 0;
        _indicatorSize = default;
        if (Indicator.IsVisible)
        {
            var constraint = vertical ? new Size(availableSize.Width, RemainingSpace + ReservedSpace)
                : new Size(RemainingSpace + ReservedSpace, availableSize.Height);
            Indicator.Measure(constraint);
            _indicatorSize = Indicator.DesiredSize;
        }
        Menu.Measure(availableSize);
        return vertical
            ? new Size(Math.Max(_itemsSize.Width, _indicatorSize.Width), _itemsSize.Height + _indicatorSize.Height)
            : new Size(_itemsSize.Width + _indicatorSize.Width, Math.Max(_itemsSize.Height, _indicatorSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Avalonia mirrors the subtree in RTL, equivalent to placeRelative(0, ...).
        Items.Arrange(new Rect(_itemsSize));
        if (Indicator.IsVisible)
            Indicator.Arrange(new Rect(Owner.IsVertical ? new Point(0, _itemsSize.Height) : new Point(_itemsSize.Width, 0), _indicatorSize));
        Menu.Arrange(default);
        return finalSize;
    }
}

internal sealed class AppBarItemsPanel : Panel
{
    private readonly List<Size> _sizes = [];
    private bool _vertical;

    protected override Size MeasureOverride(Size availableSize)
    {
        var layout = this.FindAncestorOfType<AppBarOverflowLayout>()!;
        var owner = layout.Owner;
        _vertical = owner.IsVertical;
        var scale = LayoutHelper.GetLayoutScale(this);
        double Px(double value) => double.IsPositiveInfinity(value) ? value : MathUtils.DipToPx(value, scale);
        var reserved = Px(layout.ReservedSpace);
        var remaining = Math.Max(0, Px(_vertical ? availableSize.Height : availableSize.Width) - reserved);
        var main = 0d;
        var cross = 0d;
        _sizes.Clear();
        // Instantiate all DSL items, but measure only up to the first item that cannot fit.
        foreach (var slot in Children.Cast<AppBarSlot>())
        {
            slot.ApplyTemplate();
            if (slot.Child is not AppBarItem)
                throw new InvalidOperationException("AppBarRow/Column ItemTemplate must create an AppBarItem.");
        }
        for (var i = 0; i < Children.Count; i++)
        {
            var slot = (AppBarSlot)Children[i];
            var size = slot.MeasureContent(availableSize);
            var last = i == Children.Count - 1;
            if (!last && i == owner.MaxItemCount - 1) break;
            var space = Px(_vertical ? size.Height : size.Width);
            if (space > remaining && !(last && space <= remaining + reserved)) break;
            _sizes.Add(size);
            main += space;
            cross = Math.Max(cross, Px(_vertical ? size.Width : size.Height));
            remaining = Math.Max(0, remaining - space);
        }
        for (var i = 0; i < Children.Count; i++)
        {
            Children[i].IsVisible = i < _sizes.Count;
            if (i < _sizes.Count) Children[i].Measure(availableSize);
        }
        layout.VisibleCount = _sizes.Count;
        layout.RemainingSpace = remaining / scale;
        layout.UpdateOverflow(Children.Cast<AppBarSlot>());
        return _vertical ? new Size(cross / scale, main / scale) : new Size(main / scale, cross / scale);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var position = 0d;
        for (var i = 0; i < _sizes.Count; i++)
        {
            Children[i].Arrange(new Rect(_vertical ? new Point(0, position) : new Point(position, 0), _sizes[i]));
            position += _vertical ? _sizes[i].Height : _sizes[i].Width;
        }
        return finalSize;
    }
}

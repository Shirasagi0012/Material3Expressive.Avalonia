using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace Material3Expressive.Components;

internal sealed partial class CarouselPanel
{
    private static readonly Func<IInputElement, NavigationDirection, IInputElement?> NextTab;

    // Avalonia 12.1.5's public FindNextElement uses a different Tab traversal from
    // the window's actual handler. Query that handler so TabIndex, navigation groups
    // and custom navigation retain native semantics when pages are added mid-search.
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Avalonia.Input.KeyboardNavigationHandler", "Avalonia.Base")]
    static CarouselPanel()
    {
        var method = typeof(InputElement).Assembly.GetType("Avalonia.Input.KeyboardNavigationHandler")?.GetMethod("GetNext")
            ?? throw new MissingMethodException("Avalonia.Input.KeyboardNavigationHandler.GetNext is required for Carousel Tab navigation.");
        NextTab = method.CreateDelegate<Func<IInputElement, NavigationDirection, IInputElement?>>();
    }
    private readonly SortedDictionary<int, Control> _realized = [];
    private readonly Dictionary<Control, object?> _recycleKeys = [];
    private readonly Dictionary<object, Stack<Control>> _recycled = [];
    private readonly HashSet<Control> _ownContainers = [];
    private (int First, int Last)? _beyondBounds;

    public CarouselPanel()
    {
        KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Continue);
        AddHandler(LostFocusEvent, (_, _) => InvalidateArrange());
    }

    // Fixed Carousel.kt passes two extra pages on either side for MultiBrowse/Hero,
    // and zero for Uncontained. These are Pager pages before keyline transforms.
    private IEnumerable<int> VisibleIndices(float scroll)
    {
        if (Items.Count == 0 || Strategy is not { IsValid: true } strategy || PageStride <= 0)
            return [];
        var first = Math.Clamp((int)(scroll / PageStride), 0, Items.Count - 1);
        var last = Math.Clamp((int)MathF.Ceiling((scroll + strategy.AvailableSpace) / PageStride) - 1, first, Items.Count - 1);
        return Enumerable.Range(first, last - first + 1);
    }

    private void RealizePages(float scroll, Size constraint)
    {
        var visible = VisibleIndices(Math.Clamp(CarouselStrategy.Round(scroll), 0, MaxOffset)).ToArray();
        var beyond = _owner?.Variant == CarouselVariant.Uncontained ? 0 : 2;
        var first = visible.Length == 0 ? 0 : Math.Max(0, visible[0] - beyond);
        var last = visible.Length == 0 ? -1 : Math.Min(Items.Count - 1, visible[^1] + beyond);
        if (_beyondBounds is { } extra)
        {
            first = Math.Min(first, extra.First);
            last = Math.Max(last, extra.Last);
        }
        foreach (var (index, child) in _realized.ToArray())
        {
            // Compose pins a page while its focus target is active. Keep the same
            // control until focus leaves, even when the viewport scrolls away.
            if ((index < first || index > last) && !child.IsKeyboardFocusWithin)
                Release(index, child);
        }
        for (var index = first; index <= last; index++)
        {
            if (!_realized.TryGetValue(index, out var child))
            {
                var generator = ItemContainerGenerator!;
                var item = Items[index];
                var needsContainer = generator.NeedsContainer(item, index, out var key);
                child = needsContainer
                    ? key is not null && _recycled.TryGetValue(key, out var pool) && pool.TryPop(out var cached)
                        ? cached : generator.CreateContainer(item, index, key)
                    : (Control)item!;
                _recycleKeys[child] = needsContainer ? key : null;
                if (!needsContainer) _ownContainers.Add(child);
                generator.PrepareItemContainer(child, item, index);
                _realized.Add(index, child);
                InsertInternalChild(_realized.Keys.TakeWhile(i => i < index).Count(), child);
                generator.ItemContainerPrepared(child, item, index);
            }
            child.Measure(constraint);
        }
        foreach (var (index, child) in _realized)
            if (index < first || index > last) child.Measure(constraint);
    }

    private void Release(int index, Control child)
    {
        _realized.Remove(index);
        if (!_ownContainers.Remove(child)) ItemContainerGenerator!.ClearItemContainer(child);
        RemoveInternalChild(child);
        if (_recycleKeys.Remove(child, out var key) && key is not null)
        {
            if (!_recycled.TryGetValue(key, out var pool)) _recycled.Add(key, pool = []);
            pool.Push(child);
        }
    }

    protected override void OnItemsChanged(IReadOnlyList<object?> items, NotifyCollectionChangedEventArgs e)
    {
        // Pager uses positional keys by default. Reprepare after mutations so a
        // retained index never presents stale data, theme, or automation content.
        foreach (var (index, child) in _realized.ToArray()) Release(index, child);
        _layoutScroll = null;
        InvalidateMeasure();
    }

    protected override void OnItemsControlChanged(ItemsControl? oldValue)
    {
        if (oldValue is not null)
        {
            foreach (var child in _realized.Values)
                if (!_ownContainers.Contains(child)) oldValue.ItemContainerGenerator.ClearItemContainer(child);
            _realized.Clear();
            _recycleKeys.Clear();
            _recycled.Clear();
            _ownContainers.Clear();
        }
        _owner = ItemsControl as HorizontalCarousel;
        base.OnItemsControlChanged(oldValue);
    }

    protected override Control? ContainerFromIndex(int index) => _realized.GetValueOrDefault(index);
    protected override int IndexFromContainer(Control container) =>
        _realized.FirstOrDefault(pair => ReferenceEquals(pair.Value, container), new(-1, null!)).Key;
    protected override IEnumerable<Control> GetRealizedContainers() => _realized.Values;

    protected override Control? ScrollIntoView(int index)
    {
        if (_owner is null || index < 0 || index >= Items.Count || Strategy is not { IsValid: true }) return null;
        _owner.ScrollToItem(index);
        InvalidateMeasure();
        UpdateLayout();
        return ContainerFromIndex(index);
    }

    protected override IInputElement? GetControl(NavigationDirection direction, IInputElement? from, bool wrap)
    {
        var index = from is Control control ? IndexFromContainer(control) : -1;
        var forward = direction is NavigationDirection.Next or NavigationDirection.Right;
        if (direction is NavigationDirection.Left or NavigationDirection.Right && _owner?.FlowDirection == FlowDirection.RightToLeft)
            forward = !forward;
        var target = direction switch
        {
            NavigationDirection.First => 0,
            NavigationDirection.Last => Items.Count - 1,
            NavigationDirection.Next or NavigationDirection.Previous or NavigationDirection.Left or NavigationDirection.Right => index + (forward ? 1 : -1),
            _ => -1,
        };
        // Only already composed pages participate in ordinary directional focus.
        // Explicit ItemsControl.ScrollIntoView realizes a distant page first.
        return ContainerFromIndex(target);
    }

    internal bool MoveFocus(NavigationDirection direction, KeyModifiers modifiers)
    {
        if (_owner is null || Strategy is not { IsValid: true } strategy || _beyondBounds is not null ||
            KeyboardNavigation.GetTabNavigation(_owner) is KeyboardNavigationMode.Once or KeyboardNavigationMode.None ||
            TopLevel.GetTopLevel(this)?.FocusManager is not { } manager || manager.GetFocusedElement() is not Control focused ||
            !focused.GetVisualAncestors().Contains(this)) return false;

        var forward = direction == NavigationDirection.Next;
        var visible = VisibleIndices(Math.Clamp(CarouselStrategy.Round((float)_owner.ScrollOffset * _density), 0, MaxOffset)).ToArray();
        if (visible.Length == 0) return false;
        var beyond = _owner.Variant == CarouselVariant.Uncontained ? 0 : 2;
        var first = Math.Max(0, visible[0] - beyond);
        var last = Math.Min(Items.Count - 1, visible[^1] + beyond);
        // LazyLayoutBeyondBoundsModifierLocal: one extra page per attempt, bounded by
        // twice the integer number of pages per viewport. The focus engine still owns
        // target selection within and between pages, including explicit navigation.
        var maximum = Math.Min(Items.Count, 2 * Math.Max(1, (int)(strategy.AvailableSpace / PageStride)));
        // Limit this query to the pager, without changing the window's normal
        // exit/wrap behavior after an unsuccessful search. This also prevents a
        // one-page viewport wrapping to an earlier child in the same page.
        using var navigation = SetValue(KeyboardNavigation.TabNavigationProperty, KeyboardNavigationMode.Contained, BindingPriority.Animation);
        try
        {
            for (var created = 0; ; created++)
            {
                var next = NextTab(focused, direction);
                if (next is Control candidate && candidate != focused && candidate.GetVisualAncestors().Contains(this))
                {
                    return candidate.Focus(NavigationMethod.Tab, modifiers);
                }
                if (created >= maximum || (forward ? last == Items.Count - 1 : first == 0)) return false;
                if (forward) last++; else first--;
                _beyondBounds = (first, last);
                InvalidateArrange();
                UpdateLayout();
            }
        }
        finally
        {
            _beyondBounds = null;
            InvalidateArrange();
            UpdateLayout();
        }
    }
}

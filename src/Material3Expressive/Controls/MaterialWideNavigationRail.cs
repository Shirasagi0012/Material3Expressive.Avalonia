// Upstream contract: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 WideNavigationRail.kt,
// WideNavigationRailState.kt, NavigationItem.kt, and NavigationRail*Tokens.kt VERSION v0_11_0.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Vertical arrangement of destinations within a wide navigation rail.</summary>
public enum WideNavigationRailArrangement
{
    Top,
    Center,
    Bottom
}

/// <summary>
/// A Material wide navigation rail that preserves its destinations and selection while it
/// expands between the 96dp collapsed and 220–360dp expanded layouts.
/// </summary>
public class MaterialWideNavigationRail : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.Theme.Current.IsDark ?? false;
    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    public const double CollapsedWidth = 96;
    public const double ExpandedMinimumWidth = 220;
    public const double ExpandedMaximumWidth = 360;
    public const double ContentVerticalPadding = 44;
    public const double HeaderSpacing = 40;

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MaterialWideNavigationRail, bool>(
            nameof(IsExpanded),
            false,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MaterialWideNavigationRail, int>(
            nameof(SelectedIndex),
            -1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<WideNavigationRailArrangement> ArrangementProperty =
        AvaloniaProperty.Register<MaterialWideNavigationRail, WideNavigationRailArrangement>(
            nameof(Arrangement),
            WideNavigationRailArrangement.Top,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<MaterialWideNavigationRail, object?>(nameof(Header));

    public static readonly DirectProperty<MaterialWideNavigationRail, double> ExpansionProgressProperty =
        AvaloniaProperty.RegisterDirect<MaterialWideNavigationRail, double>(
            nameof(ExpansionProgress),
            owner => owner._expansionProgress);

    private readonly HashSet<MaterialNavigationItem> _attachedItems = new();
    private NavigationVisualColors _colors = NavigationDefaults.Colors(Foundation.ResolvedTheme.Default, NavigationVariant.Rail, false);
    private SpringAnimation? _expansion;
    private double _expansionProgress;
    private bool _updatingSelection;

    public MaterialWideNavigationRail()
    {
        _material = MaterialElementContext.Ensure(this);
        Items.CollectionChanged += ItemsChanged;
        ApplyTokens();
    }

    static MaterialWideNavigationRail()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialWideNavigationRail>(false);
        FocusableProperty.OverrideDefaultValue<MaterialWideNavigationRail>(false);
        AffectsMeasure<MaterialWideNavigationRail>(
            IsExpandedProperty,
            ArrangementProperty,
            HeaderProperty);
    }

    /// <summary>Whether the rail targets the expanded leading-icon layout.</summary>
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    /// <summary>The selected destination index, or -1 when no destination is selected.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Where the destination group is placed vertically.</summary>
    public WideNavigationRailArrangement Arrangement
    {
        get => GetValue(ArrangementProperty);
        set => SetValue(ArrangementProperty, value);
    }

    /// <summary>Optional content kept at the logical start of the rail's top edge.</summary>
    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>The navigation destinations. Instances are retained across expansion changes.</summary>
    [Content]
    public AvaloniaList<MaterialNavigationItem> Items { get; } = new();

    /// <summary>Current animated collapsed-to-expanded fraction.</summary>
    public double ExpansionProgress => _expansionProgress;

    internal IReadOnlyList<MaterialNavigationItem> VisibleItems =>
        Items.Where(static item => item.IsVisible).ToArray();

    public void Toggle() => IsExpanded = !IsExpanded;

    public override void Render(DrawingContext context)
    {
        MaterialContainer.Draw(context, Bounds.Size, default, _colors.ContainerColor);
        base.Render(context);
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialWideNavigationRailAutomationPeer(this);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Motion.RequestFrame();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsExpandedProperty)
        {
            foreach (var item in Items)
            {
                if (item.FollowWideRailExpansion)
                {
                    item.IconPosition = IsExpanded
                        ? NavigationItemIconPosition.Start
                        : NavigationItemIconPosition.Top;
                }
                item.UpdateWideNavigationState();
            }
            AnimateExpansion();
        }
        else if (change.Property == SelectedIndexProperty)
        {
            SyncSelection();
        }
    }

    private void OnMotionTick()
    {
        SetExpansionProgress(_expansion?.Value ?? _expansionProgress);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;

        var focusable = Items
            .Where(static item => item.IsEffectivelyVisible && item.IsEffectivelyEnabled && item.Focusable)
            .ToArray();
        if (focusable.Length == 0)
            return;

        var current = e.Source as MaterialNavigationItem;
        var currentIndex = current is null ? -1 : Array.IndexOf(focusable, current);
        var targetIndex = e.Key switch
        {
            Key.Home => 0,
            Key.End => focusable.Length - 1,
            Key.Down => Wrap(currentIndex + 1, focusable.Length),
            Key.Up => Wrap(currentIndex - 1, focusable.Length),
            _ => -1
        };
        if (targetIndex < 0)
            return;

        var target = focusable[targetIndex];
        target.Focus(NavigationMethod.Directional);
        SelectItem(target);
        e.Handled = true;
    }

    internal void SelectFromAutomation(MaterialNavigationItem item)
    {
        if (!item.IsEffectivelyEnabled)
            throw new InvalidOperationException("The navigation destination is disabled.");
        SelectItem(item);
    }

    internal void SetExpansionForTesting(double progress)
    {
        ExpansionAnimation.SnapTo(Math.Clamp(progress, 0, 1));
        SetExpansionProgress(ExpansionAnimation.Value);
    }

    private SpringAnimation ExpansionAnimation =>
        _expansion ??= Motion.AddSpring(_expansionProgress, Spring(MotionRole.DefaultSpatial));

    private void AnimateExpansion()
    {
        var target = IsExpanded ? 1d : 0d;
        if (TopLevel.GetTopLevel(this) is null)
        {
            ExpansionAnimation.SnapTo(target);
            SetExpansionProgress(target);
            return;
        }

        ExpansionAnimation.AnimateTo(target, Spring(MotionRole.DefaultSpatial));
        Motion.RequestFrame();
    }

    private void SetExpansionProgress(double value)
    {
        value = Math.Clamp(value, 0, 1);
        if (Math.Abs(_expansionProgress - value) < 0.000001)
            return;

        var previous = _expansionProgress;
        _expansionProgress = value;
        RaisePropertyChanged(ExpansionProgressProperty, previous, value);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var item in e.OldItems.OfType<MaterialNavigationItem>())
                Detach(item);
        }

        if (e.NewItems is not null)
        {
            foreach (var item in e.NewItems.OfType<MaterialNavigationItem>())
                Attach(item);
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var item in _attachedItems.ToArray())
                Detach(item);
            foreach (var item in Items)
                Attach(item);
        }

        SyncSelection();
        InvalidateMeasure();
    }

    private void Attach(MaterialNavigationItem item)
    {
        if (!_attachedItems.Add(item))
            return;
        item.SetWideNavigationOwner(this);
        item.Surface = NavigationVariant.Rail;
        if (item.FollowWideRailExpansion)
        {
            item.IconPosition = IsExpanded
                ? NavigationItemIconPosition.Start
                : NavigationItemIconPosition.Top;
        }
        item.ApplyTokens();
        item.IsCheckedChanged += ItemCheckedChanged;
    }

    private void Detach(MaterialNavigationItem item)
    {
        if (!_attachedItems.Remove(item))
            return;
        item.IsCheckedChanged -= ItemCheckedChanged;
        if (ReferenceEquals(item.OwnerWideNavigationRail, this))
            item.SetWideNavigationOwner(null);
    }

    private void ItemCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_updatingSelection || sender is not MaterialNavigationItem item)
            return;

        if (item.IsChecked == true)
            SelectedIndex = Items.IndexOf(item);
        else
            SyncSelection();
    }

    private void SelectItem(MaterialNavigationItem item)
    {
        var index = Items.IndexOf(item);
        if (index >= 0)
            SelectedIndex = index;
    }

    private void SyncSelection()
    {
        if (_updatingSelection)
            return;
        _updatingSelection = true;
        try
        {
            for (var index = 0; index < Items.Count; index++)
                Items[index].IsChecked = index == SelectedIndex;
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private void ApplyTokens()
    {
        _colors = NavigationDefaults.Colors(ResolvedTheme, NavigationVariant.Rail, IsDarkVariant);
        foreach (var item in Items)
            item.ApplyTokens();
        InvalidateVisual();
    }

    private static int Wrap(int value, int count) => (value % count + count) % count;
}

internal sealed class MaterialWideNavigationRailLayoutPanel : Panel
{
    private double _expandedWidth = MaterialWideNavigationRail.ExpandedMinimumWidth;

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 2 || Owner() is not { } owner)
            return default;

        var header = Children[0];
        var items = Children[1];
        var maxWidth = double.IsFinite(availableSize.Width)
            ? Math.Min(MaterialWideNavigationRail.ExpandedMaximumWidth, availableSize.Width)
            : MaterialWideNavigationRail.ExpandedMaximumWidth;
        header.Measure(new Size(maxWidth, double.PositiveInfinity));
        items.Measure(
            new Size(
                Math.Max(0, maxWidth - (owner.IsExpanded ? 20 : 0)),
                double.PositiveInfinity));

        if (owner.IsExpanded)
        {
            _expandedWidth = Math.Clamp(
                Math.Max(
                    MaterialWideNavigationRail.ExpandedMinimumWidth,
                    Math.Max(header.DesiredSize.Width, items.DesiredSize.Width + 20)),
                Math.Min(MaterialWideNavigationRail.ExpandedMinimumWidth, maxWidth),
                maxWidth);
        }

        var width = Math.Min(
            maxWidth,
            Lerp(
                Math.Min(MaterialWideNavigationRail.CollapsedWidth, maxWidth),
                _expandedWidth,
                owner.ExpansionProgress));
        var headerHeight = owner.Header is null ? 0 : header.DesiredSize.Height;
        var headerGap = headerHeight > 0 ? MaterialWideNavigationRail.HeaderSpacing : 0;
        var naturalHeight = MaterialWideNavigationRail.ContentVerticalPadding * 2
            + headerHeight
            + headerGap
            + items.DesiredSize.Height;
        var height = double.IsFinite(availableSize.Height)
            ? availableSize.Height
            : naturalHeight;
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 2 || Owner() is not { } owner)
            return finalSize;

        var header = Children[0];
        var items = Children[1];
        var headerWidth = Math.Min(finalSize.Width, header.DesiredSize.Width);
        var headerHeight = owner.Header is null ? 0 : header.DesiredSize.Height;
        var headerX = owner.FlowDirection == FlowDirection.RightToLeft
            ? finalSize.Width - headerWidth
            : 0;
        header.Arrange(new Rect(
            headerX,
            MaterialWideNavigationRail.ContentVerticalPadding,
            headerWidth,
            headerHeight));

        var itemsWidth = Math.Min(finalSize.Width, items.DesiredSize.Width);
        var itemsHeight = Math.Min(finalSize.Height, items.DesiredSize.Height);
        var topStart = MaterialWideNavigationRail.ContentVerticalPadding
            + headerHeight
            + (headerHeight > 0 ? MaterialWideNavigationRail.HeaderSpacing : 0);
        var y = owner.Arrangement switch
        {
            WideNavigationRailArrangement.Center => (finalSize.Height - itemsHeight) / 2,
            WideNavigationRailArrangement.Bottom =>
                finalSize.Height - MaterialWideNavigationRail.ContentVerticalPadding - itemsHeight,
            _ => topStart
        };
        var itemsX = owner.FlowDirection == FlowDirection.RightToLeft
            ? finalSize.Width - itemsWidth
            : 0;
        items.Arrange(new Rect(itemsX, Math.Max(0, y), itemsWidth, itemsHeight));
        return finalSize;
    }

    private MaterialWideNavigationRail? Owner() =>
        this.GetVisualAncestors().OfType<MaterialWideNavigationRail>().FirstOrDefault();

    private static double Lerp(double start, double end, double fraction) =>
        start + ((end - start) * Math.Clamp(fraction, 0, 1));
}

internal sealed class MaterialWideNavigationRailItemsPanel : Panel
{
    private double[] _widths = Array.Empty<double>();
    private double[] _heights = Array.Empty<double>();

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Owner();
        var expanded = owner?.IsExpanded == true;
        var visible = Children.Where(static child => child.IsVisible).ToArray();
        if (visible.Length == 0)
            return default;

        var maxChildWidth = double.IsFinite(availableSize.Width)
            ? Math.Max(0, availableSize.Width - (expanded ? 20 : 0))
            : expanded
                ? MaterialWideNavigationRail.ExpandedMaximumWidth - 40
                : MaterialWideNavigationRail.CollapsedWidth;
        _widths = new double[visible.Length];
        _heights = new double[visible.Length];
        var width = 0d;
        var height = 0d;
        var minimumHeight = expanded ? 56 : 64;
        var spacing = expanded ? 0 : 4;
        for (var index = 0; index < visible.Length; index++)
        {
            visible[index].Measure(new Size(maxChildWidth, double.PositiveInfinity));
            _widths[index] = expanded
                ? Math.Min(maxChildWidth + 20, visible[index].DesiredSize.Width + 20)
                : Math.Min(MaterialWideNavigationRail.CollapsedWidth, maxChildWidth);
            _heights[index] = Math.Max(minimumHeight, visible[index].DesiredSize.Height);
            width = Math.Max(width, _widths[index]);
            height += _heights[index];
        }

        height += spacing * (visible.Length - 1);
        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var expanded = Owner()?.IsExpanded == true;
        var visible = Children.Where(static child => child.IsVisible).ToArray();
        var spacing = expanded ? 0 : 4;
        var y = 0d;
        for (var index = 0; index < visible.Length; index++)
        {
            var width = index < _widths.Length ? Math.Min(finalSize.Width, _widths[index]) : finalSize.Width;
            var height = index < _heights.Length ? _heights[index] : visible[index].DesiredSize.Height;
            var x = FlowDirection == FlowDirection.RightToLeft ? finalSize.Width - width : 0;
            visible[index].Arrange(new Rect(x, y, width, height));
            y += height + spacing;
        }
        return finalSize;
    }

    private MaterialWideNavigationRail? Owner() =>
        this.GetVisualAncestors().OfType<MaterialWideNavigationRail>().FirstOrDefault();
}

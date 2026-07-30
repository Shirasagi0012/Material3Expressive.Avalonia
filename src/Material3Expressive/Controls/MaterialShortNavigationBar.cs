// Upstream contract: AndroidX ff0a0a99 / Material3 1.5.0-alpha25 ShortNavigationBar.kt,
// NavigationItem.kt, and NavigationBar{Horizontal,Vertical}ItemTokens.kt VERSION v0_11_0.

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
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Arrangement of destinations in a short navigation bar.</summary>
public enum ShortNavigationBarArrangement
{
    /// <summary>Use equal weight below <see cref="MaterialShortNavigationBar.MediumWidthBreakpoint"/> and centered above it.</summary>
    Auto,
    /// <summary>Give every destination the same share of the complete bar width.</summary>
    EqualWeight,
    /// <summary>Group destinations in the center using the upstream item-count percentages.</summary>
    Centered
}

/// <summary>
/// A compact adaptive destination bar with equal-weight and centered arrangements.
/// </summary>
public class MaterialShortNavigationBar : TemplatedControl
{
    /// <summary>The Material compact-to-medium width boundary, in Avalonia logical pixels (dp).</summary>
    public const double MediumWidthBreakpoint = 600;

    public static readonly StyledProperty<ShortNavigationBarArrangement> ArrangementProperty =
        AvaloniaProperty.Register<MaterialShortNavigationBar, ShortNavigationBarArrangement>(
            nameof(Arrangement),
            ShortNavigationBarArrangement.Auto);

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MaterialShortNavigationBar, int>(
            nameof(SelectedIndex),
            -1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> ResponsiveBreakpointProperty =
        AvaloniaProperty.Register<MaterialShortNavigationBar, double>(
            nameof(ResponsiveBreakpoint),
            MediumWidthBreakpoint,
            coerce: static (_, value) => Math.Max(0, value));

    public static readonly DirectProperty<MaterialShortNavigationBar, ShortNavigationBarArrangement>
        ResolvedArrangementProperty = AvaloniaProperty.RegisterDirect<
            MaterialShortNavigationBar,
            ShortNavigationBarArrangement>(
            nameof(ResolvedArrangement),
            owner => owner.ResolvedArrangement);

    private readonly MaterialThemeWatcher _theme;
    private readonly HashSet<MaterialNavigationItem> _attachedItems = new();
    private NavigationColors _colors = NavigationTokens.Bar.Resolve(null, false);
    private ShortNavigationBarArrangement _resolvedArrangement = ShortNavigationBarArrangement.EqualWeight;
    private bool _updatingSelection;

    public MaterialShortNavigationBar()
    {
        _theme = new MaterialThemeWatcher(this);
        _theme.Changed += (_, _) => ApplyTokens();
        Items.CollectionChanged += ItemsChanged;
        ApplyTokens();
    }

    static MaterialShortNavigationBar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialShortNavigationBar>(false);
        FocusableProperty.OverrideDefaultValue<MaterialShortNavigationBar>(false);
        AffectsMeasure<MaterialShortNavigationBar>(ArrangementProperty, ResponsiveBreakpointProperty);
    }

    /// <summary>Whether destinations use equal shares, centered grouping, or the responsive default.</summary>
    public ShortNavigationBarArrangement Arrangement
    {
        get => GetValue(ArrangementProperty);
        set => SetValue(ArrangementProperty, value);
    }

    /// <summary>The selected destination index, or -1 when no destination is selected.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>The width at which Auto changes from EqualWeight to Centered.</summary>
    public double ResponsiveBreakpoint
    {
        get => GetValue(ResponsiveBreakpointProperty);
        set => SetValue(ResponsiveBreakpointProperty, value);
    }

    /// <summary>The arrangement selected for the current constrained width.</summary>
    public ShortNavigationBarArrangement ResolvedArrangement => _resolvedArrangement;

    /// <summary>The navigation destinations. Their checked state is coordinated by this bar.</summary>
    [Content]
    public AvaloniaList<MaterialNavigationItem> Items { get; } = new();

    internal IReadOnlyList<MaterialNavigationItem> VisibleItems =>
        Items.Where(static item => item.IsVisible).ToArray();

    public override void Render(DrawingContext context)
    {
        MaterialContainer.Draw(context, Bounds.Size, default, _colors.ContainerColor);
        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        ResolveArrangement(availableSize.Width);
        var measured = base.MeasureOverride(availableSize);
        return new Size(measured.Width, Math.Max(80, measured.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ResolveArrangement(finalSize.Width);
        return base.ArrangeOverride(finalSize);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SelectedIndexProperty)
            SyncSelection();
        else if (change.Property == ArrangementProperty || change.Property == ResponsiveBreakpointProperty)
            ResolveArrangement(Bounds.Width);
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
        var logicalStep = FlowDirection == FlowDirection.RightToLeft ? -1 : 1;
        var targetIndex = e.Key switch
        {
            Key.Home => 0,
            Key.End => focusable.Length - 1,
            Key.Right => Wrap(currentIndex + logicalStep, focusable.Length),
            Key.Left => Wrap(currentIndex - logicalStep, focusable.Length),
            _ => -1
        };
        if (targetIndex < 0)
            return;

        var target = focusable[targetIndex];
        target.Focus(NavigationMethod.Directional);
        SelectItem(target);
        e.Handled = true;
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialShortNavigationBarAutomationPeer(this);

    internal void SelectFromAutomation(MaterialNavigationItem item)
    {
        if (!item.IsEffectivelyEnabled)
            throw new InvalidOperationException("The navigation destination is disabled.");
        SelectItem(item);
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
        item.SetShortNavigationOwner(this);
        item.Surface = NavigationVariant.Bar;
        item.ApplyTokens();
        item.IsCheckedChanged += ItemCheckedChanged;
    }

    private void Detach(MaterialNavigationItem item)
    {
        if (!_attachedItems.Remove(item))
            return;
        item.IsCheckedChanged -= ItemCheckedChanged;
        if (ReferenceEquals(item.OwnerShortNavigationBar, this))
            item.SetShortNavigationOwner(null);
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

    private void ResolveArrangement(double width)
    {
        var resolved = Arrangement switch
        {
            ShortNavigationBarArrangement.EqualWeight => ShortNavigationBarArrangement.EqualWeight,
            ShortNavigationBarArrangement.Centered => ShortNavigationBarArrangement.Centered,
            _ when double.IsFinite(width) && width >= ResponsiveBreakpoint =>
                ShortNavigationBarArrangement.Centered,
            _ => ShortNavigationBarArrangement.EqualWeight
        };
        SetAndRaise(ResolvedArrangementProperty, ref _resolvedArrangement, resolved);
    }

    private void ApplyTokens()
    {
        _colors = _theme.Theme.GetNavigationColors(NavigationVariant.Bar, _theme.IsDark);
        foreach (var item in Items)
            item.ApplyTokens();
        InvalidateVisual();
    }

    private static int Wrap(int value, int count) => (value % count + count) % count;
}

/// <summary>Compose-compatible layout panel used by <see cref="MaterialShortNavigationBar"/>.</summary>
public sealed class MaterialShortNavigationItemsPanel : Panel
{
    public static readonly StyledProperty<ShortNavigationBarArrangement> ArrangementProperty =
        AvaloniaProperty.Register<MaterialShortNavigationItemsPanel, ShortNavigationBarArrangement>(
            nameof(Arrangement),
            ShortNavigationBarArrangement.EqualWeight);

    private double[] _measuredWidths = Array.Empty<double>();
    private double _measuredHeight = 80;

    static MaterialShortNavigationItemsPanel()
    {
        AffectsMeasure<MaterialShortNavigationItemsPanel>(ArrangementProperty);
    }

    public ShortNavigationBarArrangement Arrangement
    {
        get => GetValue(ArrangementProperty);
        set => SetValue(ArrangementProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Children.Where(static child => child.IsVisible).ToArray();
        if (children.Length == 0)
            return new Size(double.IsFinite(availableSize.Width) ? availableSize.Width : 0, 80);

        var bounded = double.IsFinite(availableSize.Width);
        if (!bounded)
        {
            foreach (var child in children)
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            _measuredWidths = children.Select(static child => child.DesiredSize.Width).ToArray();
            _measuredHeight = Math.Max(80, children.Max(static child => child.DesiredSize.Height));
            return new Size(_measuredWidths.Sum(), _measuredHeight);
        }

        var scale = GetScale();
        var totalWidthPx = ToPixels(availableSize.Width, scale);
        var widthsPx = Arrangement == ShortNavigationBarArrangement.Centered
            ? CenteredWidths(children, totalWidthPx, scale)
            : EqualWidths(children.Length, totalWidthPx);

        var widths = widthsPx.Select(value => value / scale).ToArray();
        var height = 80d;
        for (var index = 0; index < children.Length; index++)
        {
            children[index].Measure(new Size(widths[index], double.PositiveInfinity));
            height = Math.Max(height, children[index].DesiredSize.Height);
        }

        if (double.IsFinite(availableSize.Height))
            height = Math.Min(height, Math.Max(80, availableSize.Height));
        foreach (var (child, index) in children.Select((child, index) => (child, index)))
            child.Measure(new Size(widths[index], height));

        _measuredWidths = widths;
        _measuredHeight = height;
        return new Size(availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Children.Where(static child => child.IsVisible).ToArray();
        if (children.Length == 0)
            return finalSize;

        if (_measuredWidths.Length != children.Length)
            _measuredWidths = Enumerable.Repeat(finalSize.Width / children.Length, children.Length).ToArray();
        var contentWidth = _measuredWidths.Sum();
        var x = Arrangement == ShortNavigationBarArrangement.Centered
            ? Math.Max(0, (finalSize.Width - contentWidth) / 2)
            : 0;
        for (var index = 0; index < children.Length; index++)
        {
            var width = _measuredWidths[index];
            var physicalX = FlowDirection == FlowDirection.RightToLeft
                ? finalSize.Width - x - width
                : x;
            children[index].Arrange(new Rect(physicalX, 0, width, finalSize.Height));
            x += width;
        }
        return finalSize;
    }

    private int[] CenteredWidths(Control[] children, int totalWidthPx, double scale)
    {
        var count = children.Length;
        var padding = count > 6
            ? 0
            : (int)Math.Round(((100 - 10 * (count + 3)) / 2d / 100d) * totalWidthPx);
        var minimum = Math.Max(0, (totalWidthPx - padding * 2) / count);
        var maximum = totalWidthPx / count;
        var widths = new int[count];
        for (var index = 0; index < count; index++)
        {
            children[index].Measure(new Size(maximum / scale, double.PositiveInfinity));
            var desired = ToPixels(children[index].DesiredSize.Width, scale);
            widths[index] = Math.Clamp(desired, minimum, maximum);
        }
        return widths;
    }

    private static int[] EqualWidths(int count, int totalWidthPx)
    {
        var width = totalWidthPx / count;
        return Enumerable.Repeat(width, count).ToArray();
    }

    private double GetScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && double.IsFinite(scale) ? scale : 1;
    }

    private static int ToPixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));
}

// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/ListItem.kt:552-855,
// ListItemDefaults.kt:246-399, 438-536, 1135-1159, and tokens/ListTokens.kt
// (VERSION 29.0.0). Compose leaves group layout to the caller while requiring
// segmentedShapes(index, count), segmentedColors(), and the 2dp SegmentedGap. This Avalonia/XAML
// adaptation owns those correlated facts in one vertical Panel so child indices cannot drift
// from their shapes. Interaction remains owned by each MaterialListItem.InteractionMode.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>
/// Arranges <see cref="MaterialListItem" /> children as one Material segmented-list group.
/// </summary>
/// <remarks>
/// Visible children are assigned single/first/middle/last resting shapes automatically. Hidden
/// children do not consume a segment position or gap. Selection and activation are deliberately
/// not coordinated here; each row keeps its explicit <see cref="MaterialListItem.InteractionMode" />
/// and application-owned selection model.
/// </remarks>
public sealed class MaterialSegmentedList : Panel, IMaterialThemeConsumer
{
    /// <summary>Defines the vertical space between visible segments.</summary>
    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<MaterialSegmentedList, double>(
            nameof(Spacing),
            2,
            validate: static value => double.IsFinite(value) && value >= 0);

    private readonly MaterialThemeController _theme;
    private IDisposable? _spacingLease;

    public MaterialSegmentedList()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        ApplyTokens();
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    static MaterialSegmentedList()
    {
        AffectsMeasure<MaterialSegmentedList>(SpacingProperty);
    }

    /// <summary>
    /// Gets or sets the vertical space between visible segments. The Material token default is
    /// 2dp; a local value or binding is an explicit caller override.
    /// </summary>
    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        ValidateChildren();
        ApplySegmentPositions();

        var constraint = availableSize.WithHeight(double.PositiveInfinity);
        var desiredWidth = 0d;
        var desiredHeight = 0d;
        var visibleCount = 0;

        foreach (var child in Children)
        {
            child.Measure(constraint);
            if (!child.IsVisible)
                continue;

            desiredWidth = Math.Max(desiredWidth, child.DesiredSize.Width);
            desiredHeight += child.DesiredSize.Height;
            visibleCount++;
        }

        if (visibleCount > 1)
            desiredHeight += ResolveSpacing() * (visibleCount - 1);

        return new Size(desiredWidth, desiredHeight);
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        ApplySegmentPositions();

        var y = 0d;
        var spacing = ResolveSpacing();
        foreach (var child in Children)
        {
            if (!child.IsVisible)
                continue;

            var height = child.DesiredSize.Height;
            child.Arrange(new Rect(0, y, Math.Max(finalSize.Width, child.DesiredSize.Width), height));
            y += height + spacing;
        }

        return finalSize;
    }

    /// <inheritdoc />
    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var removed in e.OldItems.OfType<MaterialListItem>())
                removed.ClearSegmentPosition();
        }

        base.ChildrenChanged(sender, e);
        ApplySegmentPositions();
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialSegmentedListAutomationPeer(this);

    private void ValidateChildren()
    {
        var invalid = Children.FirstOrDefault(static child => child is not MaterialListItem);
        if (invalid is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(MaterialSegmentedList)} accepts only direct {nameof(MaterialListItem)} children; "
                + $"found {invalid.GetType().Name}.");
        }
    }

    private void ApplySegmentPositions()
    {
        var visible = Children
            .OfType<MaterialListItem>()
            .Where(static item => item.IsVisible)
            .ToArray();

        foreach (var item in Children.OfType<MaterialListItem>())
        {
            var index = Array.IndexOf(visible, item);
            item.SegmentPosition = index < 0
                ? MaterialListItemSegmentPosition.None
                : visible.Length switch
            {
                1 => MaterialListItemSegmentPosition.Single,
                _ when index == 0 => MaterialListItemSegmentPosition.First,
                _ when index == visible.Length - 1 => MaterialListItemSegmentPosition.Last,
                _ => MaterialListItemSegmentPosition.Middle
            };
        }
    }

    private double ResolveSpacing()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        if (!(scale > 0) || !double.IsFinite(scale))
            scale = 1;

        var physicalPixels = Math.Max(0, (int)Math.Floor(Spacing * scale + 0.5));
        return physicalPixels / scale;
    }

    private void ApplyTokens()
    {
        _spacingLease?.Dispose();
        _spacingLease = SetValue(
            SpacingProperty,
            ListItemDefaults.Metrics().SegmentedGap,
            BindingPriority.Template);
        InvalidateMeasure();
    }
}

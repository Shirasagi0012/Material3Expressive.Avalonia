// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/SegmentedButton.kt:304-398.
// The executable Compose Multiplatform alpha22 source has the same Row measurement, -space
// arrangement, and selectable-group distinction. Avalonia owns the correlated child shape/index
// calculation so direct XAML children cannot drift from their segment position.

using System;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Selection semantics owned by a segmented-button row.</summary>
public enum SegmentedButtonSelectionMode
{
    /// <summary>At most one child is checked and children expose radio semantics.</summary>
    Single,

    /// <summary>Children may be checked independently and expose toggle semantics.</summary>
    Multiple
}

/// <summary>
/// Arranges <see cref="MaterialSegmentedButton"/> children as a Material segmented-button row.
/// </summary>
/// <remarks>
/// This is the Avalonia adaptation of Compose's
/// <c>SingleChoiceSegmentedButtonRow</c>/<c>MultiChoiceSegmentedButtonRow</c> scope pair. It
/// keeps one XAML control with an explicit <see cref="SelectionMode"/> property, while preserving
/// bindable child state and application-owned selection models.
/// </remarks>
public sealed class MaterialSegmentedButtonRow : Panel
{
    public static readonly StyledProperty<SegmentedButtonSelectionMode> SelectionModeProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonRow, SegmentedButtonSelectionMode>(
            nameof(SelectionMode),
            SegmentedButtonSelectionMode.Single);

    public static readonly StyledProperty<double> OverlapProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonRow, double>(
            nameof(Overlap),
            1,
            validate: static value => double.IsFinite(value) && value >= 0);

    private static readonly SegmentedButtonGeometryProfile DefaultGeometryProfile = new(
        ContainerHeight: 40,
        ContainerShape: ShapeRole.CornerFull,
        IconSize: 18,
        IconSpacing: 8,
        ContentPadding: new Thickness(12, 8),
        MinimumWidth: 58,
        OutlineWidth: 1);

    /// <summary>The shared geometry profile supplied by the Material control style.</summary>
    /// <remarks>This is an implementation property, not a public authoring surface.</remarks>
    internal static readonly StyledProperty<SegmentedButtonGeometryProfile> GeometryProfileProperty =
        AvaloniaProperty.Register<MaterialSegmentedButtonRow, SegmentedButtonGeometryProfile>(
            nameof(GeometryProfile),
            DefaultGeometryProfile);

    private readonly MaterialThemeController _theme;

    public MaterialSegmentedButtonRow()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
    }

    static MaterialSegmentedButtonRow()
    {
        AffectsMeasure<MaterialSegmentedButtonRow>(
            SelectionModeProperty,
            OverlapProperty,
            GeometryProfileProperty);
    }

    /// <summary>Single (radio) or Multiple (toggle) child semantics.</summary>
    public SegmentedButtonSelectionMode SelectionMode
    {
        get => GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    /// <summary>
    /// The physical overlap between neighbouring 1dp outlines. The default is the alpha25
    /// <c>SegmentedButtonDefaults.BorderWidth</c>; set zero for separated segments.
    /// </summary>
    public double Overlap
    {
        get => GetValue(OverlapProperty);
        set => SetValue(OverlapProperty, value);
    }

    /// <summary>Gets or sets the hidden geometry profile supplied by the control style.</summary>
    internal SegmentedButtonGeometryProfile GeometryProfile
    {
        get => GetValue(GeometryProfileProperty);
        set => SetValue(GeometryProfileProperty, value);
    }

    internal IReadOnlyList<MaterialSegmentedButton> VisibleItems =>
        Children.OfType<MaterialSegmentedButton>()
            .Where(static child => child.IsVisible)
            .ToArray();

    protected override Size MeasureOverride(Size availableSize)
    {
        ValidateChildren();
        ApplySegmentPositions();

        var visible = VisibleItems;
        if (visible.Count == 0)
            return new Size(0, GeometryProfile.ContainerHeight);

        var desiredWidth = 0d;
        var desiredHeight = GeometryProfile.ContainerHeight;
        foreach (var child in visible)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            desiredWidth += Math.Max(child.DesiredSize.Width, child.MinWidth);
            desiredHeight = Math.Max(desiredHeight, child.DesiredSize.Height);
        }

        if (visible.Count > 1)
            desiredWidth -= ResolveOverlap() * (visible.Count - 1);

        return new Size(desiredWidth, desiredHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ApplySegmentPositions();

        var visible = VisibleItems;
        if (visible.Count == 0)
            return finalSize;

        var scale = GetLayoutScale();
        var totalPixels = ToComposePixels(finalSize.Width + ResolveOverlap() * (visible.Count - 1), scale);
        var baseWidth = totalPixels / visible.Count;
        var remainder = totalPixels % visible.Count;
        var leftPixels = 0;

        for (var index = 0; index < visible.Count; index++)
        {
            var widthPixels = baseWidth + (index < remainder ? 1 : 0);
            var width = widthPixels / scale;
            var left = leftPixels / scale;
            if (FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft)
                left = finalSize.Width - left - width;

            visible[index].Arrange(new Rect(left, 0, width, finalSize.Height));
            visible[index].ZIndex = visible[index].InteractionZIndex;
            leftPixels += widthPixels - ToComposePixels(ResolveOverlap(), scale);
        }

        return finalSize;
    }

    protected override void ChildrenChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (var oldItem in e.OldItems.OfType<MaterialSegmentedButton>())
                Detach(oldItem);
        }

        base.ChildrenChanged(sender, e);

        if (e.NewItems is not null)
        {
            foreach (var newItem in e.NewItems.OfType<MaterialSegmentedButton>())
                Attach(newItem);
        }

        EnforceSingleSelection();
        ApplySegmentPositions();
        InvalidateMeasure();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == GeometryProfileProperty)
        {
            ApplySegmentPositions();
            InvalidateMeasure();
        }
        else if (change.Property == SelectionModeProperty)
        {
            EnforceSingleSelection();
            ApplySegmentPositions();
            InvalidateMeasure();
        }
        else if (change.Property == FlowDirectionProperty)
        {
            InvalidateArrange();
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || SelectionMode != SegmentedButtonSelectionMode.Single)
            return;

        var visible = VisibleItems
            .Where(static item => item.IsEffectivelyEnabled)
            .ToArray();
        if (visible.Length == 0)
            return;

        var current = e.Source as MaterialSegmentedButton;
        var currentIndex = current is null ? -1 : Array.IndexOf(visible, current);
        int targetIndex;
        switch (e.Key)
        {
            case Key.Home:
                targetIndex = 0;
                break;
            case Key.End:
                targetIndex = visible.Length - 1;
                break;
            case Key.Down:
                targetIndex = WrapIndex(currentIndex + 1, visible.Length);
                break;
            case Key.Up:
                targetIndex = WrapIndex(currentIndex - 1, visible.Length);
                break;
            case Key.Right:
                targetIndex = WrapIndex(
                    currentIndex + (FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft ? -1 : 1),
                    visible.Length);
                break;
            case Key.Left:
                targetIndex = WrapIndex(
                    currentIndex + (FlowDirection == global::Avalonia.Media.FlowDirection.RightToLeft ? 1 : -1),
                    visible.Length);
                break;
            default:
                return;
        }

        var target = visible[targetIndex];
        target.Focus();
        target.IsChecked = true;
        EnforceSingleSelection(target);
        e.Handled = true;
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialSegmentedButtonRowAutomationPeer(this);

    internal void SelectFromAutomation(MaterialSegmentedButton item)
    {
        if (!item.IsEffectivelyEnabled)
            throw new InvalidOperationException("The segmented button is disabled.");

        if (SelectionMode == SegmentedButtonSelectionMode.Single)
        {
            item.IsChecked = true;
            EnforceSingleSelection(item);
        }
        else
            item.IsChecked = true;
    }

    private void Attach(MaterialSegmentedButton item)
    {
        item.OwnerRow = this;
        item.PropertyChanged += ChildPropertyChanged;
        item.IsCheckedChanged += ChildCheckedChanged;
        item.InteractionChanged += ChildInteractionChanged;
    }

    private void Detach(MaterialSegmentedButton item)
    {
        if (ReferenceEquals(item.OwnerRow, this))
            item.OwnerRow = null;

        item.PropertyChanged -= ChildPropertyChanged;
        item.IsCheckedChanged -= ChildCheckedChanged;
        item.InteractionChanged -= ChildInteractionChanged;
    }

    private void ChildCheckedChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MaterialSegmentedButton item
            && SelectionMode == SegmentedButtonSelectionMode.Single
            && item.IsChecked == true)
            EnforceSingleSelection(item);

        InvalidateArrange();
    }

    private void ChildInteractionChanged(object? sender, EventArgs e)
    {
        if (sender is MaterialSegmentedButton item)
            item.ZIndex = item.InteractionZIndex;
    }

    private void ChildPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Visual.IsVisibleProperty)
        {
            ApplySegmentPositions();
            InvalidateMeasure();
        }
        else if (e.Property == MaterialSegmentedButton.IsCheckedProperty)
        {
            InvalidateArrange();
        }
    }

    private void ValidateChildren()
    {
        var invalid = Children.FirstOrDefault(static child => child is not MaterialSegmentedButton);
        if (invalid is not null)
        {
            throw new InvalidOperationException(
                $"{nameof(MaterialSegmentedButtonRow)} accepts only direct "
                + $"{nameof(MaterialSegmentedButton)} children; found {invalid.GetType().Name}.");
        }
    }

    private void EnforceSingleSelection(MaterialSegmentedButton? selected = null)
    {
        if (SelectionMode != SegmentedButtonSelectionMode.Single)
            return;

        selected ??= VisibleItems.FirstOrDefault(static item => item.IsChecked == true);
        var kept = false;
        foreach (var item in Children.OfType<MaterialSegmentedButton>())
        {
            if (item.IsChecked != true)
                continue;

            if (!kept && (selected is null || ReferenceEquals(item, selected)))
            {
                kept = true;
                continue;
            }

            if (selected is null && !kept)
            {
                kept = true;
                continue;
            }

            item.IsChecked = false;
        }
    }

    private void ApplySegmentPositions()
    {
        var visible = VisibleItems.ToArray();
        for (var index = 0; index < Children.Count; index++)
        {
            if (Children[index] is not MaterialSegmentedButton item)
                continue;

            var visibleIndex = Array.IndexOf(visible, item);
            var position = visibleIndex < 0
                ? SegmentedButtonSegmentPosition.None
                : visible.Length switch
                {
                    1 => SegmentedButtonSegmentPosition.Single,
                    _ when visibleIndex == 0 => SegmentedButtonSegmentPosition.First,
                    _ when visibleIndex == visible.Length - 1 => SegmentedButtonSegmentPosition.Last,
                    _ => SegmentedButtonSegmentPosition.Middle
                };

            item.SegmentPosition = position;
            item.SetSegmentShape(ShapeFor(position));
            item.ZIndex = item.InteractionZIndex;
        }
    }

    private RoundedCornerShape ShapeFor(SegmentedButtonSegmentPosition position)
    {
        var baseShape = _theme.Current.Theme.Shapes.Get(GeometryProfile.ContainerShape);
        return position switch
        {
            SegmentedButtonSegmentPosition.First => baseShape.Start(),
            SegmentedButtonSegmentPosition.Middle => RoundedCornerShape.Rectangle,
            SegmentedButtonSegmentPosition.Last => baseShape.End(),
            _ => baseShape
        };
    }

    private double ResolveOverlap()
    {
        var scale = GetLayoutScale();
        var pixels = ToComposePixels(Overlap, scale);
        return pixels / scale;
    }

    private double GetLayoutScale()
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        return scale > 0 && double.IsFinite(scale) ? scale : 1;
    }

    private static int ToComposePixels(double value, double scale) =>
        Math.Max(0, (int)Math.Floor(value * scale + 0.5));

    private static int WrapIndex(int index, int count) =>
        ((index % count) + count) % count;
}

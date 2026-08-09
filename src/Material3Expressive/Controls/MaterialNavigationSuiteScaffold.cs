// Upstream contract: AndroidX ff0a0a99 / Material3 adaptive navigation suite 1.4.0,
// NavigationSuiteScaffold.kt and WindowSizeClassHelper.kt. The Avalonia adaptation retains real
// MaterialNavigationItem controls while moving them between the short bar and wide rail.

using System;
using System.Collections.Specialized;
using System.Linq;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Threading;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>The navigation component hosted by a <see cref="MaterialNavigationSuiteScaffold"/>.</summary>
public enum NavigationSuiteLayoutType
{
    /// <summary>Resolve the component from the current width and height size classes.</summary>
    Auto,
    /// <summary>A top-icon short navigation bar for compact-width windows.</summary>
    ShortNavigationBarCompact,
    /// <summary>A leading-icon short navigation bar for compact-height windows.</summary>
    ShortNavigationBarMedium,
    /// <summary>A collapsed wide navigation rail at the logical start edge.</summary>
    WideNavigationRailCollapsed,
    /// <summary>An expanded wide navigation rail at the logical start edge.</summary>
    WideNavigationRailExpanded,
    /// <summary>Do not display a navigation component.</summary>
    None
}

/// <summary>
/// Coordinates one retained navigation destination collection across adaptive short-bar and
/// wide-rail layouts while assigning the remaining area to body content.
/// </summary>
public sealed class MaterialNavigationSuiteScaffold : ContentControl, IMaterialMotionConsumer
{
    /// <summary>The lower bound of the Material medium width size class, in dp.</summary>
    public const double MediumWidthBreakpoint = 600;

    /// <summary>The lower bound of the Material medium height size class, in dp.</summary>
    public const double MediumHeightBreakpoint = 480;

    /// <summary>The gap between a horizontal navigation component and its primary action.</summary>
    public const double PrimaryActionSpacing = 16;

    public static readonly StyledProperty<NavigationSuiteLayoutType> LayoutTypeProperty =
        AvaloniaProperty.Register<MaterialNavigationSuiteScaffold, NavigationSuiteLayoutType>(
            nameof(LayoutType),
            NavigationSuiteLayoutType.Auto,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<int> SelectedIndexProperty =
        AvaloniaProperty.Register<MaterialNavigationSuiteScaffold, int>(
            nameof(SelectedIndex),
            -1,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<object?> PrimaryActionProperty =
        AvaloniaProperty.Register<MaterialNavigationSuiteScaffold, object?>(nameof(PrimaryAction));

    public static readonly StyledProperty<WideNavigationRailArrangement> VerticalArrangementProperty =
        AvaloniaProperty.Register<
            MaterialNavigationSuiteScaffold,
            WideNavigationRailArrangement>(
            nameof(VerticalArrangement),
            WideNavigationRailArrangement.Top,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<bool> IsNavigationVisibleProperty =
        AvaloniaProperty.Register<MaterialNavigationSuiteScaffold, bool>(
            nameof(IsNavigationVisible),
            true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly DirectProperty<
        MaterialNavigationSuiteScaffold,
        NavigationSuiteLayoutType> ResolvedLayoutTypeProperty =
        AvaloniaProperty.RegisterDirect<
            MaterialNavigationSuiteScaffold,
            NavigationSuiteLayoutType>(
            nameof(ResolvedLayoutType),
            owner => owner._resolvedLayoutType);

    public static readonly DirectProperty<MaterialNavigationSuiteScaffold, double>
        NavigationVisibilityProgressProperty =
        AvaloniaProperty.RegisterDirect<MaterialNavigationSuiteScaffold, double>(
            nameof(NavigationVisibilityProgress),
            owner => owner._navigationVisibilityProgress);

    private readonly MaterialThemeController _theme;
    private readonly MaterialMotionController _motion;
    private readonly MaterialShortNavigationBar _shortBar;
    private readonly MaterialWideNavigationRail _wideRail;
    private NavigationSuiteLayoutType _resolvedLayoutType =
        NavigationSuiteLayoutType.ShortNavigationBarCompact;
    private Control? _currentNavigation;
    private ContentPresenter? _navigationPresenter;
    private ContentPresenter? _primaryActionPresenter;
    private SpringAnimation? _visibility;
    private double _navigationVisibilityProgress = 1;
    private bool _updatingSelection;

    public MaterialNavigationSuiteScaffold()
    {
        _theme = MaterialElementContext.Ensure(this).Theme;
        _motion = MaterialElementContext.Ensure(this).Motion;
        _shortBar = new MaterialShortNavigationBar
        {
            Arrangement = ShortNavigationBarArrangement.EqualWeight
        };
        _wideRail = new MaterialWideNavigationRail();
        _shortBar.PropertyChanged += NavigationPropertyChanged;
        _wideRail.PropertyChanged += NavigationPropertyChanged;
        Items.CollectionChanged += ItemsChanged;
        SwitchNavigation(_resolvedLayoutType);
    }

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        SetNavigationVisibilityProgress(_visibility?.Value ?? _navigationVisibilityProgress);

    static MaterialNavigationSuiteScaffold()
    {
        FocusableProperty.OverrideDefaultValue<MaterialNavigationSuiteScaffold>(false);
        AffectsMeasure<MaterialNavigationSuiteScaffold>(
            LayoutTypeProperty,
            PrimaryActionProperty,
            VerticalArrangementProperty,
            IsNavigationVisibleProperty);
    }

    /// <summary>The requested adaptive layout, or <see cref="NavigationSuiteLayoutType.Auto"/>.</summary>
    public NavigationSuiteLayoutType LayoutType
    {
        get => GetValue(LayoutTypeProperty);
        set => SetValue(LayoutTypeProperty, value);
    }

    /// <summary>The layout selected after applying the current window-size contract.</summary>
    public NavigationSuiteLayoutType ResolvedLayoutType => _resolvedLayoutType;

    /// <summary>The selected destination index, or -1 when no destination is selected.</summary>
    public int SelectedIndex
    {
        get => GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>
    /// Optional primary action. It is placed above a short bar and becomes the wide rail header.
    /// </summary>
    public object? PrimaryAction
    {
        get => GetValue(PrimaryActionProperty);
        set => SetValue(PrimaryActionProperty, value);
    }

    /// <summary>Vertical placement used by wide-rail layouts.</summary>
    public WideNavigationRailArrangement VerticalArrangement
    {
        get => GetValue(VerticalArrangementProperty);
        set => SetValue(VerticalArrangementProperty, value);
    }

    /// <summary>Whether the navigation component is shown.</summary>
    public bool IsNavigationVisible
    {
        get => GetValue(IsNavigationVisibleProperty);
        set => SetValue(IsNavigationVisibleProperty, value);
    }

    /// <summary>The current animated hidden-to-visible fraction.</summary>
    public double NavigationVisibilityProgress => _navigationVisibilityProgress;

    /// <summary>
    /// Destinations retained while the scaffold changes navigation component. Use the explicit
    /// <c>Items</c> property element in XAML; implicit content remains the body.
    /// </summary>
    public AvaloniaList<MaterialNavigationItem> Items { get; } = new();

    internal bool IsHorizontalNavigation =>
        _resolvedLayoutType is
            NavigationSuiteLayoutType.ShortNavigationBarCompact or
            NavigationSuiteLayoutType.ShortNavigationBarMedium;

    protected override Size MeasureOverride(Size availableSize)
    {
        ResolveLayout(availableSize);
        return base.MeasureOverride(availableSize);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        ResolveLayout(finalSize);
        return base.ArrangeOverride(finalSize);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _navigationPresenter = e.NameScope.Find<ContentPresenter>("PART_Navigation");
        if (_navigationPresenter is null)
        {
            throw new InvalidOperationException(
                "MaterialNavigationSuiteScaffold template is missing PART_Navigation.");
        }
        _navigationPresenter.Content = _currentNavigation;
        _primaryActionPresenter = e.NameScope.Find<ContentPresenter>("PART_PrimaryAction");
        if (_primaryActionPresenter is null)
        {
            throw new InvalidOperationException(
                "MaterialNavigationSuiteScaffold template is missing PART_PrimaryAction.");
        }
        ApplyPrimaryAction();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _motion.RequestFrame();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LayoutTypeProperty
            || change.Property == WidthProperty
            || change.Property == HeightProperty)
        {
            ResolveLayout(new Size(Width, Height));
        }
        else if (change.Property == SelectedIndexProperty)
        {
            SyncSelectionToNavigation();
        }
        else if (change.Property == PrimaryActionProperty)
        {
            ApplyPrimaryAction();
        }
        else if (change.Property == VerticalArrangementProperty)
        {
            _wideRail.Arrangement = VerticalArrangement;
        }
        else if (change.Property == IsNavigationVisibleProperty)
        {
            AnimateNavigationVisibility();
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialNavigationSuiteScaffoldAutomationPeer(this);

    internal void SetNavigationVisibilityForTesting(double progress)
    {
        VisibilityAnimation.SnapTo(Math.Clamp(progress, 0, 1));
        SetNavigationVisibilityProgress(VisibilityAnimation.Value);
    }

    private SpringAnimation VisibilityAnimation =>
        _visibility ??= _motion.AddSpring(
            _navigationVisibilityProgress,
            _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));

    private void ResolveLayout(Size size)
    {
        size = new Size(
            ResolveConstraint(size.Width, Width, Bounds.Width),
            ResolveConstraint(size.Height, Height, Bounds.Height));
        var resolved = LayoutType == NavigationSuiteLayoutType.Auto
            ? AdaptiveNavigationResolver.Resolve(size)
            : LayoutType;
        if (_resolvedLayoutType == resolved)
            return;

        var previous = _resolvedLayoutType;
        _resolvedLayoutType = resolved;
        RaisePropertyChanged(ResolvedLayoutTypeProperty, previous, resolved);
        SwitchNavigation(resolved);
        InvalidateMeasure();
    }

    private static double ResolveConstraint(double available, double requested, double arranged)
    {
        if (double.IsFinite(available))
            return Math.Max(0, available);
        if (double.IsFinite(requested))
            return Math.Max(0, requested);
        return double.IsFinite(arranged) ? Math.Max(0, arranged) : 0;
    }

    private void SwitchNavigation(NavigationSuiteLayoutType type)
    {
        var previous = _currentNavigation;
        var focusedItem = Items.FirstOrDefault(static item => item.IsFocused);
        Control? next;
        if (type is NavigationSuiteLayoutType.ShortNavigationBarCompact
            or NavigationSuiteLayoutType.ShortNavigationBarMedium)
        {
            next = _shortBar;
        }
        else if (type is NavigationSuiteLayoutType.WideNavigationRailCollapsed
                 or NavigationSuiteLayoutType.WideNavigationRailExpanded)
        {
            next = _wideRail;
        }
        else
        {
            next = null;
        }

        ConfigureNavigation(type);
        if (!ReferenceEquals(previous, next))
        {
            RemoveItems(previous);
            _currentNavigation = next;
            AddItems(next);
            if (_navigationPresenter is not null)
                _navigationPresenter.Content = next;
        }

        if (next is not null)
        {
            next.IsVisible = IsNavigationVisible || _navigationVisibilityProgress > 0.000001;
            next.IsHitTestVisible = IsNavigationVisible;
        }

        if (focusedItem is not null && next is not null)
        {
            Dispatcher.UIThread.Post(
                () => focusedItem.Focus(NavigationMethod.Unspecified),
                DispatcherPriority.Input);
        }
    }

    private void ConfigureNavigation(NavigationSuiteLayoutType type)
    {
        var iconPosition = type is
            NavigationSuiteLayoutType.ShortNavigationBarMedium or
            NavigationSuiteLayoutType.WideNavigationRailExpanded
            ? NavigationItemIconPosition.Start
            : NavigationItemIconPosition.Top;
        foreach (var item in Items)
            item.IconPosition = iconPosition;

        _shortBar.SelectedIndex = SelectedIndex;
        _wideRail.SelectedIndex = SelectedIndex;
        _wideRail.Arrangement = VerticalArrangement;
        _wideRail.IsExpanded = type == NavigationSuiteLayoutType.WideNavigationRailExpanded;
        ApplyPrimaryAction(type);
    }

    private void ApplyPrimaryAction() =>
        ApplyPrimaryAction(_resolvedLayoutType);

    private void ApplyPrimaryAction(NavigationSuiteLayoutType type)
    {
        var isWide = type is
            NavigationSuiteLayoutType.WideNavigationRailCollapsed or
            NavigationSuiteLayoutType.WideNavigationRailExpanded;
        if (isWide)
        {
            if (_primaryActionPresenter is not null)
                _primaryActionPresenter.Content = null;
            _wideRail.Header = PrimaryAction;
        }
        else
        {
            _wideRail.Header = null;
            if (_primaryActionPresenter is not null)
            {
                _primaryActionPresenter.Content =
                    type == NavigationSuiteLayoutType.None ? null : PrimaryAction;
            }
        }
        InvalidateMeasure();
    }

    private void ItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RemoveItems(_currentNavigation);
        ConfigureNavigation(_resolvedLayoutType);
        AddItems(_currentNavigation);
        InvalidateMeasure();
    }

    private void AddItems(Control? navigation)
    {
        switch (navigation)
        {
            case MaterialShortNavigationBar shortBar:
                foreach (var item in Items)
                    shortBar.Items.Add(item);
                break;
            case MaterialWideNavigationRail wideRail:
                foreach (var item in Items)
                    wideRail.Items.Add(item);
                break;
        }
        SyncSelectionToNavigation();
    }

    private static void RemoveItems(Control? navigation)
    {
        switch (navigation)
        {
            case MaterialShortNavigationBar shortBar:
                shortBar.Items.Clear();
                break;
            case MaterialWideNavigationRail wideRail:
                wideRail.Items.Clear();
                break;
        }
    }

    private void NavigationPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_updatingSelection
            || !ReferenceEquals(sender, _currentNavigation)
            || e.Property != MaterialShortNavigationBar.SelectedIndexProperty
               && e.Property != MaterialWideNavigationRail.SelectedIndexProperty)
        {
            return;
        }

        SelectedIndex = sender switch
        {
            MaterialShortNavigationBar shortBar => shortBar.SelectedIndex,
            MaterialWideNavigationRail wideRail => wideRail.SelectedIndex,
            _ => SelectedIndex
        };
    }

    private void SyncSelectionToNavigation()
    {
        if (_updatingSelection)
            return;
        _updatingSelection = true;
        try
        {
            _shortBar.SelectedIndex = SelectedIndex;
            _wideRail.SelectedIndex = SelectedIndex;
        }
        finally
        {
            _updatingSelection = false;
        }
    }

    private void AnimateNavigationVisibility()
    {
        var target = IsNavigationVisible ? 1d : 0d;
        if (_currentNavigation is not null)
        {
            _currentNavigation.IsVisible = true;
            _currentNavigation.IsHitTestVisible = IsNavigationVisible;
        }

        if (TopLevel.GetTopLevel(this) is null)
        {
            VisibilityAnimation.SnapTo(target);
            SetNavigationVisibilityProgress(target);
            return;
        }

        VisibilityAnimation.AnimateTo(target, _theme.Current.Theme.Motion.Get(MotionRole.DefaultSpatial));
        _motion.RequestFrame();
    }

    private void SetNavigationVisibilityProgress(double value)
    {
        value = Math.Clamp(value, 0, 1);
        if (Math.Abs(_navigationVisibilityProgress - value) < 0.000001)
            return;

        var previous = _navigationVisibilityProgress;
        _navigationVisibilityProgress = value;
        RaisePropertyChanged(NavigationVisibilityProgressProperty, previous, value);
        if (_currentNavigation is not null)
            _currentNavigation.IsVisible = value > 0.000001 || IsNavigationVisible;
        InvalidateMeasure();
        InvalidateArrange();
    }
}

internal static class AdaptiveNavigationResolver
{
    public static NavigationSuiteLayoutType Resolve(Size size)
    {
        var width = double.IsFinite(size.Width) ? Math.Max(0, size.Width) : 0;
        var height = double.IsFinite(size.Height) ? Math.Max(0, size.Height) : 0;
        if (width < MaterialNavigationSuiteScaffold.MediumWidthBreakpoint)
            return NavigationSuiteLayoutType.ShortNavigationBarCompact;
        if (height < MaterialNavigationSuiteScaffold.MediumHeightBreakpoint)
            return NavigationSuiteLayoutType.ShortNavigationBarMedium;
        return NavigationSuiteLayoutType.WideNavigationRailCollapsed;
    }
}

internal sealed class MaterialNavigationSuitePanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = Owner();
        var slots = Slots();
        var loose = new Size(
            double.IsFinite(availableSize.Width) ? Math.Max(0, availableSize.Width) : double.PositiveInfinity,
            double.IsFinite(availableSize.Height) ? Math.Max(0, availableSize.Height) : double.PositiveInfinity);
        slots.Navigation.Measure(loose);
        slots.PrimaryAction.Measure(loose);

        var progress = owner.ResolvedLayoutType == NavigationSuiteLayoutType.None
            ? 0
            : owner.NavigationVisibilityProgress;
        var contentConstraint = owner.IsHorizontalNavigation
            ? new Size(
                loose.Width,
                Subtract(loose.Height, slots.Navigation.DesiredSize.Height * progress))
            : new Size(
                Subtract(loose.Width, slots.Navigation.DesiredSize.Width * progress),
                loose.Height);
        slots.Content.Measure(contentConstraint);

        if (double.IsFinite(availableSize.Width) && double.IsFinite(availableSize.Height))
            return availableSize;
        return owner.IsHorizontalNavigation
            ? new Size(
                Math.Max(slots.Content.DesiredSize.Width, slots.Navigation.DesiredSize.Width),
                slots.Content.DesiredSize.Height + slots.Navigation.DesiredSize.Height * progress)
            : new Size(
                slots.Content.DesiredSize.Width + slots.Navigation.DesiredSize.Width * progress,
                Math.Max(slots.Content.DesiredSize.Height, slots.Navigation.DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = Owner();
        var slots = Slots();
        var progress = owner.ResolvedLayoutType == NavigationSuiteLayoutType.None
            ? 0
            : owner.NavigationVisibilityProgress;
        if (owner.IsHorizontalNavigation)
        {
            var navigationHeight = slots.Navigation.DesiredSize.Height;
            var visibleHeight = navigationHeight * progress;
            slots.Content.Arrange(new Rect(0, 0, finalSize.Width, Math.Max(0, finalSize.Height - visibleHeight)));
            slots.Navigation.Arrange(
                new Rect(0, finalSize.Height - visibleHeight, finalSize.Width, navigationHeight));

            var actionSize = slots.PrimaryAction.DesiredSize;
            var actionX = owner.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
                ? MaterialNavigationSuiteScaffold.PrimaryActionSpacing
                : finalSize.Width
                    - MaterialNavigationSuiteScaffold.PrimaryActionSpacing
                    - actionSize.Width;
            slots.PrimaryAction.Arrange(
                new Rect(
                    Math.Max(0, actionX),
                    finalSize.Height
                        - visibleHeight
                        - MaterialNavigationSuiteScaffold.PrimaryActionSpacing
                        - actionSize.Height,
                    actionSize.Width,
                    actionSize.Height));
        }
        else
        {
            var navigationWidth = slots.Navigation.DesiredSize.Width;
            var visibleWidth = navigationWidth * progress;
            var navigationX = owner.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
                ? finalSize.Width - visibleWidth
                : visibleWidth - navigationWidth;
            var contentX = owner.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft
                ? 0
                : visibleWidth;
            slots.Navigation.Arrange(
                new Rect(navigationX, 0, navigationWidth, finalSize.Height));
            slots.Content.Arrange(
                new Rect(contentX, 0, Math.Max(0, finalSize.Width - visibleWidth), finalSize.Height));
            slots.PrimaryAction.Arrange(default);
        }

        return finalSize;
    }

    private MaterialNavigationSuiteScaffold Owner() =>
        TemplatedParent as MaterialNavigationSuiteScaffold
        ?? throw new InvalidOperationException(
            $"{nameof(MaterialNavigationSuitePanel)} requires a MaterialNavigationSuiteScaffold.");

    private NavigationSuiteSlots Slots()
    {
        ContentPresenter? navigation = null;
        ContentPresenter? primary = null;
        ContentPresenter? content = null;
        foreach (var presenter in Children.OfType<ContentPresenter>())
        {
            switch (presenter.Name)
            {
                case "PART_Navigation":
                    navigation = presenter;
                    break;
                case "PART_PrimaryAction":
                    primary = presenter;
                    break;
                case "PART_Content":
                    content = presenter;
                    break;
            }
        }

        return new NavigationSuiteSlots(
            navigation ?? Missing("PART_Navigation"),
            primary ?? Missing("PART_PrimaryAction"),
            content ?? Missing("PART_Content"));
    }

    private static ContentPresenter Missing(string name) =>
        throw new InvalidOperationException($"Navigation suite template is missing '{name}'.");

    private static double Subtract(double value, double amount) =>
        double.IsFinite(value) ? Math.Max(0, value - amount) : value;

    private readonly record struct NavigationSuiteSlots(
        ContentPresenter Navigation,
        ContentPresenter PrimaryAction,
        ContentPresenter Content);
}

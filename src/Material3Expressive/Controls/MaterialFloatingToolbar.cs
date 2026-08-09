// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/FloatingToolbar.kt:
// 173-477, 484-743, 1164-1458 and 1468-1924. The executable visual reference remains Compose
// Multiplatform Material3 1.12.0-alpha03 / Jetpack Material3 1.5.0-alpha22; alpha25 only removes
// the former expressive opt-in from this surface.

using System.ComponentModel;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;
using MaterialColorUtilities.Avalonia.Tokens;

namespace Material3Expressive.Controls;

/// <summary>Chooses the toolbar axis or lets its available aspect ratio choose it.</summary>
public enum FloatingToolbarOrientation
{
    Horizontal,
    Vertical,
    Auto
}

/// <summary>Logical position of an adjacent floating action button.</summary>
public enum FloatingToolbarFabPosition
{
    Start,
    End
}

/// <summary>Direction in which exit-always scrolling moves the complete toolbar off-screen.</summary>
public enum FloatingToolbarExitDirection
{
    Top,
    Bottom,
    Start,
    End
}

/// <summary>Optional coordination between a floating toolbar and scrolling content.</summary>
public enum FloatingToolbarScrollBehavior
{
    None,
    ExitAlways
}

/// <summary>Recoverable offset state used by floating-toolbar scroll coordination.</summary>
public sealed class FloatingToolbarScrollState : INotifyPropertyChanged
{
    private double _offsetLimit;
    private double _offset;
    private double _contentOffset;

    public FloatingToolbarScrollState(
        double initialOffsetLimit = 0,
        double initialOffset = 0,
        double initialContentOffset = 0)
    {
        if (!double.IsFinite(initialOffsetLimit) || initialOffsetLimit > 0)
            throw new ArgumentOutOfRangeException(nameof(initialOffsetLimit));
        if (!double.IsFinite(initialOffset) || initialOffset < initialOffsetLimit || initialOffset > 0)
            throw new ArgumentOutOfRangeException(nameof(initialOffset));
        if (!double.IsFinite(initialContentOffset))
            throw new ArgumentOutOfRangeException(nameof(initialContentOffset));

        _offsetLimit = initialOffsetLimit;
        _offset = initialOffset;
        _contentOffset = initialContentOffset;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Negative pixel limit at which the toolbar has left the screen.</summary>
    public double OffsetLimit
    {
        get => _offsetLimit;
        internal set
        {
            if (!double.IsFinite(value) || value > 0)
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Math.Abs(_offsetLimit - value) < 0.000001)
                return;
            _offsetLimit = value;
            Offset = _offset;
            PropertyChanged?.Invoke(this, new(nameof(OffsetLimit)));
        }
    }

    /// <summary>Current toolbar offset, coerced between <see cref="OffsetLimit"/> and zero.</summary>
    public double Offset
    {
        get => _offset;
        set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            var next = Math.Clamp(value, _offsetLimit, 0);
            if (Math.Abs(_offset - next) < 0.000001)
                return;
            _offset = next;
            PropertyChanged?.Invoke(this, new(nameof(Offset)));
        }
    }

    /// <summary>Total logical content offset observed by the toolbar.</summary>
    public double ContentOffset
    {
        get => _contentOffset;
        internal set
        {
            if (!double.IsFinite(value))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (Math.Abs(_contentOffset - value) < 0.000001)
                return;
            _contentOffset = value;
            PropertyChanged?.Invoke(this, new(nameof(ContentOffset)));
        }
    }

    public void Show() => Offset = 0;

    public void Hide() => Offset = OffsetLimit;
}

/// <summary>
/// A horizontal, vertical, or aspect-adaptive Material floating toolbar with optional leading,
/// trailing, and adjacent FAB content.
/// </summary>
/// <remarks>
/// Direct children form the main action sequence. <see cref="LeadingContent"/> and
/// <see cref="TrailingContent"/> are only shown while expanded and without a FAB, matching the
/// corresponding Compose overload. When <see cref="FloatingActionButton"/> is present, collapse
/// clips the complete toolbar and grows that FAB from 56 to 80 DIPs.
/// </remarks>
public sealed class MaterialFloatingToolbar : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.IsDark ?? false;
    private MaterialMotionScope Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    public static readonly StyledProperty<FloatingToolbarOrientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, FloatingToolbarOrientation>(
            nameof(Orientation),
            FloatingToolbarOrientation.Horizontal,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<FloatingToolbarVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, FloatingToolbarVariant>(
            nameof(Variant),
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, bool>(
            nameof(IsExpanded),
            true,
            defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<object?> LeadingContentProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, object?>(nameof(LeadingContent));

    public static readonly StyledProperty<object?> TrailingContentProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, object?>(nameof(TrailingContent));

    public static readonly StyledProperty<object?> FloatingActionButtonProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, object?>(nameof(FloatingActionButton));

    public static readonly StyledProperty<FloatingToolbarFabPosition> FloatingActionButtonPositionProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, FloatingToolbarFabPosition>(
            nameof(FloatingActionButtonPosition),
            FloatingToolbarFabPosition.End,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<FloatingToolbarScrollBehavior> ScrollBehaviorProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, FloatingToolbarScrollBehavior>(
            nameof(ScrollBehavior),
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<FloatingToolbarExitDirection> ExitDirectionProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, FloatingToolbarExitDirection>(
            nameof(ExitDirection),
            FloatingToolbarExitDirection.Bottom,
            validate: static value => Enum.IsDefined(value));

    public static readonly StyledProperty<ScrollViewer?> ScrollTargetProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbar, ScrollViewer?>(nameof(ScrollTarget));

    public static readonly DirectProperty<MaterialFloatingToolbar, FloatingToolbarScrollState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, FloatingToolbarScrollState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));

    public static readonly DirectProperty<MaterialFloatingToolbar, FloatingToolbarOrientation>
        ResolvedOrientationProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, FloatingToolbarOrientation>(
            nameof(ResolvedOrientation), owner => owner._resolvedOrientation);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ExpansionProgressProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ExpansionProgress), owner => owner._expansionProgress);

    public static readonly DirectProperty<MaterialFloatingToolbar, IImmutableBrush> ToolbarBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, IImmutableBrush>(
            nameof(ToolbarBrush), owner => owner.ToolbarBrush);

    public static readonly DirectProperty<MaterialFloatingToolbar, IImmutableBrush> ToolbarContentBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, IImmutableBrush>(
            nameof(ToolbarContentBrush), owner => owner.ToolbarContentBrush);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ContainerSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ContainerSize), owner => owner.ContainerSize);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ContentPaddingProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ContentPadding), owner => owner.ContentPadding);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ToolbarToFabGapProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ToolbarToFabGap), owner => owner.ToolbarToFabGap);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ScreenOffsetProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ScreenOffset), owner => owner.ScreenOffset);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> CurrentElevationProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(CurrentElevation), owner => owner.CurrentElevation);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ScrollOffsetXProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ScrollOffsetX), owner => owner.ScrollOffsetX);

    public static readonly DirectProperty<MaterialFloatingToolbar, double> ScrollOffsetYProperty =
        AvaloniaProperty.RegisterDirect<MaterialFloatingToolbar, double>(
            nameof(ScrollOffsetY), owner => owner.ScrollOffsetY);

    private FloatingToolbarMetrics _tokens = FloatingToolbarDefaults.Metrics(FloatingToolbarVariant.Standard);
    private FloatingToolbarVisualColors _colors = FloatingToolbarDefaults.Colors(Foundation.ResolvedTheme.Default, FloatingToolbarVariant.Standard, false);
    private FloatingToolbarScrollState _state = new();
    private FloatingToolbarOrientation _resolvedOrientation = FloatingToolbarOrientation.Horizontal;
    private SpringAnimation? _expansion;
    private double _expansionProgress = 1;
    private double _lastScrollOffset;
    private double _scrollOffsetX;
    private double _scrollOffsetY;
    private readonly Dictionary<Control, bool> _savedTabStops = new();

    public MaterialFloatingToolbar()
    {
        _material = MaterialElementContext.GetOrCreate(this);
        _state.PropertyChanged += OnStatePropertyChanged;
        Items.CollectionChanged += (_, _) => ApplyItemMetrics();
        AddHandler(KeyDownEvent, OnToolbarKeyDown, Avalonia.Interactivity.RoutingStrategies.Bubble);
        ApplyTokens();
    }

    static MaterialFloatingToolbar()
    {
        ClipToBoundsProperty.OverrideDefaultValue<MaterialFloatingToolbar>(false);
        AffectsMeasure<MaterialFloatingToolbar>(
            OrientationProperty,
            IsExpandedProperty,
            LeadingContentProperty,
            TrailingContentProperty,
            FloatingActionButtonProperty,
            FloatingActionButtonPositionProperty);
    }

    public FloatingToolbarOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public FloatingToolbarVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public object? LeadingContent
    {
        get => GetValue(LeadingContentProperty);
        set => SetValue(LeadingContentProperty, value);
    }

    public object? TrailingContent
    {
        get => GetValue(TrailingContentProperty);
        set => SetValue(TrailingContentProperty, value);
    }

    public object? FloatingActionButton
    {
        get => GetValue(FloatingActionButtonProperty);
        set => SetValue(FloatingActionButtonProperty, value);
    }

    public FloatingToolbarFabPosition FloatingActionButtonPosition
    {
        get => GetValue(FloatingActionButtonPositionProperty);
        set => SetValue(FloatingActionButtonPositionProperty, value);
    }

    public FloatingToolbarScrollBehavior ScrollBehavior
    {
        get => GetValue(ScrollBehaviorProperty);
        set => SetValue(ScrollBehaviorProperty, value);
    }

    public FloatingToolbarExitDirection ExitDirection
    {
        get => GetValue(ExitDirectionProperty);
        set => SetValue(ExitDirectionProperty, value);
    }

    public ScrollViewer? ScrollTarget
    {
        get => GetValue(ScrollTargetProperty);
        set => SetValue(ScrollTargetProperty, value);
    }

    public FloatingToolbarScrollState State
    {
        get => _state;
        set => SetState(value);
    }

    [Content]
    public AvaloniaList<Control> Items { get; } = new();

    public FloatingToolbarOrientation ResolvedOrientation => _resolvedOrientation;

    public double ExpansionProgress => _expansionProgress;

    public IImmutableBrush ToolbarBrush => _colors.ContainerColor;

    public IImmutableBrush ToolbarContentBrush => _colors.ContentColor;

    public double ContainerSize => _tokens.ContainerSize;

    public double ContentPadding => _tokens.ContentPadding;

    public double ToolbarToFabGap => _tokens.ToolbarToFabGap;

    public double ScreenOffset => _tokens.ScreenOffset;

    public double CurrentElevation => FloatingActionButton is null
        ? Lerp(_tokens.CollapsedElevation, _tokens.ExpandedElevation, _expansionProgress)
        : Lerp(_tokens.CollapsedElevationWithFab, _tokens.ExpandedElevationWithFab, _expansionProgress);

    public double ScrollOffsetX => _scrollOffsetX;

    public double ScrollOffsetY => _scrollOffsetY;

    public void ResetState() => SetState(null);

    /// <summary>
    /// Applies a logical scroll delta. Positive values hide the toolbar; negative values reveal it.
    /// Returns the portion consumed by the toolbar.
    /// </summary>
    public double ApplyScrollDelta(double delta)
    {
        if (!double.IsFinite(delta))
            throw new ArgumentOutOfRangeException(nameof(delta));
        if (ScrollBehavior == FloatingToolbarScrollBehavior.None || Math.Abs(delta) < 0.000001)
            return 0;

        var previous = _state.Offset;
        _state.Offset -= delta;
        _state.ContentOffset = Math.Max(0, _state.ContentOffset + delta);
        return previous - _state.Offset;
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialFloatingToolbarAutomationPeer(this);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateFocusability();
        Motion.Start();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateFocusability();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == VariantProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == IsExpandedProperty)
        {
            AnimateExpansion();
            UpdateFocusability();
        }
        else if (change.Property == FloatingActionButtonProperty)
        {
            ApplyFabDefaults();
            UpdateFocusability();
            InvalidateMeasure();
        }
        else if (change.Property == ScrollTargetProperty)
        {
            ObserveScrollTarget(
                change.GetOldValue<ScrollViewer?>(),
                change.GetNewValue<ScrollViewer?>());
        }
        else if (change.Property == ScrollBehaviorProperty
                 && ScrollBehavior == FloatingToolbarScrollBehavior.None)
        {
            _state.Show();
        }
        else if (change.Property == ExitDirectionProperty
                 || change.Property == FlowDirectionProperty)
        {
            RaiseScrollOutputs();
        }
    }

    private void OnMotionTick()
    {
        SetExpansionProgress(_expansion?.Value ?? _expansionProgress);
    }

    internal void ResolveOrientation(Size available)
    {
        var next = Orientation switch
        {
            FloatingToolbarOrientation.Horizontal => FloatingToolbarOrientation.Horizontal,
            FloatingToolbarOrientation.Vertical => FloatingToolbarOrientation.Vertical,
            _ => ResolveAutoOrientation(available)
        };
        if (_resolvedOrientation == next)
            return;

        SetAndRaise(ResolvedOrientationProperty, ref _resolvedOrientation, next);
    }

    internal void UpdateOffsetLimit(Size size)
    {
        var horizontalExit = ExitDirection is FloatingToolbarExitDirection.Start
            or FloatingToolbarExitDirection.End;
        _state.OffsetLimit = -(horizontalExit ? size.Width : size.Height) - ScreenOffset;
    }

    internal void SetExpansionForTesting(double progress)
    {
        ExpansionAnimation.SnapTo(Math.Clamp(progress, 0, 1));
        SetExpansionProgress(ExpansionAnimation.Value);
    }

    private SpringAnimation ExpansionAnimation =>
        _expansion ??= Motion.AddSpring(_expansionProgress, Spring(MotionRole.FastSpatial));

    private void AnimateExpansion()
    {
        var target = IsExpanded ? 1d : 0d;
        if (TopLevel.GetTopLevel(this) is null)
        {
            ExpansionAnimation.SnapTo(target);
            SetExpansionProgress(target);
            return;
        }

        ExpansionAnimation.AnimateTo(target, Spring(MotionRole.FastSpatial));
        Motion.Start();
    }

    private void SetExpansionProgress(double value)
    {
        value = Math.Clamp(value, 0, 1);
        if (Math.Abs(_expansionProgress - value) < 0.000001)
            return;

        var oldProgress = _expansionProgress;
        var oldElevation = CurrentElevation;
        _expansionProgress = value;
        RaisePropertyChanged(ExpansionProgressProperty, oldProgress, value);
        RaisePropertyChanged(CurrentElevationProperty, oldElevation, CurrentElevation);
        UpdateFocusability();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void ObserveScrollTarget(ScrollViewer? previous, ScrollViewer? next)
    {
        if (previous is not null)
            previous.ScrollChanged -= OnScrollChanged;
        if (next is null)
            return;

        next.ScrollChanged += OnScrollChanged;
        _lastScrollOffset = next.Offset.Y;
        _state.ContentOffset = _lastScrollOffset;
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer viewer)
            return;
        var next = viewer.Offset.Y;
        var delta = next - _lastScrollOffset;
        _lastScrollOffset = next;
        ApplyScrollDelta(delta);
        _state.ContentOffset = next;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(FloatingToolbarScrollState.Offset)
            or nameof(FloatingToolbarScrollState.OffsetLimit))
        {
            RaiseScrollOutputs();
        }
    }

    private void SetState(FloatingToolbarScrollState? value)
    {
        var next = value ?? new FloatingToolbarScrollState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStatePropertyChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStatePropertyChanged;
        RaiseScrollOutputs();
    }

    private void RaiseScrollOutputs()
    {
        var x = ExitDirection switch
        {
            FloatingToolbarExitDirection.Start => LogicalHorizontalOffset(_state.Offset),
            FloatingToolbarExitDirection.End => LogicalHorizontalOffset(-_state.Offset),
            _ => 0
        };
        var y = ExitDirection switch
        {
            FloatingToolbarExitDirection.Top => _state.Offset,
            FloatingToolbarExitDirection.Bottom => -_state.Offset,
            _ => 0
        };
        SetAndRaise(ScrollOffsetXProperty, ref _scrollOffsetX, x);
        SetAndRaise(ScrollOffsetYProperty, ref _scrollOffsetY, y);
        UpdateFocusability();
        InvalidateArrange();
    }

    private void OnToolbarKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled)
            return;

        var horizontal = ResolvedOrientation == FloatingToolbarOrientation.Horizontal;
        var forward = horizontal ? Key.Right : Key.Down;
        var backward = horizontal ? Key.Left : Key.Up;
        if (horizontal && FlowDirection == FlowDirection.RightToLeft)
            (forward, backward) = (backward, forward);

        var step = e.Key == forward ? 1 : e.Key == backward ? -1 : 0;
        var focusables = this.GetVisualDescendants()
            .OfType<Control>()
            .Where(static control => control.Focusable
                                     && control.IsEffectivelyVisible
                                     && control.IsEffectivelyEnabled)
            .ToArray();
        if (focusables.Length == 0)
            return;

        var focused = TopLevel.GetTopLevel(this)?.FocusManager.GetFocusedElement() as Control;
        var current = focused is null ? -1 : Array.IndexOf(focusables, focused);
        var target = e.Key switch
        {
            Key.Home => 0,
            Key.End => focusables.Length - 1,
            _ when step != 0 => (current + step + focusables.Length) % focusables.Length,
            _ => -1
        };
        if (target < 0)
            return;

        focusables[target].Focus(NavigationMethod.Directional);
        e.Handled = true;
    }

    private void ApplyTokens()
    {
        var oldBrush = ToolbarBrush;
        var oldContent = ToolbarContentBrush;
        var oldSize = ContainerSize;
        var oldPadding = ContentPadding;
        var oldGap = ToolbarToFabGap;
        var oldScreenOffset = ScreenOffset;
        var oldElevation = CurrentElevation;

        _tokens = FloatingToolbarDefaults.Metrics(Variant);
        _colors = FloatingToolbarDefaults.Colors(ResolvedTheme, Variant, IsDarkVariant);
        ApplyFabDefaults();

        RaisePropertyChanged(ToolbarBrushProperty, oldBrush, ToolbarBrush);
        RaisePropertyChanged(ToolbarContentBrushProperty, oldContent, ToolbarContentBrush);
        RaisePropertyChanged(ContainerSizeProperty, oldSize, ContainerSize);
        RaisePropertyChanged(ContentPaddingProperty, oldPadding, ContentPadding);
        RaisePropertyChanged(ToolbarToFabGapProperty, oldGap, ToolbarToFabGap);
        RaisePropertyChanged(ScreenOffsetProperty, oldScreenOffset, ScreenOffset);
        RaisePropertyChanged(CurrentElevationProperty, oldElevation, CurrentElevation);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void ApplyItemMetrics()
    {
        foreach (var item in Items)
        {
            item.MinWidth = Math.Max(item.MinWidth, 48);
            item.MinHeight = Math.Max(item.MinHeight, 48);
        }
        UpdateFocusability();
        InvalidateMeasure();
    }

    private void ApplyFabDefaults()
    {
        if (FloatingActionButton is not MaterialFab fab)
            return;

        fab.Size = FabSize.Small;
    }

    private void UpdateFocusability()
    {
        var onScreen = Math.Abs(_state.Offset) < 0.001;
        var mainAllowed = onScreen && (FloatingActionButton is null || IsExpanded);
        var auxiliaryAllowed = onScreen && FloatingActionButton is null && IsExpanded;
        var fabAllowed = onScreen;

        foreach (var item in Items)
            SetSubtreeTabStops(item, mainAllowed);
        if (LeadingContent is Control leading)
            SetSubtreeTabStops(leading, auxiliaryAllowed);
        if (TrailingContent is Control trailing)
            SetSubtreeTabStops(trailing, auxiliaryAllowed);
        if (FloatingActionButton is Control fab)
            SetSubtreeTabStops(fab, fabAllowed);
    }

    private void SetSubtreeTabStops(Control root, bool allowed)
    {
        var controls = root.GetVisualDescendants().OfType<Control>().Prepend(root);
        foreach (var control in controls)
        {
            if (allowed)
            {
                if (_savedTabStops.Remove(control, out var original))
                    KeyboardNavigation.SetIsTabStop(control, original);
                continue;
            }

            if (!_savedTabStops.ContainsKey(control))
                _savedTabStops.Add(control, KeyboardNavigation.GetIsTabStop(control));
            KeyboardNavigation.SetIsTabStop(control, false);
        }
    }

    private FloatingToolbarOrientation ResolveAutoOrientation(Size available)
    {
        if (double.IsFinite(available.Width) && double.IsFinite(available.Height))
            return available.Width >= available.Height
                ? FloatingToolbarOrientation.Horizontal
                : FloatingToolbarOrientation.Vertical;
        if (double.IsFinite(available.Width))
            return FloatingToolbarOrientation.Horizontal;
        if (double.IsFinite(available.Height))
            return FloatingToolbarOrientation.Vertical;
        return _resolvedOrientation;
    }

    private double LogicalHorizontalOffset(double logical)
        => FlowDirection == FlowDirection.RightToLeft ? -logical : logical;

    private static double Lerp(double start, double end, double fraction)
        => start + ((end - start) * fraction);
}

internal sealed class MaterialFloatingToolbarLayoutPanel : Panel
{
    private const double SmallFabSize = 56;
    private const double MediumFabSize = 80;

    protected override Size MeasureOverride(Size availableSize)
    {
        var owner = this.GetVisualAncestors().OfType<MaterialFloatingToolbar>().FirstOrDefault();
        if (owner is null || Children.Count < 2)
            return default;

        owner.ResolveOrientation(availableSize);
        var horizontal = owner.ResolvedOrientation == FloatingToolbarOrientation.Horizontal;
        var toolbar = Children[0];
        var fab = Children[1];
        var unconstrained = horizontal
            ? new Size(double.PositiveInfinity, availableSize.Height)
            : new Size(availableSize.Width, double.PositiveInfinity);
        toolbar.Measure(unconstrained);

        if (owner.FloatingActionButton is null)
        {
            fab.Measure(default);
            return toolbar.DesiredSize;
        }

        var fabSize = Lerp(MediumFabSize, SmallFabSize, owner.ExpansionProgress);
        fab.Measure(new Size(fabSize, fabSize));
        var along = Along(toolbar.DesiredSize, horizontal) + owner.ToolbarToFabGap + SmallFabSize;
        var across = Math.Max(Across(toolbar.DesiredSize, horizontal), MediumFabSize);
        return FromAxes(along, across, horizontal);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var owner = this.GetVisualAncestors().OfType<MaterialFloatingToolbar>().FirstOrDefault();
        if (owner is null || Children.Count < 2)
            return finalSize;

        owner.UpdateOffsetLimit(finalSize);
        var horizontal = owner.ResolvedOrientation == FloatingToolbarOrientation.Horizontal;
        var toolbar = Children[0];
        var fab = Children[1];
        if (owner.FloatingActionButton is null)
        {
            toolbar.Arrange(new Rect(finalSize));
            fab.Arrange(default);
            return finalSize;
        }

        var fullToolbarAlong = Along(toolbar.DesiredSize, horizontal);
        var toolbarAlong = fullToolbarAlong * owner.ExpansionProgress;
        var toolbarAcross = Across(toolbar.DesiredSize, horizontal);
        var fabSize = Lerp(MediumFabSize, SmallFabSize, owner.ExpansionProgress);
        var totalAlong = Along(finalSize, horizontal);
        var totalAcross = Across(finalSize, horizontal);
        var logicalFabAtEnd = owner.FloatingActionButtonPosition == FloatingToolbarFabPosition.End;
        var fabAlong = logicalFabAtEnd ? totalAlong - fabSize : 0;
        var toolbarAlongOffset = logicalFabAtEnd
            ? fullToolbarAlong - toolbarAlong
            : totalAlong - fullToolbarAlong;

        if (horizontal && owner.FlowDirection == FlowDirection.RightToLeft)
        {
            fabAlong = totalAlong - fabAlong - fabSize;
            toolbarAlongOffset = totalAlong - toolbarAlongOffset - toolbarAlong;
        }

        var toolbarAcrossOffset = (totalAcross - toolbarAcross) / 2;
        var fabAcrossOffset = (totalAcross - fabSize) / 2;
        toolbar.Arrange(ToRect(toolbarAlongOffset, toolbarAcrossOffset, toolbarAlong, toolbarAcross, horizontal));
        fab.Arrange(ToRect(fabAlong, fabAcrossOffset, fabSize, fabSize, horizontal));
        return finalSize;
    }

    private static double Along(Size size, bool horizontal) => horizontal ? size.Width : size.Height;
    private static double Across(Size size, bool horizontal) => horizontal ? size.Height : size.Width;
    private static Size FromAxes(double along, double across, bool horizontal) =>
        horizontal ? new Size(along, across) : new Size(across, along);
    private static Rect ToRect(double along, double across, double length, double thickness, bool horizontal) =>
        horizontal ? new Rect(along, across, length, thickness) : new Rect(across, along, thickness, length);
    private static double Lerp(double start, double end, double fraction) =>
        start + ((end - start) * fraction);
}

internal sealed class MaterialFloatingToolbarShadow : Control, IMaterialThemeConsumer
{
    public static readonly StyledProperty<double> ElevationProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbarShadow, double>(nameof(Elevation));

    private readonly MaterialElementContext _theme;

    public MaterialFloatingToolbarShadow()
    {
        _theme = MaterialElementContext.GetOrCreate(this);
    }

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => InvalidateVisual();

    static MaterialFloatingToolbarShadow()
    {
        AffectsRender<MaterialFloatingToolbarShadow>(ElevationProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<MaterialFloatingToolbarShadow>(false);
    }

    public double Elevation
    {
        get => GetValue(ElevationProperty);
        set => SetValue(ElevationProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var shadow = _theme.Theme.ColorScheme?.GetColor(SysColorToken.Shadow, _theme.IsDark)
                     ?? Colors.Black;
        MaterialContainer.Draw(
            context,
            this,
            Bounds.Size,
            new CornerRadius(Math.Min(Bounds.Width, Bounds.Height) / 2),
            null,
            null,
            0,
            Math.Max(0, Elevation),
            shadow);
    }
}

internal sealed class MaterialFloatingToolbarContentPanel : Panel
{
    public static readonly StyledProperty<FloatingToolbarOrientation> OrientationProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbarContentPanel, FloatingToolbarOrientation>(
            nameof(Orientation));

    public static readonly StyledProperty<double> ExpansionProgressProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbarContentPanel, double>(nameof(ExpansionProgress), 1);

    public static readonly StyledProperty<bool> HasFabProperty =
        AvaloniaProperty.Register<MaterialFloatingToolbarContentPanel, bool>(nameof(HasFab));

    static MaterialFloatingToolbarContentPanel()
    {
        AffectsMeasure<MaterialFloatingToolbarContentPanel>(
            OrientationProperty,
            ExpansionProgressProperty,
            HasFabProperty);
    }

    public FloatingToolbarOrientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public double ExpansionProgress
    {
        get => GetValue(ExpansionProgressProperty);
        set => SetValue(ExpansionProgressProperty, value);
    }

    public bool HasFab
    {
        get => GetValue(HasFabProperty);
        set => SetValue(HasFabProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count < 3)
            return default;
        var horizontal = Orientation != FloatingToolbarOrientation.Vertical;
        var unconstrained = horizontal
            ? new Size(double.PositiveInfinity, availableSize.Height)
            : new Size(availableSize.Width, double.PositiveInfinity);
        foreach (var child in Children)
            child.Measure(unconstrained);

        var main = Along(Children[1].DesiredSize, horizontal);
        var auxiliary = Along(Children[0].DesiredSize, horizontal) + Along(Children[2].DesiredSize, horizontal);
        var progress = HasFab ? 1 : Math.Clamp(ExpansionProgress, 0, 1);
        var along = main + (auxiliary * progress);
        var across = Children.Max(child => Across(child.DesiredSize, horizontal));
        return FromAxes(along, across, horizontal);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (Children.Count < 3)
            return finalSize;
        var horizontal = Orientation != FloatingToolbarOrientation.Vertical;
        var progress = HasFab ? 1 : Math.Clamp(ExpansionProgress, 0, 1);
        var offset = 0d;
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            var desired = Along(child.DesiredSize, horizontal);
            var length = index == 1 ? desired : desired * progress;
            child.Opacity = index == 1 ? 1 : progress;
            child.IsHitTestVisible = index == 1 || progress > 0.999;
            child.Arrange(ToRect(offset, 0, length, Across(finalSize, horizontal), horizontal));
            offset += length;
        }
        return finalSize;
    }

    private static double Along(Size size, bool horizontal) => horizontal ? size.Width : size.Height;
    private static double Across(Size size, bool horizontal) => horizontal ? size.Height : size.Width;
    private static Size FromAxes(double along, double across, bool horizontal) =>
        horizontal ? new Size(along, across) : new Size(across, along);
    private static Rect ToRect(double along, double across, double length, double thickness, bool horizontal) =>
        horizontal ? new Rect(along, across, length, thickness) : new Rect(across, along, thickness, length);
}

internal sealed class MaterialFloatingToolbarItemsPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = ResolveOrientation() != FloatingToolbarOrientation.Vertical;
        var unconstrained = horizontal
            ? new Size(double.PositiveInfinity, availableSize.Height)
            : new Size(availableSize.Width, double.PositiveInfinity);
        var along = 0d;
        var across = 0d;
        foreach (var child in Children)
        {
            child.Measure(unconstrained);
            along += horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            across = Math.Max(across, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }
        return horizontal ? new Size(along, across) : new Size(across, along);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = ResolveOrientation() != FloatingToolbarOrientation.Vertical;
        var offset = 0d;
        foreach (var child in Children)
        {
            var length = horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            child.Arrange(horizontal
                ? new Rect(offset, 0, length, finalSize.Height)
                : new Rect(0, offset, finalSize.Width, length));
            offset += length;
        }
        return finalSize;
    }

    private FloatingToolbarOrientation ResolveOrientation() =>
        this.GetVisualAncestors()
            .OfType<MaterialFloatingToolbar>()
            .FirstOrDefault()
            ?.ResolvedOrientation
        ?? FloatingToolbarOrientation.Horizontal;
}

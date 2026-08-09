// Upstream source: AndroidX ff0a0a99 / Material3 1.5.0-alpha25,
// compose/material3/material3/src/commonMain/kotlin/androidx/compose/material3/AppBar.kt:
// 518-968, 1384-1425, 1693-2243, 2249-2375, 3493-3761.
// Avalonia adapts Compose nested-scroll connections through an optional ScrollViewer target and
// an explicit ApplyScrollDelta method for custom scrolling containers. Positive logical deltas
// collapse the bar; negative deltas expand it according to the selected upstream behavior.

using System.ComponentModel;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Material3Expressive.Controls.Automation;
using Material3Expressive.Foundation;

namespace Material3Expressive.Controls;

/// <summary>Top app-bar size axis.</summary>
public enum AppBarSize { Small, Medium, Large }

/// <summary>Standard or alpha25 flexible top-app-bar layout.</summary>
public enum AppBarVariant
{
    Standard,
    Flexible
}

/// <summary>Desktop adaptation of the Material top-app-bar nested-scroll behaviors.</summary>
public enum AppBarScrollBehavior
{
    None,
    Pinned,
    EnterAlways,
    ExitUntilCollapsed
}

/// <summary>Recoverable application- or control-owned top-app-bar scroll state.</summary>
public sealed class AppBarScrollState : INotifyPropertyChanged
{
    private double _collapsedFraction;
    private double _contentOffset;

    public AppBarScrollState(
        double initialCollapsedFraction = 0,
        double initialContentOffset = 0)
    {
        if (!double.IsFinite(initialCollapsedFraction)
            || initialCollapsedFraction is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCollapsedFraction));
        }
        if (!double.IsFinite(initialContentOffset))
            throw new ArgumentOutOfRangeException(nameof(initialContentOffset));

        _collapsedFraction = initialCollapsedFraction;
        _contentOffset = initialContentOffset;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Zero is fully expanded and one is fully collapsed.</summary>
    public double CollapsedFraction
    {
        get => _collapsedFraction;
        private set
        {
            value = Math.Clamp(value, 0, 1);
            if (Math.Abs(_collapsedFraction - value) < double.Epsilon)
                return;
            _collapsedFraction = value;
            PropertyChanged?.Invoke(this, new(nameof(CollapsedFraction)));
        }
    }

    /// <summary>Logical content offset used to select the scrolled container colour.</summary>
    public double ContentOffset
    {
        get => _contentOffset;
        private set
        {
            if (Math.Abs(_contentOffset - value) < double.Epsilon)
                return;
            _contentOffset = value;
            PropertyChanged?.Invoke(this, new(nameof(ContentOffset)));
        }
    }

    public void Expand() => SetCollapsedFraction(0);

    public void Collapse() => SetCollapsedFraction(1);

    public void SetCollapsedFraction(double value)
    {
        if (!double.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value));
        CollapsedFraction = value;
    }

    internal void Update(double collapsedFraction, double contentOffset)
    {
        CollapsedFraction = collapsedFraction;
        ContentOffset = contentOffset;
    }
}

/// <summary>
/// A standard or flexible Material top app bar with optional scroll coordination.
/// </summary>
public class MaterialAppBar : TemplatedControl, IMaterialThemeConsumer, IMaterialMotionConsumer
{
    private MaterialElementContext? _material;

    private ResolvedTheme ResolvedTheme => _material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default;
    private bool IsDarkVariant => _material?.Theme.Current.IsDark ?? false;
    private MaterialMotionController Motion => _material?.Motion ?? throw new InvalidOperationException("Material element context is not initialized.");
    private SpringSpec Spring(MotionRole role) => (_material?.Theme.Current.Theme ?? Foundation.ResolvedTheme.Default).Motion.Get(role);

    void IMaterialThemeConsumer.OnMaterialThemeChanged(MaterialThemeState state) => ApplyTokens();

    void IMaterialMotionConsumer.OnMaterialMotionFrame() =>
        OnMotionTick();

    public static readonly StyledProperty<AppBarSize> SizeProperty =
        AvaloniaProperty.Register<MaterialAppBar, AppBarSize>(nameof(Size));
    public static readonly StyledProperty<AppBarVariant> VariantProperty =
        AvaloniaProperty.Register<MaterialAppBar, AppBarVariant>(nameof(Variant));
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(Title));
    public static readonly StyledProperty<object?> SubtitleProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(Subtitle));
    public static readonly StyledProperty<object?> NavigationIconProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(NavigationIcon));
    public static readonly StyledProperty<object?> ActionsProperty =
        AvaloniaProperty.Register<MaterialAppBar, object?>(nameof(Actions));
    public static readonly StyledProperty<bool> IsScrolledProperty =
        AvaloniaProperty.Register<MaterialAppBar, bool>(nameof(IsScrolled));
    public static readonly StyledProperty<AppBarScrollBehavior> ScrollBehaviorProperty =
        AvaloniaProperty.Register<MaterialAppBar, AppBarScrollBehavior>(nameof(ScrollBehavior));
    public static readonly StyledProperty<ScrollViewer?> ScrollTargetProperty =
        AvaloniaProperty.Register<MaterialAppBar, ScrollViewer?>(nameof(ScrollTarget));
    public static readonly DirectProperty<MaterialAppBar, AppBarScrollState> StateProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, AppBarScrollState>(
            nameof(State), owner => owner._state, (owner, value) => owner.SetState(value));
    public static readonly DirectProperty<MaterialAppBar, bool> IsStackedProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, bool>(nameof(IsStacked), owner => owner.IsStacked);
    public static readonly DirectProperty<MaterialAppBar, IImmutableBrush> TitleBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, IImmutableBrush>(
            nameof(TitleBrush), owner => owner.TitleBrush);
    public static readonly DirectProperty<MaterialAppBar, IImmutableBrush> SubtitleBrushProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, IImmutableBrush>(
            nameof(SubtitleBrush), owner => owner.SubtitleBrush);
    public static readonly DirectProperty<MaterialAppBar, double> CurrentHeightProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, double>(
            nameof(CurrentHeight), owner => owner.CurrentHeight);
    public static readonly DirectProperty<MaterialAppBar, double> InlineTitleOpacityProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, double>(
            nameof(InlineTitleOpacity), owner => owner.InlineTitleOpacity);
    public static readonly DirectProperty<MaterialAppBar, object?> InlineTitleContentProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, object?>(
            nameof(InlineTitleContent), owner => owner.InlineTitleContent);
    public static readonly DirectProperty<MaterialAppBar, object?> StackedTitleContentProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, object?>(
            nameof(StackedTitleContent), owner => owner.StackedTitleContent);
    public static readonly DirectProperty<MaterialAppBar, double> StackedTitleOpacityProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, double>(
            nameof(StackedTitleOpacity), owner => owner.StackedTitleOpacity);
    public static readonly DirectProperty<MaterialAppBar, Thickness> StackedTitleMarginProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, Thickness>(
            nameof(StackedTitleMargin), owner => owner.StackedTitleMargin);
    public static readonly DirectProperty<MaterialAppBar, double> CollapsedTitleFontSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, double>(
            nameof(CollapsedTitleFontSize), owner => owner.CollapsedTitleFontSize);
    public static readonly DirectProperty<MaterialAppBar, FontFamily> CollapsedTitleFontFamilyProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, FontFamily>(
            nameof(CollapsedTitleFontFamily), owner => owner.CollapsedTitleFontFamily);
    public static readonly DirectProperty<MaterialAppBar, FontWeight> CollapsedTitleFontWeightProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, FontWeight>(
            nameof(CollapsedTitleFontWeight), owner => owner.CollapsedTitleFontWeight);
    public static readonly DirectProperty<MaterialAppBar, double> SubtitleFontSizeProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, double>(
            nameof(SubtitleFontSize), owner => owner.SubtitleFontSize);
    public static readonly DirectProperty<MaterialAppBar, FontFamily> SubtitleFontFamilyProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, FontFamily>(
            nameof(SubtitleFontFamily), owner => owner.SubtitleFontFamily);
    public static readonly DirectProperty<MaterialAppBar, FontWeight> SubtitleFontWeightProperty =
        AvaloniaProperty.RegisterDirect<MaterialAppBar, FontWeight>(
            nameof(SubtitleFontWeight), owner => owner.SubtitleFontWeight);

    // The control owns its semantic defaults; the raw component tables stay below AppBarDefaults.
    private AppBarMetrics _tokens = AppBarDefaults.Metrics(AppBarSize.Small);
    private AppBarVisualColors _colors = AppBarDefaults.Colors(Foundation.ResolvedTheme.Default, AppBarSize.Small, false);
    private AppBarScrollState _state = new();
    private SpringAnimation? _motionFraction;
    private double _visualCollapsedFraction;
    private double _lastScrollOffset;
    private bool _updatingFromScroll;
    private double _collapsedTitleFontSize = 22;
    private FontFamily _collapsedTitleFontFamily = FontFamily.Default;
    private FontWeight _collapsedTitleFontWeight = FontWeight.Normal;
    private double _subtitleFontSize = 12;
    private FontFamily _subtitleFontFamily = FontFamily.Default;
    private FontWeight _subtitleFontWeight = FontWeight.Normal;
    private Thickness _stackedTitleMargin = new(0, 0, 0, 16);
    private ContentPresenter? _inlineTitlePresenter;
    private ContentPresenter? _stackedTitlePresenter;
    private ContentPresenter? _subtitlePresenter;

    public MaterialAppBar()
    {
        _material = MaterialElementContext.Ensure(this);
        _state.PropertyChanged += OnStateChanged;
        ApplyTokens();
    }

    static MaterialAppBar()
    {
        AffectsRender<MaterialAppBar>(
            SizeProperty,
            VariantProperty,
            IsScrolledProperty,
            StateProperty);
    }

    public AppBarSize Size
    {
        get => GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public AppBarVariant Variant
    {
        get => GetValue(VariantProperty);
        set => SetValue(VariantProperty, value);
    }

    public object? Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object? Subtitle
    {
        get => GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public object? NavigationIcon
    {
        get => GetValue(NavigationIconProperty);
        set => SetValue(NavigationIconProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    /// <summary>Explicitly selects the scrolled surface colour in addition to state-derived overlap.</summary>
    public bool IsScrolled
    {
        get => GetValue(IsScrolledProperty);
        set => SetValue(IsScrolledProperty, value);
    }

    public AppBarScrollBehavior ScrollBehavior
    {
        get => GetValue(ScrollBehaviorProperty);
        set => SetValue(ScrollBehaviorProperty, value);
    }

    /// <summary>Optional Avalonia scroll source; custom containers can call <see cref="ApplyScrollDelta"/>.</summary>
    public ScrollViewer? ScrollTarget
    {
        get => GetValue(ScrollTargetProperty);
        set => SetValue(ScrollTargetProperty, value);
    }

    /// <summary>Gets or replaces the non-null effective scroll state.</summary>
    public AppBarScrollState State
    {
        get => _state;
        set => SetState(value);
    }

    public bool IsStacked => Size != AppBarSize.Small;

    public IImmutableBrush TitleBrush => _colors.TitleColor;

    public IImmutableBrush SubtitleBrush => _colors.SubtitleColor;

    public double CurrentHeight =>
        ExpandedHeight - CollapseRange * _visualCollapsedFraction;

    public double InlineTitleOpacity =>
        IsStacked ? _visualCollapsedFraction : 1;

    public object? InlineTitleContent =>
        TitleContentFor(Title, inline: true);

    public object? StackedTitleContent =>
        TitleContentFor(Title, inline: false);

    public double StackedTitleOpacity =>
        IsStacked ? 1 - _visualCollapsedFraction : 0;

    public Thickness StackedTitleMargin => _stackedTitleMargin;

    public double CollapsedTitleFontSize => _collapsedTitleFontSize;

    public FontFamily CollapsedTitleFontFamily => _collapsedTitleFontFamily;

    public FontWeight CollapsedTitleFontWeight => _collapsedTitleFontWeight;

    public double SubtitleFontSize => _subtitleFontSize;

    public FontFamily SubtitleFontFamily => _subtitleFontFamily;

    public FontWeight SubtitleFontWeight => _subtitleFontWeight;

    /// <summary>Replaces the current state and cancels any in-flight visual collapse.</summary>
    public void ResetState() => SetState(null);

    /// <summary>
    /// Applies a logical vertical scroll delta. Positive collapses; negative expands.
    /// Returns the portion consumed by the app bar.
    /// </summary>
    public double ApplyScrollDelta(double delta, bool contentAtStart = false)
    {
        if (!double.IsFinite(delta))
            throw new ArgumentOutOfRangeException(nameof(delta));
        if (ScrollBehavior == AppBarScrollBehavior.None || Math.Abs(delta) < double.Epsilon)
            return 0;

        var previous = _state.CollapsedFraction;
        var next = previous;
        if (ScrollBehavior != AppBarScrollBehavior.Pinned && CollapseRange > 0)
        {
            if (delta > 0
                || ScrollBehavior == AppBarScrollBehavior.EnterAlways
                || contentAtStart)
            {
                next = Math.Clamp(previous + delta / CollapseRange, 0, 1);
            }
        }

        var consumed = (next - previous) * CollapseRange;
        UpdateStateFromScroll(
            next,
            Math.Max(0, _state.ContentOffset + delta));
        return consumed;
    }

    public override void Render(DrawingContext context)
    {
        var scrolled = IsScrolled
            || _state.ContentOffset > 0.5
            || _visualCollapsedFraction > 0.001;
        MaterialContainer.Draw(
            context,
            Bounds.Size,
            default,
            scrolled ? _colors.ScrolledContainerColor : _colors.ContainerColor);
        base.Render(context);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = base.MeasureOverride(availableSize);
        return new Size(size.Width, CurrentHeight);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _inlineTitlePresenter = e.NameScope.Find<ContentPresenter>("PART_InlineTitlePresenter");
        _stackedTitlePresenter = e.NameScope.Find<ContentPresenter>("PART_StackedTitlePresenter");
        _subtitlePresenter = e.NameScope.Find<ContentPresenter>("PART_SubtitlePresenter");
        ApplyPresenterTypography();
    }

    protected override AutomationPeer OnCreateAutomationPeer() =>
        new MaterialAppBarAutomationPeer(this);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == SizeProperty)
        {
            ApplyTokens(change.GetOldValue<AppBarSize>() != AppBarSize.Small);
        }
        else if (change.Property == VariantProperty
            || change.Property == SubtitleProperty)
        {
            ApplyTokens();
        }
        else if (change.Property == ScrollTargetProperty)
        {
            ObserveScrollTarget(
                change.GetOldValue<ScrollViewer?>(),
                change.GetNewValue<ScrollViewer?>());
        }
        else if (change.Property == ScrollBehaviorProperty)
        {
            if (ScrollBehavior is AppBarScrollBehavior.None or AppBarScrollBehavior.Pinned)
                UpdateStateFromScroll(0, _state.ContentOffset);
        }
        else if (change.Property == TitleProperty)
        {
            var oldTitle = change.GetOldValue<object?>();
            RaisePropertyChanged(
                InlineTitleContentProperty,
                TitleContentFor(oldTitle, inline: true),
                InlineTitleContent);
            RaisePropertyChanged(
                StackedTitleContentProperty,
                TitleContentFor(oldTitle, inline: false),
                StackedTitleContent);
        }
    }

    private void OnMotionTick()
    {
        SetVisualFraction(_motionFraction?.Value ?? _visualCollapsedFraction);
    }

    private double ExpandedHeight =>
        Variant == AppBarVariant.Flexible
            ? Subtitle is null
                ? _tokens.FlexibleContainerHeight
                : _tokens.FlexibleContainerHeightWithSubtitle
            : _tokens.ContainerHeight;

    private double CollapseRange =>
        Math.Max(0, ExpandedHeight - AppBarDefaults.Metrics(AppBarSize.Small).ContainerHeight);

    private void ObserveScrollTarget(ScrollViewer? previous, ScrollViewer? next)
    {
        if (previous is not null)
        {
            previous.ScrollChanged -= OnScrollChanged;
            previous.RemoveHandler(InputElement.PointerWheelChangedEvent, OnTargetPointerWheelChanged);
        }
        if (next is not null)
        {
            next.ScrollChanged += OnScrollChanged;
            next.AddHandler(
                InputElement.PointerWheelChangedEvent,
                OnTargetPointerWheelChanged,
                RoutingStrategies.Tunnel,
                handledEventsToo: true);
            _lastScrollOffset = next.Offset.Y;
            UpdateStateFromScroll(_state.CollapsedFraction, _lastScrollOffset);
        }
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (sender is not ScrollViewer viewer)
            return;
        var offset = viewer.Offset.Y;
        var delta = offset - _lastScrollOffset;
        _lastScrollOffset = offset;

        var previous = _state.CollapsedFraction;
        var next = previous;
        if (ScrollBehavior is AppBarScrollBehavior.None or AppBarScrollBehavior.Pinned)
        {
            next = 0;
        }
        else if (CollapseRange > 0 && delta > 0)
        {
            next = Math.Clamp(previous + delta / CollapseRange, 0, 1);
        }
        else if (CollapseRange > 0
                 && delta < 0
                 && (ScrollBehavior == AppBarScrollBehavior.EnterAlways || offset <= 0.5))
        {
            next = Math.Clamp(previous + delta / CollapseRange, 0, 1);
        }

        UpdateStateFromScroll(next, offset);
    }

    private void OnTargetPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)
            || e.Delta.Y <= 0
            || ScrollBehavior is AppBarScrollBehavior.None or AppBarScrollBehavior.Pinned)
        {
            return;
        }

        var atStart = ScrollTarget?.Offset.Y <= 0.5;
        if (ScrollBehavior == AppBarScrollBehavior.ExitUntilCollapsed && !atStart)
            return;

        var consumed = ApplyScrollDelta(-e.Delta.Y * 48, atStart);
        if (Math.Abs(consumed) > double.Epsilon)
            e.Handled = true;
    }

    private void UpdateStateFromScroll(double fraction, double contentOffset)
    {
        _updatingFromScroll = true;
        try
        {
            _state.Update(fraction, contentOffset);
            AnimateTo(fraction, snap: true);
        }
        finally
        {
            _updatingFromScroll = false;
        }
    }

    private void OnStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppBarScrollState.CollapsedFraction))
            AnimateTo(_state.CollapsedFraction, snap: _updatingFromScroll);
        InvalidateVisual();
    }

    private void SetState(AppBarScrollState? value)
    {
        var next = value ?? new AppBarScrollState();
        if (ReferenceEquals(_state, next))
            return;

        _state.PropertyChanged -= OnStateChanged;
        SetAndRaise(StateProperty, ref _state, next);
        _state.PropertyChanged += OnStateChanged;
        AnimateTo(_state.CollapsedFraction, snap: true);
        InvalidateVisual();
    }

    private void AnimateTo(double target, bool snap)
    {
        _motionFraction ??= Motion.AddSpring(
            _visualCollapsedFraction,
            Spring(MotionRole.DefaultSpatial));
        if (snap)
            _motionFraction.SnapTo(target);
        else
        {
            _motionFraction.AnimateTo(target, Spring(MotionRole.DefaultSpatial));
            Motion.RequestFrame();
        }
        SetVisualFraction(_motionFraction.Value);
    }

    private void SetVisualFraction(double value)
    {
        value = Math.Clamp(value, 0, 1);
        if (Math.Abs(_visualCollapsedFraction - value) < double.Epsilon)
            return;

        var oldHeight = CurrentHeight;
        var oldInlineOpacity = InlineTitleOpacity;
        var oldStackedOpacity = StackedTitleOpacity;
        var oldInlineContent = InlineTitleContent;
        var oldStackedContent = StackedTitleContent;
        _visualCollapsedFraction = value;
        RaisePropertyChanged(CurrentHeightProperty, oldHeight, CurrentHeight);
        RaisePropertyChanged(InlineTitleOpacityProperty, oldInlineOpacity, InlineTitleOpacity);
        RaisePropertyChanged(StackedTitleOpacityProperty, oldStackedOpacity, StackedTitleOpacity);
        RaiseTitleContentChanges(oldInlineContent, oldStackedContent);
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void RaiseTitleContentChanges(
        object? oldInlineContent,
        object? oldStackedContent)
    {
        var inlineContent = InlineTitleContent;
        var stackedContent = StackedTitleContent;

        // A Control can only have one visual parent. Clear the presenter that is
        // relinquishing it before notifying the presenter that is acquiring it.
        if (Title is Control
            && oldStackedContent is not null
            && inlineContent is not null)
        {
            RaisePropertyChanged(
                StackedTitleContentProperty,
                oldStackedContent,
                stackedContent);
            RaisePropertyChanged(
                InlineTitleContentProperty,
                oldInlineContent,
                inlineContent);
            return;
        }

        RaisePropertyChanged(
            InlineTitleContentProperty,
            oldInlineContent,
            inlineContent);
        RaisePropertyChanged(
            StackedTitleContentProperty,
            oldStackedContent,
            stackedContent);
    }

    private void ApplyTokens(bool? previousIsStacked = null)
    {
        var oldStacked = previousIsStacked ?? IsStacked;
        var oldInlineContent = TitleContentFor(Title, inline: true, oldStacked);
        var oldStackedContent = TitleContentFor(Title, inline: false, oldStacked);
        var oldTitle = TitleBrush;
        var oldSubtitle = SubtitleBrush;
        var oldHeight = CurrentHeight;
        var oldInlineOpacity = InlineTitleOpacity;
        var oldStackedOpacity = StackedTitleOpacity;
        var oldCollapsedSize = CollapsedTitleFontSize;
        var oldCollapsedFamily = CollapsedTitleFontFamily;
        var oldCollapsedWeight = CollapsedTitleFontWeight;
        var oldSubtitleSize = SubtitleFontSize;
        var oldSubtitleFamily = SubtitleFontFamily;
        var oldSubtitleWeight = SubtitleFontWeight;

        _tokens = AppBarDefaults.Metrics(Size);
        _colors = AppBarDefaults.Colors(ResolvedTheme, Size, IsDarkVariant);
        var typography = ResolvedTheme.Typography;
        var titleRole = Variant == AppBarVariant.Flexible
            ? _tokens.FlexibleTitleTextStyle
            : _tokens.TitleTextStyle;
        var subtitleRole = Variant == AppBarVariant.Flexible
            ? _tokens.FlexibleSubtitleTextStyle
            : _tokens.SubtitleTextStyle;
        var titleScale = typography.GetTypeScale(titleRole);
        var collapsedScale = typography.GetTypeScale(TypographyRole.TitleLarge);
        var subtitleScale = typography.GetTypeScale(subtitleRole);

        FontSize = titleScale.FontSize;
        FontFamily = typography.GetFontFamily(titleRole);
        FontWeight = typography.GetFontWeight(titleRole);
        _collapsedTitleFontSize = collapsedScale.FontSize;
        _collapsedTitleFontFamily = typography.GetFontFamily(TypographyRole.TitleLarge);
        _collapsedTitleFontWeight = typography.GetFontWeight(TypographyRole.TitleLarge);
        _subtitleFontSize = subtitleScale.FontSize;
        _subtitleFontFamily = typography.GetFontFamily(subtitleRole);
        _subtitleFontWeight = typography.GetFontWeight(subtitleRole);
        SetAndRaise(
            StackedTitleMarginProperty,
            ref _stackedTitleMargin,
            new Thickness(0, 0, 0, Size == AppBarSize.Medium ? 16 : 28));
        Padding = new Thickness(_tokens.HorizontalSpace, 0);

        RaisePropertyChanged(IsStackedProperty, oldStacked, IsStacked);
        RaisePropertyChanged(TitleBrushProperty, oldTitle, TitleBrush);
        RaisePropertyChanged(SubtitleBrushProperty, oldSubtitle, SubtitleBrush);
        RaisePropertyChanged(CurrentHeightProperty, oldHeight, CurrentHeight);
        RaisePropertyChanged(InlineTitleOpacityProperty, oldInlineOpacity, InlineTitleOpacity);
        RaisePropertyChanged(StackedTitleOpacityProperty, oldStackedOpacity, StackedTitleOpacity);
        RaisePropertyChanged(CollapsedTitleFontSizeProperty, oldCollapsedSize, CollapsedTitleFontSize);
        RaisePropertyChanged(CollapsedTitleFontFamilyProperty, oldCollapsedFamily, CollapsedTitleFontFamily);
        RaisePropertyChanged(CollapsedTitleFontWeightProperty, oldCollapsedWeight, CollapsedTitleFontWeight);
        RaisePropertyChanged(SubtitleFontSizeProperty, oldSubtitleSize, SubtitleFontSize);
        RaisePropertyChanged(SubtitleFontFamilyProperty, oldSubtitleFamily, SubtitleFontFamily);
        RaisePropertyChanged(SubtitleFontWeightProperty, oldSubtitleWeight, SubtitleFontWeight);
        RaiseTitleContentChanges(oldInlineContent, oldStackedContent);
        ApplyPresenterTypography();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void ApplyPresenterTypography()
    {
        if (_inlineTitlePresenter is not null)
            TypographyScheme.SetTextStyle(_inlineTitlePresenter, TypographyRole.TitleLarge);
        if (_stackedTitlePresenter is not null)
        {
            TypographyScheme.SetTextStyle(
                _stackedTitlePresenter,
                Variant == AppBarVariant.Flexible
                    ? _tokens.FlexibleTitleTextStyle
                    : _tokens.TitleTextStyle);
        }
        if (_subtitlePresenter is not null)
        {
            TypographyScheme.SetTextStyle(
                _subtitlePresenter,
                Variant == AppBarVariant.Flexible
                    ? _tokens.FlexibleSubtitleTextStyle
                    : _tokens.SubtitleTextStyle);
        }
    }

    private object? TitleContentFor(object? title, bool inline)
        => TitleContentFor(title, inline, IsStacked);

    private object? TitleContentFor(object? title, bool inline, bool isStacked)
    {
        if (title is not Control)
            return title;
        var useInline = !isStacked || _visualCollapsedFraction >= 0.5;
        return inline == useInline ? title : null;
    }
}

// SearchBar.kt / AppBarWithSearch row, insets, color and scroll modifier; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Rendering;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed class AppBarWithSearchLayout : Decorator
{
    public static readonly StyledProperty<MotionSpring?> ContainerColorAnimationProperty = AvaloniaProperty.Register<AppBarWithSearchLayout, MotionSpring?>(nameof(ContainerColorAnimation));
    public static readonly StyledProperty<MotionSpring?> ScrollSnapAnimationProperty = AvaloniaProperty.Register<AppBarWithSearchLayout, MotionSpring?>(nameof(ScrollSnapAnimation));
    public MotionSpring? ContainerColorAnimation { get => GetValue(ContainerColorAnimationProperty); set => SetValue(ContainerColorAnimationProperty, value); }
    public MotionSpring? ScrollSnapAnimation { get => GetValue(ScrollSnapAnimationProperty); set => SetValue(ScrollSnapAnimationProperty, value); }
    private AppBarWithSearch Owner => (AppBarWithSearch)TemplatedParent!;
    private MaterialSurface Surface => (MaterialSurface)Child!;
    private AppBarWithSearchRow Row => (AppBarWithSearchRow)((Border)Surface.Child!).Child!;
    internal SearchBar? Bar => Child is MaterialSurface { Child: Border { Child: AppBarWithSearchRow row } } ? row.Children.ElementAtOrDefault(1) as SearchBar : null;
    private SearchBarState? _search;
    private SearchBarScrollBehavior? _behavior;
    private AnimationChildScope? _scope;
    private AnimatedState<Color>? _color;
    private AppBarVerticalGesture? _gesture;
    private IDisposable? _background, _searchBackground, _searchOpacity, _surfaceTonal, _surfaceShadow, _searchTonal, _searchShadow;
    private Thickness _insets;
    private Size _unscrolled;
    private readonly Dictionary<Control, (Color Color, Style Style)> _actionDefaults = [];
    protected override bool BypassFlowDirectionPolicies => true;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _scope = AnimationScope.Get(this).CreateChild("Search app bar appearance");
        Observe();
        _color = AnimatedState.Create(this, ContainerTarget, AnimationCommitPhase.AfterFrame,
            AnimationInterruptionMode.Restart, label: "Search app bar container color");
        _color.ValueChanged += Paint;
        Paint(_color.Value);
        _gesture = new AppBarVerticalGesture(() => _behavior?.DirectDragEnabled == true,
            () => { _behavior?.Interrupt(); this.FindAncestorOfType<AppBarScrollScope>()?.Interrupt(); },
            (delta, _) => { if (_behavior is not { } behavior) return default; var before = behavior.State.ScrollOffset; behavior.State.ScrollOffset += delta; return new Vector(0, behavior.State.ScrollOffset - before); },
            (velocity, gestureId, cancelled) => { if (_behavior is { } behavior && _scope is { } scope) _ = scope.Launch(ct => behavior.SettleAsync(cancelled ? 0 : velocity, ct), "Search app bar drag settle"); }, directDrag: true);
        GestureRecognizers.Add(_gesture);
        UpdateAppearance();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_search is not null) { _search.LayoutChanged -= SearchChanged; _search.PropertyChanged -= SearchPropertyChanged; }
        _search = null;
        if (_behavior is not null)
        {
            _behavior.State.PropertyChanged -= ScrollChanged; _behavior.PropertyChanged -= BehaviorChanged;
            _behavior.Detach(); _behavior = null;
        }
        if (_gesture is not null) { _gesture.Cancel(); GestureRecognizers.Remove(_gesture); _gesture = null; }
        _color?.Dispose(); _color = null;
        _scope?.Dispose(); _scope = null;
        foreach (var (control, entry) in _actionDefaults) control.Styles.Remove(entry.Style);
        _actionDefaults.Clear();
        foreach (var contribution in new[] { _background, _searchBackground, _searchOpacity, _surfaceTonal, _surfaceShadow, _searchTonal, _searchShadow }) contribution?.Dispose();
        _background = _searchBackground = _searchOpacity = _surfaceTonal = _surfaceShadow = _searchTonal = _searchShadow = null;
        base.OnDetachedFromVisualTree(e);
    }
    private void Observe()
    {
        if (!ReferenceEquals(_search, Owner.State))
        {
            if (_search is not null) { _search.LayoutChanged -= SearchChanged; _search.PropertyChanged -= SearchPropertyChanged; }
            _search = Owner.State;
            if (_search is not null) { _search.LayoutChanged += SearchChanged; _search.PropertyChanged += SearchPropertyChanged; }
        }
        if (!ReferenceEquals(_behavior, Owner.ScrollBehavior))
        {
            _gesture?.Cancel();
            if (_behavior is not null)
            {
                _behavior.State.PropertyChanged -= ScrollChanged; _behavior.PropertyChanged -= BehaviorChanged;
                _behavior.Detach();
            }
            _behavior = Owner.ScrollBehavior;
            if (_behavior is not null)
            {
                _behavior.Attach(this); _behavior.State.PropertyChanged += ScrollChanged; _behavior.PropertyChanged += BehaviorChanged;
            }
        }
        ClipToBounds = _behavior is not null;
    }
    internal void OwnerChanged()
    {
        // Avalonia clears TemplatedParent before all inherited-value notifications finish.
        if (TemplatedParent is not AppBarWithSearch || Child is null) return;
        if (_scope is not null) Observe();
        UpdateAppearance(); Row.InvalidateMeasure(); InvalidateMeasure();
    }
    private void SearchPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => UpdateAppearance();
    private void SearchChanged(object? sender, EventArgs e) => UpdateAppearance();
    private void ScrollChanged(object? sender, AvaloniaPropertyChangedEventArgs e) { UpdateAppearance(); InvalidateMeasure(); }
    private void BehaviorChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // The upstream draggable modifier captures enabled when its behavior is created.
        // canScroll remains live only for nested input; running gestures/flings keep their lifetime.
        UpdateAppearance();
    }
    private Color ContainerTarget => _behavior?.State.OverlappedFraction > .01f && Owner.ScrolledContainerColor is { } scrolled ? scrolled : BaseContainer;
    private Color BaseContainer => Owner.Background is ISolidColorBrush solid ? solid.Color : Colors.Transparent;
    private void Paint(Color color)
    {
        _background?.Dispose(); _background = Surface.SetValue(MaterialSurface.BackgroundProperty, new SolidColorBrush(color), BindingPriority.Animation);
    }
    private void UpdateAppearance()
    {
        if (TemplatedParent is not AppBarWithSearch owner || Child is null || Bar is not { } bar) return;
        var searchColor = _behavior?.State.OverlappedFraction > .01f && owner.ScrolledSearchBarContainerColor is { } scrolled ? scrolled : owner.SearchBarContainerColor;
        _searchBackground?.Dispose(); _searchBackground = bar.SetValue(SearchBar.BackgroundProperty, new SolidColorBrush(searchColor), BindingPriority.Animation);
        var visible = _search is null || !_search.ExpandsToFullScreen || _search.CurrentValue == SearchBarValue.Collapsed || _search.TargetValue == SearchBarValue.Expanded;
        _searchOpacity?.Dispose(); _searchOpacity = bar.SetValue(OpacityProperty, visible ? 1d : 0d, BindingPriority.Animation);
        // Transparency is determined by the base color, not the current animated color.
        var transparent = BaseContainer == Colors.Transparent;
        _surfaceTonal?.Dispose(); _surfaceTonal = Surface.SetValue(MaterialSurface.TonalElevationProperty, transparent ? 0 : owner.TonalElevation, BindingPriority.Animation);
        _surfaceShadow?.Dispose(); _surfaceShadow = Surface.SetValue(MaterialSurface.ShadowElevationProperty, transparent ? 0 : owner.ShadowElevation, BindingPriority.Animation);
        _searchTonal?.Dispose(); _searchTonal = bar.SetValue(SearchBar.TonalElevationProperty, transparent ? owner.TonalElevation : 0, BindingPriority.Animation);
        _searchShadow?.Dispose(); _searchShadow = bar.SetValue(SearchBar.ShadowElevationProperty, transparent ? owner.ShadowElevation : 0, BindingPriority.Animation);
        if (_color is null) { Paint(ContainerTarget); return; }
        if (ContainerColorAnimation is { } spring)
            _color.UpdateTarget(ContainerTarget, new SpringSpec<Color>(spring.DampingRatio, spring.Stiffness));
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContainerColorAnimationProperty) UpdateAppearance();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (TemplatedParent is not AppBarWithSearch) return default;
        if (Owner.Background is not null and not ISolidColorBrush) throw new ArgumentException("AppBarWithSearch.Background must be a solid color brush or null.");
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        double Round(double value) => MathUtils.DipToPx((float)value, scale) / (double)scale;
        _insets = Owner.GetValue(MaterialWindowInsets.UnconsumedInsetsProperty);
        Surface.Measure(availableSize.Deflate(_insets));
        _unscrolled = Surface.DesiredSize.Inflate(_insets);
        if (double.IsFinite(availableSize.Width)) _unscrolled = _unscrolled.WithWidth(availableSize.Width);
        if (_behavior is not null) _behavior.State.ScrollOffsetLimit = -(float)_unscrolled.Height;
        ConfigureActionColors(); UpdateAppearance();
        var offset = Round(_behavior?.State.ScrollOffset ?? 0);
        return new(_unscrolled.Width, Math.Max(0, _unscrolled.Height + offset));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        if (TemplatedParent is not AppBarWithSearch) return finalSize;
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var offset = MathUtils.DipToPx(_behavior?.State.ScrollOffset ?? 0, scale) / (double)scale;
        Surface.Arrange(new Rect(_insets.Left, _insets.Top + offset, Math.Max(0, finalSize.Width - _insets.Left - _insets.Right), Math.Max(0, _unscrolled.Height - _insets.Top - _insets.Bottom)));
        return finalSize;
    }
    private void ConfigureActionColors()
    {
        // Direct items inherit from ItemsControl, bypassing ItemsPresenter's color environment.
        var controls = ((ItemsPresenter)Row.Children[2]).Panel?.Children.ToArray() ?? [];
        foreach (var removed in _actionDefaults.Keys.Except(controls).ToArray()) { removed.Styles.Remove(_actionDefaults[removed].Style); _actionDefaults.Remove(removed); }
        foreach (var control in controls)
        {
            var color = Owner.ActionIconContentColor;
            if (_actionDefaults.TryGetValue(control, out var previous)) { if (previous.Color == color) continue; control.Styles.Remove(previous.Style); }
            var style = new Style { Setters = { new Setter(TextElement.ForegroundProperty, new SolidColorBrush(color)) } };
            control.Styles.Insert(0, style); _actionDefaults[control] = (color, style);
        }
    }
}

internal sealed class AppBarWithSearchRow : Panel
{
    public static Avalonia.Data.Converters.IValueConverter Brush { get; } = new Avalonia.Data.Converters.FuncValueConverter<Color, IBrush>(color => new SolidColorBrush(color));

    public static readonly StyledProperty<double> SearchHorizontalPaddingProperty = AvaloniaProperty.Register<AppBarWithSearchRow, double>(nameof(SearchHorizontalPadding));
    public static readonly StyledProperty<double> SearchVerticalPaddingProperty = AvaloniaProperty.Register<AppBarWithSearchRow, double>(nameof(SearchVerticalPadding));
    public static readonly StyledProperty<double> IconEdgePaddingProperty = AvaloniaProperty.Register<AppBarWithSearchRow, double>(nameof(IconEdgePadding));
    public static readonly StyledProperty<double> SearchMinimumWidthProperty = AvaloniaProperty.Register<AppBarWithSearchRow, double>(nameof(SearchMinimumWidth));
    public static readonly StyledProperty<double> SearchMaximumWidthProperty = AvaloniaProperty.Register<AppBarWithSearchRow, double>(nameof(SearchMaximumWidth));
    public double SearchHorizontalPadding { get => GetValue(SearchHorizontalPaddingProperty); set => SetValue(SearchHorizontalPaddingProperty, value); }
    public double SearchVerticalPadding { get => GetValue(SearchVerticalPaddingProperty); set => SetValue(SearchVerticalPaddingProperty, value); }
    public double IconEdgePadding { get => GetValue(IconEdgePaddingProperty); set => SetValue(IconEdgePaddingProperty, value); }
    public double SearchMinimumWidth { get => GetValue(SearchMinimumWidthProperty); set => SetValue(SearchMinimumWidthProperty, value); }
    public double SearchMaximumWidth { get => GetValue(SearchMaximumWidthProperty); set => SetValue(SearchMaximumWidthProperty, value); }
    private double _navWidth, _actionsWidth;
    protected override bool BypassFlowDirectionPolicies => true;
    static AppBarWithSearchRow() => AffectsMeasure<AppBarWithSearchRow>(SearchHorizontalPaddingProperty, SearchVerticalPaddingProperty, IconEdgePaddingProperty, SearchMinimumWidthProperty, SearchMaximumWidthProperty);
    protected override Size MeasureOverride(Size availableSize)
    {
        if (TemplatedParent is not AppBarWithSearch owner) return default;
        var nav = Children[0]; var bar = (SearchBar)Children[1]; var actions = Children[2];
        var hasNav = owner.NavigationIcon is not null || owner.NavigationIconTemplate is not null;
        var hasActions = owner.ItemCount > 0;
        nav.Measure(new Size(Math.Max(0, availableSize.Width - (hasNav ? IconEdgePadding : 0)), availableSize.Height));
        _navWidth = nav.DesiredSize.Width + (hasNav ? IconEdgePadding : 0);
        actions.Measure(new Size(Math.Max(0, availableSize.Width - _navWidth - (hasActions ? IconEdgePadding : 0)), availableSize.Height));
        _actionsWidth = actions.DesiredSize.Width + (hasActions ? IconEdgePadding : 0);
        var remaining = Math.Max(0, availableSize.Width - _navWidth - _actionsWidth - 2 * SearchHorizontalPadding);
        bar.SetCurrentValue(MinWidthProperty, Math.Min(SearchMinimumWidth, remaining));
        bar.SetCurrentValue(MaxWidthProperty, Math.Min(SearchMaximumWidth, remaining));
        bar.Measure(new Size(remaining, Math.Max(0, availableSize.Height - 2 * SearchVerticalPadding)));
        return new(double.IsFinite(availableSize.Width) ? availableSize.Width : _navWidth + _actionsWidth + bar.DesiredSize.Width + 2 * SearchHorizontalPadding,
            Math.Min(availableSize.Height, Math.Max(Math.Max(nav.DesiredSize.Height, actions.DesiredSize.Height), bar.DesiredSize.Height + 2 * SearchVerticalPadding)));
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        double Center(double value) => MathUtils.RoundToInt((float)(value * scale / 2)) / (double)scale;
        var rtl = FlowDirection == FlowDirection.RightToLeft;
        void Place(Control child, double x, double y, double width, double height) => child.Arrange(new Rect(rtl ? finalSize.Width - x - width : x, y, width, height));
        var nav = Children[0]; var bar = Children[1]; var actions = Children[2];
        Place(nav, _navWidth - nav.DesiredSize.Width, Center(finalSize.Height - nav.DesiredSize.Height), nav.DesiredSize.Width, nav.DesiredSize.Height);
        Place(actions, finalSize.Width - _actionsWidth, Center(finalSize.Height - actions.DesiredSize.Height), actions.DesiredSize.Width, actions.DesiredSize.Height);
        var remaining = Math.Max(0, finalSize.Width - _navWidth - _actionsWidth);
        Place(bar, _navWidth + Center(remaining - bar.DesiredSize.Width), Center(finalSize.Height - bar.DesiredSize.Height), bar.DesiredSize.Width, bar.DesiredSize.Height);
        return finalSize;
    }
}

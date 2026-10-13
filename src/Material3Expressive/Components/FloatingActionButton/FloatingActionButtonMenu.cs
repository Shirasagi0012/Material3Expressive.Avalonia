using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Presenters;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using MotionSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

public enum FloatingActionButtonMenuAlignment { Start, Center, End }

/// <summary>Inline FAB menu. Items stay in source order; expansion reveals them from the bottom.</summary>
public class FloatingActionButtonMenu : ItemsControl
{
    private ContentPresenter? _buttonPresenter;
    public static readonly StyledProperty<bool> ExpandedProperty = AvaloniaProperty.Register<FloatingActionButtonMenu, bool>(nameof(Expanded));
    public static readonly StyledProperty<object?> ButtonProperty = AvaloniaProperty.Register<FloatingActionButtonMenu, object?>(nameof(Button));
    public static readonly StyledProperty<IDataTemplate?> ButtonTemplateProperty = AvaloniaProperty.Register<FloatingActionButtonMenu, IDataTemplate?>(nameof(ButtonTemplate));
    public static readonly StyledProperty<FloatingActionButtonMenuAlignment> MenuAlignmentProperty = AvaloniaProperty.Register<FloatingActionButtonMenu, FloatingActionButtonMenuAlignment>(nameof(MenuAlignment));
    public bool Expanded { get => GetValue(ExpandedProperty); set => SetValue(ExpandedProperty, value); }
    public object? Button { get => GetValue(ButtonProperty); set => SetValue(ButtonProperty, value); }
    public IDataTemplate? ButtonTemplate { get => GetValue(ButtonTemplateProperty); set => SetValue(ButtonTemplateProperty, value); }
    public FloatingActionButtonMenuAlignment MenuAlignment { get => GetValue(MenuAlignmentProperty); set => SetValue(MenuAlignmentProperty, value); }
    public FloatingActionButtonMenu()
    {
        AddHandler(KeyDownEvent, (_, e) =>
        {
            if (!Expanded || e.Source is not Visual source || _buttonPresenter is not { } button ||
                !button.IsVisualAncestorOf(source)) return;
            if ((e.Key == Key.Tab && !e.KeyModifiers.HasFlag(KeyModifiers.Shift)) || e.Key == Key.Down)
            {
                var first = this.GetVisualDescendants().OfType<FloatingActionButtonMenuItem>().FirstOrDefault(item => item.RevealAlpha != 0);
                if (first?.Focus(NavigationMethod.Tab) == true) e.Handled = true;
            }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
    }
    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _buttonPresenter = e.NameScope.Find<ContentPresenter>("PART_Button");
    }
}

public class FloatingActionButtonMenuItem : Button
{
    public static readonly StyledProperty<object?> IconProperty = AvaloniaProperty.Register<FloatingActionButtonMenuItem, object?>(nameof(Icon));
    public static readonly StyledProperty<IDataTemplate?> IconTemplateProperty = AvaloniaProperty.Register<FloatingActionButtonMenuItem, IDataTemplate?>(nameof(IconTemplate));
    public object? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public IDataTemplate? IconTemplate { get => GetValue(IconTemplateProperty); set => SetValue(IconTemplateProperty, value); }
    internal static readonly StyledProperty<MotionSpring?> RevealWidthMotionProperty = AvaloniaProperty.Register<FloatingActionButtonMenuItem, MotionSpring?>(nameof(RevealWidthMotion));
    internal MotionSpring? RevealWidthMotion { get => GetValue(RevealWidthMotionProperty); set => SetValue(RevealWidthMotionProperty, value); }
    internal static readonly StyledProperty<MotionSpring?> RevealAlphaMotionProperty = AvaloniaProperty.Register<FloatingActionButtonMenuItem, MotionSpring?>(nameof(RevealAlphaMotion));
    internal MotionSpring? RevealAlphaMotion { get => GetValue(RevealAlphaMotionProperty); set => SetValue(RevealAlphaMotionProperty, value); }
    internal float RevealWidth => _width?.Value ?? (_visible ? 1 : 0);
    internal float RevealAlpha => _alpha?.Value ?? (_visible ? 1 : 0);
    internal FloatingActionButtonMenuAlignment MenuAlignment { get; private set; }
    private bool _visible, _revealDeferred;
    private Animatable<float>? _width, _alpha;
    private FabMenuItemLayout? _layout;
    private AnimationChildScope? _attachScope;
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attachScope = AnimationScope.Get(this).CreateChild("FAB menu item");
    }
    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        _width = _alpha = null; _revealDeferred = false;
        base.OnApplyTemplate(e);
        _layout = e.NameScope.Find<FabMenuItemLayout>("PART_ContentLayout");
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (_revealDeferred && (change.Property == RevealWidthMotionProperty || change.Property == RevealAlphaMotionProperty))
        {
            InvalidateReveal();
            // A zero-width item can remeasure to the same size, so its parent need
            // not remeasure automatically. Revisit the current ruler in its owner.
            this.FindAncestorOfType<FabMenuLayout>()?.InvalidateMeasure();
        }
    }
    private void InvalidateReveal()
    {
        // The changed animation value is consumed inside Surface, so invalidate that
        // measuring part rather than only the owner's cached DesiredSize.
        _layout?.InvalidateMeasure(); InvalidateMeasure();
    }
    internal void Reveal(bool visible, FloatingActionButtonMenuAlignment alignment, bool initialize, bool invalidateInitial = true)
    {
        MenuAlignment = alignment;
        if (_width is null && !initialize) return;
        if (_width is null && this.IsAttachedToVisualTree() && _attachScope is { } motion
            && !motion.Lifetime.IsCancellationRequested)
        {
            _visible = visible;
            _width = new Animatable<float>(new FloatConverter(), _visible ? 1f : 0f, VisibilityThresholds.Float); _alpha = new Animatable<float>(new FloatConverter(), _visible ? 1f : 0f, VisibilityThresholds.Float);
            var initializing = true;
            motion.SubscribeValue(_width, _ => { if (!initializing || invalidateInitial) InvalidateReveal(); });
            motion.SubscribeValue(_alpha, _ =>
            {
                if (!initializing || invalidateInitial) InvalidateReveal();
                // A larger stationary item can keep the parent's layout size unchanged.
                // Alpha belongs to this item's layer, independently of parent arrange.
                if (!initializing) Opacity = Math.Clamp(RevealAlpha, 0, 1);
                IsHitTestVisible = Focusable = RevealAlpha != 0;
            });
            initializing = false;
            IsHitTestVisible = Focusable = _visible;
            if (!invalidateInitial)
            {
                // Placement initializes the value after measurement upstream. Apply it
                // to the following layout frame, preserving the first zero-width pass.
                _ = motion.Launch(async ct => { await motion.NextFrameAsync(ct); InvalidateReveal(); }, "FAB menu initial placement");
            }
        }
        if (_visible == visible)
        {
            _revealDeferred = false;
            return;
        }
        if (!this.IsAttachedToVisualTree() || _width is null || _attachScope is not { } scope
            || scope.Lifetime.IsCancellationRequested) return;
        if (!FabMotion.TrySpring(RevealWidthMotion, out var widthSpec) ||
            !FabMotion.TrySpring(RevealAlphaMotion, out var alphaSpec))
        {
            _revealDeferred = true;
            return;
        }
        _revealDeferred = false;
        _visible = visible;
        _ = scope.Launch(async ct =>
        {
            await Task.WhenAll(
                _width!.AnimateToAsync(scope, _visible ? 1 : 0, widthSpec, cancellationToken: ct),
                _alpha!.AnimateToAsync(scope, _visible ? 1 : 0, alphaSpec, cancellationToken: ct));
        }, "FAB menu item reveal");
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _attachScope?.Dispose(); _attachScope = null;
        _width = _alpha = null; _revealDeferred = false;
        base.OnDetachedFromVisualTree(e);
    }
}

internal sealed class FabMenuItemLayout : Panel
{
    public static readonly StyledProperty<double> MinimumSizeProperty = AvaloniaProperty.Register<FabMenuItemLayout, double>(nameof(MinimumSize));
    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<FabMenuItemLayout, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<double> IconLabelSpaceProperty = AvaloniaProperty.Register<FabMenuItemLayout, double>(nameof(IconLabelSpace));
    public double MinimumSize { get => GetValue(MinimumSizeProperty); set => SetValue(MinimumSizeProperty, value); }
    public Thickness ContentPadding { get => GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    public double IconLabelSpace { get => GetValue(IconLabelSpaceProperty); set => SetValue(IconLabelSpaceProperty, value); }
    static FabMenuItemLayout() => AffectsMeasure<FabMenuItemLayout>(MinimumSizeProperty, ContentPaddingProperty, IconLabelSpaceProperty);
    private double _fullWidth, _iconWidth, _labelWidth;
    private FloatingActionButtonMenuItem? Owner => this.FindAncestorOfType<FloatingActionButtonMenuItem>();
    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count < 2) return default;
        var padding = ContentPadding.Left + ContentPadding.Right;
        Children[0].Measure(new Size(Math.Max(0, available.Width - padding), available.Height));
        _iconWidth = Children[0].DesiredSize.Width;
        // The icon slot exists even when empty, just as the composable always calls icon().
        Children[1].Measure(new Size(Math.Max(0, available.Width - padding - IconLabelSpace - _iconWidth), available.Height));
        _labelWidth = Children[1].DesiredSize.Width;
        _fullWidth = Math.Min(Math.Max(MinimumSize, padding + _iconWidth + IconLabelSpace + _labelWidth), available.Width);
        var scale = LayoutHelper.GetLayoutScale(this);
        var width = MathUtils.PxToDip(MathUtils.RoundToInt((float)(_fullWidth * scale) * Math.Max(Owner?.RevealWidth ?? 0, 0)), scale);
        return new Size(width, Math.Min(Math.Max(MinimumSize, Math.Max(Children[0].DesiredSize.Height, Children[1].DesiredSize.Height)), available.Height));
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (Children.Count < 2) return final;
        var owner = Owner;
        var offset = FabMenuLayout.Align(_fullWidth, final.Width, owner?.MenuAlignment ?? FloatingActionButtonMenuAlignment.End);
        var start = ContentPadding.Left + (_fullWidth - ContentPadding.Left - ContentPadding.Right - _iconWidth - IconLabelSpace - _labelWidth) / 2;
        Children[0].Arrange(new Rect(offset + start, (final.Height - Children[0].DesiredSize.Height) / 2, _iconWidth, Children[0].DesiredSize.Height));
        Children[1].Arrange(new Rect(offset + start + _iconWidth + IconLabelSpace, (final.Height - Children[1].DesiredSize.Height) / 2, _labelWidth, Children[1].DesiredSize.Height));
        return final;
    }
}

internal sealed class FabMenuLayout : Panel
{
    public static readonly StyledProperty<double> HorizontalPaddingProperty = AvaloniaProperty.Register<FabMenuLayout, double>(nameof(HorizontalPadding));
    public static readonly StyledProperty<double> ButtonBottomPaddingProperty = AvaloniaProperty.Register<FabMenuLayout, double>(nameof(ButtonBottomPadding));
    public static readonly StyledProperty<double> ButtonBetweenSpaceProperty = AvaloniaProperty.Register<FabMenuLayout, double>(nameof(ButtonBetweenSpace));
    public double HorizontalPadding { get => GetValue(HorizontalPaddingProperty); set => SetValue(HorizontalPaddingProperty, value); }
    public double ButtonBottomPadding { get => GetValue(ButtonBottomPaddingProperty); set => SetValue(ButtonBottomPaddingProperty, value); }
    public double ButtonBetweenSpace { get => GetValue(ButtonBetweenSpaceProperty); set => SetValue(ButtonBetweenSpaceProperty, value); }
    static FabMenuLayout() => AffectsMeasure<FabMenuLayout>(HorizontalPaddingProperty, ButtonBottomPaddingProperty, ButtonBetweenSpaceProperty, StaggerMotionProperty);
    public static readonly StyledProperty<MotionSpring?> StaggerMotionProperty = AvaloniaProperty.Register<FabMenuLayout, MotionSpring?>(nameof(StaggerMotion));
    public MotionSpring? StaggerMotion { get => GetValue(StaggerMotionProperty); set => SetValue(StaggerMotionProperty, value); }
    private FloatingActionButtonMenu? _owner;
    private AnimationChildScope? _attachScope;
    private Animatable<int>? _stagger;
    private bool _pending;
    private double _itemsWidth, _itemsHeight, _buttonWidth, _buttonHeight;
    internal int VisibleCount => _stagger?.Value ?? (_owner?.Expanded == true ? Items.Length : 0);
    private FloatingActionButtonMenuItem[] Items => Children.Count == 0 ? [] : Children[0].GetVisualDescendants().OfType<FloatingActionButtonMenuItem>().ToArray();
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _attachScope = AnimationScope.Get(this).CreateChild("FAB menu");
        _owner = this.FindAncestorOfType<FloatingActionButtonMenu>()!;
        _owner.PropertyChanged += OwnerChanged;
        _owner.Items.CollectionChanged += ItemsChanged;
    }
    private void ItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) => UpdateTarget();
    private void OwnerChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == FloatingActionButtonMenu.ExpandedProperty) UpdateTarget();
        InvalidateMeasure();
    }
    private void UpdateTarget() => InvalidateMeasure();
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) { _owner.PropertyChanged -= OwnerChanged; _owner.Items.CollectionChanged -= ItemsChanged; }
        _attachScope?.Dispose(); _attachScope = null;
        _stagger = null; _pending = false;
        base.OnDetachedFromVisualTree(e);
    }
    protected override Size MeasureOverride(Size available)
    {
        if (Children.Count < 2) return default;
        // ItemsPresenter creates its realized children during its first measure. Only then
        // is the initial item-count target known, like Layout's measurable list upstream.
        Children[0].Measure(new Size(Math.Max(0, available.Width - HorizontalPadding * 2), double.PositiveInfinity));
        var items = Items;
        var initializing = _stagger is null;
        if (_stagger is null && this.IsAttachedToVisualTree() && _attachScope is { } scope
            && !scope.Lifetime.IsCancellationRequested)
        {
            _stagger = new Animatable<int>(new Int32Converter(), _owner?.Expanded == true ? items.Length : 0, VisibilityThresholds.Int32);
            scope.SubscribeValue(_stagger, _ => InvalidateMeasure());
        }
        var targetCount = _owner?.Expanded == true ? items.Length : 0;
        if (_stagger is not null && _stagger.TargetValue != targetCount && !_pending &&
            FabMotion.IsReady(StaggerMotion) && this.IsAttachedToVisualTree()
            && _attachScope is { } active && !active.Lifetime.IsCancellationRequested)
        {
            _pending = true;
            _ = active.Launch(async ct =>
            {
                await active.NextFrameAsync(ct); _pending = false;
                if (_stagger is null) return;
                if (!FabMotion.TrySpring(StaggerMotion, out var spec, 1)) return;
                await _stagger.AnimateToAsync(active, _owner?.Expanded == true ? Items.Length : 0,
                    new SpringSpec<int>(spec.DampingRatio, spec.Stiffness, 1), cancellationToken: ct);
            }, "FAB menu stagger count");
        }
        for (var i = 0; i < items.Length; i++)
        {
            items[i].Reveal(i >= items.Length - VisibleCount, _owner?.MenuAlignment ?? FloatingActionButtonMenuAlignment.End, initializing);
            var logicalEnd = _owner?.MenuAlignment == FloatingActionButtonMenuAlignment.End;
            items[i].SetCurrentValue(HorizontalAlignmentProperty, _owner?.MenuAlignment == FloatingActionButtonMenuAlignment.Center ? HorizontalAlignment.Center
                : logicalEnd ? HorizontalAlignment.Right : HorizontalAlignment.Left);
        }
        Children[0].Measure(new Size(Math.Max(0, available.Width - HorizontalPadding * 2), double.PositiveInfinity));
        Children[1].Measure(new Size(Math.Max(0, available.Width - HorizontalPadding * 2), available.Height));
        _itemsWidth = Children[0].DesiredSize.Width;
        _buttonWidth = Children[1].DesiredSize.Width; _buttonHeight = Children[1].DesiredSize.Height;
        _itemsHeight = items.Any(item => item.RevealAlpha != 0) ? Children[0].DesiredSize.Height + (_buttonHeight > 0 ? _buttonHeight + ButtonBottomPadding + ButtonBetweenSpace : 0) : 0;
        return new Size(Math.Min(Math.Max(_itemsWidth, _buttonWidth) + HorizontalPadding * 2, available.Width), Math.Min(Math.Max(_itemsHeight, _buttonHeight + ButtonBottomPadding), available.Height));
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (Children.Count < 2) return final;
        var alignment = _owner?.MenuAlignment ?? FloatingActionButtonMenuAlignment.End;
        if (Children[0] is FabMenuViewport viewport)
        {
            viewport.ScrollingEnabled = _owner?.Expanded == true;
            viewport.Padding = new Thickness(0, 0, 0, _itemsHeight > 0 && _buttonHeight > 0 ? _buttonHeight + ButtonBottomPadding + ButtonBetweenSpace : 0);
        }
        Children[0].Arrange(new Rect(HorizontalPadding + Align(_itemsWidth, final.Width - HorizontalPadding * 2, alignment), 0, _itemsWidth, final.Height));
        var items = Items;
        for (var i = 0; i < items.Length; i++)
        {
            // Upstream initializes a newly inserted item's Animatables in its layout
            // placement block, using the current ruler/count. Its first measured width
            // is still zero; the following measure consumes the initialized value.
            items[i].Reveal(i >= items.Length - VisibleCount, alignment, initialize: !_pending, invalidateInitial: false);
            items[i].Opacity = Math.Clamp(items[i].RevealAlpha, 0, 1);
        }
        Children[1].Arrange(new Rect(HorizontalPadding + Align(_buttonWidth, final.Width - HorizontalPadding * 2, alignment), final.Height - _buttonHeight - ButtonBottomPadding, _buttonWidth, _buttonHeight));
        return final;
    }
    internal static double Align(double child, double parent, FloatingActionButtonMenuAlignment alignment) => alignment switch
    {
        FloatingActionButtonMenuAlignment.Center => (parent - child) / 2,
        FloatingActionButtonMenuAlignment.Start => 0,
        _ => parent - child
    };
}

internal sealed class FabMenuViewport : ScrollViewer
{
    internal bool ScrollingEnabled { get; set; }
    public FabMenuViewport()
    {
        ClipToBounds = true;
        RenderOptions.SetRequiresFullOpacityHandling(this, true);
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden;
        Template = new FuncControlTemplate<FabMenuViewport>((owner, names) =>
        {
            var presenter = new ScrollContentPresenter { Name = "PART_ContentPresenter" };
            names.Register(presenter.Name, presenter);
            presenter.Bind(PaddingProperty, new Avalonia.Data.TemplateBinding(PaddingProperty));
            presenter.GestureRecognizers.Add(new ScrollGestureRecognizer { CanVerticallyScroll = true });
            // Keep the infinite-height measurement while disabling input during collapse,
            // as verticalScroll(enabled = expanded) does upstream.
            presenter.AddHandler(PointerWheelChangedEvent, (_, e) => { if (!ScrollingEnabled) e.Handled = true; }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            presenter.AddHandler(ScrollGestureEvent, (_, e) => { if (!ScrollingEnabled) e.Handled = true; }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            return presenter;
        });
    }
    protected override Size MeasureOverride(Size available)
    {
        // The menu computes the original column height separately from the viewport.
        // Exclude its trailing button space here; ScrollContentPresenter includes it
        // in Extent when arranged with the final viewport height.
        var result = base.MeasureOverride(available);
        return new Size(result.Width, Math.Max(0, result.Height - Padding.Bottom));
    }
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ScrollingEnabled) base.OnKeyDown(e);
    }
}

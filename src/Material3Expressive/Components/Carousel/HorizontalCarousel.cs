using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation.Input;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>A controlled horizontal keyline carousel with multi-browse, uncontained and centered hero arrangements.</summary>
public partial class HorizontalCarousel : ItemsControl
{
    public static readonly StyledProperty<CarouselVariant> VariantProperty = AvaloniaProperty.Register<HorizontalCarousel, CarouselVariant>(nameof(Variant));
    public static readonly StyledProperty<double> ItemWidthProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(ItemWidth), double.NaN,
        validate: value => double.IsNaN(value) || double.IsFinite(value) && value > 0);
    public static readonly StyledProperty<double> ItemSpacingProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(ItemSpacing), validate: NonNegative);
    public static readonly StyledProperty<double> MinSmallItemWidthProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(MinSmallItemWidth), 40, validate: Positive);
    public static readonly StyledProperty<double> MaxSmallItemWidthProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(MaxSmallItemWidth), 56, validate: Positive);
    public static readonly StyledProperty<Thickness> ContentPaddingProperty = AvaloniaProperty.Register<HorizontalCarousel, Thickness>(nameof(ContentPadding));
    public static readonly StyledProperty<double> ScrollOffsetProperty = AvaloniaProperty.Register<HorizontalCarousel, double>(nameof(ScrollOffset), validate: double.IsFinite);
    public static readonly StyledProperty<CarouselFlingMode> FlingModeProperty = AvaloniaProperty.Register<HorizontalCarousel, CarouselFlingMode>(nameof(FlingMode));
    public static readonly StyledProperty<IAnimationSpec<float>?> SnapAnimationSpecProperty = AvaloniaProperty.Register<HorizontalCarousel, IAnimationSpec<float>?>(nameof(SnapAnimationSpec));
    public static readonly StyledProperty<IDecayAnimationSpec<float>?> DecayAnimationSpecProperty = AvaloniaProperty.Register<HorizontalCarousel, IDecayAnimationSpec<float>?>(nameof(DecayAnimationSpec));
    public static readonly DirectProperty<HorizontalCarousel, double> MaximumScrollOffsetProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, double>(nameof(MaximumScrollOffset), o => o.MaximumScrollOffset);
    public static readonly DirectProperty<HorizontalCarousel, int> CurrentItemProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, int>(nameof(CurrentItem), o => o.CurrentItem);
    public static readonly DirectProperty<HorizontalCarousel, double> CurrentItemOffsetFractionProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, double>(nameof(CurrentItemOffsetFraction), o => o.CurrentItemOffsetFraction);
    public static readonly DirectProperty<HorizontalCarousel, bool> IsScrollInProgressProperty = AvaloniaProperty.RegisterDirect<HorizontalCarousel, bool>(nameof(IsScrollInProgress), o => o.IsScrollInProgress);
    private static bool NonNegative(double value) => double.IsFinite(value) && value >= 0;
    private static bool Positive(double value) => double.IsFinite(value) && value > 0;

    public CarouselVariant Variant { get => GetValue(VariantProperty); set => SetValue(VariantProperty, value); }
    /// <summary>Preferred width for MultiBrowse, item width for Uncontained, maximum width for CenteredHero. NaN fills available space.</summary>
    public double ItemWidth { get => GetValue(ItemWidthProperty); set => SetValue(ItemWidthProperty, value); }
    public double ItemSpacing { get => GetValue(ItemSpacingProperty); set => SetValue(ItemSpacingProperty, value); }
    public double MinSmallItemWidth { get => GetValue(MinSmallItemWidthProperty); set => SetValue(MinSmallItemWidthProperty, value); }
    public double MaxSmallItemWidth { get => GetValue(MaxSmallItemWidthProperty); set => SetValue(MaxSmallItemWidthProperty, value); }
    /// <summary>Logical start/top/end/bottom padding. Main-axis padding is applied after masking.</summary>
    public Thickness ContentPadding { get => GetValue(ContentPaddingProperty); set => SetValue(ContentPaddingProperty, value); }
    /// <summary>Requested logical scroll distance in DIP. Layout clamps to the current content extent.</summary>
    public double ScrollOffset { get => GetValue(ScrollOffsetProperty); set => SetValue(ScrollOffsetProperty, value); }
    public CarouselFlingMode FlingMode { get => GetValue(FlingModeProperty); set => SetValue(FlingModeProperty, value); }
    /// <summary>Optional fling snap spec; values and velocities use physical pixels as in Compose.</summary>
    public IAnimationSpec<float>? SnapAnimationSpec { get => GetValue(SnapAnimationSpecProperty); set => SetValue(SnapAnimationSpecProperty, value); }
    public IDecayAnimationSpec<float>? DecayAnimationSpec { get => GetValue(DecayAnimationSpecProperty); set => SetValue(DecayAnimationSpecProperty, value); }
    public double MaximumScrollOffset => _maximum;
    public int CurrentItem => _current;
    public double CurrentItemOffsetFraction => _fraction;
    public bool IsScrollInProgress => _scrolling;
    private double _maximum, _fraction;
    private int _current;
    private bool _scrolling;
    private CarouselPanel? _panel;
    private AnimationChildScope? _animationScope;
    private CancellationTokenSource? _scrollCancellation;
    private readonly CarouselScrollPosition _scrollPosition = new();
    private CarouselPosition? _pendingPosition;

    /// <summary>Captures the current logical item and its offset fraction, independently of viewport size.</summary>
    public CarouselPosition CapturePosition() => _pendingPosition ?? new(CurrentItem, (float)CurrentItemOffsetFraction);

    /// <summary>Restores a logical position at the next valid layout. May be called before mounting or
    /// before items arrive. A nonempty collection clamps the index to its current size.</summary>
    public void RestorePosition(CarouselPosition position)
    {
        VerifyAccess();
        CancelPointer(false);
        CancelScroll();
        _pendingPosition = position;
        _panel?.InvalidateMeasure();
    }

    internal bool TryTakeRestoredPosition(out CarouselPosition position)
    {
        if (_pendingPosition is not { } pending) { position = default; return false; }
        position = pending; _pendingPosition = null; return true;
    }

    protected override Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new CarouselItem();
    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey) => NeedsContainer<CarouselItem>(item, out recycleKey);
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        CancelPointer(false);
        CancelScroll();
        _panel = null;
        base.OnApplyTemplate(e);
    }
    internal void SetPanel(CarouselPanel panel) => _panel = panel;
    internal void RebaseAfterMeasure(double offset, float density)
    {
        if (_scrollCancellation is not null) _scrollPosition.Rebase(offset, density);
        else SetCurrentValue(ScrollOffsetProperty, offset);
    }
    internal void Publish(double maximum, int current, double fraction)
    {
        SetAndRaise(MaximumScrollOffsetProperty, ref _maximum, maximum);
        SetAndRaise(CurrentItemProperty, ref _current, current);
        SetAndRaise(CurrentItemOffsetFractionProperty, ref _fraction, fraction);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ScrollOffsetProperty) { _pendingPosition = null; _panel?.InvalidateArrange(); }
        else if (change.Property == VariantProperty || change.Property == ItemWidthProperty || change.Property == ItemSpacingProperty ||
            change.Property == MinSmallItemWidthProperty || change.Property == MaxSmallItemWidthProperty ||
            change.Property == ContentPaddingProperty || change.Property == FlowDirectionProperty)
            _panel?.InvalidateMeasure();
        if (change.Property == IsEffectivelyEnabledProperty || change.Property == UserScrollEnabledProperty || change.Property == FlowDirectionProperty)
            CancelPointer(true, afterConfigurationCommit: true);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _focusedChild = null;
        CancelPointer(false);
        CancelScroll();
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>Immediately brings an item to its focal position. Indices are clamped to the current collection.</summary>
    public void ScrollToItem(int index)
    {
        VerifyAccess();
        CancelPointer(false);
        CancelScroll();
        _pendingPosition = null;
        var panel = RequirePanel();
        SetCurrentValue(ScrollOffsetProperty, panel.OffsetForItem(index));
    }

    /// <summary>Animates to the item's focal position using Compose's default float spring unless a spec is supplied.</summary>
    public Task AnimateScrollToItemAsync(int index, IAnimationSpec<float>? animationSpec = null, CancellationToken cancellationToken = default)
        => AnimateScroll(index, animationSpec, cancellationToken, usePagerDistance: false);

    private Task AnimateScroll(int index, IAnimationSpec<float>? animationSpec, CancellationToken cancellationToken, bool usePagerDistance)
    {
        VerifyAccess();
        cancellationToken.ThrowIfCancellationRequested();
        var panel = RequirePanel();
        CancelPointer(false);
        CancelScroll();
        _pendingPosition = null;
        index = Math.Clamp(index, 0, Math.Max(0, ItemCount - 1));
        if (panel.PreJumpPosition(index, ScrollOffset) is { } preJump)
        {
            SetCurrentValue(ScrollOffsetProperty, panel.OffsetForItem(preJump));
            panel.InvalidateArrange();
        }
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        var initial = (float)Math.Clamp(ScrollOffset, 0, MaximumScrollOffset) * density;
        var displacement = panel.ScrollDistanceToItem(index, initial, usePagerDistance);
        if (displacement == 0) return Task.CompletedTask;
        _scrollPosition.Rebase(initial / density, density);
        var scope = AnimationScope.Get(this).CreateChild("Carousel programmatic scroll");
        _animationScope = scope;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scrollCancellation = cancellation;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
        return Run();

        async Task Run()
        {
            try
            {
                // A presentation contribution preserves the underlying binding while the scroll runs.
                using var binding = scope.BindAnimatedValue(this, ScrollOffsetProperty, _scrollPosition);
                var previous = 0f;
                await SuspendAnimation.AnimateAsync(scope, new FloatConverter(), 0, displacement,
                    animationSpec: animationSpec ?? new SpringSpec<float>(), block: (value, _) =>
                    {
                        var consumed = _scrollPosition.Consume(value - previous, (float)MaximumScrollOffset * density, panel);
                        previous += consumed;
                    }, cancellationToken: cancellation.Token);
            }
            finally
            {
                if (ReferenceEquals(_scrollCancellation, cancellation))
                {
                    SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
                    _scrollCancellation = null;
                    _animationScope = null;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
                }
                cancellation.Dispose();
                scope.Dispose();
            }
        }
    }

    private CarouselPanel RequirePanel() => _panel is { Strategy.IsValid: true } panel ? panel
        : throw new InvalidOperationException("Lay out the carousel at a finite width before scrolling to an item.");
    private void CancelScroll()
    {
        _bringRequests.Clear();
        _bringingIntoView = false;
        _trackingFocusedChild = false;
        _pendingSettle?.Dispose();
        _pendingSettle = null;
        var offset = ScrollOffset;
        var cancellation = _scrollCancellation;
        _scrollCancellation = null;
        cancellation?.Cancel();
        _animationScope?.Dispose();
        _animationScope = null;
        if (cancellation is not null) SetCurrentValue(ScrollOffsetProperty, offset);
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
    }

    /// <summary>Runs the configured fling with a logical velocity in DIP/s and returns unconsumed velocity.</summary>
    public Task<double> FlingAsync(double velocity, CancellationToken cancellationToken = default)
    {
        VerifyAccess();
        CancelPointer(false);
        return Fling(velocity, 0, cancellationToken);
    }

    private Task<double> Fling(double velocity, float gestureDelta, CancellationToken cancellationToken)
    {
        VerifyAccess();
        ArgumentOutOfRangeException.ThrowIfNotEqual(double.IsFinite(velocity), true, nameof(velocity));
        cancellationToken.ThrowIfCancellationRequested();
        var panel = RequirePanel();
        CancelScroll();
        _pendingPosition = null;
        var density = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1);
        _scrollPosition.Rebase(Math.Clamp(ScrollOffset, 0, MaximumScrollOffset), density);
        var mode = FlingMode == CarouselFlingMode.Default
            ? Variant == CarouselVariant.Uncontained ? CarouselFlingMode.NoSnap : CarouselFlingMode.SingleAdvance : FlingMode;
        var decay = DecayAnimationSpec ?? ComposeAnimation.Runtime.DecayAnimationSpec.SplineBasedDecay<float>(density);
        var snap = SnapAnimationSpec ?? new SpringSpec<float>(stiffness: 400);
        var scope = AnimationScope.Get(this).CreateChild("Carousel fling");
        _animationScope = scope;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _scrollCancellation = cancellation;
        SetAndRaise(IsScrollInProgressProperty, ref _scrolling, true);
        return Run();

        async Task<double> Run()
        {
            try
            {
                using var binding = scope.BindAnimatedValue(this, ScrollOffsetProperty, _scrollPosition);
                var remaining = await SnapFling.RunAsync(scope, (float)velocity * density, decay, snap,
                    delta => _scrollPosition.Consume(delta, (float)MaximumScrollOffset * density, panel),
                    (speed, decayOffset) => mode == CarouselFlingMode.NoSnap ? decayOffset
                        : panel.ApproachOffset(_scrollPosition.Position, speed, decayOffset, mode),
                    speed => mode == CarouselFlingMode.NoSnap ? 0
                        : panel.SnapOffset(_scrollPosition.Position, speed, gestureDelta, density, FlowDirection), cancellation.Token);
                return remaining / density;
            }
            finally
            {
                if (ReferenceEquals(_scrollCancellation, cancellation))
                {
                    SetCurrentValue(ScrollOffsetProperty, _scrollPosition.Value);
                    _scrollCancellation = null; _animationScope = null;
                    SetAndRaise(IsScrollInProgressProperty, ref _scrolling, false);
                }
                cancellation.Dispose(); scope.Dispose();
            }
        }
    }
}

/// <summary>A carousel's logical position. Persist this value with the page's application state;
/// the receiving carousel supplies the current collection and layout.</summary>
public readonly record struct CarouselPosition
{
    [System.Text.Json.Serialization.JsonConstructor]
    public CarouselPosition(int item, float offsetFraction = 0)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(item);
        if (!float.IsFinite(offsetFraction) || offsetFraction is < -.5f or > .5f)
            throw new ArgumentOutOfRangeException(nameof(offsetFraction), "The item offset fraction must be between -0.5 and 0.5.");
        Item = item; OffsetFraction = offsetFraction;
    }
    public int Item { get; }
    public float OffsetFraction { get; }
}

/// <summary>Current keyline mask, in DIP. The item's content retains its full layout size.</summary>
public readonly record struct CarouselItemDrawInfo(double Size, double MinSize, double MaxSize, Rect MaskRect);

/// <summary>A carousel item with an optional shape mask and border.</summary>
public class CarouselItem : ContentControl
{
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<CarouselItem>();
    /// <summary>Optional logical shape. Null uses CornerRadius. The mounted mask remembers its initial shape.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public static readonly DirectProperty<CarouselItem, CarouselItemDrawInfo> DrawInfoProperty =
        AvaloniaProperty.RegisterDirect<CarouselItem, CarouselItemDrawInfo>(nameof(DrawInfo), o => o.DrawInfo);
    private CarouselItemDrawInfo _drawInfo;
    public CarouselItemDrawInfo DrawInfo => _drawInfo;
    private CarouselMask? _mask, _clip;
    private SurfaceOutline? _outline;
    private readonly CarouselMaskShape _borderShape, _maskShape;
    private double _shapeDensity;
    public CarouselItem()
    {
        _borderShape = new(UpdateMask);
        _maskShape = new(UpdateMask);
    }
    internal void UpdateDrawInfo(CarouselItemDrawInfo info)
    {
        SetAndRaise(DrawInfoProperty, ref _drawInfo, info);
        UpdateMask();
    }
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _mask = e.NameScope.Find<CarouselMask>("PART_Mask");
        _clip = e.NameScope.Find<CarouselMask>("PART_Clip");
        _outline = e.NameScope.Find<SurfaceOutline>("PART_Outline");
        _borderShape.Reset();
        _maskShape.Reset();
        UpdateMask();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == CornerRadiusProperty || change.Property == ShapeProperty || change.Property == FlowDirectionProperty ||
            change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty)
            UpdateMask();
    }
    private void UpdateMask()
    {
        if (_mask is null || _outline is null || !this.IsAttachedToVisualTree()) return;
        // carouselItem's outer graphics layer clips both user content and maskBorder
        // to the rectangular keyline bounds. maskClip is the separate rounded inner shape.
        if (_clip is not null) _clip.Clip = DrawInfo.MaskRect == new Rect(Bounds.Size) ? null : new RectangleGeometry(DrawInfo.MaskRect);
        var rect = DrawInfo.MaskRect.Intersect(new Rect(Bounds.Size));
        if (rect.Width < 0 || rect.Height < 0) return;
        var density = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        if (_shapeDensity != density) { _shapeDensity = density; _maskShape.Reset(); _borderShape.Reset(); }
        // CarouselItemScope.rememberMaskShape keys on draw-info identity and density, not shape.
        _maskShape.Configure(Shape, CornerRadius, remember: true);
        _mask.Clip = _maskShape.Geometry(rect, FlowDirection, density);
        if (BorderBrush is null || BorderThickness == default) _borderShape.Reset();
        else _borderShape.Configure(Shape, CornerRadius, remember: true);
        if (_borderShape.HasShape && !BorderThickness.IsUniform)
            throw new InvalidOperationException("MaterialShape borders require uniform BorderThickness.");
        // Generic paths retain the translated mask origin and original layout size, just as
        // Foundation border does. SurfaceOutline's path branch owns the ceil-sized cache.
        _outline.Width = _borderShape.HasShape ? Bounds.Width : rect.Width;
        _outline.Height = _borderShape.HasShape ? Bounds.Height : rect.Height;
        _outline.PathLayoutSize = Bounds.Size;
        _outline.PathBounds = rect;
        _outline.RenderTransform = _borderShape.HasShape ? null : new TranslateTransform(rect.X, rect.Y);
        _outline.ShapePath = _borderShape.HasShape ? _borderShape.Geometry(rect, FlowDirection, density) : null;
        _outline.CornerRadius = _borderShape.Corners;
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateMask();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _maskShape.Reset(); _borderShape.Reset();
        base.OnDetachedFromVisualTree(e);
    }
}

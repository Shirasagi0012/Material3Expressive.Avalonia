using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Material3Expressive.Foundation.Rendering;

namespace Material3Expressive.Components;

/// <summary>Clips and parallaxes content at a scrolling viewport's edges without changing its layout size.</summary>
public class MultiAspectCarouselItem : ContentControl
{
    public static readonly StyledProperty<MaterialShape?> ShapeProperty = MaterialSurface.ShapeProperty.AddOwner<MultiAspectCarouselItem>();
    /// <summary>Optional logical mask shape. Null uses CornerRadius; an active border remembers its initial shape.</summary>
    public MaterialShape? Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }
    public static readonly StyledProperty<Control?> ViewportProperty =
        AvaloniaProperty.Register<MultiAspectCarouselItem, Control?>(nameof(Viewport));
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<MultiAspectCarouselItem, Orientation>(nameof(Orientation), Orientation.Horizontal);
    public static readonly StyledProperty<bool> UseItemCrossAxisSizeProperty =
        AvaloniaProperty.Register<MultiAspectCarouselItem, bool>(nameof(UseItemCrossAxisSize));
    public static readonly StyledProperty<MultiAspectCarouselItemDrawInfo?> DrawInfoProperty =
        AvaloniaProperty.Register<MultiAspectCarouselItem, MultiAspectCarouselItemDrawInfo?>(nameof(DrawInfo));
    public static readonly DirectProperty<MultiAspectCarouselItem, MultiAspectCarouselItemDrawInfo> ActualDrawInfoProperty =
        AvaloniaProperty.RegisterDirect<MultiAspectCarouselItem, MultiAspectCarouselItemDrawInfo>(nameof(ActualDrawInfo), owner => owner.ActualDrawInfo);

    /// <summary>The clipping viewport. When omitted, uses the nearest ScrollViewer's content presenter.</summary>
    public Control? Viewport { get => GetValue(ViewportProperty); set => SetValue(ViewportProperty, value); }
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }
    /// <summary>Use item height/width for the aspect ratio, as in a lazy grid. Lists use the viewport cross axis.</summary>
    public bool UseItemCrossAxisSize { get => GetValue(UseItemCrossAxisSizeProperty); set => SetValue(UseItemCrossAxisSizeProperty, value); }
    /// <summary>Optional layout-owned mask. Null observes the actual viewport and item bounds.</summary>
    public MultiAspectCarouselItemDrawInfo? DrawInfo { get => GetValue(DrawInfoProperty); set => SetValue(DrawInfoProperty, value); }

    private MultiAspectCarouselItemDrawInfo _actualDrawInfo;
    public MultiAspectCarouselItemDrawInfo ActualDrawInfo => _actualDrawInfo;
    private Border? _mask;
    private ContentPresenter? _content;
    private SurfaceOutline? _outline;
    private readonly TranslateTransform _translation = new();
    private ScrollViewer? _viewer;
    private readonly CarouselMaskShape _maskShape, _borderShape;

    public MultiAspectCarouselItem()
    {
        _maskShape = new(UpdateMask);
        _borderShape = new(UpdateMask);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _maskShape.Reset(); _borderShape.Reset();
        _mask = e.NameScope.Find<Border>("PART_Mask");
        _content = e.NameScope.Find<ContentPresenter>("PART_ContentPresenter");
        _outline = e.NameScope.Find<SurfaceOutline>("PART_Outline");
        if (_content is not null) _content.RenderTransform = _translation;
        UpdateMask();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        LayoutUpdated += LayoutChanged;
        _viewer = this.GetVisualAncestors().OfType<ScrollViewer>().FirstOrDefault();
        if (_viewer is not null) _viewer.ScrollChanged += ScrollChanged;
        UpdateMask();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        LayoutUpdated -= LayoutChanged;
        if (_viewer is not null) _viewer.ScrollChanged -= ScrollChanged;
        _viewer = null;
        _maskShape.Reset(); _borderShape.Reset();
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ViewportProperty || change.Property == OrientationProperty ||
            change.Property == UseItemCrossAxisSizeProperty || change.Property == DrawInfoProperty ||
            change.Property == CornerRadiusProperty || change.Property == ShapeProperty || change.Property == FlowDirectionProperty ||
            change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty)
            UpdateMask();
    }

    private void LayoutChanged(object? sender, EventArgs e) => UpdateMask();
    private void ScrollChanged(object? sender, ScrollChangedEventArgs e) => UpdateMask();

    private void UpdateMask()
    {
        if (_mask is null || _content is null || _outline is null || !this.IsAttachedToVisualTree()) return;
        var horizontal = Orientation == Orientation.Horizontal;
        var viewport = Viewport ?? _viewer?.GetVisualDescendants().OfType<Control>()
            .FirstOrDefault(control => control.Name == "PART_ContentPresenter" && control.TemplatedParent == _viewer);
        var size = _mask.Bounds.Size;
        var main = (float)(horizontal ? size.Width : size.Height);
        var cross = (float)(horizontal ? size.Height : size.Width);
        var info = DrawInfo;
        if (info is null && viewport is not null && this.TranslatePoint(default, viewport) is { } origin &&
            this.TranslatePoint(new Point(Bounds.Width, Bounds.Height), viewport) is { } end)
        {
            var offset = (float)(horizontal ? Math.Min(origin.X, end.X) : Math.Min(origin.Y, end.Y));
            var viewportMain = (float)(horizontal ? viewport.Bounds.Width : viewport.Bounds.Height);
            var viewportCross = (float)(horizontal ? viewport.Bounds.Height : viewport.Bounds.Width);
            info = MultiAspectCarouselItemDrawInfo.Calculate(0, viewportMain, offset, main,
                UseItemCrossAxisSize ? cross : viewportCross,
                offset < viewportMain && offset + main > 0, Orientation);
        }
        SetAndRaise(ActualDrawInfoProperty, ref _actualDrawInfo,
            info ?? MultiAspectCarouselItemDrawInfo.Calculate(0, main, 0, main, cross, orientation: Orientation));
        horizontal = ActualDrawInfo.Orientation == Orientation.Horizontal;
        // MaskShape uses the full bounds for size == 0, including invisible/unmeasured items.
        var rect = ActualDrawInfo.Size == 0 ? new Rect(size) : horizontal
            ? new Rect(ActualDrawInfo.MaskStart, 0, ActualDrawInfo.Size, size.Height)
            : new Rect(0, ActualDrawInfo.MaskStart, size.Width, ActualDrawInfo.Size);
        if (rect.Width < 0 || rect.Height < 0) return;
        var density = LayoutHelper.GetLayoutScale(this);
        _maskShape.Configure(Shape, CornerRadius, remember: false);
        _mask.Clip = _maskShape.Geometry(rect, FlowDirection, density, size);
        // MaskParallaxNode draws in physical coordinates even for a logical RTL lazy-list offset.
        // The mask bypasses automatic mirroring; content retains its inherited layout direction.
        _translation.X = horizontal ? ActualDrawInfo.Parallax : 0;
        _translation.Y = horizontal ? 0 : ActualDrawInfo.Parallax;
        if (BorderBrush is null || BorderThickness == default) _borderShape.Reset();
        else _borderShape.Configure(Shape, CornerRadius, remember: true);
        if (_borderShape.HasShape && !BorderThickness.IsUniform)
            throw new InvalidOperationException("MaterialShape borders require uniform BorderThickness.");
        _outline.Width = _borderShape.HasShape ? size.Width : rect.Width;
        _outline.Height = _borderShape.HasShape ? size.Height : rect.Height;
        _outline.PathLayoutSize = size;
        _outline.PathBounds = rect;
        _outline.RenderTransform = _borderShape.HasShape ? null : new TranslateTransform(rect.X, rect.Y);
        // maskBorder remembers MaskShape without shape keys; maskClip updates its shape.
        // Preserve that distinction rather than silently fixing the fixed upstream behavior.
        _outline.ShapePath = _borderShape.HasShape ? _borderShape.Geometry(rect, FlowDirection, density, size) : null;
        _outline.CornerRadius = _borderShape.Corners;
    }
}

internal sealed class CarouselMask : Border
{
    protected override bool BypassFlowDirectionPolicies => true;
}

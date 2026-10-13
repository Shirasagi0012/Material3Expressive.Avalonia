/* Portions translated from AndroidX Tab.kt, Copyright 2022 The Android Open Source Project.
 * Apache License 2.0. Fixed source: docs/upstream-baseline.json. */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Styling;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;
using Material3Expressive.Foundation.Typography;

namespace Material3Expressive.Components;

// Both Tab and native TabItem templates use this content/transition part. Selection stays with the owner.
internal sealed class TabHeader : Panel
{
    public static readonly StyledProperty<double> LargeContainerHeightProperty = AvaloniaProperty.Register<TabHeader, double>(nameof(LargeContainerHeight));
    public double LargeContainerHeight { get => GetValue(LargeContainerHeightProperty); set => SetValue(LargeContainerHeightProperty, value); }
    public static readonly StyledProperty<double> ContainerHeightProperty = AvaloniaProperty.Register<TabHeader, double>(nameof(ContainerHeight));
    public double ContainerHeight { get => GetValue(ContainerHeightProperty); set => SetValue(ContainerHeightProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> SelectedContentAnimationProperty = AvaloniaProperty.Register<TabHeader, Foundation.Motion.SpringSpec>(nameof(SelectedContentAnimation));
    public Foundation.Motion.SpringSpec SelectedContentAnimation { get => GetValue(SelectedContentAnimationProperty); set => SetValue(SelectedContentAnimationProperty, value); }
    public static readonly StyledProperty<Foundation.Motion.SpringSpec> UnselectedContentAnimationProperty = AvaloniaProperty.Register<TabHeader, Foundation.Motion.SpringSpec>(nameof(UnselectedContentAnimation));
    public Foundation.Motion.SpringSpec UnselectedContentAnimation { get => GetValue(UnselectedContentAnimationProperty); set => SetValue(UnselectedContentAnimationProperty, value); }
    public static readonly StyledProperty<bool> IsSelectedProperty = AvaloniaProperty.Register<TabHeader, bool>(nameof(IsSelected));
    public static readonly StyledProperty<TabContentLayout> ContentLayoutProperty = AvaloniaProperty.Register<TabHeader, TabContentLayout>(nameof(ContentLayout));
    public static readonly StyledProperty<IBrush?> AmbientBrushProperty = AvaloniaProperty.Register<TabHeader, IBrush?>(nameof(AmbientBrush));
    public static readonly StyledProperty<Color?> SelectedContentColorProperty = AvaloniaProperty.Register<TabHeader, Color?>(nameof(SelectedContentColor));
    public static readonly StyledProperty<Color?> UnselectedContentColorProperty = AvaloniaProperty.Register<TabHeader, Color?>(nameof(UnselectedContentColor));
    public static readonly StyledProperty<TextStyle?> LabelTextStyleProperty = AvaloniaProperty.Register<TabHeader, TextStyle?>(nameof(LabelTextStyle));
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public TabContentLayout ContentLayout { get => GetValue(ContentLayoutProperty); set => SetValue(ContentLayoutProperty, value); }
    public IBrush? AmbientBrush { get => GetValue(AmbientBrushProperty); set => SetValue(AmbientBrushProperty, value); }
    public Color? SelectedContentColor { get => GetValue(SelectedContentColorProperty); set => SetValue(SelectedContentColorProperty, value); }
    public Color? UnselectedContentColor { get => GetValue(UnselectedContentColorProperty); set => SetValue(UnselectedContentColorProperty, value); }
    public TextStyle? LabelTextStyle { get => GetValue(LabelTextStyleProperty); set => SetValue(LabelTextStyleProperty, value); }
    internal Color PaintedContentColor { get; private set; }
    private Color Active => SelectedContentColor ?? (AmbientBrush as ISolidColorBrush)?.Color ?? Colors.Black;
    private Color Inactive => UnselectedContentColor ?? Active;
    private Transition<bool>? _transition;
    private Transition<bool>.TransitionAnimationState<Color>? _color;
    private CancellationTokenSource? _run;
    private Task? _runTask;
    private bool _pending;
    private bool _initialSelected;
    private Rect _labelBox, _iconBox;
    private Size _inner;
    private ContentPresenter Label => (ContentPresenter)Children[0];
    private ContentPresenter Icon => (ContentPresenter)Children[1];
    private bool HasLabel => Label.Content is not null;
    private bool HasIcon => ContentLayout != TabContentLayout.Custom && (Icon.Content is not null || Icon.ContentTemplate is not null);

    static TabHeader() => AffectsMeasure<TabHeader>(ContentLayoutProperty, ContainerHeightProperty, LargeContainerHeightProperty);
    protected override void OnMeasureInvalidated()
    {
        base.OnMeasureInvalidated();
        this.GetVisualAncestors().OfType<TabRowItemsPanel>().FirstOrDefault()?.HeaderMeasureInvalidated();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Label.PropertyChanged += SlotChanged;
        Icon.PropertyChanged += SlotChanged;
        _initialSelected = IsSelected;
        ApplyContentPolicy();
        PaintColor(IsSelected ? Active : Inactive);
        InitializeColorMotion();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Label.PropertyChanged -= SlotChanged;
        Icon.PropertyChanged -= SlotChanged;
        if (_transition is not null) _transition.PropertyChanged -= TransitionChanged;
        _run?.Cancel(); _run?.Dispose(); _run = null; _runTask = null;
        _transition?.Dispose(); _transition = null; _color = null;
        _pending = false;
        base.OnDetachedFromVisualTree(e);
    }
    private void SlotChanged(object? sender, AvaloniaPropertyChangedEventArgs change)
    {
        if (change.Property != ContentPresenter.ContentProperty && change.Property != ContentPresenter.ContentTemplateProperty) return;
        // A hidden presenter need not propagate content-measure invalidation. The
        // header owns the presence fact, including reappearance after an empty slot.
        Icon.IsVisible = HasIcon;
        Label.IsVisible = HasLabel;
        InvalidateMeasure();
    }
    private void InitializeColorMotion()
    {
        if (!this.IsAttachedToVisualTree() || _transition is not null
            || SelectedContentAnimation.Stiffness <= 0 || UnselectedContentAnimation.Stiffness <= 0) return;
        var motion = AnimationScope.Get(this);
        var selected = SelectedContentAnimation;
        var unselected = UnselectedContentAnimation;
        _transition = new Transition<bool>(_initialSelected, "Tab content color");
        _color = _transition.AnimateColor(state => state ? Active : Inactive,
            segment => ColorSpec(!segment.InitialState && segment.TargetState, selected, unselected));
        motion.SubscribeValue(_color, PaintColor);
        _transition.PropertyChanged += TransitionChanged;
        UpdateColorTarget();
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if ((change.Property == LabelTextStyleProperty || change.Property == ContentLayoutProperty) && Children.Count == 2)
            ApplyContentPolicy();
        if (change.Property == SelectedContentAnimationProperty || change.Property == UnselectedContentAnimationProperty)
            InitializeColorMotion();
        if (change.Property == IsSelectedProperty || change.Property == AmbientBrushProperty || change.Property == SelectedContentColorProperty
            || change.Property == UnselectedContentColorProperty || change.Property == SelectedContentAnimationProperty || change.Property == UnselectedContentAnimationProperty)
            UpdateColorTarget();
    }
    private void UpdateColorTarget()
    {
        if (this.GetVisualAncestors().OfType<MaterialSurface>().FirstOrDefault() is { } surface)
            surface.StateLayerBrush = new SolidColorBrush(Active);
        if (!this.IsAttachedToVisualTree() || _transition is null || _pending) return;
        var motion = AnimationScope.Get(this);
        _pending = true;
        _ = motion.Launch(async ct =>
        {
            await motion.NextFrameAsync(ct);
            _pending = false;
            if (_transition is null || _color is null || SelectedContentAnimation.Stiffness <= 0 || UnselectedContentAnimation.Stiffness <= 0) return;
            var selected = SelectedContentAnimation;
            var unselected = UnselectedContentAnimation;
            var changedState = _transition.TargetState != IsSelected;
            // Target mutation evaluates the factory synchronously. Publish the valid
            // configuration first; Update preserves an animation whose target is unchanged.
            _color.Update(state => state ? Active : Inactive,
                segment => ColorSpec(!segment.InitialState && segment.TargetState, selected, unselected));
            _transition.UpdateTarget(IsSelected);
            if ((!_color.IsFinished || _transition.CurrentState != _transition.TargetState) && (_runTask is null || _runTask.IsCompleted))
            {
                // Mapping-only changes launch on the following composition frame.
                if (!changedState) await motion.NextFrameAsync(ct);
                if (_transition is null) return;
                if (_runTask is { IsCompleted: false }) return;
                _run?.Dispose(); _run = CancellationTokenSource.CreateLinkedTokenSource(ct);
                _runTask = motion.Launch(_ => _transition.RunAsync(motion, _run.Token), "Tab transition");
            }
        }, "Tab color targets");
    }
    private void PaintColor(Color color)
    {
        PaintedContentColor = color;
        TextElement.SetForeground(this, new SolidColorBrush(color));
        if (Children.Count == 2)
        {
            Label.Foreground = Icon.Foreground = new SolidColorBrush(color);
            if (this.GetVisualAncestors().OfType<MaterialSurface>().FirstOrDefault() is { } surface)
                surface.StateLayerBrush = new SolidColorBrush(Active);
        }
    }
    private void ApplyContentPolicy()
    {
        var custom = ContentLayout == TabContentLayout.Custom;
        // The label presenter is its content's inheritance parent, so explicit Content gets the style too.
        MaterialText.SetStyle(Label, custom ? null : LabelTextStyle is { } style ? style with { TextAlignment = TextAlignment.Center } : null);
        if (custom) Label.ClearValue(ContentPresenter.TextWrappingProperty);
        else Label.TextWrapping = TextWrapping.Wrap;
        BadgeBounds.SetIsEnabled(this, ContentLayout == TabContentLayout.Stacked);
    }
    private void TransitionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Transition.IsRunning) && _transition?.IsRunning == false) _run?.Cancel();
    }
    private static SpringSpec<Color> ColorSpec(bool entering, Foundation.Motion.SpringSpec selected, Foundation.Motion.SpringSpec unselected)
    {
        var spec = entering ? selected : unselected;
        return new SpringSpec<Color>(spec.DampingRatio, spec.Stiffness);
    }

    internal int IntrinsicWidth(int height, double scale)
    {
        Label.UpdateChild(); Icon.UpdateChild();
        Label.Measure(new Size(double.PositiveInfinity, height == int.MaxValue ? double.PositiveInfinity : height / scale));
        Icon.Measure(new Size(double.PositiveInfinity, height == int.MaxValue ? double.PositiveInfinity : height / scale));
        var label = HasLabel ? MathUtils.DipToPx(Label.DesiredSize.Width, scale) : 0;
        var icon = HasIcon ? MathUtils.DipToPx(Icon.DesiredSize.Width, scale) : 0;
        if (ContentLayout == TabContentLayout.Custom) return label;
        return ContentLayout == TabContentLayout.Leading
            ? label + icon + MathUtils.DipToPx(40, scale)
            : Math.Max(HasLabel ? label + MathUtils.DipToPx(32, scale) : 0, icon);
    }
    internal int IntrinsicHeight(int width, double scale)
    {
        Label.UpdateChild(); Icon.UpdateChild();
        var maximum = width == int.MaxValue ? double.PositiveInfinity : width / scale;
        Icon.Measure(new Size(maximum, double.PositiveInfinity));
        Label.Measure(new Size(Math.Max(0, maximum - (ContentLayout == TabContentLayout.Custom ? 0 : 32)
            - (ContentLayout == TabContentLayout.Leading ? Icon.DesiredSize.Width + 8 : 0)), double.PositiveInfinity));
        var label = HasLabel ? MathUtils.DipToPx(Label.DesiredSize.Height, scale) : 0;
        var icon = HasIcon ? MathUtils.DipToPx(Icon.DesiredSize.Height, scale) : 0;
        if (ContentLayout == TabContentLayout.Custom) return label;
        if (ContentLayout == TabContentLayout.Leading) return MathUtils.DipToPx(ContainerHeight, scale);
        return Math.Max(MathUtils.DipToPx(HasLabel && HasIcon ? LargeContainerHeight : ContainerHeight, scale), label + icon + MathUtils.DipToPx(20, scale));
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (Children.Count != 2) return default;
        Label.UpdateChild(); Icon.UpdateChild();
        var scale = LayoutHelper.GetLayoutScale(this);
        var maxWidth = double.IsFinite(availableSize.Width) ? MathUtils.DipToPx(availableSize.Width, scale) : int.MaxValue;
        var maxHeight = double.IsFinite(availableSize.Height) ? MathUtils.DipToPx(availableSize.Height, scale) : int.MaxValue;
        int Px(double value) => MathUtils.DipToPx((float)value, (float)scale);
        int Center(int remaining) => FlowDirection == FlowDirection.RightToLeft ? remaining / 2 : MathUtils.RoundToInt(remaining / 2f);
        Icon.IsVisible = HasIcon; Label.IsVisible = HasLabel;
        var leading = ContentLayout == TabContentLayout.Leading;
        var custom = ContentLayout == TabContentLayout.Custom;
        Icon.Measure(new Size(Math.Max(0, maxWidth - (leading ? Px(32) : 0)) / scale, maxHeight / scale));
        var iw = HasIcon ? Px(Icon.DesiredSize.Width) : 0; var ih = HasIcon ? Px(Icon.DesiredSize.Height) : 0;
        var pad = custom ? 0 : Px(32);
        // requiredWidth(8.dp) retains its drawing size, but its apparent measured width
        // is constrained to the Row's remaining width and determines sibling placement.
        var gap = leading ? Math.Min(Px(8), Math.Max(0, maxWidth - pad - iw)) : 0;
        Label.Measure(new Size(Math.Max(0, maxWidth - pad - (leading ? iw + gap : 0)) / scale,
            (leading ? Math.Min(maxHeight, Px(ContainerHeight)) : maxHeight) / scale));
        var lw = HasLabel ? Px(Label.DesiredSize.Width) : 0; var lh = HasLabel ? Px(Label.DesiredSize.Height) : 0;
        var width = maxWidth == int.MaxValue ? custom ? lw : leading ? iw + gap + lw + pad : Math.Max(HasLabel ? lw + pad : 0, iw) : maxWidth;
        var intrinsicHeight = custom ? lh : leading ? Px(ContainerHeight) : Math.Max(Px(HasIcon && HasLabel ? LargeContainerHeight : ContainerHeight), ih + lh + Px(20));
        var height = Math.Min(maxHeight, intrinsicHeight);
        _inner = new Size(width, height);
        if (!custom && !leading && HasIcon && HasLabel)
        {
            var (first, last) = Baselines(Label, scale);
            var textOffset = Px(first == last ? 14 : 6) + Px(3);
            var textY = unchecked(height - last - textOffset);
            var textWidth = Math.Min(width, lw + pad);
            var baselineWidth = Math.Max(textWidth, iw);
            var columnX = Center(width - baselineWidth);
            _labelBox = new Rect(columnX + (baselineWidth - textWidth) / 2 + pad / 2, textY, lw, lh);
            _iconBox = new Rect(columnX + (baselineWidth - iw) / 2, unchecked(textY - (ih + Px(20) - first)), iw, ih);
        }
        else if (leading)
        {
            var content = iw + gap + lw;
            var innerWidth = Math.Max(0, width - pad);
            var center = Center(innerWidth - content);
            _iconBox = new Rect(pad / 2 + center, MathUtils.RoundToInt((height - ih) / 2f), iw, ih);
            _labelBox = new Rect(_iconBox.X + iw + gap, MathUtils.RoundToInt((height - lh) / 2f), lw, lh);
        }
        else
        {
            var textWidth = Math.Min(width, lw + pad);
            _labelBox = new Rect(Center(width - textWidth) + pad / 2, custom ? MathUtils.RoundToInt((height - lh) / 2f) : (height - lh) / 2, lw, lh);
            _iconBox = new Rect(Center(width - iw), (height - ih) / 2, iw, ih);
        }
        return new Size(width / scale, height / scale);
    }
    private static (int First, int Last) Baselines(ContentPresenter presenter, double scale)
    {
        // Native text lines expose first/last baselines; non-text controls can use Avalonia's
        // existing TextBlock.BaselineOffset attached property. Missing lines stay unspecified.
        var descendants = presenter.GetVisualDescendants().OfType<Control>().ToArray();
        if (descendants.FirstOrDefault(control => control.IsSet(TextBlock.BaselineOffsetProperty)) is { } supplied)
        {
            var suppliedValue = MathUtils.DipToPx(TextBlock.GetBaselineOffset(supplied), scale);
            return (suppliedValue, suppliedValue);
        }
        if (descendants.OfType<TextBlock>().FirstOrDefault() is { TextLayout: { } layout } && layout.TextLines.Count > 0)
            return (MathUtils.DipToPx(layout.TextLines[0].Baseline, scale),
                MathUtils.DipToPx(layout.TextLines.Take(layout.TextLines.Count - 1).Sum(line => line.Height) + layout.TextLines[^1].Baseline, scale));
        return (int.MinValue, int.MinValue);
    }
    protected override Size ArrangeOverride(Size finalSize)
    {
        var scale = LayoutHelper.GetLayoutScale(this);
        var dx = (MathUtils.DipToPx(finalSize.Width, scale) - (int)_inner.Width) / 2;
        var dy = (MathUtils.DipToPx(finalSize.Height, scale) - (int)_inner.Height) / 2;
        if (HasLabel) Label.Arrange(new Rect((_labelBox.X + dx) / scale, (_labelBox.Y + dy) / scale, _labelBox.Width / scale, _labelBox.Height / scale));
        if (HasIcon) Icon.Arrange(new Rect((_iconBox.X + dx) / scale, (_iconBox.Y + dy) / scale, _iconBox.Width / scale, _iconBox.Height / scale));
        return finalSize;
    }
}

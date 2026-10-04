/*
 * Translated from AndroidX LoadingIndicator.kt (Copyright 2024 The Android Open Source
 * Project; Apache-2.0). Fixed baseline: docs/upstream-baseline.json.
 */
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using MaterialShapes;

namespace Material3Expressive.Components;

/// <summary>Shared state, layout and rendering for Compose's two LoadingIndicator variants.</summary>
public abstract class LoadingIndicatorBase : Control
{
    protected override bool BypassFlowDirectionPolicies => true;
    public static readonly StyledProperty<double> ProgressProperty = AvaloniaProperty.Register<LoadingIndicatorBase, double>(nameof(Progress));
    public double Progress { get => GetValue(ProgressProperty); set => SetValue(ProgressProperty, value); }
    public static readonly StyledProperty<bool> IsIndeterminateProperty = AvaloniaProperty.Register<LoadingIndicatorBase, bool>(nameof(IsIndeterminate), true);
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }
    public static readonly StyledProperty<IReadOnlyList<RoundedPolygon>?> PolygonsProperty =
        AvaloniaProperty.Register<LoadingIndicatorBase, IReadOnlyList<RoundedPolygon>?>(nameof(Polygons),
            validate: polygons => polygons is null || polygons.Count >= 2);
    /// <summary>Replace the list to update the morph sequence; null selects the defaults for the current mode.</summary>
    public IReadOnlyList<RoundedPolygon>? Polygons { get => GetValue(PolygonsProperty); set => SetValue(PolygonsProperty, value); }
    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<LoadingIndicatorBase>();
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public static readonly StyledProperty<Func<Size, FlowDirection, Geometry>?> ContainerShapeProperty =
        AvaloniaProperty.Register<LoadingIndicatorBase, Func<Size, FlowDirection, Geometry>?>(nameof(ContainerShape));
    /// <summary>An optional DIP outline factory for the container fill and clip; overrides CornerRadius.</summary>
    public Func<Size, FlowDirection, Geometry>? ContainerShape { get => GetValue(ContainerShapeProperty); set => SetValue(ContainerShapeProperty, value); }

    public static readonly StyledProperty<double> ContainerWidthProperty = AvaloniaProperty.Register<LoadingIndicatorBase, double>(nameof(ContainerWidth), 48);
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ContainerWidth { get => GetValue(ContainerWidthProperty); set => SetValue(ContainerWidthProperty, value); }
    public static readonly StyledProperty<double> ContainerHeightProperty = AvaloniaProperty.Register<LoadingIndicatorBase, double>(nameof(ContainerHeight), 48);
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double ContainerHeight { get => GetValue(ContainerHeightProperty); set => SetValue(ContainerHeightProperty, value); }
    public static readonly StyledProperty<double> IndicatorSizeProperty = AvaloniaProperty.Register<LoadingIndicatorBase, double>(nameof(IndicatorSize), 38);
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public double IndicatorSize { get => GetValue(IndicatorSizeProperty); set => SetValue(IndicatorSizeProperty, value); }

    LoadingIndicatorShapes? shapes;
    VisualAnimationScope? motion, effect, polygonUpdate;
    IReadOnlyList<RoundedPolygon>? activePolygons;
    Animatable<float>? morphProgress, globalRotation;
    internal VisualAnimationScope? AnimationScope => motion;
    internal IReadOnlyList<RoundedPolygon> EffectivePolygons => Polygons ?? (IsIndeterminate
        ? LoadingIndicatorDefaults.IndeterminateIndicatorPolygons : LoadingIndicatorDefaults.DeterminateIndicatorPolygons);
    internal float CoercedProgress => Math.Clamp((float)Progress, 0f, 1f);
    internal int MorphIndex { get; private set; }
    internal float MorphProgress { get; private set; }
    internal float RotationTarget { get; private set; } = 90;
    internal float GlobalRotation { get; private set; }
    protected abstract Color ActiveColor { get; }
    protected virtual Color BackgroundColor => Colors.Transparent;

    static LoadingIndicatorBase()
    {
        HorizontalAlignmentProperty.OverrideDefaultValue<LoadingIndicatorBase>(HorizontalAlignment.Left);
        VerticalAlignmentProperty.OverrideDefaultValue<LoadingIndicatorBase>(VerticalAlignment.Top);
        CornerRadiusProperty.OverrideDefaultValue<LoadingIndicatorBase>(new CornerRadius(1000));
        AffectsMeasure<LoadingIndicatorBase>(ContainerWidthProperty, ContainerHeightProperty);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        return new(Math.Min(MathF.Round((float)ContainerWidth * scale, MidpointRounding.AwayFromZero) / scale, availableSize.Width),
            Math.Min(MathF.Round((float)ContainerHeight * scale, MidpointRounding.AwayFromZero) / scale, availableSize.Height));
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        RestartMode();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        motion?.Dispose(); motion = effect = polygonUpdate = null;
        activePolygons = null;
        morphProgress = globalRotation = null;
        MorphIndex = 0; MorphProgress = GlobalRotation = 0; RotationTarget = 90;
        shapes = null;
        base.OnDetachedFromVisualTree(e);
    }

    void RestartMode()
    {
        effect?.Dispose(); effect = null;
        // Selecting the other Compose overload replaces its remembered state.
        motion?.Dispose(); motion = null;
        polygonUpdate = null;
        activePolygons = EffectivePolygons;
        morphProgress = globalRotation = null;
        MorphIndex = 0; MorphProgress = GlobalRotation = 0; RotationTarget = 90;
        shapes = null;
        if (TopLevel.GetTopLevel(this) is null) return;
        motion = VisualAnimationScope.Create(this);
        if (!IsIndeterminate) return;
        morphProgress = motion.CreateAnimatable(0f);
        globalRotation = motion.CreateAnimatable(0f);
        motion.SubscribeValue(morphProgress, value => { MorphProgress = value; InvalidateVisual(); });
        motion.SubscribeValue(globalRotation, value => { GlobalRotation = value; InvalidateVisual(); });
        RestartEffect();
    }

    void RestartEffect()
    {
        effect?.Dispose(); effect = null;
        MorphIndex = 0;
        if (!IsIndeterminate || motion is not { IsEnding: false }) return;
        var scope = effect = motion.CreateChild("Loading indicator polygon sequence");
        var progress = morphProgress!;
        var rotation = globalRotation!;
        var count = (activePolygons ?? EffectivePolygons).Count;
        var spring = new SpringSpec<float>(dampingRatio: .6, stiffness: 200, visibilityThreshold: .1f);
        _ = scope.Launch(async ct =>
        {
            try
            {
                while (true)
                {
                    var morph = scope.Launch(async token =>
                    {
                        try
                        {
                            var result = await progress.AnimateToAsync(1, spring, cancellationToken: token);
                            token.ThrowIfCancellationRequested();
                            if (result.EndReason == AnimationEndReason.Finished)
                            {
                                MorphIndex = (MorphIndex + 1) % count;
                                await progress.SnapToAsync(0, cancellationToken: token);
                                RotationTarget = (RotationTarget + 90) % 360;
                                InvalidateVisual();
                            }
                        }
                        catch { scope.Dispose(); throw; }
                    }, "Loading indicator morph");
                    // kotlinx.coroutines.delay is separate from animation frames and scaling.
                    await scope.DelayAsync(TimeSpan.FromMilliseconds(650), ct);
                    await morph;
                }
            }
            finally { scope.Dispose(); }
        }, "Loading indicator morph loop");
        _ = scope.Launch(async ct =>
        {
            try
            {
                await rotation.AnimateToAsync(360,
                    new InfiniteRepeatableSpec<float>(new TweenSpec<float>(TimeSpan.FromMilliseconds(4666), easing: new Avalonia.Animation.Easings.LinearEasing())),
                    cancellationToken: ct);
            }
            catch { scope.Dispose(); throw; }
        }, "Loading indicator global rotation");
    }

    void QueuePolygonUpdate()
    {
        polygonUpdate?.Dispose(); polygonUpdate = null;
        if ((activePolygons ?? EffectivePolygons).SequenceEqual(EffectivePolygons)) return;
        var scope = polygonUpdate = motion!.CreateChild("Loading polygon recomposition");
        _ = scope.Launch(async ct =>
        {
            // FrameRecomposer first samples the old Animatables, then applies the new
            // remember keys and LaunchedEffect. Retain that final old-frame sample before
            // canceling the effect; restarting inline would retain the previous frame.
            await scope.Clock.NextFrameAsync(ct);
            polygonUpdate = null;
            activePolygons = EffectivePolygons;
            shapes = null;
            RestartEffect();
            scope.Dispose();
            InvalidateVisual();
        }, "Apply loading polygon sequence");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsIndeterminateProperty) RestartMode();
        if (change.Property == PolygonsProperty)
        {
            if (IsIndeterminate && motion is { IsEnding: false }) QueuePolygonUpdate();
            else { activePolygons = EffectivePolygons; shapes = null; }
        }
        if (change.Property == ProgressProperty && !IsIndeterminate)
        {
            static double SemanticValue(double value) => double.IsNaN(value) ? 0 : Math.Clamp((float)value, 0f, 1f);
            ControlAutomationPeer.FromElement(this)?.RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty,
                SemanticValue(change.GetOldValue<double>()), SemanticValue(change.GetNewValue<double>()));
        }
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;
        shapes ??= new(activePolygons ?? EffectivePolygons, IsIndeterminate);
        var scale = (float)LayoutHelper.GetLayoutScale(this);
        var width = (float)Bounds.Width * scale;
        var height = (float)Bounds.Height * scale;
        var side = MathF.Min(width, height);
        var (index, progress) = IsIndeterminate ? (MorphIndex, MorphProgress) : shapes.DeterminateMorph(CoercedProgress);
        var rotation = IsIndeterminate ? MorphProgress * 90 + RotationTarget + GlobalRotation : -CoercedProgress * 180;
        var activeScale = (float)IndicatorSize / MathF.Min((float)ContainerWidth, (float)ContainerHeight);
        var shape = ContainerShape?.Invoke(Bounds.Size, FlowDirection);
        if (shape is not null)
        {
            using (context.PushGeometryClip(shape))
            {
                context.DrawGeometry(new SolidColorBrush(BackgroundColor), null, shape);
                DrawIndicator();
            }
        }
        else
        {
            // CornerBasedShape normalizes each vertical corner pair against the minimum extent.
            var extent = side / scale;
            var left = (float)(CornerRadius.TopLeft + CornerRadius.BottomLeft);
            var right = (float)(CornerRadius.TopRight + CornerRadius.BottomRight);
            var leftScale = left > extent ? extent / left : 1f;
            var rightScale = right > extent ? extent / right : 1f;
            var rect = new RoundedRect(new Rect(Bounds.Size), new CornerRadius(
                (float)CornerRadius.TopLeft * leftScale, (float)CornerRadius.TopRight * rightScale,
                (float)CornerRadius.BottomRight * rightScale, (float)CornerRadius.BottomLeft * leftScale));
            using (context.PushClip(rect))
            {
                context.DrawRectangle(new SolidColorBrush(BackgroundColor), null, rect);
                DrawIndicator();
            }
        }

        void DrawIndicator()
        {
            // NaN remains NaN in the upstream visual path; only accessibility maps it to zero.
            if (float.IsNaN(progress)) return;
            var path = shapes.ProcessPath(index, progress, side, activeScale);
            context.Custom(new LoadingIndicatorDrawOperation(Bounds.Size, scale, path, ActiveColor, rotation,
                MathF.Round((width - side) / 2, MidpointRounding.AwayFromZero),
                MathF.Round((height - side) / 2, MidpointRounding.AwayFromZero), side));
        }
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new LoadingIndicatorAutomationPeer(this);
    sealed class LoadingIndicatorAutomationPeer(LoadingIndicatorBase owner) : ControlAutomationPeer(owner), IRangeValueProvider
    {
        protected override string GetClassNameCore() => owner.GetType().Name;
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;
        protected override object? GetProviderCore(Type providerType) => providerType == typeof(IRangeValueProvider)
            ? owner.IsIndeterminate ? null : this : base.GetProviderCore(providerType);
        public bool IsReadOnly => true;
        public double Minimum => 0;
        public double Maximum => 1;
        public double Value => double.IsNaN(owner.Progress) ? 0 : owner.CoercedProgress;
        public double SmallChange => double.NaN;
        public double LargeChange => double.NaN;
        public void SetValue(double value) => throw new InvalidOperationException("Loading indicators are read-only.");
    }
}

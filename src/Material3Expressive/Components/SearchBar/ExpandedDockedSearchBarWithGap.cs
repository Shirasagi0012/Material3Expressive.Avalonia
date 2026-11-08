using Material3Expressive.Foundation.Rendering;
// SearchBar.kt / ExpandedDockedSearchBarWithGap and DockedSearchBarLayout.
// Copyright 2026 The Android Open Source Project; Apache-2.0.
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using ComposeAnimation;
using ComposeAnimation.Runtime;
using Material3Expressive.Foundation;

namespace Material3Expressive.Components;

/// <summary>A docked search popup with separate input and results surfaces and an optional scrim.</summary>
public class ExpandedDockedSearchBarWithGap : ExpandedDockedSearchBar
{
    public static readonly StyledProperty<MaterialShape?> DropdownShapeProperty = AvaloniaProperty.Register<ExpandedDockedSearchBarWithGap, MaterialShape?>(nameof(DropdownShape));
    public MaterialShape? DropdownShape { get => GetValue(DropdownShapeProperty); set => SetValue(DropdownShapeProperty, value); }
    public static readonly StyledProperty<CornerRadius> DropdownCornerRadiusProperty =
        AvaloniaProperty.Register<ExpandedDockedSearchBarWithGap, CornerRadius>(nameof(DropdownCornerRadius));
    public static readonly StyledProperty<double> DropdownGapSizeProperty =
        AvaloniaProperty.Register<ExpandedDockedSearchBarWithGap, double>(nameof(DropdownGapSize));
    public static readonly StyledProperty<Color> DropdownScrimColorProperty =
        AvaloniaProperty.Register<ExpandedDockedSearchBarWithGap, Color>(nameof(DropdownScrimColor));
    public CornerRadius DropdownCornerRadius { get => GetValue(DropdownCornerRadiusProperty); set => SetValue(DropdownCornerRadiusProperty, value); }
    public double DropdownGapSize { get => GetValue(DropdownGapSizeProperty); set => SetValue(DropdownGapSizeProperty, value); }
    /// <summary>Set to Transparent to omit the scrim and dismiss on an outside press instead of a click.</summary>
    public Color DropdownScrimColor { get => GetValue(DropdownScrimColorProperty); set => SetValue(DropdownScrimColorProperty, value); }
    protected override Type StyleKeyOverride => typeof(ExpandedDockedSearchBarWithGap);
    private AnimatedVisibility? _visibility;
    private MaterialSurface? _dropdown;
    private IDisposable? _opacity;
    private IDisposable? _scrimForeground;
    private SearchBarScrimButton? _scrimButton;
    private SearchBarState? _configuredState;
    private double _configuredScale;
    internal Control? Dropdown => _dropdown;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _opacity?.Dispose(); _opacity = null;
        _scrimForeground?.Dispose(); _scrimForeground = null;
        _scrimButton = e.NameScope.Find<SearchBarScrimButton>("PART_Scrim");
        _visibility = e.NameScope.Find<AnimatedVisibility>("PART_Results");
        _dropdown = e.NameScope.Find<MaterialSurface>("PART_Dropdown");
        _configuredState = null;
        base.OnApplyTemplate(e);
        BindScrimForeground();
    }
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        BindScrimForeground();
    }
    private void BindScrimForeground()
    {
        _scrimForeground?.Dispose(); _scrimForeground = null;
        // clickable is outside the search Surface's contentColorFor scope upstream.
        if (_scrimButton is not null && this.GetLogicalParent() is AvaloniaObject ambient)
            _scrimForeground = _scrimButton.Bind(ForegroundProperty, ambient.GetObservable(TextElement.ForegroundProperty), BindingPriority.Template);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DropdownScrimColorProperty) UpdatePresentation();
    }
    internal override void UpdateVariant(SearchBarPopupPresenter presenter, SearchBarState state)
    {
        presenter.SetScrim(DropdownScrimColor, state.Progress);
        if (_visibility is null || _dropdown is null) return;
        var scale = LayoutHelper.GetLayoutScale(this);
        if (!ReferenceEquals(_configuredState, state) || _configuredScale != scale)
        {
            _configuredState = state; _configuredScale = scale;
            double Offset(double height) => MathUtils.RoundToInt((float)(-height * scale / 2)) / scale;
            _visibility.Enter = new SlideInVertically { InitialOffsetSelector = Offset, Spec = SlideSpec(state.AnimationSpec(true), scale) };
            _visibility.Exit = new SlideOutVertically { TargetOffsetSelector = Offset, Spec = SlideSpec(state.AnimationSpec(false), scale) };
        }
        _opacity?.Dispose();
        _opacity = _visibility.SetValue(OpacityProperty, (double)state.ContentProgress, BindingPriority.Animation);
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _opacity?.Dispose(); _opacity = null; _configuredState = null;
        _scrimForeground?.Dispose(); _scrimForeground = null;
        base.OnDetachedFromVisualTree(e);
    }
    private static IFiniteAnimationSpec<Vector> SlideSpec(IAnimationSpec<float> spec, double scale) => spec switch
    {
        SpringSpec<float> spring => new SearchSlideSpring(spring, scale),
        IFiniteAnimationSpec<float> finite => new SearchSlideSpec(finite, scale),
        _ => new SnapSpec<Vector>() // Upstream's as? FiniteAnimationSpec<IntOffset> ?: snap().
    };

    // Kotlin reuses the numeric spec for IntOffset. Project the C# scalar spec onto physical
    // pixel axes, retaining SpringSpec identity so Transition keeps spring velocity on reversal.
    private sealed class SearchSlideSpring(SpringSpec<float> source, double scale)
        : SpringSpec<Vector>(source.DampingRatio, source.Stiffness), IFiniteAnimationSpec<Vector>
    {
        public new IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<Vector> converter) => new PixelSlide(source, scale);
    }
    private sealed class SearchSlideSpec(IFiniteAnimationSpec<float> source, double scale) : IFiniteAnimationSpec<Vector>
    {
        public IVectorizedFiniteAnimationSpec Vectorize(ITwoWayConverter<Vector> converter) => new PixelSlide(source, scale);
    }
    private sealed class PixelSlide(IFiniteAnimationSpec<float> source, double scale) : IVectorizedFiniteAnimationSpec
    {
        private readonly IVectorizedFiniteAnimationSpec _spec = source.Vectorize(new FloatConverter());
        public TimeSpan GetDuration(ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity)
        {
            var duration = TimeSpan.Zero;
            for (var i = 0; i < initial.Length; i++)
            {
                var next = _spec.GetDuration([initial[i] * scale], [target[i] * scale], [velocity[i] * scale]);
                if (next > duration) duration = next;
            }
            return duration;
        }
        public void GetValue(TimeSpan time, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination)
        {
            Span<double> value = stackalloc double[1];
            for (var i = 0; i < destination.Length; i++)
            {
                _spec.GetValue(time, [initial[i] * scale], [target[i] * scale], [velocity[i] * scale], value);
                destination[i] = MathUtils.RoundToInt((float)value[0]) / scale;
            }
        }
        public void GetVelocity(TimeSpan time, ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination)
        {
            Span<double> value = stackalloc double[1];
            for (var i = 0; i < destination.Length; i++)
            {
                _spec.GetVelocity(time, [initial[i] * scale], [target[i] * scale], [velocity[i] * scale], value);
                destination[i] = value[0] / scale;
            }
        }
        public void GetEndVelocity(ReadOnlySpan<double> initial, ReadOnlySpan<double> target, ReadOnlySpan<double> velocity, Span<double> destination)
        {
            Span<double> value = stackalloc double[1];
            for (var i = 0; i < destination.Length; i++)
            {
                _spec.GetEndVelocity([initial[i] * scale], [target[i] * scale], [velocity[i] * scale], value);
                destination[i] = value[0] / scale;
            }
        }
    }
}

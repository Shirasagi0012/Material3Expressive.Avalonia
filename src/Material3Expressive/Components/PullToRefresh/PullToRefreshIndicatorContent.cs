using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using ComposeAnimation;
using MaterialSpring = Material3Expressive.Foundation.Motion.SpringSpec;

namespace Material3Expressive.Components;

internal sealed class PullToRefreshIndicatorContent : Decorator
{
    public static readonly StyledProperty<MaterialSpring?> EffectsAnimationProperty =
        AvaloniaProperty.Register<PullToRefreshIndicatorContent, MaterialSpring?>(nameof(EffectsAnimation));
    public MaterialSpring? EffectsAnimation { get => GetValue(EffectsAnimationProperty); set => SetValue(EffectsAnimationProperty, value); }
    private static readonly object Refreshing = true, Pulling = false;
    private PullToRefreshIndicator? _owner;
    private Crossfade? _crossfade;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _owner = this.FindAncestorOfType<PullToRefreshIndicator>();
        if (_owner is not null) _owner.PresentationChanged += Update;
        Update();
    }
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_owner is not null) _owner.PresentationChanged -= Update;
        _owner = null;
        // End the owned transition while its scope is still available, before
        // dropping its logical parent and the nested continuous animation hosts.
        if (_crossfade is not null) _crossfade.IsVisible = false;
        Child = _crossfade = null;
        base.OnDetachedFromVisualTree(e);
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == EffectsAnimationProperty) Update();
    }
    private void Update()
    {
        if (_owner is not { } owner || EffectsAnimation is not { Stiffness: > 0 } spec) return;
        if (_crossfade is null)
        {
            _crossfade = new Crossfade
            {
                ClipToBounds = false,
                Spec = spec.ToAnimationSpec(),
                Content = owner.IsRefreshing ? Refreshing : Pulling,
                ContentTemplate = new FuncDataTemplate<bool>((refreshing, _) => new IndicatorPart(owner, refreshing, this))
            };
            RenderOptions.SetRequiresFullOpacityHandling(_crossfade, true);
            Child = _crossfade;
        }
        else
        {
            _crossfade.Spec = spec.ToAnimationSpec();
            _crossfade.Content = owner.IsRefreshing ? Refreshing : Pulling;
        }
    }

    private sealed class IndicatorPart : Decorator
    {
        private readonly PullToRefreshIndicator _owner;
        private readonly PullToRefreshIndicatorContent _configuration;
        private readonly bool _refreshing;
        private readonly RotateTransform _rotation = new();
        private IDisposable? _progressContribution;
        private IPullToRefreshState? _state;
        private Visual[] _fadeAncestors = [];
        private static readonly DirectProperty<IndicatorPart, double> RotationProperty =
            AvaloniaProperty.RegisterDirect<IndicatorPart, double>("Rotation", part => part._angle);
        private double _angle;
        internal IndicatorPart(PullToRefreshIndicator owner, bool refreshing, PullToRefreshIndicatorContent configuration)
        {
            _owner = owner; _refreshing = refreshing; _configuration = configuration;
            HorizontalAlignment = HorizontalAlignment.Center;
            VerticalAlignment = VerticalAlignment.Center;
            Child = owner is PullToRefreshLoadingIndicator
                ? new ContainedLoadingIndicator { IsIndeterminate = refreshing }
                : refreshing
                    ? new CircularProgressIndicator { IsIndeterminate = true, Width = 16, Height = 16, StrokeWidth = 2.5 }
                    : new PullToRefreshArrow { Width = 16, Height = 16 };
            if (Child is ContainedLoadingIndicator loading)
                loading.SetValue(RenderTransformProperty, _rotation, BindingPriority.Animation);
            _rotation.Bind(RotateTransform.AngleProperty, this.GetObservable(RotationProperty), BindingPriority.Animation);
            Update();
        }
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _owner.PresentationChanged += Update;
            _owner.PropertyChanged += ConfigurationChanged;
            _configuration.PropertyChanged += ConfigurationChanged;
            _fadeAncestors = this.GetVisualAncestors().TakeWhile(ancestor => !ReferenceEquals(ancestor, _configuration._crossfade)).ToArray();
            foreach (var ancestor in _fadeAncestors) ancestor.PropertyChanged += FadeChanged;
            UpdateFadeClip();
            Update();
        }
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            _owner.PresentationChanged -= Update;
            _owner.PropertyChanged -= ConfigurationChanged;
            _configuration.PropertyChanged -= ConfigurationChanged;
            foreach (var ancestor in _fadeAncestors) ancestor.PropertyChanged -= FadeChanged;
            _fadeAncestors = [];
            _progressContribution?.Dispose(); _progressContribution = null; _state = null;
            base.OnDetachedFromVisualTree(e);
        }
        private void ConfigurationChanged(object? sender, AvaloniaPropertyChangedEventArgs e) => Update();
        private void FadeChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == OpacityProperty) UpdateFadeClip();
        }
        private void UpdateFadeClip()
        {
            // Compose graphicsLayer's Auto strategy allocates a bounded offscreen
            // layer while translucent. At alpha=1 the arrow may draw past its canvas.
            ClipToBounds = _fadeAncestors.Any(ancestor => ancestor.Opacity < 1);
        }
        protected override Size MeasureOverride(Size availableSize)
        {
            if (Child is not ContainedLoadingIndicator loading) return base.MeasureOverride(availableSize);
            // Modifier.requiredSize keeps the loading canvas at its token size even
            // when IndicatorBox is constrained. Only its reported allocation is coerced.
            loading.Measure(new Size(loading.ContainerWidth, loading.ContainerHeight));
            return loading.DesiredSize.Constrain(availableSize);
        }
        protected override Size ArrangeOverride(Size finalSize)
        {
            if (Child is not ContainedLoadingIndicator loading) return base.ArrangeOverride(finalSize);
            var scale = LayoutHelper.GetLayoutScale(this);
            double Center(double outer, double inner) => Math.Floor((outer - inner) * scale / 2 + .5) / scale;
            loading.Arrange(new Rect(new Point(Center(finalSize.Width, loading.DesiredSize.Width),
                Center(finalSize.Height, loading.DesiredSize.Height)), loading.DesiredSize));
            return finalSize;
        }
        private void Update()
        {
            var fraction = _owner.EffectiveState.DistanceFraction;
            if (!ReferenceEquals(_state, _owner.EffectiveState))
            {
                _state = _owner.EffectiveState;
                _progressContribution?.Dispose();
                _progressContribution = Child switch
                {
                    PullToRefreshArrow arrow => arrow.Bind(PullToRefreshArrow.DistanceFractionProperty,
                        new Binding(nameof(IPullToRefreshState.DistanceFraction)) { Source = _state, Priority = BindingPriority.Animation }),
                    ContainedLoadingIndicator loading => loading.Bind(LoadingIndicatorBase.ProgressProperty,
                        new Binding(nameof(IPullToRefreshState.DistanceFraction)) { Source = _state, Priority = BindingPriority.Animation }),
                    _ => null
                };
            }
            switch (Child)
            {
                case PullToRefreshArrow arrow:
                    arrow.Color = _owner.Color;
                    arrow.EffectsAnimation = _configuration.EffectsAnimation;
                    break;
                case CircularProgressIndicator circular:
                    circular.Color = _owner.Color;
                    break;
                case ContainedLoadingIndicator loading:
                    loading.IndicatorColor = _owner.Color;
                    loading.ContainerColor = (_owner.Background as ISolidColorBrush)?.Color ?? Colors.Transparent;
                    SetAndRaise(RotationProperty, ref _angle, !_refreshing && fraction > 1 ? -(fraction - 1) * 180d : 0);
                    break;
            }
        }
    }
}
